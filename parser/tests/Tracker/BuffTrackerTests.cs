using EQLogParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;


namespace EQLogParserTests.Tracker
{
    public class BuffTrackerTests
    {
        const string PLAYER = "Bob";

        [Fact]
        public void Land_Self()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo { Name = "Illusions of Grandeur I", LandSelf = "Illusions of Grandeur fill your mind.", LandOthers = " is consumed by Illusions of Grandeur." });

            var buffs = new BuffTracker(spells);
            buffs.HandleEvent(new LogRawEvent() { Text = "Illusions of Grandeur fill your mind.", Timestamp = DateTime.UtcNow, Player = PLAYER });

            var list = buffs.Get(PLAYER, DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Illusions of Grandeur", list[0].Name);
        }

        [Fact]
        public void Land_Other()
        {
            var spells = new FakeSpellParser();
            spells.Spells.Add(new SpellInfo { Name = "Illusions of Grandeur I", LandSelf = "Illusions of Grandeur fill your mind.", LandOthers = " is consumed by Illusions of Grandeur." });

            var buffs = new BuffTracker(spells);
            buffs.HandleEvent(new LogRawEvent() { Text = "Tokiel is consumed by Illusions of Grandeur.", Timestamp = DateTime.UtcNow });

            var list = buffs.Get("Tokiel", DateTime.Today, DateTime.UtcNow.AddSeconds(1)).ToList();
            Assert.Single(list);
            Assert.Equal("Illusions of Grandeur", list[0].Name);
        }


    }
}
