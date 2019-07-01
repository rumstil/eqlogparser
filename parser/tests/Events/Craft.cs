using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogCraftEventTests
    {
        const string PLAYER = "Bob";

        private LogCraftEvent Parse(string text)
        {
            return LogCraftEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Craft()
        {
            var loot = Parse("You have fashioned the items together to create something new: Magi-potent Crystal.");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Char);
            Assert.Equal("Magi-potent Crystal", loot.Item);
        }

        [Fact]
        public void Parse_Craft_Alt()
        {
            var loot = Parse("You have fashioned the items together to create an alternate product: Magi-potent Crystal.");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Char);
            Assert.Equal("Magi-potent Crystal", loot.Item);
        }
    }
}
