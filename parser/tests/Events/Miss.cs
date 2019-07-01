using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogMissEventTests
    {
        const string PLAYER = "Bob";

        private LogMissEvent Parse(string text)
        {
            return LogMissEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Normal()
        {
            var miss = Parse("A black wolf tries to bite YOU, but YOU dodge!");
            Assert.NotNull(miss);
            Assert.Equal("A black wolf", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("dodge", miss.Type);
            Assert.Equal(LogEventMod.None, miss.Mod);
        }

        [Fact]
        public void Parse_Special()
        {
            var miss = Parse("You try to shoot a sarnak conscript, but miss! (Double Bow Shot)");
            Assert.NotNull(miss);
            Assert.Equal("A sarnak conscript", miss.Target);
            Assert.Equal(PLAYER, miss.Source);
            Assert.Equal("miss", miss.Type);
            //Assert.Equal(LogEventMod.Double_Bow_Shot, miss.Mod);
        }

        [Fact]
        public void Parse_Rune()
        {
            var miss = Parse("A tirun overlord tries to kick Xebn, but Xebn's magical skin absorbs the blow!");
            Assert.NotNull(miss);
            Assert.Equal("A tirun overlord", miss.Source);
            Assert.Equal("Xebn", miss.Target);
            Assert.Equal("rune", miss.Type);
        }

        [Fact]
        public void Parse_Rune_Self()
        {
            var miss = Parse("A tirun crusher tries to hit YOU, but YOUR magical skin absorbs the blow!");
            Assert.NotNull(miss);
            Assert.Equal("A tirun crusher", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("rune", miss.Type);
        }

        [Fact]
        public void Parse_Invul()
        {
            var miss = Parse("A Blackburrow lurker tries to hit YOU, but YOU are INVULNERABLE!");
            Assert.NotNull(miss);
            Assert.Equal("A Blackburrow lurker", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("invul", miss.Type);
        }

        [Fact]
        public void Parse_Resist()
        {
            var miss = Parse("A mist wolf resisted your Undermining Helix Rk. II!");
            Assert.NotNull(miss);
            Assert.Equal(PLAYER, miss.Source);
            Assert.Equal("A mist wolf", miss.Target);
            Assert.Equal("resist", miss.Type);
            Assert.Equal("Undermining Helix Rk. II", miss.Spell);

            miss = Parse("You resist a Sebilisian bonecaster's Greater Immobilize!");
            Assert.NotNull(miss);
            Assert.Equal("A Sebilisian bonecaster", miss.Source);
            Assert.Equal(PLAYER, miss.Target);
            Assert.Equal("resist", miss.Type);
            Assert.Equal("Greater Immobilize", miss.Spell);

        }


    }
}
