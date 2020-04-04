using System;
using System.IO;

namespace WindowsServiceCoreSample.Internal
{
    internal static class ApplicationContentRootResolver
    {
        public static string GetApplicationContentRoot()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            string rootDirectory = ResolveApplicationRootDirectory(currentDirectory, nameof(WindowsServiceCoreSample));
            if (rootDirectory == null)
            {
                string applicationBasePath = AppContext.BaseDirectory;
                rootDirectory = ResolveApplicationRootDirectory(applicationBasePath, nameof(WindowsServiceCoreSample));
            }

            if (rootDirectory != null)
            {
                return rootDirectory;
            }

            return currentDirectory;
        }

        private static string ResolveApplicationRootDirectory(string path, string applicationProjectName)
        {
            //Try find appsettings.json file or <applicationProjectName>\appsettings.json from
            var di = new DirectoryInfo(path);

            while (di.Parent != null)
            {
                if (File.Exists(Path.Combine(di.FullName, "appsettings.json")))
                {
                    return di.FullName;
                }

                if (File.Exists(Path.Combine(di.FullName, applicationProjectName, "appsettings.json")))
                {
                    return Path.Combine(di.FullName, applicationProjectName);
                }

                di = di.Parent;
            }

            return null;
        }
    }
}
