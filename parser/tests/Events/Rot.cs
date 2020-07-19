using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogRotEventTests
    {
        const string PLAYER = "Bob";

        private LogRotEvent Parse(string text)
        {
            return LogRotEvent.Parse(new LogRawEvent(text) { Player = PLAYER });
        }

        [Fact]
        public void Parse_Rot()
        {
            var loot = Parse("No one was interested in the 1 item(s): Glowing Sebilisian Boots. These items can be randomed again or will be available to everyone after the corpse unlocks.");
            Assert.NotNull(loot);
            Assert.Equal("Glowing Sebilisian Boots", loot.Item);
            //Assert.Equal(1, loot.Qty);
        }
    }
}
