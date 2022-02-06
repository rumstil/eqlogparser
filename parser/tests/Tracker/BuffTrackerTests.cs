using EQLogParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;


namespace EQLogParserTests.Tracker
{
    public class BuffTrackerTests
    {
        const string PLAYER = "Bob";

        /*
        [Fact]
        public void Init_From_Spell()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo { Name = "Spirit of Wolf", LandOthers = " runs like the wind." });
            var buffs = new BuffTracker(spells);
            buffs.AddSpell("Spirit of Wolf");
        }
        */

        [Fact]
        public void Group_Buff_Lands_On_Self_From_Casters_Log()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Illusions of Grandeur I", LandSelf = "Illusions of Grandeur fill your mind.", Target = (int)SpellTarget.Caster_Group });

            buffs.HandleEvent(new LogCastingEvent() { Spell = "Illusions of Grandeur I", Source = PLAYER, Timestamp = DateTime.UtcNow });
            buffs.HandleEvent(new LogRawEvent() { Text = "Illusions of Grandeur fill your mind.", Player = PLAYER, Timestamp = DateTime.UtcNow });

            var list = buffs.Get(PLAYER, DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Illusions of Grandeur", list[0].Name);
        }

        [Fact]
        public void Group_Buff_Lands_On_Caster_From_Bystanders_Log()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Illusions of Grandeur I", LandOthers = " is consumed by Illusions of Grandeur.", Target = (int)SpellTarget.Caster_Group });

            buffs.HandleEvent(new LogCastingEvent() { Spell = "Illusions of Grandeur I", Source = "Fourier", Timestamp = DateTime.UtcNow });
            buffs.HandleEvent(new LogRawEvent() { Text = "Fourier is consumed by Illusions of Grandeur.", Player = PLAYER, Timestamp = DateTime.UtcNow });

            var list = buffs.Get("Fourier", DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Illusions of Grandeur", list[0].Name);
        }

        [Fact]
        public void Group_Buff_Lands_On_Self_From_Bystanders_Log()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Illusions of Grandeur I", LandSelf = "Illusions of Grandeur fill your mind.", Target = (int)SpellTarget.Caster_Group });

            buffs.HandleEvent(new LogCastingEvent() { Spell = "Illusions of Grandeur I", Source = "Fourier", Timestamp = DateTime.UtcNow });
            buffs.HandleEvent(new LogRawEvent() { Text = "Illusions of Grandeur fill your mind.", Player = PLAYER, Timestamp = DateTime.UtcNow });


            var list = buffs.Get(PLAYER, DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Illusions of Grandeur", list[0].Name);
        }

        [Fact]
        public void Group_Buff_Lands_On_Other_From_Bystanders_Log()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Illusions of Grandeur I", LandOthers = " is consumed by Illusions of Grandeur.", Target = (int)SpellTarget.Caster_Group });

            buffs.HandleEvent(new LogCastingEvent() { Spell = "Illusions of Grandeur I", Source = "Fourier", Timestamp = DateTime.UtcNow });
            buffs.HandleEvent(new LogRawEvent() { Text = "Tokiel is consumed by Illusions of Grandeur.", Player = PLAYER, Timestamp = DateTime.UtcNow });

            var list = buffs.Get("Tokiel", DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Illusions of Grandeur", list[0].Name);
        }

        [Fact]
        public void Ignore_Group_Buff_Emote_Shared_With_Self_Buff_Lands_On_Other()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Group Guardian of the Forest I", Target = (int)SpellTarget.Caster_Group, LandSelf = "The power of the forest surges through your muscles.", LandOthers = " channels the power of the forest." });
            buffs.AddSpell(new SpellInfo { Name = "Guardian of the Forest I", Target = (int)SpellTarget.Self, LandSelf = "The power of the forest surges through your muscles.", LandOthers = " channels the power of the forest." });

            buffs.HandleEvent(new LogCastingEvent() { Spell = "Guardian of the Forest X", Source = "Rumstil", Timestamp = DateTime.UtcNow });
            buffs.HandleEvent(new LogRawEvent() { Text = "Rumstil channels the power of the forest.", Timestamp = DateTime.UtcNow, Player = PLAYER });

            var list = buffs.Get("Rumstil", DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Guardian of the Forest X", list[0].Name);
        }

        [Fact]
        public void Ignore_Group_Buff_Emote_Shared_With_Self_Buff_Lands_On_Self()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Group Guardian of the Forest I", Target = (int)SpellTarget.Caster_Group, LandSelf = "The power of the forest surges through your muscles.", LandOthers = " channels the power of the forest." });
            buffs.AddSpell(new SpellInfo { Name = "Guardian of the Forest I", Target = (int)SpellTarget.Self, LandSelf = "The power of the forest surges through your muscles.", LandOthers = " channels the power of the forest." });

            buffs.HandleEvent(new LogCastingEvent() { Spell = "Guardian of the Forest X", Source = PLAYER, Timestamp = DateTime.UtcNow });
            buffs.HandleEvent(new LogRawEvent() { Text = "The power of the forest surges through your muscles.", Timestamp = DateTime.UtcNow, Player = PLAYER });

            var list = buffs.Get(PLAYER, DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Guardian of the Forest X", list[0].Name);
        }

        [Fact]
        public void Ignore_Self_Buff_Lands_On_Caster()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Superfly TNT", LandSelf = "You should be in the front seat.", Target = (int)SpellTarget.Self });

            // self buffs are handled by the casting event so the emote must be ignored to prevent double counting
            buffs.HandleEvent(new LogRawEvent() { Text = "You should be in the front seat.", Timestamp = DateTime.UtcNow, Player = PLAYER });

            var list = buffs.Get(PLAYER, DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Empty(list);
        }

        [Fact]
        public void Self_Spell_Casting()
        {
            var spells = new FakeSpellParser();
            var buffs = new BuffTracker(spells);
            buffs.AddSpell(new SpellInfo { Name = "Guns of the Navarone I", Target = (int)SpellTarget.Self });

            // we only registered the first rank -- make sure it accepts any rank
            buffs.HandleEvent(new LogCastingEvent() { Spell = "Guns of the Navarone XV", Source = "Tokiel", Timestamp = DateTime.UtcNow });

            var list = buffs.Get("Tokiel", DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Guns of the Navarone XV", list[0].Name);
        }


    }
}
