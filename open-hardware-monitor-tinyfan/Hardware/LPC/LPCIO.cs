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
  Portions created by the Initial Developer are Copyright (C) 2009-2012
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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace OpenHardwareMonitor.Hardware.LPC {
  internal class LPCIO {

    private readonly List<ISuperIO> superIOs = new List<ISuperIO>();
    private readonly StringBuilder report = new StringBuilder();

    // I/O Ports
    private readonly ushort[] REGISTER_PORTS = new ushort[] { 0x2E, 0x4E };
    private readonly ushort[] VALUE_PORTS = new ushort[] { 0x2F, 0x4F };

    private ushort registerPort;
    private ushort valuePort;

    // Registers
    private const byte CONFIGURATION_CONTROL_REGISTER = 0x02;
    private const byte DEVCIE_SELECT_REGISTER = 0x07;
    private const byte CHIP_ID_REGISTER = 0x20;
    private const byte CHIP_REVISION_REGISTER = 0x21;
    private const byte BASE_ADDRESS_REGISTER = 0x60;

    private byte ReadByte(byte register) {
      Ring0.WriteIoPort(registerPort, register);
      return Ring0.ReadIoPort(valuePort);
    }

    private ushort ReadWord(byte register) {
      return (ushort)((ReadByte(register) << 8) |
        ReadByte((byte)(register + 1)));
    }

    private void Select(byte logicalDeviceNumber) {
      Ring0.WriteIoPort(registerPort, DEVCIE_SELECT_REGISTER);
      Ring0.WriteIoPort(valuePort, logicalDeviceNumber);
    }

    private void ReportUnknownChip(string type, int chip) {
      report.Append("Chip ID: Unknown ");
      report.Append(type);
      report.Append(" with ID 0x");
      report.Append(chip.ToString("X", CultureInfo.InvariantCulture));
      report.Append(" at 0x");
      report.Append(registerPort.ToString("X", CultureInfo.InvariantCulture));
      report.Append("/0x");
      report.AppendLine(valuePort.ToString("X", CultureInfo.InvariantCulture));
      report.AppendLine();
    }

    #region Winbond, Nuvoton, Fintek

    private const byte FINTEK_VENDOR_ID_REGISTER = 0x23;
    private const ushort FINTEK_VENDOR_ID = 0x1934;

    private const byte WINBOND_NUVOTON_HARDWARE_MONITOR_LDN = 0x0B;

    private const byte F71858_HARDWARE_MONITOR_LDN = 0x02;
    private const byte FINTEK_HARDWARE_MONITOR_LDN = 0x04;

    private void WinbondNuvotonFintekEnter() {
      Ring0.WriteIoPort(registerPort, 0x87);
      Ring0.WriteIoPort(registerPort, 0x87);
    }

    private void WinbondNuvotonFintekExit() {
      Ring0.WriteIoPort(registerPort, 0xAA);
    }

    private bool DetectWinbondFintek() {
      WinbondNuvotonFintekEnter();

      byte logicalDeviceNumber = 0;
      byte id = ReadByte(CHIP_ID_REGISTER);
      byte revision = ReadByte(CHIP_REVISION_REGISTER);
      Chip chip = Chip.Unknown;
      switch (id) {
        case 0x05:
          switch (revision) {
            case 0x07:
              chip = Chip.F71858;
              logicalDeviceNumber = F71858_HARDWARE_MONITOR_LDN;
              break;
            case 0x41:
              chip = Chip.F71882;
              logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x06:
          switch (revision) {
            case 0x01:
              chip = Chip.F71862;
              logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x07:
          switch (revision) {
            case 0x23:
              chip = Chip.F71889F;
              logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x08:
          switch (revision) {
            case 0x14:
              chip = Chip.F71869;
              logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x09:
          switch (revision) {
            case 0x09:
              chip = Chip.F71889ED;
              logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x10:
          switch (revision) {
            case 0x05:
              chip = Chip.F71889AD;
              logicalDeviceNumber = FINTEK_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x52:
          switch (revision) {
            case 0x17:
            case 0x3A:
            case 0x41:
              chip = Chip.W83627HF;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x82:
          switch (revision & 0xF0) {
            case 0x80:
              chip = Chip.W83627THF;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x85:
          switch (revision) {
            case 0x41:
              chip = Chip.W83687THF;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0x88:
          switch (revision & 0xF0) {
            case 0x50:
            case 0x60:
              chip = Chip.W83627EHF;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0xA0:
          switch (revision & 0xF0) {
            case 0x20:
              chip = Chip.W83627DHG;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0xA5:
          switch (revision & 0xF0) {
            case 0x10:
              chip = Chip.W83667HG;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0xB0:
          switch (revision & 0xF0) {
            case 0x70:
              chip = Chip.W83627DHGP;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0xB3:
          switch (revision & 0xF0) {
            case 0x50:
              chip = Chip.W83667HGB;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0xB4:
          switch (revision & 0xF0) {
            case 0x70:
              chip = Chip.NCT6771F;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
        case 0xC3:
          switch (revision & 0xF0) {
            case 0x30:
              chip = Chip.NCT6776F;
              logicalDeviceNumber = WINBOND_NUVOTON_HARDWARE_MONITOR_LDN;
              break;
          } break;
      }
      if (chip == Chip.Unknown) {
        if (id != 0 && id != 0xff) {
          WinbondNuvotonFintekExit();

          ReportUnknownChip("Winbond / Nuvoton / Fintek", 
            ((id << 8) | revision));
        }
      } else {

        Select(logicalDeviceNumber);
        ushort address = ReadWord(BASE_ADDRESS_REGISTER);
        Thread.Sleep(1);
        ushort verify = ReadWord(BASE_ADDRESS_REGISTER);

        ushort vendorID = ReadWord(FINTEK_VENDOR_ID_REGISTER);

        WinbondNuvotonFintekExit();

        if (address != verify) {
          report.Append("Chip ID: 0x");
          report.AppendLine(chip.ToString("X"));
          report.Append("Chip revision: 0x");
          report.AppendLine(revision.ToString("X",
            CultureInfo.InvariantCulture));
          report.AppendLine("Error: Address verification failed");
          report.AppendLine();
          return false;
        }

        // some Fintek chips have address register offset 0x05 added already
        if ((address & 0x07) == 0x05)
          address &= 0xFFF8;

        if (address < 0x100 || (address & 0xF007) != 0) {
          report.Append("Chip ID: 0x");
          report.AppendLine(chip.ToString("X"));
          report.Append("Chip revision: 0x");
          report.AppendLine(revision.ToString("X",
            CultureInfo.InvariantCulture));
          report.Append("Error: Invalid address 0x");
          report.AppendLine(address.ToString("X",
            CultureInfo.InvariantCulture));
          report.AppendLine();
          return false;
        }
        Console.WriteLine("the LPC chip is:" + chip);
        switch (chip) {
          case Chip.W83627DHG:
          case Chip.W83627DHGP:
          case Chip.W83627EHF:
          case Chip.W83627HF:
          case Chip.W83627THF:
          case Chip.W83667HG:
          case Chip.W83667HGB:
          case Chip.W83687THF:
            superIOs.Add(new W836XX(chip, revision, address));
            break;
          case Chip.NCT6771F:
          case Chip.NCT6776F:
            superIOs.Add(new NCT677X(chip, revision, address));
            break;
          case Chip.F71858:
          case Chip.F71862:
          case Chip.F71869:
          case Chip.F71882:
          case Chip.F71889AD:
          case Chip.F71889ED:
          case Chip.F71889F:
            if (vendorID != FINTEK_VENDOR_ID) {
              report.Append("Chip ID: 0x");
              report.AppendLine(chip.ToString("X"));
              report.Append("Chip revision: 0x");
              report.AppendLine(revision.ToString("X",
                CultureInfo.InvariantCulture));
              report.Append("Error: Invalid vendor ID 0x");
              report.AppendLine(vendorID.ToString("X",
                CultureInfo.InvariantCulture));
              report.AppendLine();
              return false;
            }
            superIOs.Add(new F718XX(chip, address));
            break;
          default: break;
        }

        return true;
      }

      return false;
    }

    #endregion

    #region ITE

    private const byte IT87_ENVIRONMENT_CONTROLLER_LDN = 0x04;
    private const byte IT87_GPIO_LDN = 0x07;
    private const byte IT87_CHIP_VERSION_REGISTER = 0x22;

    private void IT87Enter() {
      Ring0.WriteIoPort(registerPort, 0x87);
      Ring0.WriteIoPort(registerPort, 0x01);
      Ring0.WriteIoPort(registerPort, 0x55);
      Ring0.WriteIoPort(registerPort, 0x55);
    }

    private void IT87Exit() {
      Ring0.WriteIoPort(registerPort, CONFIGURATION_CONTROL_REGISTER);
      Ring0.WriteIoPort(valuePort, 0x02);
    }

    private bool DetectIT87() {

      // IT87XX can enter only on port 0x2E
      if (registerPort != 0x2E)
        return false;

      IT87Enter();

      ushort chipID = ReadWord(CHIP_ID_REGISTER);
      Chip chip;
      switch (chipID) {
        case 0x8712: chip = Chip.IT8712F; break;
        case 0x8716: chip = Chip.IT8716F; break;
        case 0x8718: chip = Chip.IT8718F; break;
        case 0x8720: chip = Chip.IT8720F; break;
        case 0x8721: chip = Chip.IT8721F; break;
        case 0x8726: chip = Chip.IT8726F; break;
        case 0x8728: chip = Chip.IT8728F; break;
        case 0x8771: chip = Chip.IT8771E; break;
        case 0x8772: chip = Chip.IT8772E; break;
        default: chip = Chip.Unknown; break;
      }
      if (chip == Chip.Unknown) {
        if (chipID != 0 && chipID != 0xffff) {
          IT87Exit();

          ReportUnknownChip("ITE", chipID);
        }
      } else {
        Select(IT87_ENVIRONMENT_CONTROLLER_LDN);
        ushort address = ReadWord(BASE_ADDRESS_REGISTER);
        Thread.Sleep(1);
        ushort verify = ReadWord(BASE_ADDRESS_REGISTER);

        byte version = (byte)(ReadByte(IT87_CHIP_VERSION_REGISTER) & 0x0F);

        Select(IT87_GPIO_LDN);
        ushort gpioAddress = ReadWord(BASE_ADDRESS_REGISTER + 2);
        Thread.Sleep(1);
        ushort gpioVerify = ReadWord(BASE_ADDRESS_REGISTER + 2);

        IT87Exit();

        if (address != verify || address < 0x100 || (address & 0xF007) != 0) {
          report.Append("Chip ID: 0x");
          report.AppendLine(chip.ToString("X"));
          report.Append("Error: Invalid address 0x");
          report.AppendLine(address.ToString("X",
            CultureInfo.InvariantCulture));
          report.AppendLine();
          return false;
        }

        if (gpioAddress != gpioVerify || gpioAddress < 0x100 ||
          (gpioAddress & 0xF007) != 0) {
          report.Append("Chip ID: 0x");
          report.AppendLine(chip.ToString("X"));
          report.Append("Error: Invalid GPIO address 0x");
          report.AppendLine(gpioAddress.ToString("X",
            CultureInfo.InvariantCulture));
          report.AppendLine();
          return false;
        }

        superIOs.Add(new IT87XX(chip, address, gpioAddress, version));
        return true;
      }

      return false;
    }

    #endregion

    #region SMSC

    private void SMSCEnter() {
      Ring0.WriteIoPort(registerPort, 0x55);
    }

    private void SMSCExit() {
      Ring0.WriteIoPort(registerPort, 0xAA);
    }

    private bool DetectSMSC() {
      SMSCEnter();

      ushort chipID = ReadWord(CHIP_ID_REGISTER);
      Chip chip;
      switch (chipID) {
        default: chip = Chip.Unknown; break;
      }
      if (chip == Chip.Unknown) {
        if (chipID != 0 && chipID != 0xffff) {
          SMSCExit();

          ReportUnknownChip("SMSC", chipID);
        }
      } else {
        SMSCExit();
        return true;
      }

      return false;
    }

    #endregion

    private void Detect() {
        Console.WriteLine("-----detect---");

      for (int i = 0; i < REGISTER_PORTS.Length; i++) {
        registerPort = REGISTER_PORTS[i];
        valuePort = VALUE_PORTS[i];

        if (DetectWinbondFintek()) continue;

        if (DetectIT87()) continue;

        if (DetectSMSC()) continue;
      }
    }

    public LPCIO() {
      if (!Ring0.IsOpen)
        return;

      if (!Ring0.WaitIsaBusMutex(100))
        return;

      Detect();

      Ring0.ReleaseIsaBusMutex();
    }

    public ISuperIO[] SuperIO {
      get {
        return superIOs.ToArray();
      }
    }

    public string GetReport() {
      if (report.Length > 0) {
        return "LPCIO" + Environment.NewLine + Environment.NewLine + report;
      } else
        return null;
    }
  }
}
