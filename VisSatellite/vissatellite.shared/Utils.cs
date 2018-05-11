using System;
using System.Reflection;
using System.Text;
using System.Globalization;
using System.IO;

namespace vissatellite.shared
{
    public static class Utils
    {
        public static string LoadFromEmbeddedResource(string resourceName)
        {
            string result = string.Empty;
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);    
			using (StreamReader sr = new StreamReader(stream)) {
				result = sr.ReadToEnd();
			}
            return result;
        }
    }
}
