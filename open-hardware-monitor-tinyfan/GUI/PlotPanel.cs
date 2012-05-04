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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;

namespace OpenHardwareMonitor.GUI {
  public class PlotPanel : UserControl {

    private PersistentSettings settings;

    private DateTime now;
    private List<ISensor> clocks = new List<ISensor>();
    private List<ISensor> temperatures = new List<ISensor>();
    private List<ISensor> fans = new List<ISensor>();
    private IDictionary<ISensor, Color> colors;

    private StringFormat centerlower;
    private StringFormat centerleft;
    private StringFormat lowerleft;
    private Brush lightBrush;
    private Pen lightPen;

    private UserRadioGroup timeWindowRadioGroup;

    public PlotPanel(PersistentSettings settings) {
      this.settings = settings;

      this.SetStyle(ControlStyles.DoubleBuffer |
        ControlStyles.UserPaint |
        ControlStyles.AllPaintingInWmPaint |
        ControlStyles.ResizeRedraw, true);
      this.UpdateStyles();

      CreateContextMenu();

      centerlower = new StringFormat();
      centerlower.Alignment = StringAlignment.Center;
      centerlower.LineAlignment = StringAlignment.Near;

      centerleft = new StringFormat();
      centerleft.Alignment = StringAlignment.Far;
      centerleft.LineAlignment = StringAlignment.Center;

      lowerleft = new StringFormat();
      lowerleft.Alignment = StringAlignment.Far;
      lowerleft.LineAlignment = StringAlignment.Near;

      lightBrush = new SolidBrush(Color.FromArgb(245, 245, 245));
      lightPen = new Pen(Color.FromArgb(200, 200, 200));
    }

    private void CreateContextMenu() {
      MenuItem timeWindow = new MenuItem("Time Scale");
      
      MenuItem[] timeWindowMenuItems = 
        { new MenuItem("Auto"), 
          new MenuItem("5 min"),
          new MenuItem("10 min"),
          new MenuItem("20 min"),
          new MenuItem("30 min"),
          new MenuItem("45 min"),
          new MenuItem("1 h"),
          new MenuItem("1.5 h"),
          new MenuItem("2 h"),
          new MenuItem("3 h"),
          new MenuItem("6 h"),
          new MenuItem("12 h"),
          new MenuItem("24 h") };

      foreach (MenuItem mi in timeWindowMenuItems)
        timeWindow.MenuItems.Add(mi);

      timeWindowRadioGroup = new UserRadioGroup("timeWindow", 0, 
        timeWindowMenuItems, settings);

      this.ContextMenu = new ContextMenu();
      this.ContextMenu.MenuItems.Add(timeWindow);
    }

    private List<float> GetTemperatureGrid() {

      float? minTempNullable = null;
      float? maxTempNullable = null;
      foreach (ISensor sensor in temperatures) {
        IEnumerable<SensorValue> values = sensor.Values;
        foreach (SensorValue value in values) {
          if (!float.IsNaN(value.Value)) {
            if (!minTempNullable.HasValue || minTempNullable > value.Value)
              minTempNullable = value.Value;
            if (!maxTempNullable.HasValue || maxTempNullable < value.Value)
              maxTempNullable = value.Value;
          }
        }
      }
      if (!minTempNullable.HasValue) {
        minTempNullable = 20;
        maxTempNullable = 30;
      }

      float maxTemp = (float)Math.Ceiling(maxTempNullable.Value / 10) * 10;
      float minTemp = (float)Math.Floor(minTempNullable.Value / 10) * 10;
      if (maxTemp == minTemp)
        maxTemp += 10;

      int countTempMax = 4;
      float deltaTemp = maxTemp - minTemp;
      int countTemp = (int)Math.Round(deltaTemp / 2);
      if (countTemp > countTempMax)
        countTemp = (int)Math.Round(deltaTemp / 5);
      if (countTemp > countTempMax)
        countTemp = (int)Math.Round(deltaTemp / 10);
      if (countTemp > countTempMax)
        countTemp = (int)Math.Round(deltaTemp / 20);

      List<float> grid = new List<float>(countTemp + 1);
      for (int i = 0; i <= countTemp; i++) {
        grid.Add(minTemp + i * deltaTemp / countTemp);
      }
      return grid;
    }

    private List<float> GetTimeGrid() {

      float maxTime;
      if (timeWindowRadioGroup.Value == 0) { // Auto
        maxTime = 5;
        if (temperatures.Count > 0) {
          IEnumerator<SensorValue> enumerator =
            temperatures[0].Values.GetEnumerator();
          if (enumerator.MoveNext()) {
            maxTime = (float)(now - enumerator.Current.Time).TotalMinutes;
          }
        }
      } else {
        float[] maxTimes = 
          { 5, 10, 20, 30, 45, 60, 90, 120, 180, 360, 720, 1440 };

        maxTime = maxTimes[timeWindowRadioGroup.Value - 1];
      }

      int countTime = 10;
      float deltaTime = 5;
      while (deltaTime + 1 <= maxTime && deltaTime < 10)
        deltaTime += 1;
      while (deltaTime + 2 <= maxTime && deltaTime < 30)
        deltaTime += 2;
      while (deltaTime + 5 <= maxTime && deltaTime < 100)
        deltaTime += 5;
      while (deltaTime + 50 <= maxTime && deltaTime < 1000)
        deltaTime += 50;
      while (deltaTime + 100 <= maxTime && deltaTime < 10000)
        deltaTime += 100;

      List<float> grid = new List<float>(countTime + 1);
      for (int i = 0; i <= countTime; i++) {
        grid.Add(i * deltaTime / countTime);
      }
      return grid;
    }

    protected override void OnPaint(PaintEventArgs e) {
      now = DateTime.UtcNow - new TimeSpan(0, 0, 4);

      List<float> timeGrid = GetTimeGrid();
      List<float> tempGrid = GetTemperatureGrid();

      Graphics g = e.Graphics;

      RectangleF r =
        new RectangleF(0, 0, Bounds.Width, Bounds.Height);

      float ml = 40;
      float mr = 15;
      float x0 = r.X + ml;
      float w = r.Width - ml - mr;

      float mt = 15;
      float mb = 28;
      float y0 = r.Y + mt;
      float h = r.Height - mt - mb;

      float leftScaleSpace = 5;
      float bottomScaleSpace = 5;

      g.Clear(Color.White);

      if (w > 0 && h > 0) {
        g.FillRectangle(lightBrush, x0, y0, w, h);

        g.SmoothingMode = SmoothingMode.HighQuality;
        for (int i = 0; i < timeGrid.Count; i++) {
          float x = x0 + i * w / (timeGrid.Count - 1);
          g.DrawLine(lightPen, x, y0, x, y0 + h);
        }

        for (int i = 0; i < tempGrid.Count; i++) {
          float y = y0 + i * h / (tempGrid.Count - 1);
          g.DrawLine(lightPen, x0, y, x0 + w, y);
        }

        float deltaTemp = tempGrid[tempGrid.Count - 1] - tempGrid[0];
        float deltaTime = timeGrid[timeGrid.Count - 1];
        foreach (ISensor sensor in temperatures) {
          using (Pen pen = new Pen(colors[sensor])) {
            IEnumerable<SensorValue> values = sensor.Values;
            PointF last = new PointF();
            bool first = true;
            foreach (SensorValue v in values) {
              if (!float.IsNaN(v.Value)) {
                PointF point = new PointF(
                    x0 + w - w * (float)(now - v.Time).TotalMinutes / deltaTime,
                    y0 + h - h * (v.Value - tempGrid[0]) / deltaTemp);
                if (!first) 
                  g.DrawLine(pen, last, point);                
                last = point;
                first = false;
              } else {
                first = true;
              }
            }
          }
        }

        g.SmoothingMode = SmoothingMode.None;
        g.FillRectangle(Brushes.White, 0, 0, x0, r.Height);
        g.FillRectangle(Brushes.White, x0 + w + 1, 0, r.Width - x0 - w,
          r.Height);

        for (int i = 1; i < timeGrid.Count; i++) {
          float x = x0 + (timeGrid.Count - 1 - i) * w / (timeGrid.Count - 1);
          g.DrawString(timeGrid[i].ToString(), Font, Brushes.Black, x,
            y0 + h + bottomScaleSpace, centerlower);
        }

        for (int i = 0; i < tempGrid.Count - 1; i++) {
          float y = y0 + (tempGrid.Count - 1 - i) * h / (tempGrid.Count - 1);
          g.DrawString(tempGrid[i].ToString(), Font, Brushes.Black,
            x0 - leftScaleSpace, y, centerleft);
        }

        g.SmoothingMode = SmoothingMode.HighQuality;
        g.DrawString("[°C]", Font, Brushes.Black, x0 - leftScaleSpace, y0,
          lowerleft);
        g.DrawString("[min]", Font, Brushes.Black, x0 + w,
          y0 + h + bottomScaleSpace, lowerleft);
      }
    }

    public void SetSensors(List<ISensor> sensors,
      IDictionary<ISensor, Color> colors) {
      this.colors = colors;
      List<ISensor> clocks = new List<ISensor>();
      List<ISensor> temperatures = new List<ISensor>();
      List<ISensor> fans = new List<ISensor>();
      foreach (ISensor sensor in sensors)
        switch (sensor.SensorType) {
          case SensorType.Clock: clocks.Add(sensor); break;
          case SensorType.Temperature: temperatures.Add(sensor); break;
          case SensorType.Fan: fans.Add(sensor); break;
        }
      this.clocks = clocks;
      this.temperatures = temperatures;
      this.fans = fans;
      Invalidate();
    }

  }
}
