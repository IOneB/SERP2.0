using RazorLight;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace UI
{
    public class RazorGenerator
    {
        static RazorLightEngine engine;

        static RazorGenerator()
        {
            if (engine is null)
            {
                string path  = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName, @"UI\Views");

                engine = new RazorLightEngineBuilder()
                        .UseFilesystemProject(path)
                        .UseMemoryCachingProvider()
                        .Build();
            }
        }

        public static string GenerateView(string viewName, object model)
        {
            return engine.CompileRenderAsync(viewName, model).Result;

        }
    }
}
