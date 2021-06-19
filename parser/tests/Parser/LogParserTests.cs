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
        public void ParseLine_Player_Not_Set()
        {
            var parser = new LogParser();
            Assert.Throws<InvalidOperationException>(() => { parser.ParseLine("test"); });
        }

        [Fact]
        public void ParseLine_Invalid()
        {
            var parser = new LogParser();
            parser.Player = "X";
            var e = parser.ParseLine("test");
            Assert.Null(e);
        }

        [Fact]
        public void ParseLine_Valid()
        {
            var parser = new LogParser();
            parser.Player = "X";
            var e = parser.ParseLine("[Sun Jul 05 20:31:58 2020] Welcome to EverQuest!");
            Assert.NotNull(e);
        }


    }
}
