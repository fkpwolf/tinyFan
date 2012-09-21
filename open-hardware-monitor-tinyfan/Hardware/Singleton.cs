using System;
using System.Collections.Generic;
using System.Text;

namespace OpenHardwareMonitor.Hardware
{
    public class Singleton
    {
        // Fields
        private static Singleton instance;

        // Constructor
        protected Singleton() { }

        // Methods
        public static Singleton Instance()
        {
            // Uses "Lazy initialization"
            if (instance == null)
                instance = new Singleton();

            return instance;
        }

        public IHardware SuperIO;
        public IHardware GPU;
    }
}
