using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogDeathEventTests
    {
        const string PLAYER = "Bob";

        private LogDeathEvent Parse(string text)
        {
            return LogDeathEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Self_KillShot()
        {
            var dead = Parse("You have slain A slag golem!");
            Assert.NotNull(dead);
            Assert.Equal(PLAYER, dead.KillShot);
            Assert.Equal("A slag golem", dead.Name);
        }

        [Fact]
        public void Parse_Self_Died()
        {
            var dead = Parse("You have been slain by A sneaky escort!");
            Assert.NotNull(dead);
            Assert.Equal("A sneaky escort", dead.KillShot);
            Assert.Equal(PLAYER, dead.Name);
        }

        [Fact]
        public void Parse_Other()
        {
            var dead = Parse("Rumstil has been slain by A supply guardian!");
            Assert.NotNull(dead);
            Assert.Equal("A supply guardian", dead.KillShot);
            Assert.Equal("Rumstil", dead.Name);
        }

        [Fact]
        public void Parse_No_KillShot()
        {
            var dead = Parse("A loyal reaver died.");
            Assert.NotNull(dead);
            Assert.Null(dead.KillShot);
            Assert.Equal("A loyal reaver", dead.Name);
        }

    }
}
