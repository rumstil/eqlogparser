using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogLootEventTests
    {
        const string PLAYER = "Bob";

        private LogLootEvent Parse(string text)
        {
            return LogLootEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Loot()
        {
            var loot = Parse("--Rumstil has looted a Alluring Flower.--");
            Assert.NotNull(loot);
            Assert.Equal("Rumstil", loot.Char);
            Assert.Equal("Alluring Flower", loot.Item);
        }
    }
}
