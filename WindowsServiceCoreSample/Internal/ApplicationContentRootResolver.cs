using System;
using System.IO;

namespace WindowsServiceCoreSample.Internal
{
    internal static class ApplicationContentRootResolver
    {
        public static string GetApplicationContentRoot()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string applicationBasePath = AppContext.BaseDirectory;

            //Try find appsettings.json file in applicationBasePath, to take applicationBasePath instead of currentDirectory (eg.: for Windows Services where currentDirectory is in 'C:\Windows\system32\')
            if (File.Exists(Path.Combine(applicationBasePath, "appsettings.json")))
            {
                return applicationBasePath;
            }

            return currentDirectory;
        }
    }
}