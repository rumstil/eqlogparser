using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogTauntEventTests
    {
        const string PLAYER = "Bob";

        private LogTauntEvent Parse(string text)
        {
            return LogTauntEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Single()
        {
            var taunt = Parse("Rumstil has captured Master Yael's attention!");
            Assert.NotNull(taunt);
            Assert.Equal("Rumstil", taunt.Source);
            Assert.Equal("Master Yael", taunt.Target);
        }

        [Fact]
        public void Parse_Partial()
        {
            var taunt = Parse("Rumstil was partially successful in capturing Cazic-Thule's attention.");
            Assert.NotNull(taunt);
            Assert.Equal("Rumstil", taunt.Source);
            Assert.Equal("Cazic-Thule", taunt.Target);
        }

        [Fact]
        public void Parse_Critical()
        {
            var taunt = Parse("Rumstil has captured a time vortex's attention with an unparalleled approach!");
            Assert.NotNull(taunt);
            Assert.Equal("Rumstil", taunt.Source);
            Assert.Equal("A time vortex", taunt.Target);

            taunt = Parse("You capture a mortiferous golem's attention with your unparalleled reproach!");
            Assert.NotNull(taunt);
            Assert.Equal(PLAYER, taunt.Source);
            Assert.Equal("A mortiferous golem", taunt.Target);
        }

        [Fact]
        public void Parse_AE()
        {
            var taunt = Parse("Rumstil captures the attention of everything in the area!");
            Assert.NotNull(taunt);
            Assert.Equal("Rumstil", taunt.Source);
            Assert.Null(taunt.Target);
        }

    }
}
