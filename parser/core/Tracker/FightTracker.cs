using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    public delegate void FightTrackerEvent(Fight args);

    /// <summary>
    /// Tracks various combat events and assembles them into fight summaries.
    /// </summary>
    public class FightTracker
    {
        public CharTracker Chars = new CharTracker();

        //private List<FightHit> OrphanHits = new List<FightHit>();
        //private string Player = null;
        private string Zone = null;
        private string Party = null;
        private DateTime Timestamp;
        private DateTime LastTimeoutCheck;
        private LogHitEvent LastHit;
        private Fight LastFight;
        private List<LogCastingEvent> Casting = new List<LogCastingEvent>();

        /// <summary>
        /// A list of all fights (including active ones)
        /// </summary>
        public readonly List<Fight> Fights = new List<Fight>();

        /// <summary>
        /// A list of active fights.
        /// </summary>
        public readonly List<Fight> ActiveFights = new List<Fight>();

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
                //Player = raw.Player;

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
                Console.WriteLine("*** " + hit);
                return;
            }

            var f = GetFight(foe);
            f.AddHit(hit);

            LastFight = f;
            LastHit = hit;
        }

        private void TrackMiss(LogMissEvent miss)
        {
            var foe = Chars.GetFoe(miss.Source, miss.Target);
            if (foe == null)
                return;

            var f = GetFight(foe);
            f.AddMiss(miss);

            LastFight = f;
        }

        private void TrackCasting(LogCastingEvent cast)
        {
            // attribute spell to most recent fight - this is probably the best compromise
            if (LastFight != null && LastFight.Finished == null)
            {
                // only include casting by the target and friends because if we if we pull a group of mobs, 
                // we don't want mob A appearing as a participant in a fight with mob B just because it cast a spell
                if (cast.Source == LastFight.Name || Chars.GetType(cast.Source) == CharType.Friend)
                    LastFight.AddCasting(cast);
            }
        }

        private void TrackHeal(LogHealEvent heal)
        {
            // attribute heal to most recent fight - this is probably the best compromise
            if (LastFight != null && LastFight.Finished == null)
                LastFight.AddHeal(heal);
        }

        private void TrackDeath(LogDeathEvent death)
        {
            if (LastFight != null && LastFight.Finished == null && death.Name != LastFight.Name)
                LastFight.Deaths += 1;

            var f = GetFight(death.Name);
            if (f != null)
            {
                if (!f.Finished.HasValue)
                    FinishFight(f);
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


        /// <summary>
        /// Close active fights that have timed out.
        /// </summary>
        public void CheckFightTimeouts()
        {
            //Console.WriteLine("Checking {0} fights for timeout", ActiveFights.Count);

            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];

                if (f.Updated + FightTimeout <= Timestamp)
                {
                    // ignore fights without any damage activity
                    // e.g. miss messages arriving after death message
                    // e.g. mob casting but never engaged
                    if (f.HP > 0)
                    {
                        FinishFight(f);
                    }
                    else
                    {
                        ActiveFights.Remove(f);
                        Fights.Remove(f);
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
            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];

                // ignore fights without any damage activity
                // e.g. miss messages arriving after death message
                // e.g. mob casting but never engaged
                if (f.HP > 0)
                {
                    FinishFight(f);
                }
                else
                {
                    ActiveFights.Remove(f);
                    Fights.Remove(f);
                }
            }
        }

        /// <summary>
        /// Finish an active fight. 
        /// </summary>
        private void FinishFight(Fight f)
        {
            foreach (var p in f.Participants)
            {
                p.PetOwner = Chars.GetOwner(p.Name);
                p.Class = Chars.GetClass(p.Name);
            }

            f.MergePets();
            f.Finish();            
            f.Party = Party;

            ActiveFights.Remove(f);
            if (OnFightFinished != null)
                OnFightFinished(f);
        }

        /// <summary>
        /// Find the active fight involving the given foe name or create a new fight if it doesn't exist.
        /// </summary>
        private Fight GetFight(string name)
        {
            // the fight list can get pretty long so we limit our check to the tail
            for (var i = ActiveFights.Count - 1; i >= 0 && i > ActiveFights.Count - 20; i--)
            {
                //if (name.EndsWith("'s corpse"))
                //    name = name.Substring(0, name.Length - 9);

                if (ActiveFights[i].Target.Name == name)
                {
                    ActiveFights[i].Updated = Timestamp;
                    return ActiveFights[i];
                }
            }

            // fights are always "foe" focused so we need to return a null if the name is a friend
            var type = Chars.GetType(name);
            if (type == CharType.Friend)
                return null;

            var f = new Fight();
            f.ID = name.Replace(' ', '-') + "-" + Fights.Count.ToString();
            f.Zone = Zone;
            f.Name = name;
            f.Target = new FightParticipant(name);
            //f.Participants.Add(new FightParticipant(name));
            f.Started = f.Updated = Timestamp;
            foreach (var p in f.Participants.Where(x => x.Class == null))
                p.Class = Chars.GetClass(p.Name);

            Fights.Add(f);
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
