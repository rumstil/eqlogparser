using EQLogParser;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace EQLogParserTests.Parser
{
    public class LogParserTests
    {
        [Fact]
        public void GetPlayerFromFileName()
        {
            // official format
            Assert.Equal("Rumstil", LogParser.GetPlayerFromFileName("eqlog_Rumstil_erollisi.txt"));

            // gzipped
            Assert.Equal("Rumstil", LogParser.GetPlayerFromFileName("eqlog_Rumstil_erollisi.txt.gz"));

            // in case we really want to be permissive with renamed files
            Assert.Equal("Larry", LogParser.GetPlayerFromFileName("eqlog_Larry_erollisi-2020-07-05.txt"));
            Assert.Equal("Curly", LogParser.GetPlayerFromFileName("eqlog_Curly-stuff.txt"));
            Assert.Equal("Moe", LogParser.GetPlayerFromFileName("eqlog_Moe.txt"));
        }

        [Fact]
        public void GetServerFromFileName()
        {
            Assert.Equal("erollisi", LogParser.GetServerFromFileName("eqlog_Rumstil_erollisi.txt"));
        }
    }
}
