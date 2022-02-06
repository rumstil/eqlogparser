using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EQLogParser
{
    public enum FightStatus
    {
        Active,
        Killed,
        Timeout,
        Merged
    }

    /// <summary>
    /// A fight is an engagement between a single target and one or more players.
    /// The target should always be a mob although in the case of charm or a parsing bug it may occasionally be a player.
    /// The participant list includes all people attacking the target or casting heals on other participants.
    /// </summary>
    public class FightInfo
    {
        // 2022-2-5, "2", added InboundHPS, FullHealSums on spells and heals, HitMax on spells and hits
        public string Version { get; set; } = "2";
        public string ID { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int IntervalDuration { get; set; } = 6; // 6 seconds
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FightStatus Status { get; set; }
        public string Player { get; set; }
        public string Server { get; set; }
        public string Zone { get; set; }
        public string Name { get; set; }
        public string Party { get; set; }
        public string MobNotes { get; set; }
        public int MobCount { get; set; } // if this is a consolidated fight, this will be the original number of fights
        public int CohortCount { get; set; } // number of active fights when this finished
        public long TopHitSum { get; set; }
        public long TopHealSum { get; set; }

        /// <summary>
        /// Mob being fought.
        /// </summary>
        public FightParticipant Target { get; set; }

        /// <summary>
        /// HP of mob being fought.
        /// </summary>
        public long HP => Target.InboundHitSum;

        /// <summary>
        /// All characters and pets participating in the fight vs target.
        /// </summary>
        public List<FightParticipant> Participants { get; set; } = new List<FightParticipant>();

        protected IEnumerable<FightParticipant> ParticipantsAndTarget
        {
            get
            {
                if (Target != null)
                    yield return Target;
                foreach (var p in Participants)
                    yield return p;
            }
        }

        /// <summary>
        /// Fight duration in seconds (excludes gaps on merged fights). Always at least 1.
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Fight duration in seconds (includes gaps on merged fights). Always at least 1.
        /// </summary>
        public int TotalDuration => (int)(UpdatedOn - StartedOn).TotalSeconds + 1;

        /// <summary>
        /// Is this probably a trash mob?
        /// These rules should make sense for both high and low level players fighting level appropriate mobs.
        /// </summary>
        public bool IsTrash => Duration < 10 || Target.InboundHitCount < 5 || HP < 1000;

        public override string ToString()
        {
            return String.Format("{0} ({1}) - {2}", Target.Name, Zone, StartedOn);
        }

        /// <summary>
        /// This constructor should only be used by serializers or unit tests.
        /// We don't want an instance where Target or Target.Name isn't set.
        /// </summary>
        public FightInfo()
        {
            ID = Guid.NewGuid().ToString();
        }

        public FightInfo(string name)
        {
            ID = Guid.NewGuid().ToString();
            Name = name;
            Target = new FightParticipant() { Name = name };
        }

        protected FightParticipant AddParticipant(string name)
        {
            if (Target.Name == name)
                return Target;

            FightParticipant p = null;
            for (int i = 0; i < Participants.Count; i++)
                if (Participants[i].Name == name)
                {
                    p = Participants[i];
                    break;
                }

            if (p == null)
            {
                p = new FightParticipant();
                p.Name = name;
                Participants.Add(p);
            }
            return p;
        }

        /// <summary>     
        /// The DPS, TankDPS and HPS arrays store data in 6 second intervals/ticks.
        /// This function calculates the tick index based on an event timestamp.
        /// Ticks are always started at 6 second wall clock increments.
        /// e.g. If a fight starts at 9:00:02, 
        ///      The first tick covers 9:00:00 to 9:00:05,
        ///      The second tick covers 9:00:06 to 9:00:11
        /// That means the first tick usually won't contain a full 6 seconds of fight data.
        /// This may seem like a strange choice but using using wall clock time modulo 6 seconds
        /// makes it possible to combine multiple fights or timespans together since the tick
        /// overlapping will always match.
        /// </summary>
        protected int GetTick(DateTime ts)
        {
            var interval = (int)((ts - StartedOn).TotalSeconds + (StartedOn.Second % 6)) / 6;

            // if the log is altered and entries are saved out of order, the tick might be negative
            if (interval < 0)
                interval = 0;
            return interval;
        }

        /// <summary>
        /// Get tick offset from beginning of fight. This is used as the index for the damage/healing arrays.
        /// </summary>
        //private int GetTick(LogEvent e)
        //{
        //    var interval = (int)(e.Timestamp - StartedOn).TotalSeconds / 6;
        //    // if the log is altered and entries are saved out of order then our intervals might be negative
        //    if (interval < 0)
        //        interval = 0;
        //    return interval;
        //}

        public void AddHit(LogHitEvent hit)
        {
            var interval = GetTick(hit.Timestamp);
            // hit source may be null if caster dies
            if (hit.Source != null)
                AddParticipant(hit.Source).AddHit(hit, interval);
            AddParticipant(hit.Target).AddHit(hit, interval);

            // special counters for the mob/target only
            //if (hit.Source == Name)
            //{
            //    if (hit.Special != null && hit.Special.Contains("strikethrough"))
            //    {
            //        StrikeCount += 1;
            //    }
            //}
        }

        public void AddMiss(LogMissEvent miss)
        {
            var interval = GetTick(miss.Timestamp);
            AddParticipant(miss.Source).AddMiss(miss, interval);
            AddParticipant(miss.Target).AddMiss(miss, interval);
        }

        public void AddHeal(LogHealEvent heal)
        {
            var interval = GetTick(heal.Timestamp);

            // ignore any heals on target
            if (heal.Target == Target.Name)
                return;

            // heal source may be null if healer dies
            if (heal.Source != null)
                AddParticipant(heal.Source).AddHeal(heal, interval);

            // only count target if it's not a self heal, because AddHeal has already
            // run both source/target legs internally and we don't want to double count
            if (heal.Source != heal.Target)
                AddParticipant(heal.Target).AddHeal(heal, interval);
        }

        public void AddCasting(LogCastingEvent cast)
        {
            AddParticipant(cast.Source).AddCasting(cast, TotalDuration - 1);
        }

        public void AddDeath(LogDeathEvent death)
        {
            AddParticipant(death.Name).DeathCount += 1;
        }

        /// <summary>
        /// Trim the long tail of participants that fall under a low threshold of activity to make the data structure smaller.
        /// These players will be consolidated into an "*Other" entry.
        /// This should be done after all other merging -- possibly just on the server side.
        /// </summary>
        public void TrimParticipants()
        {
            var other = new FightParticipant();
            other.FirstAction = StartedOn;
            other.LastAction = UpdatedOn;
            other.Name = "*Other";

            // damage: anyone that does less than 5% of top damage dealer
            var min = Participants.Max(x => x.OutboundHitSum) * 0.05;
            foreach (var p in Participants.Where(x => x.OutboundHitSum < min))
            {
                // participant names will be anonymized at this point
                //other.AttackTypes.Add(new FightHit { Type = p.Name, HitCount = p.OutboundHitCount, HitSum = (int)p.OutboundHitSum });
                other.OutboundHitSum += p.OutboundHitSum;
                p.OutboundHitSum = 0;
                other.OutboundHitCount += p.OutboundHitCount;
                p.OutboundHitCount = 0;
                p.DPS.Clear();
                p.AttackTypes.Clear();
                p.Spells.RemoveAll(x => x.Type == "hit");
            }

            // tanking: anyone that does less than 10% of the top tank
            // this just ends up being filled with swarm pets and rampage targets
            min = Participants.Max(x => x.InboundMeleeSum) * 0.1;
            foreach (var p in Participants.Where(x => x.InboundMeleeSum < min))
            {
                other.InboundMeleeSum += p.InboundMeleeSum;
                p.InboundMeleeSum = 0;
                other.InboundMeleeCount += p.InboundMeleeCount;
                other.InboundMissCount += p.InboundMissCount;
                p.InboundMeleeCount = 0;
                p.InboundMissCount = 0;
                p.TankDPS.Clear();
                p.InboundHPS.Clear();
                p.DefenseTypes.Clear();
            }

            // healing: anyone that does less than 10% of top healer
            min = Participants.Max(x => x.OutboundHealSum) * 0.1;
            foreach (var p in Participants.Where(x => x.OutboundHealSum < min))
            {
                other.OutboundHealSum += p.OutboundHealSum;
                p.OutboundHealSum = 0;
                p.HPS.Clear();
                foreach (var h in p.Heals)
                {
                    var ot = other.Heals.Find(x => x.Target == h.Target);
                    if (ot == null)
                    {
                        ot = new FightHeal() { Target = h.Target };
                        other.Heals.Add(h);
                    }
                    ot.HitSum += h.HitSum;
                    ot.HitCount += h.HitCount;
                }
                p.Heals.Clear();
                p.Spells.RemoveAll(x => x.Type == "heal");
            }


            if (other.OutboundHitSum > 0 || other.OutboundHealSum > 0 || other.InboundMeleeSum > 0)
                Participants.Add(other);

            Participants.RemoveAll(x => x.OutboundHealSum == 0 && x.OutboundHitSum == 0 && x.InboundHitSum == 0 && !x.DPS.Any(y => y > 0));
        }

        /// <summary>
        /// Perform final summarization, sorting and clean-up once a fight has completed.
        /// </summary>
        public virtual void Finish()
        {
            Duration = TotalDuration;
            var ticks = Duration / 6;

            if (Participants.Count == 0)
                return;

            // merge pets prior to sorting 
            MergePets();

            // get top performers
            TopHitSum = Participants.Max(x => x.OutboundHitSum);
            TopHealSum = Participants.Max(x => x.OutboundHealSum);

            // sort participants by damage
            Participants = Participants.OrderByDescending(x => x.OutboundHitSum).ThenBy(x => x.Name).ToList();

            // update participants (including target)
            foreach (var p in ParticipantsAndTarget)
            {
                // calculate active duration
                if (p.FirstAction.HasValue && p.LastAction.HasValue)
                    p.Duration = (int)(p.LastAction.Value - p.FirstAction.Value).TotalSeconds + 1;

                // sort attacks in damage order
                p.AttackTypes = p.AttackTypes.OrderByDescending(x => x.HitSum).ThenBy(x => x.Type).ToList();

                // sort defense types in game check order
                p.DefenseTypes = p.DefenseTypes.OrderBy(x => Array.IndexOf(FightMiss.MissOrder, x.Type)).ToList();

                // attempts should only count the remaining hit attempts after earlier checks fail
                var attempts = p.InboundMeleeCount + p.InboundMissCount - p.InboundStrikeCount;
                foreach (var def in p.DefenseTypes.Where(x => FightMiss.MissOrder.Contains(x.Type)))
                {
                    def.Attempts = attempts;
                    attempts -= def.Count;
                }
                p.InboundStrikeProneCount = p.InboundMissCount - p.DefenseTypes.Where(x => x.Type == "miss" || x.Type == "invul" || x.Type == "rune").Sum(x => x.Count);

                // DPS buckets may be missing tail entries if no data was recorded
                // these are important if we are going to be combining fights
                //while (p.DPS.Count < ticks)
                //    p.DPS.Add(0);
                //while (p.TankDPS.Count < ticks)
                //    p.TankDPS.Add(0);

                // sort spells in damage/healing order
                p.Spells = p.Spells.OrderBy(x => x.Type).ThenByDescending(x => x.HitSum).ThenBy(x => x.Name).ToList();

                // remove heals that always landed for 0
                // groups heals and songs will produce a lot of spam entries like this
                p.Heals.RemoveAll(x => x.HitSum == 0);

                // sort heals in healing order
                p.Heals = p.Heals.OrderByDescending(x => x.HitSum).ToList();

                // empty unused arrays
                if (p.DPS.All(x => x == 0))
                    p.DPS.Clear();
                if (p.HPS.All(x => x == 0))
                    p.HPS.Clear();
                if (p.InboundHPS.All(x => x == 0))
                    p.InboundHPS.Clear();
                if (p.TankDPS.All(x => x == 0))
                    p.TankDPS.Clear();
            }

            // update target
            Target.Duration = Duration;

            // don't track participants that were mostly idle
            // these were probably added to the fight via casting events
            Participants.RemoveAll(x => x.OutboundHealSum == 0 && x.OutboundHitSum == 0 && x.InboundHitSum == 0 && !x.DPS.Any(y => y > 0));
        }

        /// <summary>
        /// Replace real player names with fake names and return a decoder dictionary that can be
        /// used to undo the anonymization. This should be run after MergePets()
        /// </summary>
        public Dictionary<string, string> Anonymize()
        {
            Player = null;
            Server = null;
            MobNotes = null;

            // todo: don't anonymize charm pet names
            var names = new Dictionary<string, string>();
            // target name doesnt get anonymized
            names[Target.Name] = Target.Name;
            var i = 0;
            string Anon(string name)
            {
                if (names.TryGetValue(name, out string alias))
                    return alias;

                i++;
                alias = "Player" + i;
                names[name] = alias;
                return alias;
            }

            foreach (var p in Participants)
            {
                p.Name = Anon(p.Name);
                if (p.PetOwner != null)
                    p.PetOwner = Anon(p.PetOwner);

                foreach (var h in p.Heals)
                    h.Target = Anon(h.Target);
            }

            // invert the dictionary from "real=>alias" to "alias=>real"
            return names.ToDictionary(x => x.Value, x => x.Key);
        }

        /// <summary>
        /// Merge all pet damage into their owner's damage.
        /// The pet will be left in the participant list (as a tanking/healing target).
        /// </summary>
        public void MergePets()
        {
            // using a temporary list since we're modifying the collection while enumerating it
            var pets = Participants.Where(x => x.PetOwner != null).ToList();

            foreach (var pet in pets)
            {
                AddParticipant(pet.PetOwner).MergePet(pet);

                // append owner name to pet name - this should probably be done client side
                // also removing because this isn't handled well in anonymize
                //if (!pet.Name.StartsWith(pet.PetOwner))
                //    pet.Name = String.Format("{0} ({1})", pet.Name, pet.PetOwner);
            }

            // get top performers
            TopHitSum = Participants.Max(x => x.OutboundHitSum);
            TopHealSum = Participants.Max(x => x.OutboundHealSum);
        }
    }
}
