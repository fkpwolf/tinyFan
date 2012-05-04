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

namespace OpenHardwareMonitor.Hardware.Mainboard {

  internal enum Model {
    // ASRock
    _880GMH_USB3,
    AOD790GX_128M,
    P55_Deluxe,

    // ASUS
    Crosshair_III_Formula,
    M2N_SLI_DELUXE,
    M4A79XTD_EVO,
    P5W_DH_Deluxe,    
    P6T,
    P6X58D_E,
    P8P67,
    P8P67_EVO,
    P8P67_PRO,
    P8P67_M_PRO,
    P9X79,
    Rampage_Extreme,
    Rampage_II_GENE,

    // DFI
    LP_BI_P45_T2RS_Elite,
    LP_DK_P55_T3eH9,

    // ECS
    A890GXM_A,

    // EVGA
    X58_SLI_Classified,

    // Gigabyte
    _965P_S3,
    EP45_DS3R,
    EP45_UD3R,
    EX58_EXTREME,
    GA_MA770T_UD3,
    GA_MA785GMT_UD2H,
    H67A_UD3H_B3,
    P35_DS3,
    P35_DS3L,
    P55_UD4,
    P55M_UD4,
    P67A_UD4_B3,
    P8Z68_V_PRO,
    X38_DS5,
    X58A_UD3R,
    Z68X_UD7_B3,

    // Shuttle
    FH67,

    // Unknown
    Unknown    
  }
}
