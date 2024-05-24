using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogHitEventTests
    {
        const string PLAYER = "Bob";

        private LogHitEvent Parse(string text)
        {
            return LogHitEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Melee()
        {
            var hit = Parse("You punch a corrupt orbweaver for 678 points of damage.");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A corrupt orbweaver", hit.Target);
            Assert.Equal(678, hit.Amount);
            Assert.Equal("punch", hit.Type);
        }

        [Fact]
        public void Parse_Melee_Critical()
        {
            var hit = Parse("You kick a corrupt orbweaver for 7977 points of damage. (Critical)");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A corrupt orbweaver", hit.Target);
            Assert.Equal(7977, hit.Amount);
            Assert.Equal("kick", hit.Type);
            Assert.Equal(LogEventMod.Critical, hit.Mod);
        }

        [Fact]
        public void Parse_Melee_Crippling()
        {
            var hit = Parse("Scooby hits Queen Velazul Di`Zok for 85993 points of damage. (Crippling Blow)");
            Assert.NotNull(hit);
            Assert.Equal("Scooby", hit.Source);
            Assert.Equal("Queen Velazul Di`Zok", hit.Target);
            Assert.Equal(85993, hit.Amount);
            Assert.Equal("hit", hit.Type);
            Assert.Equal(LogEventMod.Critical, hit.Mod);
        }

        [Fact]
        public void Parse_Melee_Multiple_Mods()
        {
            var hit = Parse("You slash a clockwork chef for 24703 points of damage. (Riposte Strikethrough Critical)");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A clockwork chef", hit.Target);
            Assert.Equal(24703, hit.Amount);
            Assert.Equal("slash", hit.Type);
            Assert.Equal(LogEventMod.Critical | LogEventMod.Riposte | LogEventMod.Strikethrough, hit.Mod);
        }

        [Fact]
        public void Parse_Melee_Finishing_Blow()
        {
            var hit = Parse("You kick a corrupt orbweaver for 79777 points of damage. (Finishing Blow)");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A corrupt orbweaver", hit.Target);
            Assert.Equal(79777, hit.Amount);
            Assert.Equal("kick", hit.Type);
            Assert.Equal(LogEventMod.Finishing_Blow, hit.Mod);
        }

        [Fact]
        public void Parse_Melee_Slay_Undead()
        {
            var hit = Parse("Pally slashes a wandering corpse for 216691 points of damage. (Slay Undead)");
            Assert.NotNull(hit);
            Assert.Equal("Pally", hit.Source);
            Assert.Equal("A wandering corpse", hit.Target);
            Assert.Equal(216691, hit.Amount);
            Assert.Equal("slash", hit.Type);
            Assert.Equal(LogEventMod.Slay_Undead | LogEventMod.Critical, hit.Mod);
        }

        [Fact]
        public void Parse_Melee_Self()
        {
            var hit = Parse("Curly hits himself for 2165 points of damage. (Strikethrough Lucky Crippling Blow)");
            Assert.NotNull(hit);
            Assert.Equal("Curly", hit.Source);
            Assert.Equal("Curly", hit.Target);
            Assert.Equal(2165, hit.Amount);
            Assert.Equal("hit", hit.Type);
            Assert.Equal(LogEventMod.Strikethrough | LogEventMod.Lucky | LogEventMod.Critical, hit.Mod);
        }

        [Fact]
        public void Parse_DD()
        {
            var hit = Parse("Fourier hit a tirun crusher for 4578 points of magic damage by Force of Magic VII.");
            Assert.NotNull(hit);
            Assert.Equal("Fourier", hit.Source);
            Assert.Equal("A tirun crusher", hit.Target);
            Assert.Equal(4578, hit.Amount);
            Assert.Equal("dd", hit.Type);
            Assert.Equal("Force of Magic VII", hit.Spell);
        }

        [Fact]
        public void Parse_DD_Critical()
        {
            var hit = Parse("Fourier hit a tirun crusher for 4578 points of magic damage by Force of Magic VII. (Critical)");
            Assert.NotNull(hit);
            Assert.Equal("Fourier", hit.Source);
            Assert.Equal("A tirun crusher", hit.Target);
            Assert.Equal(4578, hit.Amount);
            Assert.Equal("dd", hit.Type);
            Assert.Equal("Force of Magic VII", hit.Spell);
            Assert.Equal(LogEventMod.Critical, hit.Mod);
        }

        private void Parse_Obsolete_DD()
        {
            var hit = Parse("Rumstil hit a scaled wolf for 726 points of non-melee damage.");
            Assert.NotNull(hit);
            Assert.Equal("Rumstil", hit.Source);
            Assert.Equal("A scaled wolf", hit.Target);
            Assert.Equal(726, hit.Amount);
            Assert.Equal("dd", hit.Type);
        }

        private void Parse_Obsolete_DD_Critical()
        {
            var hit = Parse("Rumstil hit a kodiak bear for 2515 points of non-melee damage. (Critical)");
            Assert.NotNull(hit);
            Assert.Equal("Rumstil", hit.Source);
            Assert.Equal("A kodiak bear", hit.Target);
            Assert.Equal(2515, hit.Amount);
            Assert.Equal("dd", hit.Type);
            Assert.Equal(LogEventMod.Critical, hit.Mod);
        }

        [Fact]
        public void Parse_DoT_Own()
        {
            var hit = Parse("A tree snake has taken 1923 damage from your Breath of Queen Malarian.");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A tree snake", hit.Target);
            Assert.Equal(1923, hit.Amount);
            Assert.Equal("dot", hit.Type);
            Assert.Equal("Breath of Queen Malarian", hit.Spell);
        }

        [Fact]
        public void Parse_DoT_Own_Critical()
        {
            var hit = Parse("A tree snake has taken 6677 damage from your Breath of Queen Malarian. (Critical)");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A tree snake", hit.Target);
            Assert.Equal(6677, hit.Amount);
            Assert.Equal("dot", hit.Type);
            Assert.Equal("Breath of Queen Malarian", hit.Spell);
            Assert.Equal(LogEventMod.Critical, hit.Mod);
        }

        [Fact]
        public void Parse_DoT_Other()
        {
            var hit = Parse("A Drogan berserker has taken 34993 damage from Mind Tempest by Fourier.");
            Assert.NotNull(hit);
            Assert.Equal("Fourier", hit.Source);
            Assert.Equal("A Drogan berserker", hit.Target);
            Assert.Equal(34993, hit.Amount);
            Assert.Equal("dot", hit.Type);
            Assert.Equal("Mind Tempest", hit.Spell);
        }

        [Fact]
        public void Parse_DoT_Incoming()
        {
            var hit = Parse("You have taken 52 damage from Chaos Claws by an ukun warhound's corpse.");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("An ukun warhound's corpse", hit.Target);
            Assert.Equal(52, hit.Amount);
            Assert.Equal("dot", hit.Type);
            Assert.Equal("Chaos Claws", hit.Spell);
        }

        // is this worthwhile if we can't assign it to anyone?
        [Fact(Skip = "Unattributed hits are not useful")]
        private void Parse_DoT_Unattributed()
        {
            var hit = Parse("Vallon Zek has taken 3343 damage by Slitheren Venom Rk. III.");
            Assert.NotNull(hit);
            Assert.Null(hit.Source);
            Assert.Equal("Vallon Zek", hit.Target);
            Assert.Equal(3343, hit.Amount);
            Assert.Equal("dot", hit.Type);
            Assert.Equal("Slitheren Venom Rk. III", hit.Spell);
        }

        [Fact]
        public void Parse_DS_Own()
        {
            var hit = Parse("A kodiak bear is pierced by YOUR thorns for 114 points of non-melee damage.");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal("A kodiak bear", hit.Target);
            Assert.Equal(114, hit.Amount);
            Assert.Equal("ds", hit.Type);
        }

        [Fact]
        public void Parse_DS_Other()
        {
            var hit = Parse("An Iron Legion admirer is pierced by Garantik's thorns for 144 points of non-melee damage.");
            Assert.NotNull(hit);
            Assert.Equal("Garantik", hit.Source);
            Assert.Equal("An Iron Legion admirer", hit.Target);
            Assert.Equal(144, hit.Amount);
            Assert.Equal("ds", hit.Type);
        }

        [Fact]
        public void Parse_DS_Incoming()
        {
            var hit = Parse("YOU are burned by Yunta Soothsayer's flames for 11 points of non-melee damage!");
            Assert.NotNull(hit);
            Assert.Equal("Yunta Soothsayer", hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal(11, hit.Amount);
            Assert.Equal("ds", hit.Type);
        }

        [Fact]
        public void Parse_Self_Hit()
        {
            var hit = Parse("You hit yourself for 390 points of fire damage by Rain of Skyfire.");
            Assert.NotNull(hit);
            Assert.Equal(PLAYER, hit.Source);
            Assert.Equal(PLAYER, hit.Target);
            Assert.Equal(390, hit.Amount);
            Assert.Equal("dd", hit.Type);
            Assert.Equal("Rain of Skyfire", hit.Spell);
        }

    }
}
