using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogDiceRollEventTests
    {
        const string PLAYER = "Bob";

        private LogDiceRollEvent Parse(string text)
        {
            return LogDiceRollEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Roll()
        {
            var roll = Parse("**A Magic Die is rolled by Rumstil. It could have been any number from 0 to 1000, but this time it turned up a 775.");
            Assert.NotNull(roll);
            Assert.Equal("Rumstil", roll.Source);
            Assert.Equal(0, roll.Min);
            Assert.Equal(1000, roll.Max);
            Assert.Equal(775, roll.Roll);
        }

    }
}
