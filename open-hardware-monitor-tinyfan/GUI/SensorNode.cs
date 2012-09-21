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
using OpenHardwareMonitor.Hardware;
using OpenHardwareMonitor.Utilities;

namespace OpenHardwareMonitor.GUI {
  public class SensorNode : Node {
    
    private ISensor sensor;
    private PersistentSettings settings;
    private UnitManager unitManager;
    private string format;
    private bool plot = false;       

    public string ValueToString(float? value) {
      if (value.HasValue) {
        if (sensor.SensorType == SensorType.Temperature && 
          unitManager.TemperatureUnit == TemperatureUnit.Fahrenheit) {
          return string.Format("{0:F1} °F", value * 1.8 + 32);
        } else {
          return string.Format(format, value);
        }                
      } else
        return "-";
    }

    public SensorNode(ISensor sensor, PersistentSettings settings, 
      UnitManager unitManager) : base() {  
      this.sensor = sensor;
      this.settings = settings;
      this.unitManager = unitManager;
      switch (sensor.SensorType) {
        case SensorType.Voltage: format = "{0:F3} V"; break;
        case SensorType.Clock: format = "{0:F0} MHz"; break;
        case SensorType.Load: format = "{0:F1} %"; break;
        case SensorType.Temperature: format = "{0:F1} °C"; break;
        case SensorType.Fan: format = "{0:F0} RPM"; break;
        case SensorType.Flow: format = "{0:F0} L/h"; break;
        case SensorType.Control: format = "{0:F1} %"; break;
        case SensorType.TinyFanControl: format = "{0:F1} %"; break;
        case SensorType.Level: format = "{0:F1} %"; break;
        case SensorType.Power: format = "{0:F1} W"; break;
        case SensorType.Data: format = "{0:F1} GB"; break;
        case SensorType.Factor: format = "{0:F3}"; break;
      }

      bool hidden = settings.GetValue(new Identifier(sensor.Identifier, 
        "hidden").ToString(), sensor.IsDefaultHidden);
      base.IsVisible = !hidden;

      this.Plot = settings.GetValue(new Identifier(sensor.Identifier, 
        "plot").ToString(), false);
    }

    public override string Text {
      get { return sensor.Name; }
      set { sensor.Name = value; }
    }

    public override bool IsVisible {
      get { return base.IsVisible; }
      set { 
        base.IsVisible = value;
        settings.SetValue(new Identifier(sensor.Identifier,
          "hidden").ToString(), !value);
      }
    }

    public bool Plot {
      get { return plot; }
      set { 
        plot = value;
        settings.SetValue(new Identifier(sensor.Identifier, "plot").ToString(), 
          value);
        if (PlotSelectionChanged != null)
          PlotSelectionChanged(this, null);
      }
    }

    public event EventHandler PlotSelectionChanged;
    private System.Drawing.Image image;

    public ISensor Sensor {
      get { return sensor; }
    }

    public string Value {
      get {
          //Console.WriteLine("---------------sensorNode.getValue----");//it will continue update
          return ValueToString(sensor.Value); }
    }


    public new System.Drawing.Image Image{
        get{
            //fan
            if (sensor.SensorType.Equals(SensorType.TinyFanControl)){
               if(sensor.Control.FanMode.Equals(FanMode.Pin3))
                    this.image = Utilities.EmbeddedResources.GetImage("fan3pin.png");
                else
                    this.image = Utilities.EmbeddedResources.GetImage("fan4pin.png");
               //merge
               /*IHardware gpu = Singleton.Instance().GPU;
               System.Drawing.Image followGPUImage = null;
               if (gpu.HardwareType.Equals(HardwareType.GpuAti))
                    followGPUImage = Utilities.EmbeddedResources.GetImage("ati.png");//we can reuse this kind of image
               else if(gpu.HardwareType.Equals(HardwareType.GpuNvidia))
                   followGPUImage = Utilities.EmbeddedResources.GetImage("nvidia.png");
               else
                   followGPUImage = Utilities.EmbeddedResources.GetImage("chip.png");

               System.Drawing.Image followCPUImage = Utilities.EmbeddedResources.GetImage("cpu.png");
               List<System.Drawing.Image> images = new List<System.Drawing.Image>();
               images.Add(this.image);
               if(sensor.Control.FanFollow.Equals(FanFollow.CPU))
                   images.Add(followCPUImage);
               else if(sensor.Control.FanFollow.Equals(FanFollow.GPU))
                   images.Add(followGPUImage);
               System.Drawing.Bitmap result = Utilities.EmbeddedResources.Combine(images.ToArray());
               this.image = result;*/
            }
            //fan
            return this.image;
        }
    }

    /*
     * property for tinyfan fan control "follow mode". Configure it in MainForm.cs Design treeView's Properties Tab 
     */
    public new System.Drawing.Image LastImage{
        get{
            if (sensor.SensorType.Equals(SensorType.TinyFanControl)){
                if (sensor.Control.FanFollow.Equals(FanFollow.CPU))
                    return Utilities.EmbeddedResources.GetImage("cpu.png");
                else if (sensor.Control.FanFollow.Equals(FanFollow.GPU)){
                    IHardware gpu = Singleton.Instance().GPU;
                    System.Drawing.Image followGPUImage = null;
                    if (gpu.HardwareType.Equals(HardwareType.GpuAti))
                        followGPUImage = Utilities.EmbeddedResources.GetImage("ati.png");//we can reuse this kind of image
                    else if (gpu.HardwareType.Equals(HardwareType.GpuNvidia))
                        followGPUImage = Utilities.EmbeddedResources.GetImage("nvidia.png");
                    else
                        followGPUImage = Utilities.EmbeddedResources.GetImage("chip.png");
                    return followGPUImage;
                }
            }
            return null;
        }
    }

    public string Min {
      get { return ValueToString(sensor.Min); }
    }

    public string Max {
      get { return ValueToString(sensor.Max); }
    }

    public override bool Equals(System.Object obj) {
      if (obj == null) 
        return false;

      SensorNode s = obj as SensorNode;
      if (s == null) 
        return false;

      return (sensor == s.sensor);
    }

    public override int GetHashCode() {
      return sensor.GetHashCode();
    }
  }
}
