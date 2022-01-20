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
        public void Parse_Single()
        {
            var rot = Parse("--You left a Darkhollow Spider Eye on a cave fisher.--");
            Assert.NotNull(rot);
            Assert.Equal(PLAYER, rot.Char);
            Assert.Equal("Darkhollow Spider Eye", rot.Item);
            Assert.Equal("A cave fisher", rot.Source);
            //Assert.Equal(1, loot.Qty);
        }

        [Fact]
        public void Parse_Multiple()
        {
            var rot = Parse("--You left 2 Regrua Claws on a regrua guardian.--");
            Assert.NotNull(rot);
            Assert.Equal(PLAYER, rot.Char);
            Assert.Equal("Regrua Claws", rot.Item);
            Assert.Equal("A regrua guardian", rot.Source);
            //Assert.Equal(2, loot.Qty);
        }

        [Fact]
        public void Parse_Master_Leave()
        {
            var rot = Parse("--Rumstil left a Darkhollow Spider Eye on a cave fisher.--");
            Assert.NotNull(rot);
            Assert.Equal("Rumstil", rot.Char);
            Assert.Equal("Darkhollow Spider Eye", rot.Item);
            Assert.Equal("A cave fisher", rot.Source);
            //Assert.Equal(1, loot.Qty);
        }

        [Fact]
        public void Parse_Unclaimed_Single()
        {
            var rot = Parse("--a Velium Laced Taipan Venom was left on an aggressive corpse's corpse.--");
            Assert.NotNull(rot);
            Assert.Equal("Velium Laced Taipan Venom", rot.Item);
            Assert.Equal("An aggressive corpse", rot.Source);
            //Assert.Equal(1, loot.Qty);
        }

        [Fact]
        public void Parse_Unclaimed_Multiple()
        {
            var rot = Parse("--2 Velium Shard were left on a golem usher's corpse.--");
            Assert.NotNull(rot);
            Assert.Equal("Velium Shard", rot.Item);
            Assert.Equal("A golem usher", rot.Source);
            //Assert.Equal(2, loot.Qty);
        }
    }
}
