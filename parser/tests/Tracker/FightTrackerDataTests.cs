using EQLogParser;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace EQLogParserTests.Tracker
{
    public class FightTrackerDataTests
    {
        const string MOB1 = "a skeleton";
        const string PLAYER1 = "Fred";

        [Fact]
        public void Flags_Riposte()
        {
            var fs = new FightInfo(MOB1);

            fs.AddHit(new LogHitEvent { Source = PLAYER1, Target = MOB1, Mod = LogEventMod.Riposte | LogEventMod.Strikethrough | LogEventMod.Lucky, Amount = 1 });

            var p = fs.Participants[0];
            Assert.Equal(1, fs.Target.InboundRiposteSum);
        }

        [Fact]
        public void Flags_Strikethrough()
        {
            var fs = new FightInfo(MOB1);

            fs.AddHit(new LogHitEvent { Source = PLAYER1, Target = MOB1, Mod = LogEventMod.Riposte | LogEventMod.Strikethrough | LogEventMod.Lucky, Amount = 1 });

            var p = fs.Participants[0];
            Assert.Equal(1, p.OutboundStrikeCount);
        }

        [Fact]
        public void Flags_Critical()
        {
            var fs = new FightInfo(MOB1);

            fs.AddHit(new LogHitEvent { Source = PLAYER1, Target = MOB1, Type = "slash", Mod = LogEventMod.Strikethrough | LogEventMod.Lucky, Amount = 1 });
            fs.AddHit(new LogHitEvent { Source = PLAYER1, Target = MOB1, Type = "slash", Mod = LogEventMod.Critical | LogEventMod.Strikethrough | LogEventMod.Lucky, Amount = 3 });

            var p = fs.Participants[0];
            Assert.Equal(1, p.AttackTypes[0].CritCount);
            Assert.Equal(3, p.AttackTypes[0].CritSum);
        }
    }
}
