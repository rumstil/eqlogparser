using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogSkillEventTests
    {
        private LogSkillEvent Parse(string text)
        {
            return LogSkillEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_Normal()
        {
            LogSkillEvent skill = null;

            skill = Parse("You have become better at Specialize Divination! (53)");
            Assert.Equal("Specialize Divination", skill.Name);
            Assert.Equal(53, skill.Level);
        }

    }
}
