using Xunit;

namespace EQLogParser
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
            Assert.Equal("Rumstil", loot.Looter);
            Assert.Equal("a Alluring Flower", loot.Item);
        }

        [Fact]
        public void Parse_Craft()
        {
            var loot = Parse("You have fashioned the items together to create something new: Magi-potent Crystal.");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Looter);
            Assert.Equal("Magi-potent Crystal", loot.Item);
        }

        [Fact]
        public void Parse_Craft_Alt()
        {
            var loot = Parse("You have fashioned the items together to create an alternate product: Magi-potent Crystal.");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Looter);
            Assert.Equal("Magi-potent Crystal", loot.Item);
        }
    }
}
