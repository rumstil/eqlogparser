using EQLogParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace EQLogParserTests.Tracker
{
    public class CharTrackerTests
    {
        [Fact]
        public void Log_Owner_Is_Friend()
        {
            var chars = new CharTracker();
            chars.HandleEvent(new LogOpenEvent() { Player = "Rumstil" });
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil"));
        }

        [Fact]
        public void Who_Player_Is_Friend()
        {
            var chars = new CharTracker();
            // "/who" should always tag players
            chars.HandleEvent(new LogWhoEvent() { Name = "Rumstil" });
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil"));
        }

        [Fact]
        public void Pet_Owner_Status_Propogates_To_Pet()
        {
            var chars = new CharTracker();

            Assert.Equal(CharType.Unknown, chars.GetType("Rumstil`s pet"));
            Assert.Equal(CharType.Unknown, chars.GetType("Rumstil`s warder"));

            chars.GetOrAdd("Rumstil").Type = CharType.Friend;
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil`s pet"));
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil`s warder"));
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil`s ward"));

            Assert.Equal(CharType.Unknown, chars.GetType("Xantik"));
            chars.GetOrAdd("Xantik").Owner = "Rumstil";
            Assert.Equal(CharType.Friend, chars.GetType("Xantik"));
        }

        [Fact]
        public void Owner_Self_Name()
        {
            var chars = new CharTracker();

            // `pet and `warder are always assigned to the obvious owner
            Assert.Equal("Rumstil", chars.GetOrAdd("Rumstil`s pet")?.Owner);
            Assert.Equal("Rumstil", chars.GetOrAdd("Rumstil`s warder")?.Owner);
            Assert.Equal("Rumstil", chars.GetOrAdd("Rumstil`s ward")?.Owner);
        }

        [Fact]
        public void Owner_Pet_Leader()
        {
            var chars = new CharTracker();

            // "/pet leader" command will announce owner
            chars.HandleEvent(new LogChatEvent() { Source = "Xantik", Message = "My leader is Rumstil.", Channel = "say" });
            Assert.Equal("Rumstil", chars.Get("Xantik")?.Owner);
        }

        [Fact]
        public void Friend_Outbound_Hit_Make_Foe()
        {
            var chars = new CharTracker();
            chars.GetOrAdd("Rumstil").Type = CharType.Friend;
            chars.HandleEvent(new LogHitEvent() { Source = "Rumstil", Target = "froglok jin shaman", Amount = 1 });

            // target should become a foe
            Assert.Equal(CharType.Foe, chars.GetType("froglok jin shaman"));
        }

        [Fact]
        public void Friend_Inbound_Hit_Make_Foe()
        {
            var chars = new CharTracker();
            chars.GetOrAdd("Rumstil").Type = CharType.Friend;
            chars.HandleEvent(new LogHitEvent() { Source = "froglok jin shaman", Target = "Rumstil", Amount = 1 });

            // source should become a foe
            Assert.Equal(CharType.Foe, chars.GetType("froglok jin shaman"));
        }

        [Fact]
        public void Foe_Outbound_Hit_Make_Friend()
        {
            var chars = new CharTracker();
            chars.GetOrAdd("froglok jin shaman").Type = CharType.Foe;
            chars.HandleEvent(new LogHitEvent() { Source = "froglok jin shaman", Target = "Rumstil", Amount = 1 });

            // target should become a friend
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil"));
        }

        [Fact]
        public void Foe_Inbound_Hit_Make_Friend()
        {
            var chars = new CharTracker();
            chars.GetOrAdd("froglok jin shaman").Type = CharType.Foe;
            chars.HandleEvent(new LogHitEvent() { Source = "Rumstil", Target = "froglok jin shaman", Amount = 1 });

            // source should become a friend
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil"));
        }

        [Fact]
        public void IsPetName()
        {
            Assert.True(CharTracker.IsPetName("Xantik"));
            Assert.False(CharTracker.IsPetName("Spot"));
        }

        [Fact]
        public void Healer_May_Be_Foe()
        {
            var chars = new CharTracker();
            chars.HandleEvent(new LogHealEvent() { Source = "froglok jin shaman", Target = "froglok dar knight", Amount = 3, Spell = "Doomscale Focusing" });

            // mobs can be healers too, a heal shouldn't flag the source as a friend (unless the target is a player)
            Assert.Equal(CharType.Unknown, chars.GetType("froglok jin shaman"));
            Assert.Equal(CharType.Unknown, chars.GetType("froglok dar knight"));
        }

        [Fact]
        public void Heal_May_Be_Reverse_DS()
        {
            var chars = new CharTracker();
            chars.GetOrAdd("Rumstil").Type = CharType.Friend;
            chars.GetOrAdd("Zlandicar").Type = CharType.Foe;
            chars.HandleEvent(new LogHealEvent() { Source = "Rumstil", Target = "Zlandicar", Amount = 3, Spell = "Summer's Sleet Rk. III" });

            // reverse DS can make players heal mobs and shouldn't transfer friend/foe status
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil"));
            Assert.Equal(CharType.Foe, chars.GetType("Zlandicar"));
        }

        [Fact]
        public void Dead_Mob_Becomes_Unknown()
        {
            var chars = new CharTracker();
            chars.GetOrAdd("Zlandicar").Type = CharType.Foe;
            chars.HandleEvent(new LogDeathEvent() { Name = "Zlandicar" });

            Assert.Equal(CharType.Unknown, chars.GetType("Zlandicar"));
        }

        [Fact]
        public void Spell_Disc_Should_Assign_Class()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo() { Name = "Super Fire Arrow", ClassesMask = (int)ClassesMaskShort.RNG, ClassesCount = 1 });

            var chars = new CharTracker(spells);
            chars.HandleEvent(new LogCastingEvent() { Source = "Rumstil", Spell = "Super Fire Arrow", Type = CastingType.Disc });
            Assert.Equal("RNG", chars.Get("Rumstil")?.Class);
        }

        [Fact]
        public void Spell_With_Ambiguous_Class_Shouldnt_Assign_Class()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo() { Name = "Invis", ClassesMask = (int)(ClassesMaskShort.RNG | ClassesMaskShort.ENC), ClassesCount = 2 });

            var chars = new CharTracker(spells);
            chars.HandleEvent(new LogCastingEvent() { Source = "Rumstil", Spell = "Invis", Type = CastingType.Spell });
            Assert.Null(chars.Get("Rumstil")?.Class);
        }

        [Fact]
        public void Spell_Click_Shouldnt_Assign_Class()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo() { Name = "Super Fire Arrow", ClassesMask = (int)ClassesMaskShort.RNG, ClassesCount = 1 });

            var chars = new CharTracker(spells);
            chars.HandleEvent(new LogCastingEvent() { Source = "Rumstil", Spell = "Super Fire Arrow", Type = CastingType.Spell });
            // click/procs (which are never rank 2/3) can misidentify a class
            Assert.Null(chars.Get("Rumstil")?.Class);
        }

        [Fact]
        public void Spell_Rank_2_Should_Assign_Class()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo() { Name = "Super Fire Arrow Rk. II", ClassesMask = (int)ClassesMaskShort.RNG, ClassesCount = 1 });

            var chars = new CharTracker(spells);
            chars.HandleEvent(new LogCastingEvent() { Source = "Rumstil", Spell = "Super Fire Arrow Rk. II", Type = CastingType.Spell });
            Assert.Equal("RNG", chars.Get("Rumstil")?.Class);
        }

        [Fact]
        public void ExportPlayers()
        {
            var spells = new FakeSpellParser();
            var chars = new CharTracker(spells);
            var today = DateTime.Today;
            
            var red = chars.GetOrAdd("Red");
            red.IsPlayer = true;
            red.UpdatedOn = today;
            
            var blue = chars.GetOrAdd("Blue");
            blue.UpdatedOn = today;
            
            var green = chars.GetOrAdd("Green");
            green.IsPlayer = true;
            green.Class = "CLR";
            green.UpdatedOn = today;

            var spot = chars.GetOrAdd("Spot");
            spot.Owner = "Green";
            spot.UpdatedOn = today;

            var s = chars.ExportPlayers();
            Assert.Equal($"Red:::{today:yyyy-MM-dd};Green:CLR::{today:yyyy-MM-dd};Spot::Green:{today:yyyy-MM-dd};", s);
        }

        [Fact]
        public void ImportPlayers()
        {
            var spells = new FakeSpellParser();
            var chars = new CharTracker(spells);
            var today = DateTime.Today;
            chars.ImportPlayers($"Red:::{today:yyyy-MM-dd};Green:CLR::{today:yyyy-MM-dd};Spot::Green:{today:yyyy-MM-dd};");

            var red = chars.Get("Red");
            Assert.NotNull(red);
            Assert.True(red.IsPlayer);
            Assert.Null(red.Class);
            Assert.Equal(today, red.UpdatedOn);

            var green = chars.Get("Green");
            Assert.NotNull(green);
            Assert.True(green.IsPlayer);
            Assert.Equal("CLR", green.Class);
            Assert.Equal(today, green.UpdatedOn);

            var spot = chars.Get("Spot");
            Assert.NotNull(spot);
            Assert.False(spot.IsPlayer);
            Assert.Null(spot.Class);
            Assert.Equal("Green", spot.Owner);
            Assert.Equal(today, spot.UpdatedOn);
        }

        [Fact]
        public void ImportPlayers_Skip_Stale()
        {
            var spells = new FakeSpellParser();
            var chars = new CharTracker(spells);
            var today = DateTime.Today;
            chars.ImportPlayers($"Red:CLR::{today:yyyy-MM-dd};Green:CLR::{today.AddDays(-50):yyyy-MM-dd}");
            Assert.NotNull(chars.Get("Red"));
            Assert.Null(chars.Get("Green"));
        }

        /*
        // this test will only work if we use an hardcoded spell exclusion list
        [Fact]
        public void Spell_Shouldnt_Match()
        {
            var spells = new FakeSpellParser();
            // Lure of Ice is a level 60 WIZ spell and also appears as a melee weapon proc
            spells.Spells.Add(new SpellInfo() { Name = "Lure of Ice", ClassesMask = (int)ClassesMaskShort.WIZ, ClassesCount = 1 });

            var chars = new CharTracker(spells);
            chars.HandleEvent(new LogCastingEvent() { Source = "Rumstil", Spell = "Lure of Ice" });
            Assert.Null(chars.GetClass("Rumstil"));
        }
        */
    }
}
