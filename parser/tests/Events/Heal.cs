using EQLogParser;
using Xunit;


namespace EQLogParserTests.Event
{
    public class LogHealEventTests
    {
        const string PLAYER = "Bob";

        private LogHealEvent Parse(string text)
        {
            return LogHealEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Instant()
        {
            var heal = Parse("Uteusher healed Lenantik for 9875 hit points by Devout Light.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal("Lenantik", heal.Target);
            Assert.Equal(9875, heal.Amount);
            Assert.Equal(9875, heal.FullAmount);
            Assert.Equal("Devout Light", heal.Spell);
        }

        [Fact]
        public void Parse_Obsolete_Instant()
        {
            var heal = Parse("Lenantik is bathed in a devout light. Uteusher healed Lenantik for 9875 hit points by Devout Light.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal("Lenantik", heal.Target);
            Assert.Equal(9875, heal.Amount);
            Assert.Equal(9875, heal.FullAmount);
            Assert.Equal("Devout Light", heal.Spell);
        }

        [Fact]
        public void Parse_Instant_No_Landed_Text()
        {
            var heal = Parse("You healed Sorethumb for 8 hit points by Blood of the Devoted.");
            Assert.NotNull(heal);
            Assert.Equal(PLAYER, heal.Source);
            Assert.Equal("Sorethumb", heal.Target);
            Assert.Equal(8, heal.Amount);
            Assert.Equal("Blood of the Devoted", heal.Spell);
        }

        [Fact]
        public void Parse_Instant_From_Long_Name()
        {
            var heal = Parse("Dude`s ward healed Sponge for 200 hit points by Nature's Restoration VIII.");
            Assert.NotNull(heal);
            Assert.Equal("Dude`s ward", heal.Source);
            Assert.Equal("Sponge", heal.Target);
            Assert.Equal(200, heal.Amount);
            Assert.Equal("Nature's Restoration VIII", heal.Spell);
        }

        [Fact]
        public void Parse_Instant_No_Spell_Name()
        {
            // lifetap weapons don't list a spell name
            // bard regen too?
            var heal = Parse("Blurr healed itself for 12 (617) hit points.");                              
            Assert.NotNull(heal);
            Assert.Equal("Blurr", heal.Source);
            Assert.Equal("Blurr", heal.Target);
            Assert.Equal(12, heal.Amount);
            Assert.Equal(617, heal.FullAmount);
            Assert.Null(heal.Spell);
        }

        [Fact]
        public void Parse_Instant_Partial()
        {
            var heal = Parse("Uteusher healed Lenantik for 2153 (9875) hit points by Devout Light.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal("Lenantik", heal.Target);
            Assert.Equal(2153, heal.Amount);
            Assert.Equal(9875, heal.FullAmount);
            Assert.Equal("Devout Light", heal.Spell);
        }

        [Fact]
        public void Parse_Instant_Zero()
        {
            // make sure heals for zero aren't suppressed
            var heal = Parse("Uteusher healed Lenantik for 0 (9875) hit points by Devout Light.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal("Lenantik", heal.Target);
            Assert.Equal(0, heal.Amount);
            Assert.Equal(9875, heal.FullAmount);
            Assert.Equal("Devout Light", heal.Spell);
        }

        [Fact]
        public void Parse_Instant_Mod()
        {
            // capture critical
            var heal = Parse("You healed Uteusher for 361 hit points by HandOfHolyVengeanceVRecourse. (Critical)");
            Assert.NotNull(heal);
            Assert.Equal(PLAYER, heal.Source);
            Assert.Equal("Uteusher", heal.Target);
            Assert.Equal(361, heal.Amount);
            Assert.Equal(361, heal.FullAmount);
            Assert.Equal("HandOfHolyVengeanceVRecourse", heal.Spell);
            Assert.Equal(LogEventMod.Critical, heal.Mod);
        }

        [Fact(Skip = "Obsolete")]
        public void Parse_Obsolete_Instant_Mod()
        {
            // capture critical
            var heal = Parse("A holy light surrounds you. You healed Uteusher for 361 hit points by HandOfHolyVengeanceVRecourse. (Critical)");
            Assert.NotNull(heal);
            Assert.Equal(PLAYER, heal.Source);
            Assert.Equal("Uteusher", heal.Target);
            Assert.Equal(361, heal.Amount);
            Assert.Equal(361, heal.FullAmount);
            Assert.Equal("HandOfHolyVengeanceVRecourse", heal.Spell);
            Assert.Equal(LogEventMod.Critical, heal.Mod);
        }

        [Fact]
        public void Parse_Instant_Himself()
        {
            // convert himself/herself/itself
            var heal = Parse("Brugian healed himself for 44363 hit points by Promised Remedy Trigger II.");
            Assert.NotNull(heal);
            Assert.Equal("Brugian", heal.Source);
            Assert.Equal("Brugian", heal.Target);
            Assert.Equal(44363, heal.Amount);
            Assert.Equal("Promised Remedy Trigger II", heal.Spell);
        }

        [Fact]
        public void Parse_Obsolete_Instant_Himself()
        {
            // convert himself/herself/itself
            var heal = Parse("Brugian is infused by divine healing. Brugian healed himself for 44363 hit points by Promised Remedy Trigger II.");
            Assert.NotNull(heal);
            Assert.Equal("Brugian", heal.Source);
            Assert.Equal("Brugian", heal.Target);
            Assert.Equal(44363, heal.Amount);
            Assert.Equal("Promised Remedy Trigger II", heal.Spell);
        }

        [Fact]
        public void Parse_Obsolete_Instant_NonGreedy_Capture()
        {
            // could be incorrectly parsed as source="is" target="by life-giving energy. Brugian healed Xebn"
            var heal = Parse("Xebn is healed by life-giving energy. Brugian healed Xebn for 84056 hit points by Furial Renewal.");
            Assert.NotNull(heal);
            Assert.Equal("Brugian", heal.Source);
            Assert.Equal("Xebn", heal.Target);
            Assert.Equal(84056, heal.Amount);
            Assert.Equal("Furial Renewal", heal.Spell);
        }

        [Fact]
        public void Parse_HoT()
        {
            var heal = Parse("Uteusher healed you over time for 1797 hit points by Devout Elixir.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(1797, heal.Amount);
            Assert.Equal(1797, heal.FullAmount);
            Assert.Equal("Devout Elixir", heal.Spell);
        }

        [Fact]
        public void Parse_HoT_Gross()
        {
            var heal = Parse("Uteusher healed you over time for 208 (1797) hit points by Devout Elixir.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(208, heal.Amount);
            Assert.Equal(1797, heal.FullAmount);
            Assert.Equal("Devout Elixir", heal.Spell);
        }

        [Fact]
        public void Parse_HoT_Self()
        {
            var heal = Parse("Uteusher healed himself over time for 1797 hit points by Devout Elixir.");
            Assert.NotNull(heal);
            Assert.Equal("Uteusher", heal.Source);
            Assert.Equal("Uteusher", heal.Target);
            Assert.Equal(1797, heal.Amount);
            Assert.Equal(1797, heal.FullAmount);
            Assert.Equal("Devout Elixir", heal.Spell);
        }

        [Fact]
        public void Parse_Hot_From_Long_Name()
        {
            var heal = Parse("a steadfast servant healed Sponge over time for 1562 hit points by Servant's Elixir IX.");
            Assert.NotNull(heal);
            Assert.Equal("A steadfast servant", heal.Source);
            Assert.Equal("Sponge", heal.Target);
            Assert.Equal(1562, heal.Amount);
            Assert.Equal("Servant's Elixir IX", heal.Spell);
        }

        [Fact]
        public void Parse_HoT_No_Source()
        {
            var heal = Parse("Uteusher has been healed over time for 0 (900) hit points by Celestial Regeneration XVIII.");
            Assert.NotNull(heal);
            Assert.Null(heal.Source);
            Assert.Equal("Uteusher", heal.Target);
            Assert.Equal(0, heal.Amount);
            Assert.Equal(900, heal.FullAmount);
            Assert.Equal("Celestial Regeneration XVIII", heal.Spell);
        }

        [Fact]
        public void Parse_HoT_No_Source_Self()
        {
            var heal = Parse("You have been healed over time for 9525 hit points by Merciful Elixir Rk. II.");
            Assert.NotNull(heal);
            Assert.Null(heal.Source);
            Assert.Equal(PLAYER, heal.Target);
            Assert.Equal(9525, heal.Amount);
            Assert.Equal(9525, heal.FullAmount);
            Assert.Equal("Merciful Elixir Rk. II", heal.Spell);
        }

        [Fact]
        public void Parse_Pet_Aura()
        {
            var heal = Parse("Kobekn has been healed for 1720 (20000) hit points by Enhanced Theft of Essence Effect XI.");
            Assert.NotNull(heal);
            Assert.Null(heal.Source);
            Assert.Equal("Kobekn", heal.Target);
            Assert.Equal(1720, heal.Amount);
            Assert.Equal(20000, heal.FullAmount);
            Assert.Equal("Enhanced Theft of Essence Effect XI", heal.Spell);
        }


    }
}
