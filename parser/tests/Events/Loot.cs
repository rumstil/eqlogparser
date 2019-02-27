using Xunit;

namespace EQLogParser
{
    public class LogLootEventTests
    {
        private LogLootEvent Parse(string text)
        {
            return LogLootEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse()
        {
            var loot = Parse("--Rumstil has looted a Alluring Flower.--");
            Assert.NotNull(loot);
            Assert.Equal("Rumstil", loot.Looter);
            Assert.Equal("a Alluring Flower", loot.Item);
        }

    }
}
