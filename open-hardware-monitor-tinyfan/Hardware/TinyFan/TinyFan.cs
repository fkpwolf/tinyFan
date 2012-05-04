using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.TinyFan
{
    internal class TinyFan : Hardware, IDisposable {
        private readonly Sensor[] fans;
        private readonly Sensor[] controls;

        public TinyFan(string name,  ISettings settings)
            :base("TinyFan", new Identifier("TinyFan",
            name.TrimStart(new [] {'/'}).ToLowerInvariant()), settings)
        {
            int fanCount = 4;
            fans = new Sensor[fanCount];
            controls = new Sensor[fanCount];
            for (int i = 0; i < fanCount; i++)
            {
                int device = 33 + i;
                string n = "Fan" + (i + 1);
                fans[i] = new Sensor(n, device, SensorType.Fan, this, settings);
                fans[i].Value = 123;
                ActivateSensor(fans[i]);
                controls[i] =
                 new Sensor(n, device, SensorType.Control, this, settings);
                controls[i].Value = 223;
                ActivateSensor(controls[i]);
            }
        }

        public override HardwareType HardwareType
        {
            get { return HardwareType.Heatmaster; }
        }

        public override void Update()
        {
        }

        public void Dispose()
        {
        }

    }
}
