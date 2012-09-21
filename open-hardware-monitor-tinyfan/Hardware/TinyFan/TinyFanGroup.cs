using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Management;
using USBInterface;


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
            USBInterface.HIDInterface.interfaceDetails[] allDevices = this.getDevicesDetails();
            if (allDevices.Length == 0)
            {
                report.Append("Didn't find TinyFan USB device.");
                return;
            }
            for (int i = 0; i < allDevices.Length; i++)
            {
                USBInterface.HIDInterface.interfaceDetails deviceInfos = allDevices[i];
                report.AppendLine("Find one device. It's path is:" + deviceInfos.devicePath);
                TinyFan tinyFan = new TinyFan("tinyfan", settings, deviceInfos);
                this.hardware.Add(tinyFan);
            }
            
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

        /*copy from http://openavrusb.com/index.php/hid-device-class
         * only use it to fetch all tinyfan devices
         * failed when use it to send HID report(fallback to hidtool.dll)
         */
        public USBInterface.HIDInterface.interfaceDetails[] getDevicesDetails()
        {
            List<USBInterface.HIDInterface.interfaceDetails> result = new List<USBInterface.HIDInterface.interfaceDetails>();
            USBInterface.HIDInterface.interfaceDetails[] devices = USBInterface.HIDInterface.getConnectedDevices();
            for (int i = 0; i < devices.Length; i++)
            {
                USBInterface.HIDInterface.interfaceDetails device = devices[i];
                //report.AppendLine("USB device " + device.manufacturer + ", it's sn is:" + device.serialNumber);
                if (device.VID == 5824 && device.PID == 1503)
                {
                    result.Add(device);
                }
            }
            return result.ToArray();
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
