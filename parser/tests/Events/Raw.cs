using EQLogParser;
using System;
using Xunit;

namespace EQLogParserTests.Event
{
    public class LogRawEventTests
    {
        [Fact]
        public void Parse_Valid()
        {
            var raw = LogRawEvent.Parse("[Wed Feb 20 18:02:00 2019] LOADING, PLEASE WAIT...");
            Assert.NotNull(raw);
            Assert.Equal(new DateTime(2019, 2, 20, 18, 2, 00).ToUniversalTime(), raw.Timestamp);
            Assert.Equal("LOADING, PLEASE WAIT...", raw.Text);
        }

        [Fact]
        public void Parse_Invalid()
        {
            var raw = LogRawEvent.Parse("");
            Assert.Null(raw);

            raw = LogRawEvent.Parse("Wed Feb 20 18:02:00 2019] LOADING, PLEASE WAIT...");
            Assert.Null(raw);
        }


    }
}
