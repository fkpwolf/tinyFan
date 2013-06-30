using System;
using System.Collections.Generic;
using System.Text;
using USBInterface;

namespace OpenHardwareMonitor.Hardware.TinyFan
{
    using System.Runtime.InteropServices;
    using System;
    using Microsoft.Win32;

    internal class TinyFan : Hardware, IDisposable {

        [DllImport("hidtool.dll")]
        private static extern string get_tach(
            [MarshalAs(UnmanagedType.LPArray)] byte[] devicePath,
            [MarshalAs(UnmanagedType.LPArray)] byte[] ret,
            [MarshalAs(UnmanagedType.LPArray)] byte[] response
        );

        [DllImport("hidtool.dll")]
        private static extern void set_duty(
            [MarshalAs(UnmanagedType.LPArray)] byte[] devicePath,
            [MarshalAs(UnmanagedType.LPArray)] byte[] duty,
            [MarshalAs(UnmanagedType.LPArray)] byte[] response
        );

        [DllImport("hidtool.dll")]
        private static extern void set_fan_mode(
            [MarshalAs(UnmanagedType.LPArray)] byte[] devicePath,
            [MarshalAs(UnmanagedType.LPArray)] byte[] duty,
            [MarshalAs(UnmanagedType.LPArray)] byte[] response
        );

        private readonly Sensor[] fans;
        private readonly Sensor[] controls;
        private USBInterface.HIDInterface.interfaceDetails deviceInfo;

        public TinyFan(string name, ISettings settings, USBInterface.HIDInterface.interfaceDetails deviceInfo)
            : base("TinyFan", new Identifier("TinyFan", deviceInfo.serialNumber), settings)
        {
            this.deviceInfo = deviceInfo;
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
                c.FanFollowChanged += (cc) =>
                {
                    this.setTach(); //when close "fan follow", I have to invoke this since Update() will not invoke setTach at this case.
                };
                controls[i].Control = c;
                ActivateSensor(controls[i]);
                controls[i].Value = controls[i].Control.SoftwareValue;
            }
            //init device, since now device is passive and didn't save setting itself
            this.setFanPinMode();
            this.setTach();
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.TinyFan; }
        }
        public override void Update(){
            int sec = DateTime.Now.Second;
            if (needFollowOtherFan() && sec % 3 == 0){
                setTach(); //sync
            }
            else {
                int fanCount = 4;
                byte[] tach = this.getTach();
                for (int i = 0; i < fanCount; i++){
                    fans[i].Value = tach[i] * 60 / 2;
                }
            }
        }

        private bool needFollowOtherFan(){
            foreach (ISensor sub in this.Sensors){
                if (sub.SensorType.Equals(SensorType.TinyFanControl)) {
                    if (!sub.Control.FanFollow.Equals(FanFollow.NONE))
                        return true;
                }
            }
            return false;
        }

        public void Dispose(){
        }

        private byte[] getTach() {
            byte[] ret = new byte[4];
            byte[] response = new byte[400];
            get_tach(System.Text.Encoding.ASCII.GetBytes(this.deviceInfo.devicePath), ret, response);
            return ret;
        }

        private void setTach() {
            byte[] duty = new byte[4];
            for (int i = 0; i < 4; i++){
                IControl c = this.controls[i].Control;
                if (c.FanFollow.Equals(FanFollow.CPU)){
                    this.controls[i].Value = this.getCPUFanDuty() * c.SoftwareValue / 100;
                }
                else if (c.FanFollow.Equals(FanFollow.GPU)){
                    this.controls[i].Value = this.getGPUFanDuty() * c.SoftwareValue / 100;
                } else
                    this.controls[i].Value = c.SoftwareValue;

                duty[i] = (byte)(this.controls[i].Value * 2.55); 
                //duty[i] = (byte)(c.SoftwareValue * 2.55); //for range [0-100] to [0-255]
            }
            byte[] response = new byte[400];
            set_duty(System.Text.Encoding.ASCII.GetBytes(this.deviceInfo.devicePath), duty, response); //write value of all fans. TODO should follow style of NCT677X.cs's WriteByte.
        }

        private void setFanPinMode() {
            byte[] duty = new byte[4];
            for (int i = 0; i < 4; i++) {
                IControl c = this.controls[i].Control;
                duty[i] = (byte)(c.FanMode == 0 ? 33 : 44);
                Console.Out.WriteLine("fan(" + i + ")'s mode is:" + c.FanMode + ", byte:" + duty[i]);
            }
            byte[] response = new byte[400];
            set_fan_mode(System.Text.Encoding.ASCII.GetBytes(this.deviceInfo.devicePath), duty, response);
        }

        public override string GetReport() {
            StringBuilder r = new StringBuilder();

            r.AppendLine("TinyFan");
            r.AppendLine("device path:" + this.deviceInfo.devicePath);

            return r.ToString();
        }

        public int getGPUFanDuty(){
            IHardware gpu = Singleton.Instance().GPU;
            if (gpu != null){
                ISensor gpuControl = null;
                foreach (ISensor sub in gpu.Sensors){
                    if (sub.SensorType.Equals(SensorType.Control)){
                        gpuControl = sub;
                        break;
                    }
                }
                if (gpuControl != null)
                    return (int)gpuControl.Value;
            }
            return 0;
        }

        public int getCPUFanDuty(){
            IHardware motherBoard = Singleton.Instance().SuperIO;
            if (motherBoard != null){
                ISensor cpuSensor = null;
                foreach (ISensor sub in motherBoard.Sensors) {
                    if (sub.SensorType.Equals(SensorType.Control) && sub.Name.ToLower().Contains("cpu")){
                        cpuSensor = sub;
                        break;
                    }
                }
                if (cpuSensor != null)
                    return (int)cpuSensor.Value;
            }
            return 0;
        }
    }
}
