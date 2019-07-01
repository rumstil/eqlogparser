using System;
using System.Collections.Generic;
using System.Text;

namespace EQLogParser
{
    //public delegate void IntervalTrackerEvent(Fight args);


    /// <summary>
    /// Tracks various combat events and assembles them into time interval summaries.
    /// i.e. all mobs fought in the time interval are assembled into a single fight summary.
    /// </summary>
    public class IntervalTracker
    {
        const string NAME = "Target";

        private CharTracker Chars = new CharTracker();
        private TimeSpan Interval = TimeSpan.FromMinutes(30);
        private string Zone = null;
        //private string Party = null;
        private DateTime Timestamp;
        private FightSummary Fight;

        public event FightTrackerEvent OnFightStarted;
        public event FightTrackerEvent OnFightFinished;

        public IntervalTracker()
        {
        }

        public void HandleEvent(LogEvent e)
        {
            Chars.HandleEvent(e);

            Timestamp = e.Timestamp;

            if (Fight == null || Fight.StartedOn + Interval < Timestamp)
            {
                if (Fight != null && Fight.Participants.Count > 0)
                {
                    foreach (var p in Fight.Participants)
                    {
                        p.PetOwner = Chars.GetOwner(p.Name);
                        p.Class = Chars.GetClass(p.Name);
                    }

                    Fight.Status = FightStatus.Timeout;
                    Fight.Finish();

                    if (OnFightFinished != null)
                        OnFightFinished(Fight);
                }
                Fight = new FightSummary();
                Fight.Id = Guid.NewGuid().ToString();
                Fight.Target = new FightParticipant(NAME);
                Fight.Name = NAME;
                Fight.StartedOn = Timestamp;
                if (OnFightStarted != null)
                    OnFightStarted(Fight);
            }

            Fight.UpdatedOn = Timestamp;

            if (e is LogHitEvent hit)
            {
                TrackHit(hit);
            }

            if (e is LogMissEvent miss)
            {
                TrackMiss(miss);
            }

            if (e is LogHealEvent heal)
            {
                TrackHeal(heal);
            }

            if (e is LogZoneEvent zone)
            {
                TrackZone(zone);
            }

            if (e is LogDeathEvent death)
            {
                TrackDeath(death);
            }

        }

        private void TrackHit(LogHitEvent hit)
        {
            var foe = Chars.GetFoe(hit.Source, hit.Target);
            if (foe == null)
                return;

            // normalize target name so that the participant code handles it properly
            if (foe == hit.Source)
                hit = new LogHitEvent()
                {
                    Timestamp = hit.Timestamp,
                    Source = NAME,
                    Target = hit.Target,
                    Type = hit.Type,
                    Amount = hit.Amount,
                    Mod = hit.Mod,
                    Spell = hit.Spell
                };

            else if (foe == hit.Target)
                hit = new LogHitEvent()
                {
                    Timestamp = hit.Timestamp,
                    Source = hit.Source,
                    Target = NAME,
                    Type = hit.Type,
                    Amount = hit.Amount,
                    Mod = hit.Mod,
                    Spell = hit.Spell
                };

            Fight.AddHit(hit);
        }

        private void TrackMiss(LogMissEvent miss)
        {
            var foe = Chars.GetFoe(miss.Source, miss.Target);
            if (foe == null)
                return;

            // normalize target name so that the participant code handles it properly
            if (foe == miss.Source)
                miss = new LogMissEvent()
                {
                    Timestamp = miss.Timestamp,
                    Source = NAME,
                    Target = miss.Target,
                    Type = miss.Type,
                    Mod = miss.Mod,
                    Spell = miss.Spell
                };

            else if (foe == miss.Target)
                miss = new LogMissEvent()
                {
                    Timestamp = miss.Timestamp,
                    Source = miss.Source,
                    Target = NAME,
                    Type = miss.Type,
                    Mod = miss.Mod,
                    Spell = miss.Spell
                };

            Fight.AddMiss(miss);
        }

        private void TrackHeal(LogHealEvent heal)
        {
            Fight.AddHeal(heal);
        }

        private void TrackZone(LogZoneEvent zone)
        {
            Zone = zone.Name;
        }

        private void TrackDeath(LogDeathEvent death)
        {
            var c = Chars.GetType(death.Name);
            if (c != CharType.Foe)
                return;

            //Fight.DeathCount += 1;
        }

        public void ForceFightTimeouts()
        {

        }

    }
}
