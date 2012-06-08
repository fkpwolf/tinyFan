using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.TinyFan
{
    using System.Runtime.InteropServices;
    using System;

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
                fans[i].Value = tach[i] * 60 / 2;
                ActivateSensor(fans[i]);
                controls[i] = new Sensor(n, device, SensorType.TinyFanControl, this, settings);
                controls[i].Value = 0;
                Control c = new Control(controls[i], settings, 0, 100);
                c.ControlModeChanged += (cc) => //copy from SuperIOHardware.cs
                {
                    Console.WriteLine("fan mode changed.");
                    /*if (cc.ControlMode == ControlMode.Default)
                    {
                        superIO.SetControl(index, null);
                    }
                    else
                    {
                        superIO.SetControl(index, (byte)(cc.SoftwareValue * 2.55));
                    }*/
                };
                c.SoftwareControlValueChanged += (cc) =>
                {
                    //if (cc.ControlMode == ControlMode.Software)
                    //    superIO.SetControl(index, (byte)(cc.SoftwareValue * 2.55));
                    this.setTach();
                };
                controls[i].Control = c;
                ActivateSensor(controls[i]);
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
                duty[i] = (byte)(c.SoftwareValue * 2.55);
                this.controls[i].Value = c.SoftwareValue;
            }
            byte[] response = new byte[400];
            set_duty(duty, response); //write value of all fans. TODO should follow style of NCT677X.cs's WriteByte.
            Console.Out.WriteLine("The return of setTach is:" + System.Text.Encoding.ASCII.GetString(response));
        }
    }
}
