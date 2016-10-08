using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    public delegate void FightTrackerEvent(Fight args);

    //[Flags]
    //public enum FightTrackerOptions
    //{
    //}

    /// <summary>
    /// This class consumes parser events and compiles them into fight summaries.
    /// </summary>
    public class FightTracker
    {
        // players are tracked so that we don't create fights for them
        public PlayerTracker Players = new PlayerTracker();
        private HashSet<string> Mobs = new HashSet<string>();
        private Dictionary<string, int> MinDoTDamage = new Dictionary<string, int>();
        //private List<FightHit> OrphanHits = new List<FightHit>();
        private string Zone;
        private DateTime Timestamp;
        private DateTime LastTimeout;
        private FightCritEvent LastCritical;
        private FightHitEvent LastHit;
        private Fight LastFight;

        /// <summary>
        /// A list of all fights (including active ones)
        /// </summary>
        public readonly List<Fight> Fights = new List<Fight>();

        /// <summary>
        /// A list of active fights.
        /// </summary>
        public readonly List<Fight> ActiveFights = new List<Fight>();

        /// <summary>
        /// finish a fight after this duration of no activity is detected
        /// </summary>
        public TimeSpan FightTimeout = TimeSpan.FromSeconds(30);

        public event FightTrackerEvent OnFightStarted;
        public event FightTrackerEvent OnFightFinished;

        public FightTracker()
        {
            Zone = "Unknown";
        }

        public FightTracker(LogParser parser) : base()
        {
            Players = new PlayerTracker(parser);
            Subscribe(parser);
        }

        public virtual void Subscribe(LogParser parser)
        {
            parser.OnBeforeEvent += BeforeTracking;
            parser.OnZone += TrackZoneChanged;
            parser.OnFightCrit += TrackFightCrit;
            parser.OnFightHit += TrackFightHit;
            parser.OnFightMiss += TrackFightMiss;
            parser.OnDeath += TrackDeath;
        }

        public virtual void Unsubscribe(LogParser parser)
        {
            parser.OnBeforeEvent -= BeforeTracking;
            parser.OnZone -= TrackZoneChanged;
            parser.OnFightCrit -= TrackFightCrit;
            parser.OnFightHit -= TrackFightHit;
            parser.OnFightMiss -= TrackFightMiss;
            parser.OnDeath -= TrackDeath;
        }

        public virtual void BeforeTracking(RawLogEvent log)
        {
            Timestamp = log.Timestamp;

            // no need to check for timeouts more often than every few seconds
            if (LastTimeout + TimeSpan.FromSeconds(5) <= Timestamp)
            {
                CheckFightTimeouts();
                LastTimeout = Timestamp;
            }
        }

        public virtual void TrackZoneChanged(ZoneEvent zone)
        {
            Zone = zone.Name;
        }

        public virtual void TrackFightHit(FightHitEvent hit)
        {
            var f = AddFight(hit);
            if (f == null)
                return;

            var source = f.Participants.First(x => x.Name == hit.Source);
            source.AddHit(hit);

            var target = f.Participants.First(x => x.Name == hit.Target);
            target.AddHit(hit);

            // there are no crit notifications for DoTs but we can guess when they occur by
            // treating everything that does 125% more damage than the minimum as a crit
            // todo: do DoTs have partial resists?
            if (hit.Type == "dot" && hit.Spell != null)
            {
                int min;
                if (!MinDoTDamage.TryGetValue(hit.Spell, out min) || min > hit.Amount)
                    MinDoTDamage[hit.Spell] = min = hit.Amount;
                if (hit.Amount >= min * 2.25)
                    LastCritical = new FightCritEvent { Timestamp = hit.Timestamp, Source = hit.Source, Amount = hit.Amount, Sequence = FightCritEventSequence.BeforeHit };
            }

            // update hit for crit notification that occured before hit
            if (LastCritical != null && LastCritical.Sequence == FightCritEventSequence.BeforeHit && LastCritical.Source == hit.Source)
            {
                var ht = source.AttackTypes.First(x => x.Type == hit.Type);
                ht.NormalHitCount -= 1;
                ht.NormalHitSum -= hit.Amount;
                ht.CritHitCount += 1;
                ht.CritHitSum += hit.Amount;
            }

            LastFight = f;
            LastHit = hit;
            LastCritical = null;
        }

        public virtual void TrackFightCrit(FightCritEvent crit)
        {
            if (crit.Sequence == FightCritEventSequence.BeforeHit)
            {
                // crit update will be done in TrackFightHit() for before hit
                LastCritical = crit;
            }

            if (crit.Sequence == FightCritEventSequence.AfterHit && LastHit != null)
            {
                // crit notifications don't include a target so we need to use the target from the last hit
                var p = LastFight.Participants.FirstOrDefault(x => x.Name == crit.Source);
                if (p == null)
                    return;

                var ht = p.AttackTypes.FirstOrDefault(x => x.Type == LastHit.Type);
                if (ht == null)
                    return;

                ht.NormalHitCount -= 1;
                ht.NormalHitSum -= LastHit.Amount;
                ht.CritHitCount += 1;
                ht.CritHitSum += LastHit.Amount;

                LastCritical = null;
            }
            
        }

        public virtual void TrackFightMiss(FightMissEvent miss)
        {
            var f = AddFight(miss);
            if (f == null)
                return;

            var source = f.Participants.First(x => x.Name == miss.Source);
            source.AddMiss(miss);

            var target = f.Participants.First(x => x.Name == miss.Target);
            target.AddMiss(miss);

            // clear the critical in case a rune is present and the actual hit never lands
            LastCritical = null;

            // should runes count as hits? maybe this should be an option
            if (miss.Type == "rune")
            {
                //LastHit = new FightHitEvent { Timestamp = miss.Timestamp, Source = miss.Source, Target = miss.Target, Amount = 0, Type = "rune" };
            }
        }

        public virtual void TrackDeath(DeathEvent death)
        {
            var f = GetFight(death.Name);
            if (f != null && !f.Finished.HasValue)
            {
                f.Finished = death.Timestamp;
                ActiveFights.Remove(f);
                if (OnFightFinished != null)
                    OnFightFinished(f);
            }
        }

        public virtual void TrackSpellCasting(SpellCastingEvent cast)
        {
            // the spell cast will be added to all active fights
            foreach (var f in ActiveFights)
            {
                var p = f.Participants.FirstOrDefault(x => x.Name == cast.Source);
                if (p == null)
                {
                    p = new Combatant(cast.Source);
                    f.Participants.Add(p);
                }
                p.Casting.Add(cast);
            }
        }

        /// <summary>
        /// Close inactive fights after a timeout.
        /// </summary>
        public virtual void CheckFightTimeouts()
        {
            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];

                if (f.LastActive + FightTimeout <= Timestamp)
                {
                    f.Finished = f.LastActive;
                    ActiveFights.Remove(f);
                    if (OnFightFinished != null)
                        OnFightFinished(f);
                }
                else
                {
                    i++;
                }
            }
        }

        /*
        private IEnumerable<Fight> GetActiveFights()
        {
            // the fight list can get pretty long so limit our check to the tail end
            int i = Fights.Count - 20;
            if (i < 0)
                i = 0;

            for (; i < Fights.Count; i++)
            {
                var f = Fights[i];
                if (!f.Finished.HasValue)
                    yield return f;
            }
        }
        */

        /// <summary>
        /// Initialize a fight for the hit attempt.
        /// </summary>
        private Fight AddFight(FightHitAttemptEvent hit)
        {
            // all fights are tracked by mob name so before anything else we need to
            // determine who the mob is
            string player = null;
            string mob = null;
            bool mobIsCorpse = false;

            if (Players.Contains(hit.Source) || Mobs.Contains(hit.Target))
            {
                player = hit.Source;
                mob = hit.Target;
            }
            else if (Players.Contains(hit.Target) || Mobs.Contains(hit.Source))
            {
                player = hit.Target;
                mob = hit.Source;
                mobIsCorpse = hit.SourceIsCorpse;
            }

            // ignore the hit if we can't determine who the player is
            // or the player is hitting themselves
            // or the player is charmed
            if (mob == null || player == null || mob == player || Players.Contains(mob))
            {
                //Console.Error.WriteLine("Ignored: {0}", hit);
                return null;
            }

            Mobs.Add(mob);

            // find the active fight that this hit belongs to
            var f = GetFight(mob);
            if (f == null || (f.Finished.HasValue && !mobIsCorpse))
            {
                f = new Fight();
                f.Zone = Zone;
                f.Opponent = new Combatant(mob);
                f.Participants.Add(f.Opponent);
                f.Started = hit.Timestamp;
                Fights.Add(f);
                ActiveFights.Add(f);
                if (OnFightStarted != null)
                    OnFightStarted(f);
            }
            f.LastActive = hit.Timestamp;

            // find the player
            var p = f.Participants.FirstOrDefault(x => x.Name == player);
            if (p == null)
            {
                p = new Combatant(player);
                f.Participants.Add(p);
            }

            return f;
        }

        /// <summary>
        /// Find the most recent fight involving the supplied mob/player name.
        /// </summary>
        private Fight GetFight(string name)
        {
            // the fight list can get pretty long so limit our check to the tail end
            for (var i = Fights.Count - 1; i >= 0 && i > Fights.Count - 20; i--)
            {
                var f = Fights[i];

                //if (name.EndsWith("'s corpse"))
                //    name = name.Substring(0, name.Length - 9);

                // many log entries are reported with the first letter capitalized (but the parser currently capitalizes them)
                //if (f.Opponent.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && f.Zone == Zone.Name)
                if (f.Opponent.Name == name)
                    return f;

            }
            return null;
        }

        //private string StripCorpse(string name)
        //{
        //    if (name.EndsWith("'s corpse"))
        //        name = name.Substring(0, name.Length - 9);
        //    return name;
        //}
    }
}
