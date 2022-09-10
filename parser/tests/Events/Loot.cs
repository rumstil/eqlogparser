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
        public void Parse_Loot_Obsolete()
        {
            var loot = Parse("--Rumstil has looted a Alluring Flower.--");
            Assert.NotNull(loot);
            Assert.Equal("Rumstil", loot.Char);
            Assert.Equal("Alluring Flower", loot.Item);
            Assert.Equal(1, loot.Qty);
            Assert.Null(loot.Source);
        }

        [Fact]
        public void Parse_Loot_Single()
        {
            // containers always seem you have an extra space at the end
            var loot = Parse("--Balterz has looted a Ry`Gorr Glass Gem from a frozen chest .--");
            Assert.NotNull(loot);
            Assert.Equal("Balterz", loot.Char);
            Assert.Equal("Ry`Gorr Glass Gem", loot.Item);
            Assert.Equal("A frozen chest", loot.Source);
            Assert.Equal(1, loot.Qty);
        }

        [Fact]
        public void Parse_Loot_Multiple()
        {
            var loot = Parse("--You have looted 2 Clockwork Gnome Spring from a steamwork shockstriker's corpse.--");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Char);
            Assert.Equal("Clockwork Gnome Spring", loot.Item);
            Assert.Equal("A steamwork shockstriker", loot.Source);
            Assert.Equal(2, loot.Qty);
        }

        [Fact]
        public void Parse_Grabbed()
        {
            var loot = Parse("Rumstil grabbed a Restless Ice Cloth Legs Ornament from an icebound chest .");
            Assert.NotNull(loot);
            Assert.Equal("Rumstil", loot.Char);
            Assert.Equal("Restless Ice Cloth Legs Ornament", loot.Item);
            Assert.Equal("An icebound chest", loot.Source);
            Assert.Equal(1, loot.Qty);
        }

        [Fact]
        public void Parse_Stolen()
        {
            var loot = Parse("--Rumstil stole Grimy Spell Scroll from froglok krup enchanter!--");
            Assert.NotNull(loot);
            Assert.Equal("Rumstil", loot.Char);
            Assert.Equal("Grimy Spell Scroll", loot.Item);
            Assert.Equal("Froglok krup enchanter", loot.Source);
            Assert.Equal(1, loot.Qty);
        }

    }
}
