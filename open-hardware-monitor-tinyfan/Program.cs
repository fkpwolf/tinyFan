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
  Portions created by the Initial Developer are Copyright (C) 2009-2010
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
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using OpenHardwareMonitor.GUI;
using Microsoft.Win32;

namespace OpenHardwareMonitor {
  public static class Program {

    [STAThread]
    public static void Main() {
      #if !DEBUG
        Application.ThreadException += 
          new ThreadExceptionEventHandler(Application_ThreadException);
        Application.SetUnhandledExceptionMode(
          UnhandledExceptionMode.CatchException);

        AppDomain.CurrentDomain.UnhandledException += 
          new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
      #endif

      if (!AllRequiredFilesAvailable())
        Environment.Exit(0);

      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
       form = new GUI.MainForm();
        form.FormClosed += delegate(Object sender, FormClosedEventArgs e) {
          Application.Exit();
        };        
        Application.Run();
      
    }

    static GUI.MainForm form;

    private static void SystemEvents_PowerModeChanged(object sender,
                       PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Console.WriteLine("-----------------------I weak from resume--program.css");
            form.reload();
        }
    }

    private static bool IsFileAvailable(string fileName) {
      string path = Path.GetDirectoryName(Application.ExecutablePath) +
        Path.DirectorySeparatorChar;

      if (!File.Exists(path + fileName)) {
        MessageBox.Show("The following file could not be found: " + fileName + 
          "\nPlease extract all files from the archive.", "Error",
           MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
      }
      return true;      
    }

    private static bool AllRequiredFilesAvailable() {
      if (!IsFileAvailable("Aga.Controls.dll"))
        return false;
      if (!IsFileAvailable("OpenHardwareMonitorLib.dll"))
        return false;

      return true;
    }

    private static void ReportException(Exception e) {
      CrashForm form = new CrashForm();
      form.Exception = e;
      form.ShowDialog();
    }

    public static void Application_ThreadException(object sender, 
      ThreadExceptionEventArgs e) 
    {
      try {
        ReportException(e.Exception);
      } catch {
      } finally {
        Application.Exit();
      }
    }

    public static void CurrentDomain_UnhandledException(object sender, 
      UnhandledExceptionEventArgs args) 
    {
      try {
        Exception e = args.ExceptionObject as Exception;
        if (e != null)
          ReportException(e);
      } catch {
      } finally {
        Environment.Exit(0);
      }
    }   
  }
}
