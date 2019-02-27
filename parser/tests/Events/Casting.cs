using Xunit;

namespace EQLogParser
{
    public class LogCastingEventTests
    {
        const string PLAYER = "Bob";

        private LogCastingEvent Parse(string text)
        {
            return LogCastingEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Other()
        {
            var cast = Parse("a woundhealer goblin begins to cast a spell. <Inner Fire>");
            Assert.NotNull(cast);
            Assert.Equal("A woundhealer goblin", cast.Source);
            Assert.Equal("Inner Fire", cast.Spell);
        }

        [Fact]
        public void Parse_Self()
        {
            var cast = Parse("You begin casting Group Perfected Invisibility.");
            Assert.NotNull(cast);
            Assert.Equal(PLAYER, cast.Source);
            Assert.Equal("Group Perfected Invisibility", cast.Spell);
        }

        [Fact]
        public void Parse_Song_Other()
        {
            var cast = Parse("Celine begins to sing a song. <Requiem of Time>");
            Assert.NotNull(cast);
            Assert.Equal("Celine", cast.Source);
            Assert.Equal("Requiem of Time", cast.Spell);
        }
    }
}
