using System;
using System.IO;
using System.Reflection;

namespace Utilities
{
    public class EmbeddedResourcesHandler
    {
        public static string GetResource(string resourceFile)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                // Just for info
                string[] resources = assembly.GetManifestResourceNames();

                var stream = assembly.GetManifestResourceStream("Utilities.Resources." + resourceFile);

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                throw new Exception();
            }
        }
    }
}

