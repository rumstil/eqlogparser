using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogShieldEventTests
    {
        const string PLAYER = "Bob";

        private LogShieldEvent Parse(string text)
        {
            return LogShieldEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Shield_Start()
        {
            var shield = Parse("Technician Masterwork begins to use a steamwork trooper as a living shield!");
            Assert.NotNull(shield);
            Assert.Equal("A steamwork trooper", shield.Source);
            Assert.Equal("Technician Masterwork", shield.Target);
        }

        [Fact(Skip = "Probably not a useful event")]
        public void Parse_Shield_Stop()
        {
            var shield = Parse("A Di`Zok dragoon ceases protecting a Di`Zok evoker's corpse.");
            Assert.NotNull(shield);
            Assert.Equal("A Di`Zok dragoon", shield.Source);
            Assert.Equal("A Di`Zok evoker", shield.Target);
        }

    }
}
