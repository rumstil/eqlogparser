using Xunit;

namespace EQLogParser
{
    public class LogWhoEventTests
    {

        private LogWhoEvent Parse(string text)
        {
            return LogWhoEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse()
        {
            LogWhoEvent who = null;

            // anon/role
            who = Parse("[ANONYMOUS] Rumstil");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
            Assert.Null(who.Class);

            // basic class name
            who = Parse("[1 Shadow Knight] Scary (Froglok)  ZONE: bazaar");
            Assert.NotNull(who);
            Assert.Equal("Scary", who.Name);
            Assert.Equal("Shadow Knight", who.Class);
            Assert.Equal(1, who.Level);

            // level based class name (basic class name)
            who = Parse("[105 Huntmaster (Ranger)] Rumstil (Halfling)  ZONE: kattacastrumb");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
            Assert.Equal("Ranger", who.Class);
            Assert.Equal(105, who.Level);

            // afk/trader
            who = Parse(" AFK [105 Huntmaster (Ranger)] Rumstil (Halfling)  ZONE: kattacastrumb");
            Assert.NotNull(who);
            Assert.Equal("Rumstil", who.Name);
            Assert.Equal("Ranger", who.Class);
            Assert.Equal(105, who.Level);
        }

    }
}
