﻿/*
  
  Version: MPL 1.1/GPL 2.0/LGPL 2.1

  The contents of this file are subject to the Mozilla Public License Version
  1.1 (the "License"); you may not use this file except in compliance with
  the License. You may obtain a copy of the License at
 
  http://www.mozilla.org/MPL/

  Software distributed under the License is distributed on an "AS IS" basis,
  WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
  for the specific language governing rights and limitations under the License.

  The Original Code is the Open Hardware Monitor code.

  The Initial Developer of the Original Code is 
  Michael Möller <m.moeller@gmx.ch>.
  Portions created by the Initial Developer are Copyright (C) 2009-2011
  the Initial Developer. All Rights Reserved.

  Contributor(s):

  Alternatively, the contents of this file may be used under the terms of
  either the GNU General Public License Version 2 or later (the "GPL"), or
  the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
  in which case the provisions of the GPL or the LGPL are applicable instead
  of those above. If you wish to allow use of your version of this file only
  under the terms of either the GPL or the LGPL, and not to allow others to
  use your version of this file under the terms of the MPL, indicate your
  decision by deleting the provisions above and replace them with the notice
  and other provisions required by the GPL or the LGPL. If you do not delete
  the provisions above, a recipient may use your version of this file under
  the terms of any one of the MPL, the GPL or the LGPL.
 
*/

using System;
using System.Globalization;
using System.Text;

namespace OpenHardwareMonitor.Hardware.CPU {
  internal sealed class IntelCPU : GenericCPU {

    private enum Microarchitecture {
      Unknown,
      NetBurst,
      Core,
      Atom,
      Nehalem,
      SandyBridge
    }

    private readonly Sensor[] coreTemperatures;
    private readonly Sensor packageTemperature;
    private readonly Sensor[] coreClocks;
    private readonly Sensor busClock;
    private readonly Sensor[] powerSensors;

    private readonly Microarchitecture microarchitecture;
    private readonly double timeStampCounterMultiplier;

    private const uint IA32_THERM_STATUS_MSR = 0x019C;
    private const uint IA32_TEMPERATURE_TARGET = 0x01A2;
    private const uint IA32_PERF_STATUS = 0x0198;
    private const uint MSR_PLATFORM_INFO = 0xCE;
    private const uint IA32_PACKAGE_THERM_STATUS = 0x1B1;
    private const uint MSR_RAPL_POWER_UNIT = 0x606;
    private const uint MSR_PKG_ENERY_STATUS = 0x611;
    private const uint MSR_DRAM_ENERGY_STATUS = 0x619;
    private const uint MSR_PP0_ENERY_STATUS = 0x639;
    private const uint MSR_PP1_ENERY_STATUS = 0x641;

    private readonly uint[] energyStatusMSRs = { MSR_PKG_ENERY_STATUS, 
      MSR_PP0_ENERY_STATUS, MSR_PP1_ENERY_STATUS, MSR_DRAM_ENERGY_STATUS };
    private readonly string[] powerSensorLabels = 
      { "CPU Package", "CPU Cores", "CPU Graphics", "CPU DRAM" };
    private float energyUnitMultiplier = 0;
    private DateTime[] lastEnergyTime;
    private uint[] lastEnergyConsumed;


    private float[] Floats(float f) {
      float[] result = new float[coreCount];
      for (int i = 0; i < coreCount; i++)
        result[i] = f;
      return result;
    }

    private float[] GetTjMaxFromMSR() {
      uint eax, edx;
      float[] result = new float[coreCount];
      for (int i = 0; i < coreCount; i++) {
        if (Ring0.RdmsrTx(IA32_TEMPERATURE_TARGET, out eax,
          out edx, 1UL << cpuid[i][0].Thread)) {
          result[i] = (eax >> 16) & 0xFF;
        } else {
          result[i] = 100;
        }
      }
      return result;
    }

    public IntelCPU(int processorIndex, CPUID[][] cpuid, ISettings settings)
      : base(processorIndex, cpuid, settings) {
      // set tjMax
      float[] tjMax;
      switch (family) {
        case 0x06: {
            switch (model) {
              case 0x0F: // Intel Core 2 (65nm)
                microarchitecture = Microarchitecture.Core;
                switch (stepping) {
                  case 0x06: // B2
                    switch (coreCount) {
                      case 2:
                        tjMax = Floats(80 + 10); break;
                      case 4:
                        tjMax = Floats(90 + 10); break;
                      default:
                        tjMax = Floats(85 + 10); break;
                    }
                    tjMax = Floats(80 + 10); break;
                  case 0x0B: // G0
                    tjMax = Floats(90 + 10); break;
                  case 0x0D: // M0
                    tjMax = Floats(85 + 10); break;
                  default:
                    tjMax = Floats(85 + 10); break;
                } break;
              case 0x17: // Intel Core 2 (45nm)
                microarchitecture = Microarchitecture.Core;
                tjMax = Floats(100); break;
              case 0x1C: // Intel Atom (45nm)
                microarchitecture = Microarchitecture.Atom;
                switch (stepping) {
                  case 0x02: // C0
                    tjMax = Floats(90); break;
                  case 0x0A: // A0, B0
                    tjMax = Floats(100); break;
                  default:
                    tjMax = Floats(90); break;
                } break;
              case 0x1A: // Intel Core i7 LGA1366 (45nm)
              case 0x1E: // Intel Core i5, i7 LGA1156 (45nm)
              case 0x1F: // Intel Core i5, i7 
              case 0x25: // Intel Core i3, i5, i7 LGA1156 (32nm)
              case 0x2C: // Intel Core i7 LGA1366 (32nm) 6 Core
              case 0x2E: // Intel Xeon Processor 7500 series
                microarchitecture = Microarchitecture.Nehalem;
                tjMax = GetTjMaxFromMSR();
                break;
              case 0x2A: // Intel Core i5, i7 2xxx LGA1155 (32nm)
              case 0x2D: // Next Generation Intel Xeon Processor
                microarchitecture = Microarchitecture.SandyBridge;
                tjMax = GetTjMaxFromMSR();
                break;
              default:
                microarchitecture = Microarchitecture.Unknown;
                tjMax = Floats(100);
                break;
            }
          } break;
        case 0x0F: {
            switch (model) {
              case 0x00: // Pentium 4 (180nm)
              case 0x01: // Pentium 4 (130nm)
              case 0x02: // Pentium 4 (130nm)
              case 0x03: // Pentium 4, Celeron D (90nm)
              case 0x04: // Pentium 4, Pentium D, Celeron D (90nm)
              case 0x06: // Pentium 4, Pentium D, Celeron D (65nm)
                microarchitecture = Microarchitecture.NetBurst;
                tjMax = Floats(100);
                break;
              default:
                microarchitecture = Microarchitecture.Unknown;
                tjMax = Floats(100);
                break;
            }
          } break;
        default:
          microarchitecture = Microarchitecture.Unknown;
          tjMax = Floats(100);
          break;
      }

      // set timeStampCounterMultiplier
      switch (microarchitecture) {
        case Microarchitecture.NetBurst:
        case Microarchitecture.Atom:
        case Microarchitecture.Core: {
            uint eax, edx;
            if (Ring0.Rdmsr(IA32_PERF_STATUS, out eax, out edx)) {
              timeStampCounterMultiplier =
                ((edx >> 8) & 0x1f) + 0.5 * ((edx >> 14) & 1);
            }
          } break;
        case Microarchitecture.Nehalem:
        case Microarchitecture.SandyBridge: {
            uint eax, edx;
            if (Ring0.Rdmsr(MSR_PLATFORM_INFO, out eax, out edx)) {
              timeStampCounterMultiplier = (eax >> 8) & 0xff;
            }
          } break;
        default: {
            timeStampCounterMultiplier = 1;
            uint eax, edx;
            if (Ring0.Rdmsr(IA32_PERF_STATUS, out eax, out edx)) {
              timeStampCounterMultiplier =
                ((edx >> 8) & 0x1f) + 0.5 * ((edx >> 14) & 1);
            }
          } break;
      }

      // check if processor supports a digital thermal sensor at core level
      if (cpuid[0][0].Data.GetLength(0) > 6 &&
        (cpuid[0][0].Data[6, 0] & 1) != 0) {
        coreTemperatures = new Sensor[coreCount];
        for (int i = 0; i < coreTemperatures.Length; i++) {
          coreTemperatures[i] = new Sensor(CoreString(i), i,
            SensorType.Temperature, this, new[] { 
              new ParameterDescription(
                "TjMax [°C]", "TjMax temperature of the core sensor.\n" + 
                "Temperature = TjMax - TSlope * Value.", tjMax[i]), 
              new ParameterDescription("TSlope [°C]", 
                "Temperature slope of the digital thermal sensor.\n" + 
                "Temperature = TjMax - TSlope * Value.", 1)}, settings);
          ActivateSensor(coreTemperatures[i]);
        }
      } else {
        coreTemperatures = new Sensor[0];
      }

      // check if processor supports a digital thermal sensor at package level
      if (cpuid[0][0].Data.GetLength(0) > 6 &&
        (cpuid[0][0].Data[6, 0] & 0x40) != 0) {
        packageTemperature = new Sensor("CPU Package",
          coreTemperatures.Length, SensorType.Temperature, this, new[] { 
              new ParameterDescription(
                "TjMax [°C]", "TjMax temperature of the package sensor.\n" + 
                "Temperature = TjMax - TSlope * Value.", tjMax[0]), 
              new ParameterDescription("TSlope [°C]", 
                "Temperature slope of the digital thermal sensor.\n" + 
                "Temperature = TjMax - TSlope * Value.", 1)}, settings);
        ActivateSensor(packageTemperature);
      }

      busClock = new Sensor("Bus Speed", 0, SensorType.Clock, this, settings);
      coreClocks = new Sensor[coreCount];
      for (int i = 0; i < coreClocks.Length; i++) {
        coreClocks[i] =
          new Sensor(CoreString(i), i + 1, SensorType.Clock, this, settings);
        if (HasTimeStampCounter)
          ActivateSensor(coreClocks[i]);
      }

      if (microarchitecture == Microarchitecture.SandyBridge) {

        powerSensors = new Sensor[energyStatusMSRs.Length];
        lastEnergyTime = new DateTime[energyStatusMSRs.Length];
        lastEnergyConsumed = new uint[energyStatusMSRs.Length];

        uint eax, edx;
        if (Ring0.Rdmsr(MSR_RAPL_POWER_UNIT, out eax, out edx))
          energyUnitMultiplier = 1.0f / (1 << (int)((eax >> 8) & 0x1FF));

        if (energyUnitMultiplier != 0) {
          for (int i = 0; i < energyStatusMSRs.Length; i++) {
            if (!Ring0.Rdmsr(energyStatusMSRs[i], out eax, out edx))
              continue;

            lastEnergyTime[i] = DateTime.UtcNow;
            lastEnergyConsumed[i] = eax;
            powerSensors[i] = new Sensor(powerSensorLabels[i], i,
              SensorType.Power, this, settings);
            ActivateSensor(powerSensors[i]);
          }
        }
      }

      Update();
    }

    protected override uint[] GetMSRs() {
      return new[] {
        MSR_PLATFORM_INFO,
        IA32_PERF_STATUS ,
        IA32_THERM_STATUS_MSR,
        IA32_TEMPERATURE_TARGET,
        IA32_PACKAGE_THERM_STATUS,
        MSR_RAPL_POWER_UNIT,
        MSR_PKG_ENERY_STATUS,
        MSR_DRAM_ENERGY_STATUS,
        MSR_PP0_ENERY_STATUS,
        MSR_PP1_ENERY_STATUS
      };
    }

    public override string GetReport() {
      StringBuilder r = new StringBuilder();
      r.Append(base.GetReport());

      r.Append("Microarchitecture: ");
      r.AppendLine(microarchitecture.ToString());
      r.Append("Time Stamp Counter Multiplier: ");
      r.AppendLine(timeStampCounterMultiplier.ToString(
        CultureInfo.InvariantCulture));
      r.AppendLine();

      return r.ToString();
    }

    public override void Update() {
      base.Update();

      for (int i = 0; i < coreTemperatures.Length; i++) {
        uint eax, edx;
        if (Ring0.RdmsrTx(
          IA32_THERM_STATUS_MSR, out eax, out edx,
            1UL << cpuid[i][0].Thread)) {
          // if reading is valid
          if ((eax & 0x80000000) != 0) {
            // get the dist from tjMax from bits 22:16
            float deltaT = ((eax & 0x007F0000) >> 16);
            float tjMax = coreTemperatures[i].Parameters[0].Value;
            float tSlope = coreTemperatures[i].Parameters[1].Value;
            coreTemperatures[i].Value = tjMax - tSlope * deltaT;
          } else {
            coreTemperatures[i].Value = null;
          }
        }
      }

      if (packageTemperature != null) {
        uint eax, edx;
        if (Ring0.RdmsrTx(
          IA32_PACKAGE_THERM_STATUS, out eax, out edx,
            1UL << cpuid[0][0].Thread)) {
          // get the dist from tjMax from bits 22:16
          float deltaT = ((eax & 0x007F0000) >> 16);
          float tjMax = packageTemperature.Parameters[0].Value;
          float tSlope = packageTemperature.Parameters[1].Value;
          packageTemperature.Value = tjMax - tSlope * deltaT;
        } else {
          packageTemperature.Value = null;
        }
      }

      if (HasTimeStampCounter) {
        double newBusClock = 0;
        uint eax, edx;
        for (int i = 0; i < coreClocks.Length; i++) {
          System.Threading.Thread.Sleep(1);
          if (Ring0.RdmsrTx(IA32_PERF_STATUS, out eax, out edx,
            1UL << cpuid[i][0].Thread)) {
            newBusClock =
              TimeStampCounterFrequency / timeStampCounterMultiplier;
            switch (microarchitecture) {
              case Microarchitecture.Nehalem: {
                  uint multiplier = eax & 0xff;
                  coreClocks[i].Value = (float)(multiplier * newBusClock);
                } break;
              case Microarchitecture.SandyBridge: {
                  uint multiplier = (eax >> 8) & 0xff;
                  coreClocks[i].Value = (float)(multiplier * newBusClock);
                } break;
              default: {
                  double multiplier =
                    ((eax >> 8) & 0x1f) + 0.5 * ((eax >> 14) & 1);
                  coreClocks[i].Value = (float)(multiplier * newBusClock);
                } break;
            }
          } else {
            // if IA32_PERF_STATUS is not available, assume TSC frequency
            coreClocks[i].Value = (float)TimeStampCounterFrequency;
          }
        }
        if (newBusClock > 0) {
          this.busClock.Value = (float)newBusClock;
          ActivateSensor(this.busClock);
        }
      }

      if (powerSensors != null) {
        foreach (Sensor sensor in powerSensors) {
          if (sensor == null)
            continue;

          uint eax, edx;
          if (!Ring0.Rdmsr(energyStatusMSRs[sensor.Index], out eax, out edx))
            continue;

          DateTime time = DateTime.UtcNow;
          uint energyConsumed = eax;
          float deltaTime =
            (float)(time - lastEnergyTime[sensor.Index]).TotalSeconds;
          if (deltaTime < 0.01)
            continue;

          sensor.Value = energyUnitMultiplier * unchecked(
            energyConsumed - lastEnergyConsumed[sensor.Index]) / deltaTime;
          lastEnergyTime[sensor.Index] = time;
          lastEnergyConsumed[sensor.Index] = energyConsumed;
        }
      }
    }
  }
}
