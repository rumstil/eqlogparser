using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogRescuedEventTests
    {
        const string PLAYER = "Bob";

        private LogRescuedEvent Parse(string text)
        {
            return LogRescuedEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_DI()
        {
            var rescue = Parse("Rumstil has been rescued by divine intervention!");
            Assert.NotNull(rescue);
            Assert.Equal("Rumstil", rescue.Target);
        }

    }
}
