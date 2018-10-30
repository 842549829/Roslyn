using System;
using System.IO;
using System.Linq;
using System.Reflection;
using InterFace;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Proxy;

namespace ConsoleAppTest
{
    class Program
    {
        static void Main(string[] args)
        {

            /*动态编译*/
            var order = ProxyClass.Generate<IOrder>();
            var dss = order.Add(2);


            /*简单实例*/
            //Roslyn.Create();
            //Roslyn.Test();
            Console.WriteLine("");
        }
    }

    /*简单实例*/
    public static class Roslyn
    {
        public static Assembly assembly;
        public static void Create()
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(@"
    using System;
    namespace RoslynCompileSample1
    {
        public class Writer
        {
            public void Write(string message)
            {
                Console.WriteLine(message);
            }
        }

public class Writer1
        {
            public void Write(string message)
            {
                Console.WriteLine(message);
            }
        }

    }");
            string assemblyName = Path.GetRandomFileName();
            var references = AppDomain.CurrentDomain.GetAssemblies().Where(x => x.Location != "").Select(x => MetadataReference.CreateFromFile(x.Location));
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }
            }
        }

        public static void Test()
        {
            Type type = assembly.GetType("RoslynCompileSample1.Writer1");
            object obj = Activator.CreateInstance(type);
            type.InvokeMember("Write", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, new object[] { "Hello World" });
        }
    }
}