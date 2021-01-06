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
        public string Version { get; set; } = "1";
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
        //public bool IsRare { get; set; }
        public string MobNotes { get; set; }
        public int MobCount { get; set; } // if this is a consolidated fight, this will be the original number of fights
        public int CohortCount { get; set; } // number of active fights when this finished
        public long TopHitSum { get; set; }
        public long TopHealSum { get; set; }
        public List<FightDeath> Deaths { get; set; } = new List<FightDeath>();
            
        /// <summary>
        /// Mob being attacked.
        /// </summary>
        public FightParticipant Target { get; set; }

        /// <summary>
        /// Friends attacking the mob or assisting them.
        /// </summary>
        public List<FightParticipant> Participants { get; set; } = new List<FightParticipant>();

        // special target counters
        //public int StrikeCount;
        //public FightHitEvent LastHit;
        //public FightHitEvent LastSpell;

        /// <summary>
        /// Fight duration in seconds. Always at least 1.
        /// </summary>
        public int Duration { get; set; }
        
        public int Elapsed => (int)(UpdatedOn - StartedOn).TotalSeconds + 1;

        public long HP => Target.InboundHitSum;


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
            // only count target if it's not a self heal (otherwise we would double count)
            // temporarily removing this because it adds afk players that are soaking up group heals
            //if (heal.Source != heal.Target)
            //    AddParticipant(heal.Target).AddHeal(heal, interval);
        }

        public void AddCasting(LogCastingEvent cast)
        {
            AddParticipant(cast.Source).AddCasting(cast, Elapsed - 1);
        }

        //public void AddCastingFail(LogCastingFailEvent cast)
        //{
        //}

        public void AddDeath(LogDeathEvent death, IEnumerable<LogEvent> replay)
        {
            AddParticipant(death.Name).DeathCount += 1;

            var audit = new FightDeath();
            //audit.Timestamp = death.Timestamp;
            audit.Time = (int)(death.Timestamp - StartedOn).TotalSeconds;
            audit.Name = death.Name;

            foreach (var e in replay)
            {
                // show time as # seconds prior to death
                var time = (e.Timestamp - death.Timestamp).TotalSeconds.ToString() + "s";
                if (e is LogHitEvent hit && hit.Target == death.Name)
                    audit.Replay.Add(String.Format("Hit: {0} for {1}", hit.Source, hit.Amount, time));
                if (e is LogHealEvent heal && heal.Target == death.Name)
                    audit.Replay.Add(String.Format("Heal: {0} for {1}", heal.Source, heal.Amount, time));
                // healers can't be expected to respond to runes - maybe don't show these
                //if (e is LogMissEvent miss && miss.Target == death.Name && miss.Type == "rune")
                //    audit.Replay.Add(miss.ToString() + time);
            }

            Deaths.Add(audit);
        }

        public override string ToString()
        {
            return String.Format("{0} ({1}) - {2}", Target.Name, Zone, StartedOn);
        }

        /// <summary>
        /// Perform final summarization, sorting and clean-up once a fight has completed.
        /// </summary>
        public virtual void Finish()
        {
            Duration = Elapsed;
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
            var everyone = Participants.ToList();
            everyone.Add(Target);
            foreach (var p in everyone)
            {
                // calculate active duration
                if (p.FirstAction.HasValue && p.LastAction.HasValue)
                    p.Duration = (int)(p.LastAction.Value - p.FirstAction.Value).TotalSeconds + 1;

                // sort attacks in damage order
                p.AttackTypes = p.AttackTypes.OrderByDescending(x => x.HitSum).ThenBy(x => x.Type).ToList();

                // sort defense types in game check order
                p.DefenseTypes = p.DefenseTypes.OrderBy(x => Array.IndexOf(FightMiss.MissOrder, x.Type)).ToList();

                // attempts should only count the remaining hit attempts after earlier checks fail
                var attempts = p.InboundHitCount + p.InboundMissCount;
                foreach (var def in p.DefenseTypes.Where(x => FightMiss.MissOrder.Contains(x.Type)))
                {
                    def.Attempts = attempts;
                    attempts -= def.Count;
                }

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
                if (p.TankDPS.All(x => x == 0))
                    p.TankDPS.Clear();
            }

            // update target
            Target.Duration = Duration;

            // don't track participants that were mostly idle
            // these were probably added to the fight via casting events
            Participants.RemoveAll(x => x.OutboundHealSum == 0 && x.OutboundHitSum == 0 && x.InboundHitSum == 0 && !x.DPS.Any(y => y > 0));

            // don't track replays on wipes because they take too much space
            // and by then things have gone downhill too much
            if (Deaths.Count > 10)
            {
                foreach (var d in Deaths)
                    d.Replay.Clear();
            }

            // don't track pet deaths
            Deaths.RemoveAll(x => AddParticipant(x.Name).PetOwner != null);
        }

        /// <summary>
        /// Replace real player names with fake names and return a decoder dictionary that can be
        /// used to undo the anonymization. This should be run after MergePets()
        /// </summary>
        public Dictionary<string, string> Anonymize()
        {
            Player = null;
            Server = null;

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

            foreach (var d in Deaths)
            {
                d.Name = Anon(d.Name);
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
                var owner = AddParticipant(pet.PetOwner);

                foreach (var at in pet.AttackTypes)
                {
                    at.Type = "pet:" + at.Type;
                    //owner.AttackTypes.Add(hit);
                    var match = owner.AttackTypes.FirstOrDefault(x => x.Type == at.Type);
                    if (match != null)
                    {
                        match.Merge(at);
                    }
                    else
                    {
                        owner.AttackTypes.Add(at);
                    }
                }

                foreach (var spell in pet.Spells.Where(x => x.Type == "hit"))
                {
                    spell.Name = "pet:" + spell.Name;
                    var match = owner.Spells.FirstOrDefault(x => x.Type == spell.Type && x.Name == spell.Name);
                    if (match != null)
                    {
                        match.Merge(spell);
                    }
                    else
                    {
                        owner.Spells.Add(spell);
                    }
                }

                owner.OutboundHitCount += pet.OutboundHitCount;
                owner.OutboundHitSum += pet.OutboundHitSum;
                owner.OutboundMissCount += pet.OutboundMissCount;

                // merges DPS and HPS intervals (but not TankDPS)
                for (var i = 0; i < pet.DPS.Count; i++)
                {
                    while (owner.DPS.Count <= i)
                        owner.DPS.Add(0);
                    owner.DPS[i] += pet.DPS[i];
                }

                for (var i = 0; i < pet.HPS.Count; i++)
                {
                    while (owner.HPS.Count <= i)
                        owner.HPS.Add(0);
                    owner.HPS[i] += pet.HPS[i];
                }

                // removing the pet has the downside of hiding pet tanking
                //Participants.Remove(pet);

                // clear the damage on the pet but keep it for tanking stats
                pet.OutboundHitCount = 0;
                pet.OutboundHitSum = 0;
                pet.OutboundMissCount = 0;
                pet.AttackTypes.Clear();
                //pet.Spells.Clear();
                pet.Spells.RemoveAll(x => x.Type == "hit"); // leave heals on pet so it shows on the healer list
                pet.DPS.Clear();
                pet.HPS.Clear();

                // append owner name to pet name - this should probably be done client side
                // also removing because this isn't handled well in anonymize
                //if (!pet.Name.StartsWith(pet.PetOwner))
                //    pet.Name = String.Format("{0} ({1})", pet.Name, pet.PetOwner);
            }

            // get top performers
            TopHitSum = Participants.Max(x => x.OutboundHitSum);
            TopHealSum = Participants.Max(x => x.OutboundHealSum);
        }

        public void WriteAll(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("**{0}** {1:N0} HP in {2}s at {3}", Name, Target.InboundHitSum, Duration, StartedOn.ToLocalTime(), Zone);
            if (Participants.Count == 0)
                return;

            var top = Participants.Max(x => x.OutboundHitSum);
            foreach (var p in Participants)
            {
                var pct = (float)p.OutboundHitSum / top;

                writer.WriteLine(" {0} {1:P0} {2} to {3}", p, pct, p.FirstAction?.ToLocalTime().ToString("T"), p.LastAction?.ToLocalTime().ToString("T"));

                writer.WriteLine("   {0,-10} {1,11:N0} / {2,6:N0} DPS", "total", p.OutboundHitSum, p.OutboundHitSum / Duration);
                foreach (var ht in p.AttackTypes)
                    writer.WriteLine("   {0,-10} {1,11:N0} / {2,6:N0} DPS", ht.Type, ht.HitSum, ht.HitSum / Duration);

                foreach (var d in p.DefenseTypes)
                    writer.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*" + d.Type + "*", d.Count, d.Attempts, (double)d.Count / d.Attempts);

                writer.WriteLine("   {0,-10} {1,6:N0} of {2} {3:P0}", "*hit*", p.InboundHitCount, p.InboundHitCount + p.InboundMissCount, (double)p.InboundHitCount / (p.InboundHitCount + p.InboundMissCount));

                foreach (var s in p.Spells)
                {
                    writer.WriteLine("   spell {0} {1}: {2:N0}", s.Type, s.Name, s.HitSum);
                }

                foreach (var h in p.Heals)
                {
                    writer.WriteLine("   healed {0}: {1:N0}", h.Target, h.HitSum);
                }


            }
        }

        public void WriteNotes(TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine("**{0}** {1} HP in {2}s at {3}", Name, FormatNum(Target.InboundHitSum), Duration, StartedOn.ToLocalTime().ToShortTimeString(), Zone);
            if (Participants.Count == 0)
                return;

            var top = Participants.Max(x => x.OutboundHitSum);
            foreach (var p in Participants.Take(10).Where(x => x.OutboundHitSum > 0))
            {
                // percent = share of damage vs mob
                //var pct = (float)p.OutboundHitSum / Target.InboundHitSum;
                // percent = damage relative to top player
                var pct = (float)p.OutboundHitSum / top;
                writer.WriteLine(" {0,-20} --- {1,4:P0} {2,8} / {3,5} DPS", p, pct, FormatNum(p.OutboundHitSum), FormatNum(p.OutboundHitSum / Duration));
            }
        }

        public static string FormatNum(long n)
        {
            if (n >= 1000000000)
                return (n / 1000000000F).ToString("F2") + 'B';
            if (n >= 1000000)
                return (n / 1000000F).ToString("F2") + 'M';
            if (n >= 10000)
                return (n / 1000F).ToString("F0") + 'K';
            if (n > 1000)
                return (n / 1000F).ToString("F1") + 'K';
            return n.ToString();
        }


    }
}
