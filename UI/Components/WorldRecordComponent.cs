﻿using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using LiveSplit.Web.Share;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.WorldRecord.UI.Components
{
    public class WorldRecordComponent : IComponent
    {
        protected InfoTextComponent InternalComponent { get; set; }

        protected WorldRecordSettings Settings { get; set; }

        private GraphicsCache Cache { get; set; }
        private ITimeFormatter TimeFormatter { get; set; }
        private LiveSplitState State { get; set; }
        private TimeStamp LastUpdate { get; set; }
        private TimeSpan RefreshInterval { get; set; }
        public SpeedrunCom.Record WorldRecord { get; protected set; }

        public string ComponentName
        {
            get { return "World Record"; }
        }

        public float PaddingTop { get { return InternalComponent.PaddingTop; } }
        public float PaddingLeft { get { return InternalComponent.PaddingLeft; } }
        public float PaddingBottom { get { return InternalComponent.PaddingBottom; } }
        public float PaddingRight { get { return InternalComponent.PaddingRight; } }

        public float VerticalHeight { get { return InternalComponent.VerticalHeight; } }
        public float MinimumWidth { get { return InternalComponent.MinimumWidth; } }
        public float HorizontalWidth { get { return InternalComponent.HorizontalWidth; } }
        public float MinimumHeight { get { return InternalComponent.MinimumHeight; } }

        public IDictionary<string, Action> ContextMenuControls { get { return null; } }

        public WorldRecordComponent(LiveSplitState state)
        {
            State = state;

            RefreshInterval = TimeSpan.FromMinutes(5);
            Cache = new GraphicsCache();
            TimeFormatter = new RegularTimeFormatter();
            InternalComponent = new InfoTextComponent("World Record", "-");
            Settings = new WorldRecordSettings()
            {
                CurrentState = state
            };
        }

        public void Dispose()
        {
        }

        private void RefreshWorldRecord()
        {
            LastUpdate = TimeStamp.Now;

            if (State != null && State.Run != null &&
                !string.IsNullOrEmpty(State.Run.GameName) && !string.IsNullOrEmpty(State.Run.CategoryName))
            {
                WorldRecord = SpeedrunCom.Instance.GetWorldRecord(State.Run.GameName, State.Run.CategoryName);
            }
            else
            {
                WorldRecord = default(SpeedrunCom.Record);
            }

            ShowWorldRecord();
        }

        private void ShowWorldRecord()
        {
            if (WorldRecord.Runners != null)
            {
                var timingMethod = State.CurrentTimingMethod;
                if (!WorldRecord.Time[timingMethod].HasValue)
                {
                    if (timingMethod == TimingMethod.RealTime)
                        timingMethod = TimingMethod.GameTime;
                    else
                        timingMethod = TimingMethod.RealTime;
                }

                var time = TimeFormatter.Format(WorldRecord.Time[timingMethod]);
                var runners = WorldRecord.Runners.Aggregate((a, b) => a + " & " + b);

                if (Settings.CenteredText && !Settings.Display2Rows)
                {
                    InternalComponent.InformationName = string.Format("World Record is {0} by {1}", time, runners);
                    InternalComponent.AlternateNameText = new[]
                    {
                        string.Format("World Record: {0} by {1}", time, runners),
                        string.Format("WR: {0} by {1}", time, runners),
                        string.Format("WR is {0} by {1}", time, runners)
                    };
                }
                else
                {
                    InternalComponent.InformationValue = string.Format("{0} by {1}", time, runners);
                }
            }
            else
            {
                if (Settings.CenteredText && !Settings.Display2Rows)
                {
                    InternalComponent.InformationName = "Unknown World Record";
                    InternalComponent.AlternateNameText = new[] { "Unknown WR" };
                }
                else
                {
                    InternalComponent.InformationValue = "-";
                }
            }
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            Cache.Restart();
            Cache["Game"] = state.Run.GameName;
            Cache["Category"] = state.Run.CategoryName;

            if (Cache.HasChanged || (LastUpdate != null && TimeStamp.Now - LastUpdate >= RefreshInterval))
            {
                Task.Factory.StartNew(RefreshWorldRecord);
            }
            else
            {
                Cache["TimingMethod"] = state.CurrentTimingMethod;
                Cache["CenteredText"] = Settings.CenteredText && !Settings.Display2Rows;

                if (Cache.HasChanged)
                {
                    ShowWorldRecord();
                }
            }

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.ToArgb() != Color.Transparent.ToArgb()
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.ToArgb() != Color.Transparent.ToArgb())
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        private void PrepareDraw(LiveSplitState state, LayoutMode mode)
        {
            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            if (Settings.CenteredText && !Settings.Display2Rows)
            {
                InternalComponent.NameLabel.HorizontalAlignment = StringAlignment.Center;
                InternalComponent.ValueLabel.HorizontalAlignment = StringAlignment.Center;
                InternalComponent.NameLabel.VerticalAlignment = StringAlignment.Center;
                InternalComponent.ValueLabel.VerticalAlignment = StringAlignment.Center;
                InternalComponent.InformationValue = "";
            }
            else
            {
                InternalComponent.InformationName = "World Record";
                InternalComponent.AlternateNameText = new[]
                {
                    "WR"
                };
                InternalComponent.NameLabel.HorizontalAlignment = StringAlignment.Near;
                InternalComponent.ValueLabel.HorizontalAlignment = StringAlignment.Far;
                InternalComponent.NameLabel.VerticalAlignment =
                    mode == LayoutMode.Horizontal || Settings.Display2Rows ? StringAlignment.Near : StringAlignment.Center;
                InternalComponent.ValueLabel.VerticalAlignment =
                    mode == LayoutMode.Horizontal || Settings.Display2Rows ? StringAlignment.Far : StringAlignment.Center;
            }

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state, LayoutMode.Horizontal);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);
            PrepareDraw(state, LayoutMode.Vertical);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }
    }
}
