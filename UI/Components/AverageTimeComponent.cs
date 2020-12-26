using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using SpeedrunComSharp;
using LiveSplit.Options;

namespace LiveSplit.AverageTime.UI.Components
{
    public class AverageTimeComponent : IComponent
    {
        protected InfoTextComponent InternalComponent { get; set; }

        protected AverageTimeSettings Settings { get; set; }

        private GraphicsCache Cache { get; set; }
        private ITimeFormatter TimeFormatter { get; set; }
        private RegularTimeFormatter LocalTimeFormatter { get; set; }
        private LiveSplitState State { get; set; }
        private TimeStamp LastUpdate { get; set; }
        private TimeSpan RefreshInterval { get; set; }
        public Record AverageTime { get; protected set; }
        private bool IsLoading { get; set; }
        private SpeedrunComClient Client { get; set; }

        public string ComponentName => "Average Time";

        public float PaddingTop => InternalComponent.PaddingTop;
        public float PaddingLeft => InternalComponent.PaddingLeft;
        public float PaddingBottom => InternalComponent.PaddingBottom;
        public float PaddingRight => InternalComponent.PaddingRight;

        public float VerticalHeight => InternalComponent.VerticalHeight;
        public float MinimumWidth => InternalComponent.MinimumWidth;
        public float HorizontalWidth => InternalComponent.HorizontalWidth;
        public float MinimumHeight => InternalComponent.MinimumHeight;

        public IDictionary<string, Action> ContextMenuControls => null;

        public AverageTimeComponent(LiveSplitState state)
        {
            State = state;

            Client = new SpeedrunComClient(userAgent: Updates.UpdateHelper.UserAgent, maxCacheElements: 0);

            RefreshInterval = TimeSpan.FromMinutes(5);
            Cache = new GraphicsCache();
            TimeFormatter = new AutomaticPrecisionTimeFormatter();
            LocalTimeFormatter = new RegularTimeFormatter();
            InternalComponent = new InfoTextComponent("Average Time", TimeFormatConstants.DASH);
            Settings = new AverageTimeSettings()
            {
                CurrentState = state
            };
        }

        public void Dispose()
        {
        }

        private void RefreshAverageTime()
        {
            LastUpdate = TimeStamp.Now;

            AverageTime = null;

            try
            {
                if (State != null && State.Run != null
                    && State.Run.Metadata.Game != null && State.Run.Metadata.Category != null)
                {
                    var variableFilter = Settings.FilterVariables ? State.Run.Metadata.VariableValues.Values : null;
                    var regionFilter = Settings.FilterRegion && State.Run.Metadata.Region != null ? State.Run.Metadata.Region.ID : null;
                    var platformFilter = Settings.FilterPlatform && State.Run.Metadata.Platform != null ? State.Run.Metadata.Platform.ID : null;
                    EmulatorsFilter emulatorFilter = EmulatorsFilter.NotSet;
                    if (Settings.FilterPlatform)
                    {
                        if (State.Run.Metadata.UsesEmulator)
                            emulatorFilter = EmulatorsFilter.OnlyEmulators;
                        else
                            emulatorFilter = EmulatorsFilter.NoEmulators;
                    }

                    var leaderboard = Client.Leaderboards.GetLeaderboardForFullGameCategory(State.Run.Metadata.Game.ID, State.Run.Metadata.Category.ID, 
                        top: 1,
                        platformId: platformFilter, regionId: regionFilter, 
                        emulatorsFilter: emulatorFilter, variableFilters: variableFilter);

                    if (leaderboard != null)
                    {
                        // TODO: This is where we get average time
                        AverageTime = leaderboard.Records.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            IsLoading = false;
            ShowAverageTime(State.Layout.Mode);
        }

        private void ShowAverageTime(LayoutMode mode)
        {
            var centeredText = Settings.CenteredText && !Settings.Display2Rows && mode == LayoutMode.Vertical;
            if (AverageTime != null)
            {
                var time = AverageTime.Times.Primary;
                var game = State.Run.Metadata.Game;
                if (game != null)
                {
                    LocalTimeFormatter.Accuracy = game.Ruleset.ShowMilliseconds ? TimeAccuracy.Hundredths : TimeAccuracy.Seconds;
                }

                var formatted = TimeFormatter.Format(time);

                if (centeredText)
                {
                    var textList = new List<string>();

                    textList.Add(string.Format("Average Time is {0}", formatted));
                    textList.Add(string.Format("Average Time: {0}", formatted));
                    textList.Add(string.Format("Avg: {0}", formatted));
                    textList.Add(string.Format("Avg. is {0}", formatted));

                    InternalComponent.InformationName = textList.First();
                    InternalComponent.AlternateNameText = textList;
                }
                else
                {
                    InternalComponent.InformationValue = string.Format("{0}", formatted);
                }
            }
            else if (IsLoading)
            {
                if (centeredText)
                {
                    InternalComponent.InformationName = "Loading Average Time...";
                    InternalComponent.AlternateNameText = new[] { "Loading Avg..." };
                }
                else
                {
                    InternalComponent.InformationValue = "Loading...";
                }
            }
            else
            {
                if (centeredText)
                {
                    InternalComponent.InformationName = "Unknown Average Time";
                    InternalComponent.AlternateNameText = new[] { "Unknown Avg." };
                }
                else
                {
                    InternalComponent.InformationValue = TimeFormatConstants.DASH;
                }
            }
        }


        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            Cache.Restart();
            Cache["Game"] = state.Run.GameName;
            Cache["Category"] = state.Run.CategoryName;
            Cache["PlatformID"] = Settings.FilterPlatform ? state.Run.Metadata.PlatformName : null;
            Cache["RegionID"] = Settings.FilterRegion ? state.Run.Metadata.RegionName : null;
            Cache["UsesEmulator"] = Settings.FilterPlatform ? (bool?)state.Run.Metadata.UsesEmulator : null;
            Cache["Variables"] = Settings.FilterVariables ? string.Join(",", state.Run.Metadata.VariableValueNames.Values) : null;

            if (Cache.HasChanged)
            {
                IsLoading = true;
                AverageTime = null;
                ShowAverageTime(mode);
                Task.Factory.StartNew(RefreshAverageTime);
            }
            else if (LastUpdate != null && TimeStamp.Now - LastUpdate >= RefreshInterval)
            {
                Task.Factory.StartNew(RefreshAverageTime);
            }
            else
            {
                Cache["CenteredText"] = Settings.CenteredText && !Settings.Display2Rows && mode == LayoutMode.Vertical;

                if (Cache.HasChanged)
                {
                    ShowAverageTime(mode);
                }
            }

            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
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

            if (Settings.CenteredText && !Settings.Display2Rows && mode == LayoutMode.Vertical)
            {
                InternalComponent.NameLabel.HorizontalAlignment = StringAlignment.Center;
                InternalComponent.ValueLabel.HorizontalAlignment = StringAlignment.Center;
                InternalComponent.NameLabel.VerticalAlignment = StringAlignment.Center;
                InternalComponent.ValueLabel.VerticalAlignment = StringAlignment.Center;
                InternalComponent.InformationValue = "";
            }
            else
            {
                InternalComponent.InformationName = "Average Time";
                InternalComponent.AlternateNameText = new[]
                {
                    "Avg"
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

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, System.Drawing.Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state, LayoutMode.Horizontal);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, System.Drawing.Region clipRegion)
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

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
