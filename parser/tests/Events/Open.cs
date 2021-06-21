using EQLogParser;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogOpenEventTests
    {
        [Fact]
        public void GetPlayerFromFileName()
        {
            // official format
            Assert.Equal("Rumstil", LogOpenEvent.GetPlayerFromFileName("eqlog_Rumstil_erollisi.txt"));

            // gzipped
            Assert.Equal("Rumstil", LogOpenEvent.GetPlayerFromFileName("eqlog_Rumstil_erollisi.txt.gz"));

            // in case we really want to be permissive with renamed files
            Assert.Equal("Larry", LogOpenEvent.GetPlayerFromFileName("eqlog_Larry_erollisi-2020-07-05.txt"));
            Assert.Equal("Curly", LogOpenEvent.GetPlayerFromFileName("eqlog_Curly-stuff.txt"));
            Assert.Equal("Moe", LogOpenEvent.GetPlayerFromFileName("eqlog_Moe.txt"));
        }

        [Fact]
        public void GetServerFromFileName()
        {
            Assert.Equal("erollisi", LogOpenEvent.GetServerFromFileName("eqlog_Rumstil_erollisi.txt"));
        }

    }
}
