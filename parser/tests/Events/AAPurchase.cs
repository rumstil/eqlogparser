using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogAAPurchaseEventTests
    {
        private LogAAPurchaseEvent Parse(string text)
        {
            return LogAAPurchaseEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_First()
        {
            LogAAPurchaseEvent aa = null;

            aa = Parse("You have gained the ability \"Veteran's Wrath\" at a cost of 3 ability points.");
            Assert.Equal("Veteran's Wrath", aa.Name);
            Assert.Equal(3, aa.Cost);
        }

        [Fact]
        public void Parse_Improved()
        {
            LogAAPurchaseEvent aa = null;

            aa = Parse("You have improved Friendly Stasis 27 at a cost of 0 ability points.");
            Assert.Equal("Friendly Stasis 27", aa.Name);
            Assert.Equal(0, aa.Cost);
        }

    }
}
