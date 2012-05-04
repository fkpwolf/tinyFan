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
    Paul Werelds
 
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

namespace OpenHardwareMonitor.Hardware.HDD {
  using System.Collections.Generic;

  [NamePrefix(""), RequireSmart(0xAB), RequireSmart(0xB1)]
  internal class SSDSandforce : AbstractHarddrive {

    private static readonly IEnumerable<SmartAttribute> smartAttributes =
      new List<SmartAttribute> {
      new SmartAttribute(0x01, SmartNames.RawReadErrorRate),
      new SmartAttribute(0x05, SmartNames.RetiredBlockCount, RawToInt),
      new SmartAttribute(0x09, SmartNames.PowerOnHours, RawToInt),
      new SmartAttribute(0x0C, SmartNames.PowerCycleCount, RawToInt),
      new SmartAttribute(0xAB, SmartNames.ProgramFailCount, RawToInt),
      new SmartAttribute(0xAC, SmartNames.EraseFailCount, RawToInt),
      new SmartAttribute(0xAE, SmartNames.UnexpectedPowerLossCount, RawToInt),
      new SmartAttribute(0xB1, SmartNames.WearRangeDelta, RawToInt),
      new SmartAttribute(0xB5, SmartNames.AlternativeProgramFailCount, RawToInt),
      new SmartAttribute(0xB6, SmartNames.AlternativeEraseFailCount, RawToInt),
      new SmartAttribute(0xBB, SmartNames.UncorrectableErrorCount, RawToInt),
      new SmartAttribute(0xC2, SmartNames.Temperature, (byte[] raw, byte value) 
        => { return value; }, SensorType.Temperature, 0, true), 
      new SmartAttribute(0xC3, SmartNames.UnrecoverableEcc), 
      new SmartAttribute(0xC4, SmartNames.ReallocationEventCount, RawToInt),
      new SmartAttribute(0xE7, SmartNames.RemainingLife, null, 
        SensorType.Level, 0),
      new SmartAttribute(0xE9, SmartNames.ControllerWritesToNAND, RawToInt,
        SensorType.Data, 0),
      new SmartAttribute(0xEA, SmartNames.HostWritesToController, RawToInt, 
        SensorType.Data, 1),
      new SmartAttribute(0xF1, SmartNames.HostWrites, RawToInt, 
        SensorType.Data, 1),
      new SmartAttribute(0xF2, SmartNames.HostReads, RawToInt, 
        SensorType.Data, 2)
    };

    private Sensor writeAmplification;

    public SSDSandforce(ISmart smart, string name, string firmwareRevision, 
      int index, ISettings settings) 
      : base(smart, name, firmwareRevision,  index, smartAttributes, settings) 
    {
      this.writeAmplification = new Sensor("Write Amplification", 1, 
        SensorType.Factor, this, settings);    
    }

    public override void UpdateAdditionalSensors(DriveAttributeValue[] values) {
      float? controllerWritesToNAND = null;
      float? hostWritesToController = null;
      foreach (DriveAttributeValue value in values) {
        if (value.Identifier == 0xE9)
          controllerWritesToNAND = RawToInt(value.RawValue, value.AttrValue);

        if (value.Identifier == 0xEA)
          hostWritesToController = RawToInt(value.RawValue, value.AttrValue);
      }
      if (controllerWritesToNAND.HasValue && hostWritesToController.HasValue) {
        if (hostWritesToController.Value > 0)
          writeAmplification.Value =
            controllerWritesToNAND.Value / hostWritesToController.Value;
        else
          writeAmplification.Value = 0;
        ActivateSensor(writeAmplification);
      }
    }
  }
}
