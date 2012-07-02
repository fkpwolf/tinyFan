using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.TinyFan
{
    using System.Runtime.InteropServices;
    using System;
    using Microsoft.Win32;

    internal class TinyFan : Hardware, IDisposable {

        [DllImport("hidtool.dll")]
        private static extern string get_tach(
            [MarshalAs(UnmanagedType.LPArray)] byte[] ret,
            [MarshalAs(UnmanagedType.LPArray)] byte[] response
        );

        [DllImport("hidtool.dll")]
        private static extern void set_duty(
            [MarshalAs(UnmanagedType.LPArray)] byte[] duty,
            [MarshalAs(UnmanagedType.LPArray)] byte[] response
        );

        [DllImport("hidtool.dll")]
        private static extern void set_fan_mode(
            [MarshalAs(UnmanagedType.LPArray)] byte[] duty,
            [MarshalAs(UnmanagedType.LPArray)] byte[] response
        );

        private readonly Sensor[] fans;
        private readonly Sensor[] controls;

        public TinyFan(string name,  ISettings settings)
            :base("TinyFan", new Identifier("TinyFan",
            name.TrimStart(new [] {'/'}).ToLowerInvariant()), settings)
        {
            int fanCount = 4;
            fans = new Sensor[fanCount];
            controls = new Sensor[fanCount];
            byte[] tach = this.getTach();
            for (int i = 0; i < fanCount; i++)
            {
                int device = 33 + i;
                string n = "Fan" + (i + 1);
                fans[i] = new Sensor(n, device, SensorType.Fan, this, settings);
                //fans[i].Value = tach[i] * 60 / 2;
                ActivateSensor(fans[i]);
                controls[i] = new Sensor(n, device, SensorType.TinyFanControl, this, settings);
                Control c = new Control(controls[i], settings, 0, 100);
                c.ControlModeChanged += (cc) => //copy from SuperIOHardware.cs
                {
                    Console.WriteLine("fan mode changed.");
                };
                c.SoftwareControlValueChanged += (cc) =>
                {
                    this.setTach();
                };
                c.FanModeChanged += (cc) =>
                {
                    this.setFanPinMode();
                };
                controls[i].Control = c;
                ActivateSensor(controls[i]);
                controls[i].Value = controls[i].Control.SoftwareValue;
            }
            //init device, since now device is passive and didn't save setting itself
            this.setFanPinMode();
            this.setTach();

            //test.
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
        }

        private void SystemEvents_PowerModeChanged(object sender,
                        PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Console.WriteLine("-----------------------I weak from resume");
                //here need to notify all controls to write value to all kinds of devices
            }
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.TinyFan; }
        }

        public override void Update()
        {
            Console.Out.WriteLine("update......");
            int fanCount = 4;
            byte[] tach = this.getTach();
            for (int i = 0; i < fanCount; i++)
            {
                fans[i].Value = tach[i] * 60 / 2;
            }
        }

        public void Dispose()
        {
        }

        private byte[] getTach()
        {
            byte[] ret = new byte[4];
            byte[] response = new byte[400];
            get_tach(ret, response);
            Console.Out.WriteLine("The return of getTach is:" + System.Text.Encoding.ASCII.GetString(response));
            return ret;
        }

        private void setTach()
        {
            byte[] duty = new byte[4];
            for (int i = 0; i < 4; i++){
                IControl c = this.controls[i].Control;
                this.controls[i].Value = c.SoftwareValue;
                duty[i] = (byte)(c.SoftwareValue * 2.55);
            }
            byte[] response = new byte[400];
            set_duty(duty, response); //write value of all fans. TODO should follow style of NCT677X.cs's WriteByte.
            Console.Out.WriteLine("The return of setTach is:" + System.Text.Encoding.ASCII.GetString(response));
        }

        private void setFanPinMode()
        {
            byte[] duty = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                IControl c = this.controls[i].Control;
                duty[i] = (byte)(c.FanMode == 0 ? 33 : 44);
                Console.Out.WriteLine("fan(" + i + ")'s mode is:" + c.FanMode + ", byte:" + duty[i]);
            }
            byte[] response = new byte[400];
            set_fan_mode(duty, response);
            Console.Out.WriteLine("The return of setFanPinMode is:" + System.Text.Encoding.ASCII.GetString(response));
        }

        public override string GetReport()
        {
            StringBuilder r = new StringBuilder();

            r.AppendLine("TinyFan");
            r.AppendLine();

            return r.ToString();
        }
    }
}
