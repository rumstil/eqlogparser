using EQLogParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace EQLogParserTests.Tracker
{
    public class FightTrackerTests
    {

        [Fact]
        public void Timestamps()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });

            var t1 = DateTime.Now;
            tracker.HandleEvent(new LogHitEvent { Timestamp = t1, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            var t2 = t1.AddSeconds(3);
            tracker.HandleEvent(new LogMissEvent { Timestamp = t2, Source = "Player1", Target = "Mob1", Type = "dodge" });

            var f = tracker.ActiveFights[0];
            Assert.Equal(t1, f.StartedOn);
            Assert.Equal(t2, f.UpdatedOn);

            var t3 = t2.AddSeconds(3);
            tracker.HandleEvent(new LogDeathEvent { Timestamp = t3, Name = "Mob1" });
            Assert.Equal(FightStatus.Killed, f.Status);
            Assert.Equal(t3, f.UpdatedOn);
        }

        [Fact]
        public void One_Fight()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });

            var f = tracker.ActiveFights[0];
            Assert.Equal("Mob1", f.Name);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Equal("Player1", f.Participants[0].Name);
        }

        [Fact]
        public void Two_Fights_Concurrent()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            Assert.Empty(tracker.ActiveFights);

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player2", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Mob1", Target = "Player1", Type = "slash", Amount = 100 });
            Assert.Equal("Mob1", tracker.ActiveFights[0].Target.Name);
            Assert.Single(tracker.ActiveFights);

            tracker.HandleEvent(new LogMissEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob2", Type = "dodge" });
            Assert.Equal("Mob2", tracker.ActiveFights[1].Target.Name);
            Assert.Equal(2, tracker.ActiveFights.Count);
        }

        [Fact]
        public void Two_Fights_Back_to_Back()
        {
            var tracker = new FightTracker();
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            Assert.Single(tracker.ActiveFights);
            
            tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now, Name = "Mob1" });
            Assert.Empty(tracker.ActiveFights);

            // same mob name, but it should be treated as a new fight since it died
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 101 });
            Assert.Single(tracker.ActiveFights);
        }

        [Fact]
        public void Death_of_Mob()
        {
            FightSummary f = null;
            var tracker = new FightTracker();
            tracker.OnFightFinished += (args) => f = args;

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now.AddSeconds(1), Name = "Mob1" });

            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Empty(tracker.ActiveFights);
        }

        [Fact]
        public void Death_of_Player()
        {
            var tracker = new FightTracker();

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 100 });
            tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now, Name = "Player1" });

            Assert.Single(tracker.ActiveFights);
        }

        [Fact]
        public void Timeout()
        {
            FightSummary f = null;
            var tracker = new FightTracker();
            tracker.OnFightFinished += (args) => f = args;

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            Assert.Null(f);

            tracker.HandleEvent(new LogRawEvent { Timestamp = DateTime.Now + tracker.FightTimeout, Text = "..." });
            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Empty(tracker.ActiveFights);
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

        [Fact]
        public void Dont_Double_Count_Self_Heal()
        {
            var tracker = new FightTracker();

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 100 });
            tracker.HandleEvent(new LogHealEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Player1", Amount = 201 });

            var f = tracker.ActiveFights[0];
            
            Assert.Equal(201, f.Participants[0].InboundHealSum);
            Assert.Equal(201, f.Participants[0].OutboundHealSum);
        }





    }
}
