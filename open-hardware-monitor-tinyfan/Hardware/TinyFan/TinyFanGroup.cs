using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware.TinyFan
{
    internal class TinyFanGroup : IGroup
    {
        private ISettings settings;
        private readonly  List<TinyFan> hardware = new List<TinyFan>();

        public TinyFanGroup(ISettings settings)
        {
            // TODO: Complete member initialization
            this.settings = settings;
            TinyFan tinyFan = new TinyFan("xixi", settings);
            this.hardware.Add(tinyFan);
        }

        public IHardware[] Hardware
        {
            get
            {
                return hardware.ToArray();
            }
        }

        public string GetReport()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            //throw new NotImplementedException();
        }
    }
}
