using System;
using System.Reflection;
using System.IO;

namespace vissatellite
{
    public static class Utils
    {
        public static Stream OpenEmbeddedResource(string resourceName)
        {
            var assembly = typeof(Utils).Assembly;
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            return resourceStream;
        }

        public static string LoadEmbeddedResourceAsString(string resourceName)
        {
            string result = string.Empty;
            using(var stream = OpenEmbeddedResource(resourceName))
            using(var reader = new StreamReader(stream)) {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}
