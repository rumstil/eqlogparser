using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogCastingEventTests
    {
        const string PLAYER = "Bob";

        private LogCastingEvent Parse(string text)
        {
            return LogCastingEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Self()
        {
            var cast = Parse("You begin casting Group Perfected Invisibility.");
            Assert.NotNull(cast);
            Assert.Equal(PLAYER, cast.Source);
            Assert.Equal("Group Perfected Invisibility", cast.Spell);
            Assert.Equal(CastingType.Spell, cast.Type);
        }

        [Fact]
        public void Parse_Other()
        {
            var cast = Parse("Saity begins casting Promised Remedy Rk. II.");
            Assert.NotNull(cast);
            Assert.Equal("Saity", cast.Source);
            Assert.Equal("Promised Remedy Rk. II", cast.Spell);
            Assert.Equal(CastingType.Spell, cast.Type);
        }

        [Fact]
        public void Parse_Other_Obsolete()
        {
            var cast = Parse("a woundhealer goblin begins to cast a spell. <Inner Fire>");
            Assert.NotNull(cast);
            Assert.Equal("A woundhealer goblin", cast.Source);
            Assert.Equal("Inner Fire", cast.Spell);
            Assert.Equal(CastingType.Spell, cast.Type);
        }

        [Fact]
        public void Parse_Song_Self()
        {
            var cast = Parse("You begin singing Requiem of Time.");
            Assert.NotNull(cast);
            Assert.Equal(PLAYER, cast.Source);
            Assert.Equal("Requiem of Time", cast.Spell);
            Assert.Equal(CastingType.Song, cast.Type);
        }

        [Fact]
        public void Parse_Song_Other()
        {
            var cast = Parse("Celine begins singing Requiem of Time.");
            Assert.NotNull(cast);
            Assert.Equal("Celine", cast.Source);
            Assert.Equal("Requiem of Time", cast.Spell);
            Assert.Equal(CastingType.Song, cast.Type);
        }

        [Fact]
        public void Parse_Song_Other_Obsolete()
        {
            var cast = Parse("Celine begins to sing a song. <Requiem of Time>");
            Assert.NotNull(cast);
            Assert.Equal("Celine", cast.Source);
            Assert.Equal("Requiem of Time", cast.Spell);
            Assert.Equal(CastingType.Song, cast.Type);
        }

        [Fact]
        public void Parse_Disc_Self()
        {
            var cast = Parse("You activate Weapon Shield Discipline.");
            Assert.NotNull(cast);
            Assert.Equal(PLAYER, cast.Source);
            Assert.Equal("Weapon Shield Discipline", cast.Spell);
            Assert.Equal(CastingType.Disc, cast.Type);
        }

        [Fact]
        public void Parse_Disc_Other()
        {
            var cast = Parse("Rumstil activates Weapon Shield Discipline.");
            Assert.NotNull(cast);
            Assert.Equal("Rumstil", cast.Source);
            Assert.Equal("Weapon Shield Discipline", cast.Spell);
            Assert.Equal(CastingType.Disc, cast.Type);
        }

    }
}
