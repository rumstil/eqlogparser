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
        const string PET2 = "Rex";

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
        public void MergePets_Single()
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

        [Fact]
        public void MergePets_Multiple()
        {
            var fs = new FightInfo(MOB1);

            var p1 = new FightParticipant() { Name = PLAYER1, OutboundHitSum = 100 };
            fs.Participants.Add(p1);
            var pet1 = new FightParticipant() { Name = PET1, OutboundHitSum = 50, AttackTypes = new List<FightHit> { new FightHit() { Type = "kick", HitCount = 1, HitSum = 50 } }, PetOwner = PLAYER1 };
            fs.Participants.Add(pet1);
            var pet2 = new FightParticipant() { Name = PET2, OutboundHitSum = 60, AttackTypes = new List<FightHit> { new FightHit() { Type = "kick", HitCount = 1, HitSum = 60 } }, PetOwner = PLAYER1 };
            fs.Participants.Add(pet2);

            fs.MergePets();

            var ap1 = fs.Participants.Find(x => x.Name == PLAYER1);
            Assert.Equal(210, ap1.OutboundHitSum);

            var ap1kick = ap1.AttackTypes.Find(x => x.Type == "pet:kick");
            Assert.NotNull(ap1kick);
            Assert.Equal(2, ap1kick.HitCount);
            Assert.Equal(110, ap1kick.HitSum);

            var apet1 = fs.Participants.Find(x => x.Name == PET1);
            Assert.Equal(0, apet1.OutboundHitSum);

            var apet2 = fs.Participants.Find(x => x.Name == PET2);
            Assert.Equal(0, apet2.OutboundHitSum);
        }

        [Fact]
        public void Merge_DPS_Intervals_For_Target_With_Gap()
        {
            var a = new FightInfo();
            a.StartedOn = DateTime.Parse("11:01:05");
            a.UpdatedOn = a.StartedOn.AddSeconds(14);
            a.Target = new FightParticipant() { Name = "Mob1", DPS = new List<int> { 0, 3, 0 } };

            // second fight starts with a gap after the first fight
            var b = new FightInfo();
            b.Target = new FightParticipant() { Name = "Mob2", DPS = new List<int> { 7, 23 } };
            b.StartedOn = a.StartedOn.AddMinutes(1);
            b.UpdatedOn = b.StartedOn.AddSeconds(12);

            // act
            var total = new MergedFightInfo();
            total.Merge(a);
            total.Merge(b);
            total.Finish();

            // assert
            var t = total.Target;
            // :00 to :05, :06 to :11, :12 to :17, :18 to :23, gap should be ignored, :00 to :05, :06 to :11
            Assert.Equal(new[] { 0, 3, 0, 0, 7, 23 }, t.DPS);
        }

        [Fact]
        public void Merge_DPS_Intervals_For_Participant_With_Gap()
        {
            var a = new FightInfo();
            a.StartedOn = DateTime.Parse("11:01:05");
            a.UpdatedOn = a.StartedOn.AddSeconds(14);
            a.Target = new FightParticipant() { Name = "Mob1" };
            a.Participants.Add(new FightParticipant() { Name = "Player1", DPS = new List<int> { 0, 3, 0 } });
            a.Participants.Add(new FightParticipant() { Name = "Player2", DPS = new List<int> { 2, 6, 3 } });

            // second fight starts with a gap after the first fight
            var b = new FightInfo();
            b.Target = new FightParticipant() { Name = "Mob2" };
            b.StartedOn = a.StartedOn.AddMinutes(1);
            b.UpdatedOn = b.StartedOn.AddSeconds(12);
            b.Participants.Add(new FightParticipant() { Name = "Player1", DPS = new List<int> { 7, 23 } });

            // act
            var total = new MergedFightInfo();
            total.Merge(a);
            total.Merge(b);
            total.Finish();

            // assert
            Assert.Equal(2, total.Participants.Count);
            var p = total.Participants[0];
            // :00 to :05, :06 to :11, :12 to :17, :18 to :23, gap should be ignored, :00 to :05, :06 to :11
            Assert.Equal(new[] { 0, 3, 0, 0, 7, 23 }, p.DPS);
        }

        [Fact]
        public void Merge_DPS_Intervals_For_Participant_With_Overlap()
        {
            var a = new FightInfo();
            a.StartedOn = DateTime.Parse("11:01:05");
            a.UpdatedOn = a.StartedOn.AddSeconds(14);
            a.Target = new FightParticipant() { Name = "Mob1" };
            a.Participants.Add(new FightParticipant() { Name = "Player1", DPS = new List<int> { 0, 3, 0 } });
            a.Participants.Add(new FightParticipant() { Name = "Player2", DPS = new List<int> { 2, 6, 3 } });

            // second fight starts before the first is finished
            var b = new FightInfo();
            b.Target = new FightParticipant() { Name = "Mob2" };
            b.StartedOn = a.StartedOn.AddSeconds(2);
            b.UpdatedOn = b.StartedOn.AddSeconds(12);
            b.Participants.Add(new FightParticipant() { Name = "Player1", DPS = new List<int> { 7, 23 } });

            // act
            var total = new MergedFightInfo();
            total.Merge(a);
            total.Merge(b);
            total.Finish();

            // assert
            Assert.Equal(2, total.Participants.Count);
            var p = total.Participants[0];
            // :00 to :05, :06 to :11, :12 to :17
            Assert.Equal(new[] { 0, 10, 23 }, p.DPS);
        }

        [Fact(Skip = "Buff merging disabled because it currently creates duplicates")]
        public void Merge_Buffs_With_Gap()
        {
            var a = new FightInfo();
            a.StartedOn = DateTime.Parse("11:01:05");
            a.UpdatedOn = a.StartedOn.AddSeconds(14); // 4 intervals: 0..5, 6..11, 12..15, 16..19
            a.Target = new FightParticipant() { Name = "Mob1" };
            var ap1 = new FightParticipant() { Name = "Player1", OutboundHitSum = 1 };
            ap1.Buffs.Add(new FightBuff { Name = "Super Speed", Time = 2 });
            a.Participants.Add(ap1);

            // second fight starts with a gap after the first fight
            var b = new FightInfo();
            b.Target = new FightParticipant() { Name = "Mob2" };
            b.StartedOn = a.StartedOn.AddMinutes(1);
            b.UpdatedOn = b.StartedOn.AddSeconds(12);
            var bp1 = new FightParticipant() { Name = "Player1", OutboundHitSum = 1 };
            bp1.Buffs.Add(new FightBuff { Name = "Super Speed", Time = 3 });
            b.Participants.Add(bp1);

            // act
            var total = new MergedFightInfo();
            total.Merge(a);
            total.Merge(b);
            total.Finish();

            // assert
            Assert.Single(total.Participants);
            var p = total.Participants[0];
            Assert.Equal(2, p.Buffs.Count);
            Assert.Equal(2, p.Buffs[0].Time);
            Assert.Equal(27, p.Buffs[1].Time); // 24 from 4*6 intervals in first fight + 3 second offset
        }

    }
}
