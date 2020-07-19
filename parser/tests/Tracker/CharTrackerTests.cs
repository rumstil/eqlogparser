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
        public void Player_Friend()
        {
            var chars = new CharTracker();
            // "/who" should always tag players
            chars.HandleEvent(new LogWhoEvent() { Name = "Rumstil" });
            Assert.Equal("a mosquito", chars.GetFoe("Rumstil", "a mosquito"));
            Assert.Equal("a mosquito", chars.GetFoe("a mosquito", "Rumstil"));
        }

        [Fact]
        public void Healer_May_Be_Foe()
        {
            // mobs can be healers too, a heal shouldn't flag the source as a friend (unless the target is a player)
            var chars = new CharTracker();
            chars.HandleEvent(new LogHealEvent() { Source = "froglok jin shaman", Target = "froglok dar knight", Amount = 3, Spell = "Doomscale Focusing" });
            Assert.Equal(CharType.Unknown, chars.GetType("froglok jin shaman"));
            Assert.Equal(CharType.Unknown, chars.GetType("froglok dar knight"));
        }

        [Fact]
        public void Pet_Owner_Status_Propogates_To_Pet()
        {
            var chars = new CharTracker();

            Assert.Equal(CharType.Unknown, chars.GetType("Rumstil`s pet"));
            Assert.Equal(CharType.Unknown, chars.GetType("Rumstil`s warder"));

            chars.HandleEvent(new LogWhoEvent() { Name = "Rumstil" });
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil`s pet"));
            Assert.Equal(CharType.Friend, chars.GetType("Rumstil`s warder"));

            Assert.Equal(CharType.Unknown, chars.GetType("Xantik"));
            chars.HandleEvent(new LogChatEvent() { Source = "Xantik", Message = "My leader is Rumstil." });
            Assert.Equal(CharType.Friend, chars.GetType("Xantik"));
        }

        [Fact]
        public void GetOwner()
        {
            var chars = new CharTracker();
            Assert.Null(chars.GetOwner("Rumstil"));

            // `pet and `warder are always assigned to the obvious owner
            Assert.Equal("Rumstil", chars.GetOwner("Rumstil`s pet"));
            Assert.Equal("Rumstil", chars.GetOwner("Rumstil`s warder"));

            // other pets need to be announced first
            Assert.Null(chars.GetOwner("Xantik"));
            chars.HandleEvent(new LogChatEvent() { Source = "Xantik", Message = "My leader is Rumstil." });
            Assert.Equal("Rumstil", chars.GetOwner("Xantik"));
        }

        [Fact]
        public void IsPetName()
        {
            Assert.True(CharTracker.IsPetName("Xantik"));
            Assert.False(CharTracker.IsPetName("Spot"));
        }

    }
}
