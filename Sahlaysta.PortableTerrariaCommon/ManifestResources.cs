using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Reads the assembly manifest resources and data.
    /// </summary>
    internal static class ManifestResources
    {

        public static byte[] ReadByteArray(string resourceName)
        {
            byte[] byteArray;
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ArgumentException("Resource not found: " + resourceName);
                }
                byteArray = new byte[resourceStream.Length];
                using (MemoryStream memoryStream = new MemoryStream(byteArray))
                {
                    resourceStream.CopyTo(memoryStream);
                }
            }
            return byteArray;
        }

        public static string ReadUTF8String(string resourceName)
        {
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new ArgumentException("Resource not found: " + resourceName);
                }
                using (StreamReader streamReader = new StreamReader(resourceStream, new UTF8Encoding(false, true)))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

    }
}