using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    public delegate void FightTrackerEvent(FightSummary args);

    /// <summary>
    /// Tracks various combat events and assembles them into individual fight summaries.
    /// </summary>
    public class FightTracker
    {
        private CharTracker Chars = new CharTracker();
        private List<LogEvent> Events = new List<LogEvent>(1000);
        //private string Player = null;
        private string Zone = null;
        private string Party = null;
        private DateTime Timestamp;
        private DateTime LastTimeoutCheck;
        private LogHitEvent LastHit;
        private FightSummary LastFight;

        public readonly List<FightSummary> ActiveFights = new List<FightSummary>();

        /// <summary>
        /// Finish a fight after this duration of no activity is detected.
        /// This should be as least as long as mez or root.
        /// </summary>
        public TimeSpan FightTimeout = TimeSpan.FromSeconds(90);

        public event FightTrackerEvent OnFightStarted;
        public event FightTrackerEvent OnFightFinished;

        public FightTracker()
        {
        }

        public void HandleEvent(LogEvent e)
        {
            Chars.HandleEvent(e);

            Timestamp = e.Timestamp;

            // we only need keep enough events to backtrack on player death
            Events.Add(e);
            if (Events.Count >= 1000)
                Events.RemoveRange(0, 500);

            // no need to check for timeouts more often than every few seconds
            if (LastTimeoutCheck + TimeSpan.FromSeconds(5) <= Timestamp)
            {
                CheckFightTimeouts();
                LastTimeoutCheck = Timestamp;
            }

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

            if (e is LogCastingEvent cast)
            {
                TrackCasting(cast);
            }

            if (e is LogDeathEvent death)
            {
                TrackDeath(death);
            }

            if (e is LogZoneEvent zone)
            {
                TrackZone(zone);
            }

            if (e is LogPartyEvent party)
            {
                TrackParty(party);
            }

            if (e is LogRawEvent raw)
            {
                TrackMisc(raw);
            }

        }

        private void TrackZone(LogZoneEvent zone)
        {
            Zone = zone.Name;
        }

        private void TrackHit(LogHitEvent hit)
        {
            var foe = Chars.GetFoe(hit.Source, hit.Target);
            if (foe == null)
            {
                //Console.WriteLine("*** " + hit);
                return;
            }

            if (hit.Source.EndsWith("'s corpse"))
            {
                if (hit.Target == foe)
                {
                    // rename source to track damage from dead player
                    hit = new LogHitEvent
                    {
                        Timestamp = hit.Timestamp,
                        Type = hit.Type,
                        Source = hit.Source.Substring(0, hit.Source.Length - 9),
                        Target = hit.Target,
                        Amount = hit.Amount,
                        Mod = hit.Mod,
                        Spell = hit.Spell
                    };
                }
                else
                {
                    // do not track damage from a dead mob
                    return;
                }

            }

            var f = GetFight(foe);
            if (f == null)
                return;

            f.AddHit(hit);
            LastHit = hit;
            LastFight = f;
        }

        private void TrackMiss(LogMissEvent miss)
        {
            var foe = Chars.GetFoe(miss.Source, miss.Target);
            if (foe == null)
                return;

            var f = GetFight(foe);
            if (f == null)
                return;

            f.AddMiss(miss);
            LastFight = f;
        }

        private void TrackHeal(LogHealEvent heal)
        {
            // attribute heal to most recent fight - this is probably the best compromise
            if (LastFight != null && LastFight.Status == FightStatus.Active)
                LastFight.AddHeal(heal);
        }

        private void TrackCasting(LogCastingEvent cast)
        {
            // attribute spell to most recent fight - this is probably the best compromise
            if (LastFight != null && LastFight.Status == FightStatus.Active)
            {
                // only include casting by the target and friends because if we if we pull a group of mobs, 
                // we don't want mob A appearing as a participant in a fight with mob B just because it cast a spell
                if (cast.Source == LastFight.Name || Chars.GetType(cast.Source) == CharType.Friend)
                    LastFight.AddCasting(cast);
            }
        }

        private void TrackDeath(LogDeathEvent death)
        {
            if (LastFight != null && LastFight.Status == FightStatus.Active && death.Name != LastFight.Name)
            {
                // todo: use killshot name instead of lastfight
                //LastFight.DeathCount += 1;
                //LastFight.AddDeath(death);
            }

            var f = GetFight(death.Name);
            if (f != null)
            {
                f.Status = FightStatus.Killed;
                f.CohortCount = ActiveFights.Count - 1;
                FinishFight(f);
            }
            else
            {
                var replay = Replay(death.Timestamp.AddSeconds(-3));

                if (death.KillShot != null)
                {
                    f = GetFight(death.KillShot);
                    if (f != null)
                        f.AddDeath(death, replay);
                }
                else if (LastFight != null && LastFight.Status == FightStatus.Active)
                {
                    LastFight.AddDeath(death, replay);
                }
            }
        }

        private void TrackParty(LogPartyEvent party)
        {
            if (party.Status == PartyStatus.GroupXP)
                Party = "Group";

            if (party.Status == PartyStatus.RaidXP)
                Party = "Raid";

            if (party.Status == PartyStatus.SoloXP)
                Party = "Solo";
        }

        private void TrackMisc(LogRawEvent raw)
        {
            

        }

        /// <summary>
        /// Get all events that occured since the specified timestamp.
        /// </summary>
        private IEnumerable<LogEvent> Replay(DateTime timestamp)
        {
            var i = Events.Count - 1;
            if (i < 0)
                yield break;
            while (i > 0 && Events[i - 1].Timestamp >= timestamp)
                i--;
            for (int j = i; j < Events.Count; j++)
                yield return Events[j];
        }

        /// <summary>
        /// Close active fights that have timed out.
        /// </summary>
        public void CheckFightTimeouts()
        {
            //Console.WriteLine("Checking {0} fights for timeout", ActiveFights.Count);

            var cohorts = ActiveFights.Count - 1;

            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];

                if (f.UpdatedOn + FightTimeout <= Timestamp)
                {
                    // ignore fights without any damage activity
                    // e.g. miss messages arriving after death message
                    // e.g. mob casting but never engaged
                    if (f.HP > 0)
                    {
                        f.Status = FightStatus.Timeout;
                        f.CohortCount = cohorts;
                        FinishFight(f);
                    }
                    else
                    {
                        ActiveFights.Remove(f);
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        public void ForceFightTimeouts()
        {
            var cohorts = ActiveFights.Count - 1;

            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];

                // ignore fights without any damage activity
                // e.g. miss messages arriving after death message
                // e.g. mob casting but never engaged
                if (f.HP > 0)
                {
                    f.Status = FightStatus.Timeout;
                    f.CohortCount = cohorts;
                    FinishFight(f);
                }
                else
                {
                    ActiveFights.Remove(f);
                }
            }
        }

        /// <summary>
        /// Finish an active fight. 
        /// </summary>
        private void FinishFight(FightSummary f)
        {
            ActiveFights.Remove(f);

            foreach (var p in f.Participants)
            {
                p.PetOwner = Chars.GetOwner(p.Name);
                p.Class = Chars.GetClass(p.Name);
            }

            f.Finish();
            f.Party = Party;

            if (OnFightFinished != null)
                OnFightFinished(f);
        }

        /// <summary>
        /// Find the active fight involving the given foe name or create a new fight if it doesn't exist.
        /// This will return a null if it the name is a friend.
        /// </summary>
        private FightSummary GetFight(string name)
        {
            // fights are always "foe" focused so we need to return a null if the name is a friend
            var type = Chars.GetType(name);
            if (type == CharType.Friend)
                return null;
            //if (type == CharType.Foe && name.EndsWith("'s corpse"))
            //    return null;

            // the fight list can get pretty long so we limit our check to the tail
            for (var i = ActiveFights.Count - 1; i >= 0 && i > ActiveFights.Count - 20; i--)
            {
                if (ActiveFights[i].Target.Name == name)
                {
                    ActiveFights[i].UpdatedOn = Timestamp;
                    return ActiveFights[i];
                }
            }

            // start a new fight
            var f = new FightSummary();
            f.Id = name.Replace(' ', '-') + "-" + Environment.TickCount.ToString();
            f.Zone = Zone;
            f.Name = name;
            f.Target = new FightParticipant(f.Name);
            // todo: always start fights at multiple of 6s for better alignment of data when merging fights?
            f.StartedOn = f.UpdatedOn = Timestamp;

            ActiveFights.Add(f);
            if (OnFightStarted != null)
                OnFightStarted(f);

            return f;
        }


        //private string StripCorpse(string name)
        //{
        //    if (name.EndsWith("'s corpse"))
        //        name = name.Substring(0, name.Length - 9);
        //    return name;
        //}
    }
}
