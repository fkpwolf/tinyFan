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
  Michael M�ller <m.moeller@gmx.ch>.
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace OpenHardwareMonitor.Utilities {
  public class EmbeddedResources {

    public static Image GetImage(string name) {
      name = "OpenHardwareMonitor.Resources." + name;

      string[] names = 
        Assembly.GetExecutingAssembly().GetManifestResourceNames();
      for (int i = 0; i < names.Length; i++) {
        if (names[i].Replace('\\', '.') == name) {
          using (Stream stream = Assembly.GetExecutingAssembly().
            GetManifestResourceStream(names[i])) {

            // "You must keep the stream open for the lifetime of the Image."
            Image image = Image.FromStream(stream);

            // so we just create a copy of the image 
            Bitmap bitmap = new Bitmap(image);

            // and dispose it right here
            image.Dispose();

            return bitmap;
          }
        }
      } 

      return new Bitmap(1, 1);    
    }

    public static Icon GetIcon(string name) {
      name = "OpenHardwareMonitor.Resources." + name;

      string[] names =
        Assembly.GetExecutingAssembly().GetManifestResourceNames();
      for (int i = 0; i < names.Length; i++) {
        if (names[i].Replace('\\', '.') == name) {
          using (Stream stream = Assembly.GetExecutingAssembly().
            GetManifestResourceStream(names[i])) {
            return new Icon(stream);
          }
        }          
      } 

      return null;
    }

    public static System.Drawing.Bitmap Combine(Image[] files)
    {
        int space = 5;
        //read all images into memory
        List<System.Drawing.Bitmap> images = new List<System.Drawing.Bitmap>();
        System.Drawing.Bitmap finalImage = null;

        try
        {
            int width = 0;
            int height = 0;

            foreach (Image image in files)
            {
                //create a Bitmap from the file and add it to the list
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image);

                //update the size of the final bitmap
                width += bitmap.Width + space;
                height = bitmap.Height > height ? bitmap.Height : height;

                //bitmap.MakeTransparent();
                images.Add(bitmap);
            }

            //create a bitmap to hold the combined image
            finalImage = new System.Drawing.Bitmap(width, height);

            //get a graphics object from the image so we can draw on it
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalImage))
            {
                //set background color
                g.Clear(System.Drawing.Color.Transparent);

                //go through each image and draw it on the final image
                int offset = 0;
                foreach (System.Drawing.Bitmap image in images)
                {
                    g.DrawImage(image,
                      new System.Drawing.Rectangle(offset, 0, image.Width, image.Height));
                    offset += image.Width + space;
                }
            }
            //finalImage.MakeTransparent();

            return finalImage;
        }
        catch (Exception ex)
        {
            if (finalImage != null)
                finalImage.Dispose();

            throw ex;
        }
        finally
        {
            //clean up memory
            foreach (System.Drawing.Bitmap image in images)
            {
                image.Dispose();
            }
        }
    }

         
  }
}
