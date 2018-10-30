using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Model
{
    /// <summary>
    /// 接口实例与枚举导入
    /// </summary>
    public static class PortsImporter
    {

        /// <summary>
        /// 应用程序执行目录
        /// </summary>
        public static string ExecuteDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.RelativeSearchPath ?? BaseDirectory;
            }
        }


        /// <summary>
        /// 应用程序执行目录
        /// </summary>
        public static string BaseDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        const int SearchDirectoryDeep = 3;
        const string CommonLanguageRuntimeLibrary = "CommonLanguageRuntimeLibrary";
        static object _lockObj = new object();
        static bool _parsed = false;
        static Type _portAttributeType = typeof(ExportAttribute);
        static Assembly _coreAssembly = System.Reflection.Assembly.GetAssembly(_portAttributeType);
        static IDictionary<string, Assembly> _assemblyMapping;
        static IDictionary<string, string> _assemblyLocation;
        static string _executeDirectory = ExecuteDirectory;


        /// <summary>
        /// 接口实现分组字典
        /// </summary>
        static IDictionary<Type, List<Type>> _typedPorts;

        /// <summary>
        /// 获取程序集的文件路径
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="location">程序集的文件路径</param>
        /// <returns></returns>
        public static bool TryGetLocation(this Assembly ass, out string location)
        {
            if (_assemblyLocation != null && _assemblyLocation.TryGetValue(ass.FullName, out string _location))
            {
                location = _location;
                return true;
            }

            location = GetLocation(ass);
            return location != null;
        }

        private static string GetLocation(Assembly ass)
        {
            string basecode = ass.CodeBase;
            if (basecode.StartsWith("file:///"))
            {
                return System.Web.HttpUtility.UrlDecode(basecode.Substring(8)).Replace('/', Path.DirectorySeparatorChar);
            }
            return null;
        }

        /// <summary>
        /// 接口实例与枚举导入
        /// </summary>
        static PortsImporter()
        {
            _assemblyLocation = new ConcurrentDictionary<string, string>(3, 255);
            AppDomain.CurrentDomain.TypeResolve += CurrentDomain_TypeResolve;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        static Assembly CurrentDomain_TypeResolve(object sender, ResolveEventArgs args)
        {
            return FindAssembly(args.Name);
        }

        private static Assembly FindAssembly(string chainName)
        {
            Assembly assembly = _coreAssembly;
            if (_assemblyMapping != null && !_assemblyMapping.TryGetValue(chainName, out assembly))
            {
                int p = chainName.LastIndexOf('.');
                if (p > 0)
                {
                    assembly = FindAssembly(chainName.Substring(0, p));
                }
                else
                {
                    return _coreAssembly;
                }
            }
            return assembly;
        }

        /// <summary>
        /// 解析当前域的所有程序集
        /// </summary>
        private static void ParseAssemblies()
        {
            lock (_lockObj)
            {
                if (_parsed)
                {
                    return;
                }

                _parsed = true;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var ass in assemblies)
                {
                    if (!ass.IsDynamic && !_assemblyLocation.ContainsKey(ass.FullName))
                    {
                        _assemblyLocation.Add(ass.FullName, GetLocation(ass));
                    }
                }

                DirectoryInfo rd = new DirectoryInfo(_executeDirectory);
                LoadAssembly(rd, SearchDirectoryDeep + 1);

                //ParallelOptions options = new ParallelOptions();
                //options.MaxDegreeOfParallelism = HardwareLibrary.NumberOfCores;
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
                _assemblyMapping =
                    new ConcurrentDictionary<string, Assembly>(3, assemblies.Length);
                foreach (var item in assemblies)
                {
                    AppendAssemblyMapping(item);
                }

                //Parallel.ForEach<Assembly>(assemblies, options, AppendAssemblyMapping);
                _typedPorts =
                    new ConcurrentDictionary<Type, List<Type>>(3, _assemblyMapping.Count);

                foreach (var item in _assemblyMapping.Values)
                {
                    ParseAssembly(item);
                }

                //Parallel.ForEach<Assembly>(_assemblyMapping.Values, options, ParseAssembly);
            }
        }
        /// <summary>
        /// 获取程序集短名称
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>程序集短名称</returns>
        private static string getShortAssemblyName(string assemblyName)
        {
            int p = assemblyName.IndexOf(',');
            if (p > 0)
            {
                return assemblyName.Substring(0, p);
            }
            return assemblyName;
        }

        /// <summary>
        /// 程序集解析失败事件,手动返回依赖程序集
        /// </summary>
        /// <param name="sender">事件原</param>
        /// <param name="args">事件数据</param>
        /// <returns>找到的依赖程序集</returns>
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly;
            if (_assemblyMapping != null && _assemblyMapping.TryGetValue(getShortAssemblyName(args.Name), out assembly))
            {
                return assembly;
            }
            return _coreAssembly;
        }

        /// <summary>
        /// 判断是否为公共运行时程序集,用于忽略公共运行时的扫描加快接口扫描速度
        /// </summary>
        /// <param name="assembly">程序集</param>
        /// <returns>是否为公共运行时程序集的检测结果标志</returns>
        private static bool IsCommonLanguageRuntimeLibrary(System.Reflection.Assembly assembly)
        {
            return assembly.ManifestModule.ScopeName.Equals(CommonLanguageRuntimeLibrary, StringComparison.Ordinal);
        }

        /// <summary>
        /// 加载目录中的所有DLL
        /// </summary>
        /// <param name="dir">目录路径</param>
        /// <param name="deep">子目录的扫描深度</param>
        private static void LoadAssembly(DirectoryInfo dir, int deep)
        {
            if (!dir.Name.Equals("html", StringComparison.OrdinalIgnoreCase) && !dir.Name.Equals("roslyn", StringComparison.OrdinalIgnoreCase))
            {
                foreach (FileInfo libFile in dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly))
                {
                    LoadAssembly(libFile);
                }
                if (deep > 1)
                {
                    int _deep = deep - 1;
                    foreach (DirectoryInfo chldDir in dir.GetDirectories())
                    {
                        LoadAssembly(chldDir, _deep);
                    }
                }
            }
        }

        /// <summary>
        /// 加载DLL
        /// </summary>
        /// <param name="assemblyFile">DLL程序集路径</param>
        private static void LoadAssembly(FileInfo assemblyFile)
        {
            try
            {
                var path = assemblyFile.FullName;
                if (!_assemblyLocation.Values.Contains(path))
                {
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFile(assemblyFile.FullName);
                    //using (var stream = assemblyFile.OpenRead())
                    //{
                    //    if (stream.Length > 0x3d)
                    //    {
                    //        var buf = new byte[stream.Length];
                    //        stream.Read(buf, 0, 0x3e);
                    //        if (BitConverter.ToChar(buf, 0x3c) == 0x0080)
                    //        {
                    //            System.Reflection.Assembly assembly;
                    //            var symbol = loadSymbol(assemblyFile.FullName);

                    //            stream.Read(buf, 0x3e, buf.Length - 0x3e);
                    //            if(symbol != null)
                    //            {
                    //                assembly = System.Reflection.Assembly.Load(buf, symbol);
                    //            }
                    //            else
                    //            {
                    //                assembly = System.Reflection.Assembly.Load(buf);
                    //            }
                    //        }
                    //    }
                    //}
                    _assemblyLocation[assembly.FullName] = path;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private static byte[] loadSymbol(string assemblyFile)
        {
            var pdbFile = assemblyFile.Substring(0, assemblyFile.Length - 3) + "pdb";
            if (File.Exists(pdbFile))
            {
                return File.ReadAllBytes(pdbFile);
            }

            return null;
        }

        /// <summary>
        /// 判断是否定义了ExportAttribute的特性
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>定义标志</returns>
        private static bool IsDefinedExport(Type type)
        {
            return type.IsDefined(_portAttributeType, false);
        }

        /// <summary>
        /// 添加程序集导映射字典
        /// </summary>
        /// <param name="assembly"></param>
        private static void AppendAssemblyMapping(Assembly assembly)
        {
            if (!assembly.IsDynamic && !IsCommonLanguageRuntimeLibrary(assembly))
            {
                string assName = assembly.FullName;
                int nameSeparatorIndexOf = assembly.FullName.IndexOf(',');

                if (nameSeparatorIndexOf > 0)
                {
                    assName = assembly.FullName.Substring(0, nameSeparatorIndexOf);
                }
                if (!_assemblyMapping.ContainsKey(assName))
                {
                    _assemblyMapping[assName] = assembly;
                }
            }
        }

        public static string GetAssemblyPath(System.Reflection.Assembly ass)
        {
            if (!ass.IsDynamic && ass.TryGetLocation(out string location))
            {
                return location;
            }

            return String.Empty;
        }

        /// <summary>
        /// 解析程序集
        /// </summary>
        /// <param name="assembly">程序集实例</param>
        private static void ParseAssembly(System.Reflection.Assembly assembly)
        {
            string assemblyPath = GetAssemblyPath(assembly);
            if (assemblyPath.StartsWith(_executeDirectory))
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsClass && !type.IsAbstract)
                        {
                            type.FindInterfaces(InterfaceFilter, type);
                        }
                        else if (type.IsEnum)
                        {
                            if (IsDefinedExport(type))
                            {
                                TypeMapping(typeof(System.Enum), type);
                            }
                        }
                    }
                }
                catch/* (Exception ex)*/
                {
                }
            }
        }


        /// <summary>
        /// 映射接口与实现
        /// </summary>
        /// <param name="ifType">接口类型</param>
        /// <param name="isType">实例类型</param>
        private static void TypeMapping(Type ifType, Type isType)
        {
            List<Type> interfaces;

            if (!_typedPorts.TryGetValue(ifType, out interfaces))
            {
                interfaces = new List<Type>();
                _typedPorts[ifType] = interfaces;
            }
            interfaces.Add(isType);
        }

        /// <summary>
        /// 实例类型的接口过滤
        /// </summary>
        /// <param name="ifType">接口类型</param>
        /// <param name="filterCriteria">实例类型</param>
        /// <returns></returns>
        private static bool InterfaceFilter(Type ifType, object filterCriteria)
        {
            Type isType = (Type)filterCriteria;

            if (!IsCommonLanguageRuntimeLibrary(ifType.Assembly) && IsDefinedExport(ifType))
            {
                TypeMapping(ifType, (Type)filterCriteria);
            }
            return false;
        }

        /// <summary>
        /// 接口的实现集合
        /// </summary>
        /// <typeparam name="T">接口定义类型泛型,如果是枚举类型使用System.Enum</typeparam>
        public static List<Type> Ports<T>()
        {
            ParseAssemblies();
            List<Type> ports;
            if (!_typedPorts.TryGetValue(typeof(T), out ports))
            {
                ports = new List<Type>();
            }
            return ports;
        }

        /// <summary>
        /// 所有程序集
        /// </summary>
        public static IEnumerable<Assembly> Assemblies
        {
            get
            {
                ParseAssemblies();
                return _assemblyMapping.Values;
            }
        }
    }
}
