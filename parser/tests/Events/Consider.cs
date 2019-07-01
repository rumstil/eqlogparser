using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogConEventTests
    {
        private LogConEvent Parse(string text)
        {
            return LogConEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_Normal()
        {
            var con = Parse("An iksar alchemist scowls at you, ready to attack -- You could probably win this fight. (Lvl: 82)");
            Assert.NotNull(con);
            Assert.Equal("An iksar alchemist", con.Name);
            Assert.Equal("scowls at you, ready to attack", con.Faction);
            Assert.Equal(82, con.Level);
            Assert.False(con.Rare);
        }

        [Fact]
        public void Parse_Rare()
        {
            var con = Parse("Roon - </c><c \"#E1B511\">a rare creature</c><c \"#00F0F0\"> - scowls at you, ready to attack -- looks kind of dangerous. (Lvl: 104)");
            Assert.NotNull(con);
            Assert.Equal("Roon", con.Name);
            Assert.Equal("scowls at you, ready to attack", con.Faction);
            Assert.Equal(104, con.Level);
            Assert.True(con.Rare);
        }

    }
}
