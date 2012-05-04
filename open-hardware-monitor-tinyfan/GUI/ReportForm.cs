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
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace OpenHardwareMonitor.GUI {
  public partial class ReportForm : Form {

    private string report;

    public ReportForm() {
      InitializeComponent();
      try {
        titleLabel.Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);      
        reportTextBox.Font = new Font(FontFamily.GenericMonospace,
          SystemFonts.DefaultFont.Size);
      } catch { }
    }

    public string Report {
      get { return report; }
      set { 
        report = value;
        reportTextBox.Text = report;
      }      
    }

    private void sendButton_Click(object sender, EventArgs e) {
      Version version = typeof(CrashForm).Assembly.GetName().Version;
      WebRequest request = WebRequest.Create(
        "http://openhardwaremonitor.org/report.php");
      request.Method = "POST";
      request.Timeout = 5000;
      request.ContentType = "application/x-www-form-urlencoded";

      string report =
        "type=hardware&" +
        "version=" + Uri.EscapeDataString(version.ToString()) + "&" +
        "report=" + Uri.EscapeDataString(reportTextBox.Text) + "&" +
        "comment=" + Uri.EscapeDataString(commentTextBox.Text) + "&" +
        "email=" + Uri.EscapeDataString(emailTextBox.Text);
      byte[] byteArray = Encoding.UTF8.GetBytes(report);
      request.ContentLength = byteArray.Length;

      try {
        Stream dataStream = request.GetRequestStream();
        dataStream.Write(byteArray, 0, byteArray.Length);
        dataStream.Close();

        WebResponse response = request.GetResponse();
        dataStream = response.GetResponseStream();
        StreamReader reader = new StreamReader(dataStream);
        string responseFromServer = reader.ReadToEnd();
        reader.Close();
        dataStream.Close();
        response.Close();

        Close();
      } catch (WebException) {
        MessageBox.Show("Sending the hardware report failed.", "Error",
          MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }  
}
