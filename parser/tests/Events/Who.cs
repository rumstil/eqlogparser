using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogWhoEventTests
    {

        private LogWhoEvent Parse(string text)
        {
            return LogWhoEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_Anon()
        {
            // anon/role
            var who = Parse("[ANONYMOUS] Rumstil");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
            Assert.Null(who.Class);
        }

        [Fact]
        public void Parse_Normal()
        {
            // basic class name
            var who = Parse("[1 Shadow Knight] Scary (Froglok)  ZONE: bazaar");
            Assert.NotNull(who);
            Assert.Equal("Scary", who.Name);
            Assert.Equal("SHD", who.Class);
            Assert.Equal(1, who.Level);

            // level based class name (basic class name)
            who = Parse("[105 Huntmaster (Ranger)] Rumstil (Halfling)  ZONE: kattacastrumb");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
            Assert.Equal("RNG", who.Class);
            Assert.Equal(105, who.Level);
        }


        [Fact]
        public void Parse_Prefixed()
        {
            // afk
            var who = Parse(" AFK [105 Huntmaster (Ranger)] Rumstil (Halfling)  ZONE: kattacastrumb");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
            Assert.Equal("RNG", who.Class);
            Assert.Equal(105, who.Level);

            // trader
            who = Parse(" TRADER[1 Enchanter] Buymystuff (High Elf)");
            Assert.NotNull(who);
            Assert.Equal("Buymystuff", who.Name);
            Assert.Equal("ENC", who.Class);
            Assert.Equal(1, who.Level);
        }

        [Fact]
        public void Parse_Target()
        {
            var who = Parse("Targeted (Player): Rumstil");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
        }

        [Fact]
        public void Parse_Target_NPC()
        {
            var who = Parse("Targeted (NPC): Rumstil");
            Assert.Null(who);
        }

    }
}
