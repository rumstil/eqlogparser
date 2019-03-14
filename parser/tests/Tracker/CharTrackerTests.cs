using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace EQLogParser
{
    public class PlayerTrackerTests
    {
        [Fact]
        public void GetFoe()
        {
            var chars = new CharTracker();
            chars.HandleEvent(new LogWhoEvent() { Name = "Rumstil" });
            Assert.Equal("a mosquito", chars.GetFoe("Rumstil", "a mosquito"));
            Assert.Equal("a mosquito", chars.GetFoe("a mosquito", "Rumstil"));
        }

        [Fact]
        public void GetType_Pet_Same_As_Owner()
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
