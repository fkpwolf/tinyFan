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
using OpenHardwareMonitor.Collections;

namespace OpenHardwareMonitor.Hardware {

  public enum SensorType {
    Voltage, // V
    Clock, // MHz
    Temperature, // °C
    Load, // %
    Fan, // RPM
    Flow, // L/h
    Control, // %
    Level, // %
    Factor, // 1
    Power, // W
    Data, // GB = 2^30 Bytes    
  }

  public struct SensorValue {
    private readonly float value;
    private readonly DateTime time;

    public SensorValue(float value, DateTime time) {
      this.value = value;
      this.time = time;
    }

    public float Value { get { return value; } }
    public DateTime Time { get { return time; } }
  }

  public interface ISensor : IElement {

    IHardware Hardware { get; }

    SensorType SensorType { get; }
    Identifier Identifier { get; }

    string Name { get; set; }
    int Index { get; }

    bool IsDefaultHidden { get; }

    IReadOnlyArray<IParameter> Parameters { get; }

    float? Value { get; }
    float? Min { get; }
    float? Max { get; }

    void ResetMin();
    void ResetMax();

    IEnumerable<SensorValue> Values { get; }

    IControl Control { get; }
  }

}
