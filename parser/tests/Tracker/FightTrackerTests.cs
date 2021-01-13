﻿using EQLogParser;
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
        /// <summary>
        /// Most of the tests here use a LogWhoEvent tag a player. This tests what
        /// happens when that isn't done.
        /// </summary>
        [Fact]
        public void Fight_Ignored_If_Foe_Unknown()
        {
            var tracker = new FightTracker(new SpellParser());
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 200 });

            // since the tracker doesn't know if Player1 or Mob1 is the foe it 
            // can't track the fight yet
            Assert.Empty(tracker.ActiveFights);
        }

        [Fact]
        public void Fight_Has_Correct_Timestamps()
        {
            var tracker = new FightTracker(new SpellParser());
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
            var tracker = new FightTracker(new SpellParser());
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
            var tracker = new FightTracker(new SpellParser());
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
        public void Two_Fights_Back_to_Back_With_Same_Mob_Name()
        {
            var tracker = new FightTracker(new SpellParser());
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
            FightInfo f = null;
            var tracker = new FightTracker(new SpellParser());
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
            var tracker = new FightTracker(new SpellParser());

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 100 });
            tracker.HandleEvent(new LogDeathEvent { Timestamp = DateTime.Now, Name = "Player1" });

            Assert.Single(tracker.ActiveFights);
        }

        [Fact]
        public void Timeout()
        {
            FightInfo f = null;
            var tracker = new FightTracker(new SpellParser());
            tracker.OnFightFinished += (args) => f = args;

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Type = "slash", Amount = 100 });
            Assert.Null(f);

            tracker.HandleEvent(new LogRawEvent { Timestamp = DateTime.Now + tracker.GroupFightTimeout, Text = "..." });
            Assert.NotNull(f);
            Assert.Equal("Mob1", f.Target.Name);
            Assert.Empty(tracker.ActiveFights);
        }

        [Fact]
        public void SumHits()
        {
            var tracker = new FightTracker(new SpellParser());

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
            var tracker = new FightTracker(new SpellParser());

            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Mob1", Amount = 100 });
            tracker.HandleEvent(new LogHealEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Player1", Amount = 201 });

            var f = tracker.ActiveFights[0];

            Assert.Equal(201, f.Participants[0].InboundHealSum);
            Assert.Equal(201, f.Participants[0].OutboundHealSum);
        }

        [Fact]
        public void Ignore_Self_Hits()
        {
            var tracker = new FightTracker(new SpellParser());

            tracker.HandleEvent(new LogHitEvent { Timestamp = DateTime.Now, Source = "Player1", Target = "Player1", Amount = 100 });
            Assert.Empty(tracker.ActiveFights);
        }


        [Fact]
        public void Check_DPS_Intervals()
        {
            var tracker = new FightTracker(new SpellParser());
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });


            var start = DateTime.Today.AddHours(3).AddSeconds(34);
            var time = start;
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player1", Target = "Mob", Amount = 100 });

            time = start.AddSeconds(1);
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player1", Target = "Mob", Amount = 50 });

            time = start.AddSeconds(3);
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player1", Target = "Mob", Amount = 21 });

            time = start.AddSeconds(4);
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player2", Target = "Mob", Amount = 35 });

            var f = tracker.ActiveFights[0];
            f.Finish();

            // the whole fight is only 4 seconds long so far but since the hits 
            // cross a 6 second wall clock interval we should split the hits into
            // two intervals
            Assert.Equal(4, f.Participants[0].Duration);
            Assert.Equal(150, f.Participants[0].DPS[0]);
            Assert.Equal(21, f.Participants[0].DPS[1]);

            Assert.Equal(1, f.Participants[1].Duration);
            Assert.Equal(0, f.Participants[1].DPS[0]);
            Assert.Equal(35, f.Participants[1].DPS[1]);
        }


        [Fact]
        public void Raid_Matched()
        {
            var temp = new RaidTemplate()
            {
                Zone = "Crystallos, Lair of the Awakened",
                Name = "Kerafyrm",
                Mobs = new[] { "Kerafyrm the Awakened", "A wyvern assassin" },
                EndsOnDeath = new[] { "Kerafyrm the Awakened" }
            };

            var tracker = new FightTracker(new SpellParser());
            tracker.HandleEvent(new LogWhoEvent { Name = "Player1" });
            tracker.HandleEvent(new LogWhoEvent { Name = "Player2" });
            tracker.AddTemplate(temp);
            var results = new List<FightInfo>();
            tracker.OnFightFinished += e => results.Add(e);

            // act
            var time = DateTime.Now;
            tracker.HandleEvent(new LogZoneEvent { Timestamp = time, Name = temp.Zone });
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player1", Target = temp.Mobs[0], Amount = 100 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player2", Target = temp.Mobs[1], Amount = 20 });
            tracker.HandleEvent(new LogHitEvent { Timestamp = time, Source = "Player2", Target = "Unrelated Mob", Amount = 30 });
            tracker.HandleEvent(new LogDeathEvent { Timestamp = time, Name = temp.EndsOnDeath[0] });

            // assert
            var raids = results.OfType<RaidFightInfo>().ToArray();
            Assert.Single(raids);
            Assert.Equal("Kerafyrm", raids[0].Name);
            Assert.Equal("Crystallos, Lair of the Awakened", raids[0].Zone);            
            Assert.Equal(2, raids[0].MobCount);
            Assert.Equal(120, raids[0].HP);
            Assert.Single(tracker.ActiveFights); // the unrelated mob


            /*

            // this should be ignored
            var f3 = new FightInfo() { Name = "Fippy Darkpaw", Zone = "Crystallos, Lair of the Awakened", Status = FightStatus.Killed };
            f3.Target.InboundHitSum = 20;
            tracker.HandleFight(f3);

            // this should be included, and create a new raid
            var f2 = new FightInfo() { Name = "A wyvern assassin", Zone = "Crystallos, Lair of the Awakened", Status = FightStatus.Killed };
            f2.Target.InboundHitSum = 10;
            tracker.HandleFight(f2);

            // this should finish the raid
            var f1 = new FightInfo() { Name = "Kerafyrm the Awakened", Zone = "Crystallos, Lair of the Awakened", Status = FightStatus.Killed };
            f1.Target.InboundHitSum = 100;
            tracker.HandleFight(f1);

            Assert.Single(results);
            Assert.Equal("Kerafyrm", results[0].Name);
            Assert.Equal("Crystallos, Lair of the Awakened", results[0].Zone);
            Assert.Equal(2, results[0].MobCount);
            Assert.Equal(110, results[0].HP);

            */


        }

    }

}