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

        /*
        [Fact]
        public void FightHit_TrackDeadPlayer()
        {
            var tracker = new FightTracker();
            tracker.Chars.Add("Player1");

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "dot", Amount = 200 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1's corpse", Target = "Mob1", Type = "dot", Amount = 350 });

            var f = tracker.Fights[0];
            Assert.Equal(2, f.Participants.Count);
            var p = f.Participants[1];
            Assert.Equal("Player1", p.Name);
            Assert.Equal(2, p.SourceHitCount);
            Assert.Equal(550, p.SourceHitSum);
        }
        */

        /*
        [Fact]
        public void FightHit_TrackDeadMob()
        {
            var tracker = new FightTracker();
            tracker.Chars.Add("Player1");

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Mob1", Target = "Player1", Type = "dot", Amount = 200 });
            //tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Mob1's corpse", Target = "Player1", Type = "dot", Amount = 350 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Mob1", SourceIsCorpse = true, Target = "Player1", Type = "dot", Amount = 350 });

            Assert.Equal(1, tracker.Fights.Count);
            var f = tracker.Fights[0];
            Assert.Equal(2, f.Participants.Count);
            Assert.Equal(2, f.Opponent.SourceHitCount);
            Assert.Equal(550, f.Opponent.SourceHitSum);
        }
        */

        [Fact]
        public void Timestamps()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });

            var t1 = DateTime.Now;
            tracker.HandleEvent(new LogHitEvent { Timestamp = t1, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            var t2 = t1.AddSeconds(3);
            tracker.HandleEvent(new LogMissEvent { Timestamp = t2, Source = "Player1", Target = "Mob1", Type = "dodge" });

            var f = tracker.Fights[0];
            Assert.Equal(t1, f.Started);
            Assert.Equal(t2, f.Updated);

            var t3 = t2.AddSeconds(3);
            tracker.HandleEvent(new LogDeathEvent { Timestamp = t3, Name = "Mob1" });
            Assert.Equal(t3, f.Finished.Value);
        }

        [Fact]
        public void One_Fight()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });

            var f = tracker.Fights[0];
            Assert.Equal("Mob1", f.Name);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Equal("Player1", f.Participants[0].Name);
        }

        [Fact]
        public void Two_Fights_Concurrent()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            Assert.Equal(0, tracker.Fights.Count);

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player2", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Mob1", Target = "Player1", Type = "slash", Amount = 100 });
            Assert.Equal("Mob1", tracker.Fights[0].Target.Name);
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(1, tracker.ActiveFights.Count);

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob2", Type = "slash", Amount = 100 });
            Assert.Equal("Mob2", tracker.Fights[1].Target.Name);
            Assert.Equal(2, tracker.Fights.Count);
            Assert.Equal(2, tracker.ActiveFights.Count);
        }

        [Fact]
        public void Two_Fights_Back_To_Back()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(1, tracker.ActiveFights.Count);
            
            tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now, Name = "Mob1" });
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(0, tracker.ActiveFights.Count);

            // same mob name, but it should be treated as a new fight since it died
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 101 });
            Assert.Equal(2, tracker.Fights.Count);
            Assert.Equal(1, tracker.ActiveFights.Count);
        }

        [Fact]
        public void Death()
        {
            Fight f = null;
            var tracker = new FightTracker();
            tracker.OnFightFinished += (args) => f = args;

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now.AddSeconds(1), Name = "Mob1" });

            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(0, tracker.ActiveFights.Count);
        }

        [Fact]
        public void Timeout()
        {
            Fight f = null;
            var tracker = new FightTracker();
            tracker.OnFightFinished += (args) => f = args;

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            Assert.Null(f);

            tracker.HandleEvent(new LogRawEvent { Timestamp = DateTime.Now + tracker.FightTimeout, Text = "..." });
            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Equal(1, tracker.Fights.Count);
            Assert.Equal(0, tracker.ActiveFights.Count);
        }

        [Fact]
        public void SumHits()
        {
            var tracker = new FightTracker();

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogWhoEvent { Name = "Player2" });

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 100 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 200 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player2", Target = "Mob1", Amount = 350 });
            //tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now.AddSeconds(1), Name = "Mob1" });

            var f = tracker.ActiveFights[0];
            Assert.Equal(2, f.Participants.Count);

            Assert.Equal("Mob1", f.Target.Name);
            Assert.Equal(3, f.Target.InboundHitCount);
            Assert.Equal(650, f.Target.InboundHitSum);

            Assert.Equal("Player1", f.Participants[0].Name);
            Assert.Equal(2, f.Participants[0].OutboundHitCount);
            Assert.Equal(300, f.Participants[0].OutboundHitSum);

            Assert.Equal("Player2", f.Participants[1].Name);
            Assert.Equal(1, f.Participants[1].OutboundHitCount);
            Assert.Equal(350, f.Participants[1].OutboundHitSum);
        }


    }
}
