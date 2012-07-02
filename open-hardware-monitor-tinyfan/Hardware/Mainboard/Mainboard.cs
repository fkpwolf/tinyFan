/*
  
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
using System.Text;
using OpenHardwareMonitor.Hardware.LPC;

namespace OpenHardwareMonitor.Hardware.Mainboard {
  internal class Mainboard : IHardware {
    private readonly SMBIOS smbios;
    private readonly string name;
    private string customName;
    private readonly ISettings settings;
    private readonly LPCIO lpcio;
    private readonly LMSensors lmSensors;
    private readonly Hardware[] superIOHardware;

    public Mainboard(ISettings settings) {
      this.settings = settings;
      this.smbios = new SMBIOS();
     
      if (smbios.Board != null) {
        if (!string.IsNullOrEmpty(smbios.Board.ProductName)) {
          if (smbios.Board.Manufacturer == Manufacturer.Unknown)
            this.name = smbios.Board.ProductName;
          else
            this.name = smbios.Board.Manufacturer + " " +
              smbios.Board.ProductName;
        } else {
          this.name = smbios.Board.Manufacturer.ToString();
        }
      } else {
        this.name = Manufacturer.Unknown.ToString();
      }

      this.customName = settings.GetValue(
        new Identifier(Identifier, "name").ToString(), name);

      ISuperIO[] superIO;
      int p = (int)Environment.OSVersion.Platform;

      if ((p == 4) || (p == 128)) {
        this.lmSensors = new LMSensors();
        superIO = lmSensors.SuperIO;
      } else {
        this.lpcio = new LPCIO();       
        superIO = lpcio.SuperIO;
      }
      
      superIOHardware = new Hardware[superIO.Length];
      for (int i = 0; i < superIO.Length; i++)
        superIOHardware[i] = new SuperIOHardware(this, superIO[i], 
          smbios.Board != null ? smbios.Board.Manufacturer : 
          Manufacturer.Unknown, smbios.Board != null ? smbios.Board.Model :
          Model.Unknown, settings);
    }

    public string Name {
      get {
        return customName;
      }
      set {
        if (!string.IsNullOrEmpty(value))
          customName = value;
        else
          customName = name;
        settings.SetValue(new Identifier(Identifier, "name").ToString(),
          customName);
      }
    }

    public Identifier Identifier {
      get { return new Identifier("mainboard"); }
    }

    public HardwareType HardwareType {
      get { return HardwareType.Mainboard; }
    }

    public virtual IHardware Parent {
      get { return null; }
    }

    public string GetReport() {
      StringBuilder r = new StringBuilder(); 

      r.AppendLine("Mainboard");
      r.AppendLine();           
      r.Append(smbios.GetReport());

      if (lpcio != null)
        r.Append(lpcio.GetReport());

      byte[] table = 
        FirmwareTable.GetTable(FirmwareTable.Provider.ACPI, "TAMG");
      if (table != null) {
        GigabyteTAMG tamg = new GigabyteTAMG(table);
        r.Append(tamg.GetReport());
      }

      return r.ToString();
    }

    public void Update() { }

    public void Close() {
      if (lmSensors != null)
        lmSensors.Close();
      foreach (Hardware hardware in superIOHardware)
        hardware.Close();
    }

    public IHardware[] SubHardware {
      get { return superIOHardware; }
    }

    public ISensor[] Sensors {
      get { return new ISensor[0]; }
    }

    #pragma warning disable 67
    public event SensorEventHandler SensorAdded;
    public event SensorEventHandler SensorRemoved;
    #pragma warning restore 67

    public void Accept(IVisitor visitor) {
      if (visitor == null)
        throw new ArgumentNullException("visitor");
      visitor.VisitHardware(this);
    }

    public void Traverse(IVisitor visitor) {
      foreach (IHardware hardware in superIOHardware)
        hardware.Accept(visitor);     
    }
  }
}
