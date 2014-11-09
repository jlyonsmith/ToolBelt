using System;
using System.IO;
using System.Reflection;

namespace Shared.ServiceInterface
{
    public class ResourceManager : IResourceManager
    {
        public ResourceManager()
        {
        }

        #region IResourceManager

        public string GetResource(string resourceName)
        {
            // BUG #98: Cache these using the ICache 
            using (StreamReader reader = new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        #endregion
    }
}

