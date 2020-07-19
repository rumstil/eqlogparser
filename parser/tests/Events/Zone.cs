using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogZoneEventTests
    {
        private LogZoneEvent Parse(string text)
        {
            return LogZoneEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_Normal()
        {
            LogZoneEvent zone = null;

            zone = Parse("You have entered Plane of Knowledge.");
            Assert.NotNull(zone);
            Assert.Equal("Plane of Knowledge", zone.Name);
        }

        [Fact]
        public void Parse_Ignore()
        {
            LogZoneEvent zone = null;

            // ignore special messages that look like zoning
            zone = Parse("You have entered an area where levitation effects do not function.");
            Assert.Null(zone);

            zone = Parse("You have entered an Arena (PvP) area.");
            Assert.Null(zone);

            zone = Parse("You have entered an area where Bind Affinity is allowed.");
            Assert.Null(zone);
        }
    }
}
