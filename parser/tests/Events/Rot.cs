using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogRotEventTests
    {
        const string PLAYER = "Bob";

        private LogRotEvent Parse(string text)
        {
            return LogRotEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Rot_Single()
        {
            var loot = Parse("--You left a Darkhollow Spider Eye on a cave fisher.--");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Char);
            Assert.Equal("Darkhollow Spider Eye", loot.Item);
            Assert.Equal("A cave fisher", loot.Source);
            //Assert.Equal(1, loot.Qty);
        }

        [Fact]
        public void Parse_Rot_Multiple()
        {
            var loot = Parse("--You left 2 Regrua Claws on a regrua guardian.--");
            Assert.NotNull(loot);
            Assert.Equal(PLAYER, loot.Char);
            Assert.Equal("Regrua Claws", loot.Item);
            Assert.Equal("A regrua guardian", loot.Source);
            //Assert.Equal(2, loot.Qty);
        }

        [Fact(Skip = "Disabled because i'm not sure it's useful.")]
        public void Parse_Unclaimed()
        {
            var loot = Parse("No one was interested in the 1 item(s): Glowing Sebilisian Boots. These items can be randomed again or will be available to everyone after the corpse unlocks.");
            Assert.NotNull(loot);
            Assert.Equal("Glowing Sebilisian Boots", loot.Item);
            //Assert.Equal(1, loot.Qty);
        }
    }
}
