using Xunit;
using EQLogParser;

namespace EQLogParserTests.Event
{
    public class LogCastingFailEventTests
    {
        const string PLAYER = "Bob";

        private LogCastingFailEvent Parse(string text)
        {
            return LogCastingFailEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }
        
        [Fact]
        public void Fizzle_Other()
        {
            // this test will also check for the double "'s" 
            var fail = Parse("a clockwork defender CXCIX's Night's Perpetual Darkness spell fizzles!");
            Assert.NotNull(fail);
            Assert.Equal("A clockwork defender CXCIX", fail.Source);
            Assert.Equal("Night's Perpetual Darkness", fail.Spell);
            Assert.Equal("fizzle", fail.Type);
        }

        [Fact]
        public void Fizzle_Self()
        {
            var fail = Parse("Your Shield of Shadethorns spell fizzles!");
            Assert.NotNull(fail);
            Assert.Equal(PLAYER, fail.Source);
            Assert.Equal("Shield of Shadethorns", fail.Spell);
            Assert.Equal("fizzle", fail.Type);
        }

        [Fact]
        public void Interrupted_Other()
        {
            var fail = Parse("Fourier's Mind Coil spell is interrupted.");
            Assert.NotNull(fail);
            Assert.Equal("Fourier", fail.Source);
            Assert.Equal("Mind Coil", fail.Spell);
            Assert.Equal("interrupt", fail.Type);
        }

        [Fact]
        public void Interrupted_Self()
        {
            var fail = Parse("Your Claimed Shots spell is interrupted.");
            Assert.NotNull(fail);
            Assert.Equal(PLAYER, fail.Source);
            Assert.Equal("Claimed Shots", fail.Spell);
            Assert.Equal("interrupt", fail.Type);
        }

        /*

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

        [Fact]
        public void Take_Hold_Other()
        {
            var fail = Parse("Your Strength of the Dusksage Stalker spell did not take hold on Kircye. (Blocked by Brell's Blessed Bastion.)");
            Assert.NotNull(fail);
            Assert.Equal(PLAYER, fail.Source);
            Assert.Equal("Shield of Shadethorns", fail.Spell);
            Assert.Equal("blocked", fail.Type);
        }

        [Fact]
        public void Take_Hold_Self()
        {
            var fail = Parse("Your Strength of the Arbor Stalker spell did not take hold. (Blocked by Strength of the Dusksage Stalker.)");
            Assert.NotNull(fail);
            Assert.Equal(PLAYER, fail.Source);
            Assert.Equal("Shield of Shadethorns", fail.Spell);
            Assert.Equal("blocked", fail.Type);
        }

        [Fact]
        public void Resist_Outgoing()
        {
            var fail = Parse("A mist wolf resisted your Undermining Helix Rk. II!");
            Assert.NotNull(fail);
            Assert.Equal(PLAYER, fail.Source);
            Assert.Equal("A mist wolf", fail.Target);
            Assert.Equal("resist", fail.Type);
            Assert.Equal("Undermining Helix Rk. II", fail.Spell);
        }

        [Fact]
        public void Resist_Incoming()
        {
            var fail = Parse("You resist a Sebilisian bonecaster's Greater Immobilize!");
            Assert.NotNull(fail);
            Assert.Equal("A Sebilisian bonecaster", fail.Source);
            Assert.Equal(PLAYER, fail.Target);
            Assert.Equal("resist", fail.Type);
            Assert.Equal("Greater Immobilize", fail.Spell);
        }

        */

    }
}
