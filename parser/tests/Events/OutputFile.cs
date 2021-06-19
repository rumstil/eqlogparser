using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogOutputFileEventTests
    {
        private LogOutputFileEvent Parse(string text)
        {
            return LogOutputFileEvent.Parse(new LogRawEvent(text));
        }

        [Fact]
        public void Parse_Name()
        {
            var file = Parse("Outputfile Complete: RaidRoster_erollisi-20200802-190436.txt");
            Assert.NotNull(file);
            Assert.Equal("RaidRoster_erollisi-20200802-190436.txt", file.FileName);
        }

        [Fact]
        public void Parse_Name_With_Spaces()
        {
            var file = Parse("Outputfile Complete: Derelict Space Toilet-20200802-190441.txt");
            Assert.NotNull(file);
            Assert.Equal("Derelict Space Toilet-20200802-190441.txt", file.FileName);
        }

    }
}
