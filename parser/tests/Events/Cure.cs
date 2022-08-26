using EQLogParser;
using Xunit;


namespace EQLogParserTests.Event
{
    public class LogCureEventTests
    {
        const string PLAYER = "Bob";

        private LogCureEvent Parse(string text)
        {
            return LogCureEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Other()
        {
            var cure = Parse("Griklor the Restless is cured of Hemorrhagic Venom Rk. III by Griklor the Restless.");
            Assert.NotNull(cure);
            Assert.Equal("Griklor the Restless", cure.Source);
            Assert.Equal("Griklor the Restless", cure.Target);
            Assert.Equal("Hemorrhagic Venom Rk. III", cure.Spell);
        }

        [Fact]
        public void Parse_Self()
        {
            var cure = Parse("You are cured of Mists of Enlightenment by Buffy.");
            Assert.NotNull(cure);
            Assert.Equal("Buffy", cure.Source);
            Assert.Equal(PLAYER, cure.Target);
            Assert.Equal("Mists of Enlightenment", cure.Spell);
        }

    }
}
