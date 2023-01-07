using ChartZ.Engine;
using Phoenix.Common.Logging;

namespace Phoenix.Tests.Server
{
    public class TestChartDialogueCommand : ChartCommand
    {
        public override string CommandID => "dialogue";

        public override bool Handle(ChartChain chain, ChartSegment segment)
        {
            Logger.GetLogger("Dialogue test").Info(segment.Payload[0] + ": " + segment.Payload[1]);
            return true;
        }
    }
}
