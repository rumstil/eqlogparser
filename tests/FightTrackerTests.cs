using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace EQLogParser
{
    public class FightTrackerTests
    {
        [Fact]
        public void FightHit_TrackOpponent()
        {
            var tracker = new FightTracker();
            tracker.Players.Add("Player1");
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });

            var f = tracker.Fights[0];
            Assert.Equal(f.Opponent.Name, "Mob1");
            Assert.Equal(f.Opponent, f.Participants[0]);
        }

        /*
        [Fact]
        public void FightHit_TrackPlayers()
        {
            var tracker = new FightTracker();

            // Player1 is registered
            // Player2 should be auto tracked because they were attacking the same mob as Player1
            tracker.Players.Add("Player1");

            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player2", Target = "Mob1", Type = "slash", Amount = 350 });

            var f = tracker.Fights[0];
            Assert.Equal(3, f.Participants.Count);
            Assert.Equal("Player1", f.Participants[1].Name);
            Assert.Equal("Player2", f.Participants[2].Name);
        }

        [Fact]
        public void FightHit_TrackDeadPlayer()
        {
            var tracker = new FightTracker();
            tracker.Players.Add("Player1");

            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "dot", Amount = 200 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1's corpse", Target = "Mob1", Type = "dot", Amount = 350 });

            var f = tracker.Fights[0];
            Assert.Equal(2, f.Participants.Count);
            var p = f.Participants[1];
            Assert.Equal("Player1", p.Name);
            Assert.Equal(2, p.SourceHitCount);
            Assert.Equal(550, p.SourceHitSum);
        }
        */

        [Fact]
        public void FightHit_TrackDeadMob()
        {
            var tracker = new FightTracker();
            tracker.Players.Add("Player1");

            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Mob1", Target = "Player1", Type = "dot", Amount = 200 });
            //tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Mob1's corpse", Target = "Player1", Type = "dot", Amount = 350 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Mob1", SourceIsCorpse = true, Target = "Player1", Type = "dot", Amount = 350 });

            Assert.Equal(1, tracker.Fights.Count);
            var f = tracker.Fights[0];
            Assert.Equal(2, f.Participants.Count);
            Assert.Equal(2, f.Opponent.SourceHitCount);
            Assert.Equal(550, f.Opponent.SourceHitSum);
        }

        [Fact]
        public void FightStart()
        {
            Fight f = null;
            
            var tracker = new FightTracker();
            tracker.OnFightStarted += (args) => f = args;
            tracker.Players.Add("Player1");
            //tracker.TrackPlayerFound(new PlayerFoundEvent { Timestamp = DateTime.Now, Name = "Player1" });
            Assert.Equal(0, tracker.Fights.Count);

            f = null;
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player2", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Mob1", Target = "Player1", Type = "slash", Amount = 100 });
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(1, tracker.ActiveFights.Count);
            Assert.NotNull(f);

            f = null;
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob2", Type = "slash", Amount = 100 });
            Assert.Equal(2, tracker.Fights.Count);
            Assert.Equal(2, tracker.ActiveFights.Count);
            Assert.NotNull(f);
        }

        [Fact]
        public void FightFinish_Death()
        {
            Fight f = null;
            var tracker = new FightTracker();
            tracker.OnFightFinished += (args) => f = args;

            tracker.Players.Add("Player1");
            //tracker.TrackPlayerFound(new PlayerFoundEvent { Timestamp = DateTime.Now, Name = "Player1" });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.TrackDeath(new DeathEvent { Timestamp = DateTime.Now.AddSeconds(1), Name = "Mob1" });

            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Opponent.Name);
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(0, tracker.ActiveFights.Count);
        }

        [Fact]
        public void FightFinish_Timeout()
        {
            Fight f = null;
            var tracker = new FightTracker();
            tracker.OnFightFinished += (args) => f = args;

            tracker.Players.Add("Player1");
            //tracker.TrackPlayerFound(new PlayerFoundEvent { Timestamp = DateTime.Now, Name = "Player1" });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });

            tracker.BeforeTracking(new RawLogEvent { Timestamp = DateTime.Now + tracker.FightTimeout, RawText = "..." });
            //tracker.CheckFightTimeouts();

            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Opponent.Name);
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(0, tracker.ActiveFights.Count);
        }

        [Fact]
        public void FightCrit_BeforeHit()
        {
            var tracker = new FightTracker();
            tracker.Players.Add("Player1");

            tracker.TrackFightCrit(new FightCritEvent { Timestamp = DateTime.Now, Source = "Player1", Amount = 200, Sequence = FightCritEventSequence.BeforeHit });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });

            var f = tracker.Fights[0];
            Assert.Equal(0, f.Participants[1].AttackTypes[0].NormalHitCount);
            Assert.Equal(0, f.Participants[1].AttackTypes[0].NormalHitSum);
            Assert.Equal(1, f.Participants[1].AttackTypes[0].CritHitCount);
            Assert.Equal(200, f.Participants[1].AttackTypes[0].CritHitSum);
        }

        [Fact]
        public void FightCrit_AfterHit()
        {
            var tracker = new FightTracker();
            tracker.Players.Add("Player1");

            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });
            tracker.TrackFightCrit(new FightCritEvent { Timestamp = DateTime.Now, Source = "Player1", Amount = 200, Sequence = FightCritEventSequence.AfterHit });

            var f = tracker.Fights[0];
            Assert.Equal(0, f.Participants[1].AttackTypes[0].NormalHitCount);
            Assert.Equal(0, f.Participants[1].AttackTypes[0].NormalHitSum);
            Assert.Equal(1, f.Participants[1].AttackTypes[0].CritHitCount);
            Assert.Equal(200, f.Participants[1].AttackTypes[0].CritHitSum);
        }

        [Fact]
        public void FightCrit_Ignore()
        {
            var tracker = new FightTracker();
            tracker.Players.Add("Player1");

            // ignore crits where the damage is absorbed by a rune
            tracker.TrackFightCrit(new FightCritEvent { Timestamp = DateTime.Now, Source = "Player1", Amount = 200, Sequence = FightCritEventSequence.BeforeHit });
            tracker.TrackFightMiss(new FightMissEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "rune" });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });

            var f = tracker.Fights[0];
            Assert.Equal(1, f.Participants[1].AttackTypes[0].NormalHitCount);
            Assert.Equal(100, f.Participants[1].AttackTypes[0].NormalHitSum);
            Assert.Equal(0, f.Participants[1].AttackTypes[0].CritHitCount);
            Assert.Equal(0, f.Participants[1].AttackTypes[0].CritHitSum);
        }

        [Fact]
        public void SumHits()
        {
            var tracker = new FightTracker();

            tracker.Players.Add("Player1");
            tracker.Players.Add("Player2");
            //tracker.TrackPlayerFound(new PlayerFoundEvent { Timestamp = DateTime.Now, Name = "Player1" });
            //tracker.TrackPlayerFound(new PlayerFoundEvent { Timestamp = DateTime.Now, Name = "Player2" });

            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 100 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 200 });
            tracker.TrackFightHit(new FightHitEvent { Timestamp = DateTime.Now, Source = "Player2", Target = "Mob1", Amount = 350 });
            //tracker.TrackDeath(new DeathEvent { Timestamp = DateTime.Now.AddSeconds(1), Name = "Mob1" });

            var f = tracker.ActiveFights[0];
            Assert.Equal(3, f.Participants.Count);

            Assert.Equal("Mob1", f.Participants[0].Name);
            Assert.Equal(3, f.Participants[0].TargetHitCount);
            Assert.Equal(650, f.Participants[0].TargetHitSum);

            Assert.Equal("Player1", f.Participants[1].Name);
            Assert.Equal(2, f.Participants[1].SourceHitCount);
            Assert.Equal(300, f.Participants[1].SourceHitSum);

            Assert.Equal("Player2", f.Participants[2].Name);
            Assert.Equal(1, f.Participants[2].SourceHitCount);
            Assert.Equal(350, f.Participants[2].SourceHitSum);
        }


    }
}
