using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogXPEventTests
    {
        private LogXPEvent Parse(string text)
        {
            return LogXPEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_With_Amount()
        {
            var xp = Parse("You gain party experience! (0.009%)");
            Assert.NotNull(xp);
            Assert.Equal(0.009M, xp.Amount);
            Assert.Equal(XPType.GroupXP, xp.Type);
        }

        [Fact]
        public void Parse_Obsolete()
        {
            var xp = Parse("You gain experience!");
            Assert.NotNull(xp);
            Assert.Equal(XPType.SoloXP, xp.Type);

            xp = Parse("You gain party experience!");
            Assert.NotNull(xp);
            Assert.Equal(XPType.GroupXP, xp.Type);

            xp = Parse("You gain party experience (with a bonus)!");
            Assert.NotNull(xp);
            Assert.Equal(XPType.GroupXP, xp.Type);

            xp = Parse("You gained raid experience!");
            Assert.NotNull(xp);
            Assert.Equal(XPType.RaidXP, xp.Type);

            xp = Parse("You gained raid experience (with a bonus)!");
            Assert.NotNull(xp);
            Assert.Equal(XPType.RaidXP, xp.Type);
        }


    }
}
