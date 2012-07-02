

#region Using directives
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Management;
#endregion

//copy from http://1code.codeplex.com/SourceControl/changeset/view/39074#842775

namespace OpenHardwareMonitor.Hardware
{
    class Util
    {
        #region Is64BitOperatingSystem (IsWow64Process)

        /// <summary>
        /// The function determines whether the current operating system is a 
        /// 64-bit operating system.
        /// </summary>
        /// <returns>
        /// The function returns true if the operating system is 64-bit; 
        /// otherwise, it returns false.
        /// </returns>
        public static bool Is64BitOperatingSystem()
        {
            if (IntPtr.Size == 8)  // 64-bit programs run only on Win64
            {
                return true;
            }
            else  // 32-bit programs run on both 32-bit and 64-bit Windows
            {
                // Detect whether the current process is a 32-bit process 
                // running on a 64-bit system.
                bool flag;
                return ((DoesWin32MethodExist("kernel32.dll", "IsWow64Process") &&
                    IsWow64Process(GetCurrentProcess(), out flag)) && flag);
            }
        }

        /// <summary>
        /// The function determins whether a method exists in the export 
        /// table of a certain module.
        /// </summary>
        /// <param name="moduleName">The name of the module</param>
        /// <param name="methodName">The name of the method</param>
        /// <returns>
        /// The function returns true if the method specified by methodName 
        /// exists in the export table of the module specified by moduleName.
        /// </returns>
        static bool DoesWin32MethodExist(string moduleName, string methodName)
        {
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            if (moduleHandle == IntPtr.Zero)
            {
                return false;
            }
            return (GetProcAddress(moduleHandle, methodName) != IntPtr.Zero);
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule,
            [MarshalAs(UnmanagedType.LPStr)]string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        #endregion


        #region Is64BitOperatingSystem (WMI)

        /// <summary>
        /// The function determines whether the operating system of the 
        /// current machine of any remote machine is a 64-bit operating 
        /// system through Windows Management Instrumentation (WMI).
        /// </summary>
        /// <param name="machineName">
        /// The full computer name or IP address of the target machine. "." 
        /// or null means the local machine.
        /// </param>
        /// <param name="domain">
        /// NTLM domain name. If the parameter is null, NTLM authentication 
        /// will be used and the NTLM domain of the current user will be used.
        /// </param>
        /// <param name="userName">
        /// The user name to be used for the connection operation. If the 
        /// user name is from a domain other than the current domain, the 
        /// string may contain the domain name and user name, separated by a 
        /// backslash: string 'username' = "DomainName\\UserName". If the 
        /// parameter is null, the connection will use the currently logged-
        /// on user
        /// </param>
        /// <param name="password">
        /// The password for the specified user.
        /// </param>
        /// <returns>
        /// The function returns true if the operating system is 64-bit; 
        /// otherwise, it returns false.
        /// </returns>
        /// <exception cref="System.Management.ManagementException">
        /// The ManagementException exception is generally thrown with the  
        /// error code: System.Management.ManagementStatus.InvalidParameter.
        /// You need to check whether the parameters for ConnectionOptions 
        /// (e.g. user name, password, domain) are set correctly.
        /// </exception>
        /// <exception cref="System.Runtime.InteropServices.COMException">
        /// A common error accompanied with the COMException is "The RPC 
        /// server is unavailable. (Exception from HRESULT: 0x800706BA)". 
        /// This is usually caused by the firewall on the target machine that 
        /// blocks the WMI connection or some network problem.
        /// </exception>
        public static bool Is64BitOperatingSystem(string machineName,
            string domain, string userName, string password)
        {
            ConnectionOptions options = null;
            if (!string.IsNullOrEmpty(userName))
            {
                // Build a ConnectionOptions object for the remote connection 
                // if you plan to connect to the remote with a different user 
                // name and password than the one you are currently using.
                options = new ConnectionOptions();
                options.Username = userName;
                options.Password = password;
                options.Authority = "NTLMDOMAIN:" + domain;
            }
            // Else the connection will use the currently logged-on user

            // Make a connection to the target computer.
            ManagementScope scope = new ManagementScope("\\\\" +
                (string.IsNullOrEmpty(machineName) ? "." : machineName) +
                "\\root\\cimv2", options);
            scope.Connect();

            // Query Win32_Processor.AddressWidth which dicates the current 
            // operating mode of the processor (on a 32-bit OS, it would be 
            // "32"; on a 64-bit OS, it would be "64").
            // Note: Win32_Processor.DataWidth indicates the capability of 
            // the processor. On a 64-bit processor, it is "64".
            // Note: Win32_OperatingSystem.OSArchitecture tells the bitness
            // of OS too. On a 32-bit OS, it would be "32-bit". However, it 
            // is only available on Windows Vista and newer OS.
            ObjectQuery query = new ObjectQuery(
                "SELECT AddressWidth FROM Win32_Processor");

            // Perform the query and get the result.
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();
            foreach (ManagementObject queryObj in queryCollection)
            {
                if (queryObj["AddressWidth"].ToString() == "64")
                {
                    return true;
                }
            }

            return false;
        }

        #endregion


        static void Main(string[] args)
        {
            // Solution 1. Is64BitOperatingSystem (IsWow64Process)
            // Determine whether the current operating system is a 64 bit 
            // operating system.
            bool f64bitOS = Is64BitOperatingSystem();
            Console.WriteLine("The current operating system {0} 64-bit.",
                f64bitOS ? "is" : "is not");

            // Solution 2. Is64BitOperatingSystem (WMI)
            // Determine whether the current operating system is a 64 bit 
            // operating system through WMI. The function is also able to 
            // query the bitness of OS on a remote machine.
            try
            {
                f64bitOS = Is64BitOperatingSystem(".", null, null, null);
                Console.WriteLine("The current operating system {0} 64-bit.",
                    f64bitOS ? "is" : "is not");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Is64BitOperatingSystem throws the exception: {0}",
                    ex.Message);
            }
        }
    }
}