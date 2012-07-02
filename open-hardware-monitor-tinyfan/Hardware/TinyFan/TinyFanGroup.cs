using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Management;

namespace OpenHardwareMonitor.Hardware.TinyFan
{
    internal class TinyFanGroup : IGroup
    {
        private ISettings settings;
        private readonly  List<TinyFan> hardware = new List<TinyFan>();
        private readonly StringBuilder report = new StringBuilder();

        public TinyFanGroup(ISettings settings)
        {
            // TODO: Complete member initialization
            this.settings = settings;
            if (!this.isInstall())
            {
                report.Append("Didn't find TinyFan USB device.");
                return;
            }
            TinyFan tinyFan = new TinyFan("tinyfan", settings);
            this.hardware.Add(tinyFan);
        }

        public IHardware[] Hardware
        {
            get
            {
                return hardware.ToArray();
            }
        }

        bool isInstall()
        {
           /* RegistryKey key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Enum\USB\VID_16C0&PID_05DC");
            if (key == null)
                return false;
            else
                return true;
            * */
            return IsUsbDeviceConnected("16C0", "05DF");
        }

        public bool IsUsbDeviceConnected(string pid, string vid)
        {
            using (var searcher =
              new ManagementObjectSearcher(@"Select * From Win32_USBControllerDevice"))
            {
                using (var collection = searcher.Get())
                {
                    foreach (var device in collection)
                    {
                        var usbDevice = Convert.ToString(device);
                        if (usbDevice.Contains(pid) && usbDevice.Contains(vid))
                            return true;
                    }
                }
            }
            return false;
        }

        public string GetReport()
        {
            if (report.Length > 0)
            {
                StringBuilder r = new StringBuilder();
                r.AppendLine("TinyFan USB Fan Controller");
                r.AppendLine();
                r.Append(report);
                r.AppendLine();
                return r.ToString();
            }
            else
                return null;
        }

        public void Close()
        {
            //throw new NotImplementedException();
        }
    }
}
