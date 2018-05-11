using System;
using System.Reflection;
using System.IO;

namespace vissatellite
{
    public static class Utils
    {
        public static string LoadEmbeddedResourceAsString(string resourceName)
        {
            string result = string.Empty;
            var assembly = typeof(Utils).Assembly;
            var resourceStream = assembly.GetManifestResourceStream(resourceName);
            using(var reader = new StreamReader(resourceStream)) {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}
