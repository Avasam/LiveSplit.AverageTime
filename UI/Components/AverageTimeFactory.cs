using LiveSplit.AveragePrimaryTime.UI.Components;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;

[assembly:ComponentFactory(typeof(LiveSplit.AverageTime.UI.Components.AverageTimeFactory))]

namespace LiveSplit.AverageTime.UI.Components
{
    public class AverageTimeFactory : IComponentFactory
    {
        public string ComponentName => "Average Time";

        public string Description => "Shows the Average Time for the run. Or alternatively the goal time to drop a score on https://www.avasam.dev";

        public ComponentCategory Category => ComponentCategory.Information;

        public IComponent Create(LiveSplitState state) => new AverageTimeComponent(state);

        public string UpdateName => ComponentName;

        public string XMLURL => "http://livesplit.org/update/Components/update.LiveSplit.AverageTime.xml";

        public string UpdateURL => "http://livesplit.org/update/";

        public Version Version => Version.Parse("1.8.0");
    }
}
