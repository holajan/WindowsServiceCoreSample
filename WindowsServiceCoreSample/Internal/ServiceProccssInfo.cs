using System;
using System.Runtime.InteropServices;

namespace WindowsServiceCoreSample.Internal
{
    internal static class ServiceProccssInfo
    {
        #region member types declarations
        private static class NativeMethods
        {
            #region constants
            public const int SC_STATUS_PROCESS_INFO = 0;
            public const int ERROR_INSUFFICIENT_BUFFER = 122;
            #endregion

            #region member types declarations
            [StructLayoutAttribute(LayoutKind.Sequential)]
            public struct SERVICE_STATUS_PROCESS
            {
                public uint dwServiceType;
                public uint dwCurrentState;
                public uint dwControlsAccepted;
                public uint dwWin32ExitCode;
                public uint dwServiceSpecificExitCode;
                public uint dwCheckPoint;
                public uint dwWaitHint;
                public uint dwProcessId;
                public uint dwServiceFlags;
            }
            #endregion

            #region native methods
            [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool CloseServiceHandle(IntPtr hService);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool QueryServiceStatusEx(IntPtr hService, int InfoLevel, IntPtr serviceStatusBuffer, uint bufSize, out uint bytesNeeded);
            #endregion
        }
        #endregion

        #region action methods
        public static System.ServiceProcess.ServiceController GetServiceByProcessId(int serviceProcessId)
        {
            var services = System.ServiceProcess.ServiceController.GetServices();

            foreach (var service in services)
            {
                if (service.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                {
                    if (GetServicePID(service.ServiceName) == serviceProcessId)
                    {
                        return service;
                    }
                }
            }

            return null;
        }
        #endregion

        #region private member functions
        private static int GetServicePID(string serviceName)
        {
            IntPtr serviceControlManagerHandler = NativeMethods.OpenSCManager(null, null, 0xF003F);
            try
            {
                IntPtr serviceHandler = NativeMethods.OpenService(serviceControlManagerHandler, serviceName, 0xF003F);

                if (serviceHandler != IntPtr.Zero)
                {
                    try
                    {
                        IntPtr buffer = IntPtr.Zero;

                        //Call once to figure the size of the output buffer.
                        uint bytesNeeded;
                        NativeMethods.QueryServiceStatusEx(serviceHandler, NativeMethods.SC_STATUS_PROCESS_INFO, buffer, 0, out bytesNeeded);
                        if (Marshal.GetLastWin32Error() == NativeMethods.ERROR_INSUFFICIENT_BUFFER)
                        {
                            // Allocate required buffer and call again.
                            buffer = Marshal.AllocHGlobal((int)bytesNeeded);

                            if (NativeMethods.QueryServiceStatusEx(serviceHandler, NativeMethods.SC_STATUS_PROCESS_INFO, buffer, bytesNeeded, out bytesNeeded))
                            {
                                var ssp = Marshal.PtrToStructure<NativeMethods.SERVICE_STATUS_PROCESS>(buffer);
                                return (int)ssp.dwProcessId;
                            }
                        }
                    }
                    finally
                    {
                        NativeMethods.CloseServiceHandle(serviceHandler);
                    }
                }
            }
            finally
            {
                NativeMethods.CloseServiceHandle(serviceControlManagerHandler);
            }

            return 0;
        }
        #endregion
    }
}