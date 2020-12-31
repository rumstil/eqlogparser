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
        const string PLAYER2 = "Derf";
        const string PET1 = "Xibaber";

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

        [Fact]
        public void MergePets()
        {
            var fs = new FightInfo(MOB1);

            var p1 = new FightParticipant() { Name = PLAYER1, OutboundHitSum = 100 };
            fs.Participants.Add(p1);
            var p2 = new FightParticipant() { Name = PLAYER2, OutboundHitSum = 110 };
            fs.Participants.Add(p2);
            var pet1 = new FightParticipant() { Name = PET1, OutboundHitSum = 50, PetOwner = PLAYER1 };
            fs.Participants.Add(pet1);

            fs.MergePets();

            var ap1 = fs.Participants.Find(x => x.Name == PLAYER1);
            Assert.Equal(150, ap1.OutboundHitSum);
            var ap2 = fs.Participants.Find(x => x.Name == PLAYER2);
            Assert.Equal(110, ap2.OutboundHitSum);
            var apet1 = fs.Participants.Find(x => x.Name == PET1);
            Assert.Equal(0, apet1.OutboundHitSum);
        }

    }
}
