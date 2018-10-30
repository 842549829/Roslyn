using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Autofac;
using InterFace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.DependencyInjection;
using Model;

namespace Proxy
{
    public class ProxyClass
    {
        private const string address = "http://localhost:57649/api/";

        static readonly IDictionary<string, Type> services = new ConcurrentDictionary<string, Type>(8, 128);

        static ProxyClass()
        {
            //PortsImporter.Ports<IService>();
            IEnumerable<Type> typeServices = typeof(IService).Assembly.GetTypes().Where(type =>
            {
                var typeInfo = type.GetTypeInfo();
                return typeInfo.IsInterface && typeInfo.GetCustomAttribute<BundleAttribute>() != null;
            }).ToList();

            foreach (var typeService in typeServices)
            {
                string code = GetCode(typeService);
                var assembly = GenerateProxyTree(code);
                var type = assembly.GetExportedTypes()[0];
                var fullName = typeService.FullName;
                services.Add(fullName, type);
            }
        }

        public static T CreateProxy<T>(Type proxyType, object context)
        {
            return (T)Create(proxyType, context);
        }

        public static object Create(Type proxyType, object context)
        {
            var instance = proxyType.GetTypeInfo().GetConstructors().First().Invoke(null);
            return instance;
        }

        public static T Generate<T>()
        {
            if (services.TryGetValue(typeof(T).FullName, out var type))
            {
                return CreateProxy<T>(type, null);
            }
            throw new Exception("未找到实现");
        }

        private static string GetCode(Type typeService)
        {
            StringBuilder codes = new StringBuilder();
            codes.AppendLine("using System;");
            codes.AppendLine("using Model;");
            codes.AppendLine("using System.Linq;");
            codes.AppendFormat("using {0};", typeService.Namespace);
            codes.AppendLine();
            codes.AppendLine("namespace RoslynCompileSample");
            codes.AppendLine("{");
            codes.AppendFormat("public class Proxy{0} : {1}", typeService.Name, typeService.Name);
            codes.AppendLine();
            codes.AppendLine("{");
            var methods = typeService.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var method in methods)
            {
                codes.AppendLine();
                codes.AppendFormat("public {0} {1} (", method.ReturnType.FullName, method.Name);
                List<string> parameterList = new List<string>();
                var parameters = method.GetParameters();
                foreach (var parameter in parameters)
                {
                    parameterList.Add($"{parameter.ParameterType.FullName} {parameter.Name}");
                }

                codes.Append(string.Join(',', parameterList));
                codes.AppendFormat(")");
                codes.AppendLine();
                codes.AppendLine("{");


                #region 需要自己实现的业务代码

                /*业务*/
                string url = address + typeService.Name.ToLower();
                if (method.GetCustomAttribute(typeof(HttpGetAttribute), false) is HttpGetAttribute httpgetattribute)
                {
                    var parameterValueList = new List<string>();
                    foreach (var parameter in parameters)
                    {
                        parameterValueList.Add(parameter.Name + ".ToString()");
                    }
                    string parameterValue = string.Join(",", parameterValueList);
                    if (httpgetattribute.Template == null)
                    {
                        string httpGetMethodBody = CreateHttpGetMethodBody(url, method.ReturnType.ToString(), parameterValue);
                        codes.AppendLine(httpGetMethodBody);
                    }
                    else
                    {
                        var template = httpgetattribute.Template;
                        string[] templateArray = template.Split('/');
                        if (templateArray.Length >= 1)
                        {
                            if (templateArray[0].Contains("{"))
                            {
                                parameterValueList.Clear();
                                foreach (var t in templateArray)
                                {
                                    parameterValueList.Add(t.Replace("{", string.Empty).Replace("}", string.Empty) + ".ToString()");
                                }
                                string httpGetMethodBody = CreateHttpGetMethodBody(url, method.ReturnType.ToString(), string.Join(",", parameterValueList));
                                codes.AppendLine(httpGetMethodBody);
                            }
                            else
                            {
                                url += "/" + templateArray[0];
                                parameterValueList.Clear();
                                for (int i = 0; i < templateArray.Length; i++)
                                {
                                    if (i != 0)
                                    {
                                        parameterValueList.Add(templateArray[i].Replace("{", string.Empty).Replace("}", string.Empty) + ".ToString()");
                                    }
                                }
                                string httpGetMethodBody = CreateHttpGetMethodBody(url, method.ReturnType.ToString(), string.Join(",", parameterValueList));
                                codes.AppendLine(httpGetMethodBody);
                            }
                        }
                        else
                        {
                            codes.AppendLine("return null;");
                        }
                    }
                }
                else if (method.GetCustomAttribute(typeof(HttpPostAttribute), false) is HttpPostAttribute httpPostAttribute)
                {
                    var parameterType = parameters.First();
                    if (httpPostAttribute.Template == null)
                    {
                        string httpGetMethodBody = CreateHttpPostMethodBody(url, parameterType.ParameterType.FullName, method.ReturnType.ToString(), parameterType.Name);
                        codes.AppendLine(httpGetMethodBody);
                    }
                    else
                    {
                        var template = httpPostAttribute.Template;
                        string[] templateArray = template.Split('/');
                        if (templateArray.Length == 1)
                        {
                            url += "/" + templateArray[0];
                            string httpGetMethodBody = CreateHttpPostMethodBody(url, parameterType.ParameterType.FullName, method.ReturnType.ToString(), parameterType.Name);
                            codes.AppendLine(httpGetMethodBody);
                        }
                        else
                        {
                            codes.AppendLine("return null;");
                        }
                    }
                }
                else
                {
                    codes.AppendLine("return null;");
                }

                #endregion
                codes.AppendLine("}");
            }
            codes.AppendLine("}");
            codes.AppendLine("}");
            var result = codes.ToString();
            return result;
        }

        private static string CreateHttpGetMethodBody(string url, string returnType, string parameterValue)
        {
            StringBuilder codes = new StringBuilder();
            codes.AppendFormat("HttpClientUtility client = new HttpClientUtility(\"{0}\");", url);
            codes.AppendLine();
            codes.AppendFormat("return client.Get<{0}>(new string[] {{ {1} }});", returnType, parameterValue);
            codes.AppendLine();
            return codes.ToString();
        }


        private static string CreateHttpPostMethodBody(string url, string parameterType, string returnType, string parameterName)
        {
            StringBuilder codes = new StringBuilder();
            codes.AppendFormat("HttpClientUtility client = new HttpClientUtility(\"{0}\");", url);
            codes.AppendLine();
            codes.AppendFormat("return client.Post<{0},{1}>({2});", parameterType, returnType, parameterName);
            codes.AppendLine();
            return codes.ToString();
        }

        /// <summary>
        /// 万能接口
        /// </summary>
        /// <param name="code">传入你要实现的代码</param>
        /// <returns>动态生成一个程序集</returns>
        public static Assembly GenerateProxyTree(string code)
        {
            Assembly assembly = null;
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            string assemblyName = Path.GetRandomFileName();
            var references = AppDomain.CurrentDomain.GetAssemblies().Select(x => MetadataReference.CreateFromFile(x.Location));
            CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }
            }
            return assembly;
        }

        public static void Tets()
        {
            //var code = @"using System; namespace RoslynCompileSample { public class Writer { public void Write(string message) { Console.WriteLine(message); } } }";
            //var assembly = GenerateProxyTree(code);
            //Type type = assembly.GetType("RoslynCompileSample.Writer");
            //object obj = Activator.CreateInstance(type);
            //type.InvokeMember("Write", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, new object[] { "打印一句话" });
        }
    }
}