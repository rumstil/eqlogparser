using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    /// <summary>
    /// An aggregate of damage that occurs in a fight: melee hit, ranged hit, spell, proc.
    /// </summary>
    public class FightHit
    {
        public string Type;
        public int HitCount;
        public int HitSum;
        public int CritCount;
        public int CritSum;
        //public int HitMin;
        //public int HitMax;

        public void Add(FightHit x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
            CritSum += x.CritSum;
            CritCount += x.CritCount;
            //if (HitMin > x.HitMin || HitMin == 0)
            //    HitMin = x.HitMin;
            //if (HitMax < x.HitMax)
            //    HitMax = x.HitMax;
        }
    }

    /// <summary>
    ///  An aggregate of defense that occurs in a fight: invulnerability, riposte, parry, dodge, block, miss, etc..
    /// </summary>
    public class FightMiss
    {
        public static readonly string[] MissOrder = new[] { "invul", "riposte", "parry", "dodge", "block", "miss", "rune" };

        public string Type;
        public int Count;
        public int Attempts;

        public void Add(FightMiss x)
        {
            Count += x.Count;
            Attempts += x.Attempts;
        }
    }

    /// <summary>
    /// An aggregate of any spell that landed or was cast during a fight.
    /// </summary>
    public class FightSpell
    {
        public string Name;
        public int ResistCount;
        public int HitCount;
        public int HitSum;
        //public int HitMin;
        //public int HitMax;
        public int CritCount;
        public int CritSum;
        public int TwinCount;
        public int HealCount;
        public int HealSum;
        public int HealGross;
        //public int HealMin; // heal min is going to be 0 often
        //public int HealMax;

        /// <summary>
        /// Each time the spell is cast an entry is added with the # seconds from start of fight
        /// </summary>
        public List<int> Times = new List<int>();

        public void Add(FightSpell x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
            CritSum += x.CritSum;
            CritCount += x.CritCount;
            HealSum += x.HealSum;
            HealGross += x.HealGross;
            HealCount += x.HealCount;
            //if (HitMin > x.HitMin || HitMin == 0)
            //    HitMin = x.HitMin;
            //if (HitMax < x.HitMax)
            //    HitMax = x.HitMax;
            //if (HealMax < x.HealMax)
            //    HealMax = x.HealMax;
        }

    }

    /// <summary>
    /// Any buff or debuff that landed on a character during a fight.
    /// </summary>
    public class FightEffect
    {
        public string Name;
        //public List<(int Start, int End)> Times = new List<(int, int)>();
    }

    /// <summary>
    /// A short history of activity that occured before a death.
    /// </summary>
    public class FightDeath
    {
        public string Name;
        public string Class;
        //public DateTime Timestamp;
        public int Time;
        public List<string> Replay = new List<string>();
    }

    /// <summary>
    /// A summary of activity for a player or mob in a single fight.
    /// </summary>
    public class FightParticipant
    {
        public string Name;
        public string PetOwner;
        public string Class;

        public DateTime? FirstAction;
        public DateTime? LastAction;

        /// <summary>
        /// Damage activity duration in seconds. Can be zero.
        /// </summary>
        public int Duration => FirstAction.HasValue && LastAction.HasValue ? (int)(LastAction.Value - FirstAction.Value).TotalSeconds + 1 : 0;

        public int OutboundMissCount;
        public int OutboundHitCount; // includes all damage
        public int OutboundHitSum;
        public int OutboundMeleeCount; // includes melee/combat skills
        public int OutboundMeleeSum;
        public int OutboundSpellCount; // includes dd/dot  
        public int OutboundSpellSum;
        //public int OutboundRiposteSum;
        public int OutboundStrikeCount;

        public int InboundMissCount;
        public int InboundHitCount;
        public int InboundHitSum;
        public int InboundMeleeCount;
        public int InboundMeleeSum;
        public int InboundRiposteSum;
        //public int InboundSpellCount;
        //public int InboundSpellSum;

        public int OutboundHealSum;
        public int InboundHealSum;

        public int DeathCount;

        // store damage, tanking, healing summaries at fixed intervals rather than storing every data point
        // e.g. storing 6 seconds worth of hits as 1 integer takes a lot less space than 30 hits
        public List<int> DPS = new List<int>();
        public List<int> HPS = new List<int>();
        public List<int> TankDPS = new List<int>();

        public List<FightHit> AttackTypes = new List<FightHit>();
        public List<FightMiss> DefenseTypes = new List<FightMiss>();
        public List<FightSpell> Spells = new List<FightSpell>();
        public List<FightEffect> Effects = new List<FightEffect>();

        public override string ToString()
        {
            if (PetOwner != null)
                return String.Format("{0} - Pet ({1})", Name, PetOwner);

            if (Class != null)
                return String.Format("{0} - {1}", Name, Class);

            return Name;
        }

        public FightParticipant(string name)
        {
            Name = name;
        }

        public void AddHit(LogHitEvent hit, int interval)
        {
            if (FirstAction == null)
                FirstAction = hit.Timestamp;
            LastAction = hit.Timestamp;

            if (hit.Source == Name)
            {
                OutboundHitCount += 1;
                OutboundHitSum += hit.Amount;

                if (hit.Spell != null)
                {
                    OutboundSpellCount += 1;
                    OutboundSpellSum += hit.Amount;

                    var spell = AddSpell(hit.Spell);
                    spell.HitCount += 1;
                    spell.HitSum += hit.Amount;
                    //if (hit.Amount > spell.HitMax)
                    //    spell.HitMax = hit.Amount;
                    //if (hit.Amount < spell.HitMin || spell.HitMin == 0)
                    //    spell.HitMin = hit.Amount;

                    if (hit.Mod.HasFlag(LogEventMod.Critical))
                    {
                        spell.CritCount += 1;
                        spell.CritSum += hit.Amount;
                    }

                    if (hit.Mod.HasFlag(LogEventMod.Twincast))
                    {
                        spell.TwinCount += 1;
                        //spell.TwinSum += hit.Amount;
                    }

                }
                else
                {
                    OutboundMeleeCount += 1;
                    OutboundMeleeSum += hit.Amount;
                }

                var type = hit.Type;

                // alter the attack type on some special hits
                if (hit.Mod.HasFlag(LogEventMod.Finishing_Blow))
                    type = "finish";
                //else if (hit.Mod.HasFlag(LogEventMod.Headshot))
                //    type += ":headshot";
                //else if (hit.Mod.HasFlag(LogEventMod.Assassinate))
                //    type += ":assassinate";
                else if (hit.Mod.HasFlag(LogEventMod.Special))
                    type += ":special";
                else if (hit.Mod.HasFlag(LogEventMod.Riposte))
                    type = "riposte";

                var at = AddAttack(type);
                at.HitCount += 1;
                at.HitSum += hit.Amount;
                //if (hit.Amount > at.HitMax)
                //    at.HitMax = hit.Amount;
                //if (hit.Amount < at.HitMin || at.HitMin == 0)
                //    at.HitMin = hit.Amount;

                if (hit.Mod.HasFlag(LogEventMod.Critical))
                {
                    at.CritCount += 1;
                    at.CritSum += hit.Amount;
                }

                /*
                if (hit.Mod.HasFlag(LogEventMod.Riposte))
                {
                    // riposte type is a duplicate and should not be included in the overall sum
                    // it might make more sense to add it as an attribute (like crit damage)
                    var rip = AddAttack("riposte");
                    rip.HitCount += 1;
                    rip.HitSum += hit.Amount;
                }
                */

                if (hit.Mod.HasFlag(LogEventMod.Strikethrough))
                {
                    OutboundStrikeCount += 1;
                }

                while (DPS.Count <= interval)
                    DPS.Add(0);
                DPS[interval] += hit.Amount;

            }
            else if (hit.Target == Name)
            {
                InboundHitCount += 1;
                InboundHitSum += hit.Amount;

                if (hit.Spell != null)
                {
                    //InboundSpellSum += hit.Amount;
                }
                else
                {
                    InboundMeleeCount += 1;
                    InboundMeleeSum += hit.Amount;

                    while (TankDPS.Count <= interval)
                        TankDPS.Add(0);
                    TankDPS[interval] += hit.Amount;

                    //TankHits.TryGetValue(hit.Amount, out int count);
                    //TankHits[hit.Amount] = count + 1;
                }

                if (hit.Mod.HasFlag(LogEventMod.Riposte))
                {
                    InboundRiposteSum += hit.Amount;
                }

            }
        }

        public void AddMiss(LogMissEvent miss, int interval)
        {
            if (FirstAction == null)
                FirstAction = miss.Timestamp;
            LastAction = miss.Timestamp;

            // don't count spell resist/invul in defense stats
            if (miss.Spell != null)
            {
                if (miss.Source == Name)
                {
                    var spell = AddSpell(miss.Spell);
                    spell.ResistCount += 1;
                }
                return;
            }

            if (miss.Source == Name)
            {
                OutboundMissCount += 1;

                //if (miss.Spell != null)
                //{
                //    var spell = AddSpell(miss.Spell);
                //    spell.ResistCount += 1;
                //}
            }
            else if (miss.Target == Name)
            {
                InboundMissCount += 1;

                var dt = DefenseTypes.FirstOrDefault(x => x.Type == miss.Type);
                if (dt == null)
                {
                    dt = new FightMiss();
                    dt.Type = miss.Type;
                    DefenseTypes.Add(dt);
                }
                dt.Count += 1;
            }
        }

        public void AddHeal(LogHealEvent heal, int interval)
        {
            if (FirstAction == null)
                FirstAction = heal.Timestamp;
            LastAction = heal.Timestamp;

            // promised heals appear as self heals 
            // we may want to ignore them for self healing stats
            //var promised = heal.Source == Name && heal.Target == Name && heal.Spell != null && heal.Spell.StartsWith("Promised");

            //if (heal.Source == Name && heal.Target == Name)
            //{
            //    SelfHealSum += heal.Amount;
            //}

            if (heal.Source == Name)
            {
                OutboundHealSum += heal.Amount;

                if (heal.Spell != null)
                {
                    var spell = AddSpell(heal.Spell);
                    spell.HealCount += 1;
                    spell.HealSum += heal.Amount;
                    spell.HealGross += heal.GrossAmount;
                    //if (heal.Amount > spell.HealMax)
                    //    spell.HealMax = heal.Amount;

                    if (heal.Mod.HasFlag(LogEventMod.Critical))
                    {
                        spell.CritCount += 1;
                        spell.CritSum += heal.Amount;
                    }
                }

                while (HPS.Count <= interval)
                    HPS.Add(0);
                HPS[interval] += heal.Amount;
            }

            if (heal.Target == Name)
            {
                InboundHealSum += heal.Amount;
            }
        }

        public void AddCasting(LogCastingEvent cast, int time)
        {
            var spell = AddSpell(cast.Spell);
            spell.Times.Add(time);
        }

        private FightSpell AddSpell(string name)
        {
            var spell = Spells.FirstOrDefault(x => x.Name == name);
            if (spell == null)
            {
                spell = new FightSpell() { Name = name };
                Spells.Add(spell);
            }
            return spell;
        }

        private FightHit AddAttack(string type)
        {
            //var at = AttackTypes.FirstOrDefault(x => x.Type == hit.Type);
            FightHit at = null;
            for (int i = 0; i < AttackTypes.Count; i++)
                if (AttackTypes[i].Type == type)
                {
                    at = AttackTypes[i];
                    break;
                }

            if (at == null)
            {
                at = new FightHit();
                at.Type = type;
                AttackTypes.Add(at);
            }

            return at;
        }
    }

    public enum FightStatus
    {
        Active,
        Killed,
        Timeout,
        Interval
    }

    /// <summary>
    /// A summary of activity for a single fight.
    /// </summary>
    public class FightSummary
    {
        public string Version = "1";
        public string OwnerId;
        public string Id;
        public DateTime StartedOn;
        public DateTime UpdatedOn;
        public FightStatus Status;
        public string Player;
        public string Server;
        public string Zone;
        public string Name;
        public string Party;
        public bool IsRare;
        public int CohortCount;
        public int TopHitSum;
        public int TopHealSum;
        public List<FightDeath> Deaths = new List<FightDeath>();
        // special target counters
        //public int StrikeCount;

        //public FightHitEvent LastHit;
        //public FightHitEvent LastSpell;

        public int HP => Target.InboundHitSum;

        /// <summary>
        /// Fight duration in seconds. Always at least 1.
        /// </summary>
        public int Duration => (int)(UpdatedOn - StartedOn).TotalSeconds + 1;

        //public int Interval => Duration / 6;

        public FightParticipant Target;

        public List<FightParticipant> Participants = new List<FightParticipant>();

        public FightSummary()
        {

        }

        public FightSummary(string name)
        {
            Name = name;
            Target = new FightParticipant(name);
        }

        private FightParticipant AddParticipant(string name)
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
                p = new FightParticipant(name);
                Participants.Add(p);
            }
            return p;
        }

        private int GetInterval(LogEvent e)
        {
            // going to store DPS at 6 second intervals - this smooths the damage spikes and produces a better
            // comparison between DoT DPS and other DPS
            var interval = (int)(e.Timestamp - StartedOn).TotalSeconds / 6;
            // if the log is altered and entries are saved out of order then our intervals might be negative
            if (interval < 0)
                interval = 0;
            return interval;
        }

        public void AddHit(LogHitEvent hit)
        {
            var interval = GetInterval(hit);
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
            var interval = GetInterval(miss);
            AddParticipant(miss.Source).AddMiss(miss, interval);
            AddParticipant(miss.Target).AddMiss(miss, interval);
        }

        public void AddHeal(LogHealEvent heal)
        {
            var interval = GetInterval(heal);
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
            AddParticipant(cast.Source).AddCasting(cast, Duration - 1);
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
        /// Finalize fight info when it is finished.
        /// </summary>
        public void Finish()
        {
            var ticks = Duration / 6;

            if (Participants.Count == 0)
                return;

            // get top performers
            TopHitSum = Participants.Max(x => x.OutboundHitSum);
            TopHealSum = Participants.Max(x => x.OutboundHealSum);

            // sort by most damage to least damage
            Participants.Sort((a, b) => b.OutboundHitSum - a.OutboundHitSum);
            foreach (var p in Participants)
            {
                // sort attacks in alpha order
                p.AttackTypes.Sort((a, b) => a.Type.CompareTo(b.Type));

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

                // sort spells in alpha order
                p.Spells.Sort((a, b) => a.Name.CompareTo(b.Name));
            }

            // don't track replays on wipes because they take too much space
            // and by then things have gone downhill too much
            if (Deaths.Count > 10)
            {
                foreach (var d in Deaths)
                    d.Replay.Clear();
            }

            MergePets();
        }

        /// <summary>
        /// Merge all pet damage into their owner's damage.
        /// Tanking will remain unmerged.
        /// </summary>
        public void MergePets()
        {
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
                        match.Add(at);
                    }
                    else
                    {
                        owner.AttackTypes.Add(at);
                    }
                }

                foreach (var spell in pet.Spells)
                {
                    spell.Name = "pet:" + spell.Name;
                    owner.Spells.Add(spell);
                }

                owner.OutboundHitCount += pet.OutboundHitCount;
                owner.OutboundHitSum += pet.OutboundHitSum;
                owner.OutboundMissCount += pet.OutboundMissCount;

                // removing the pet has the downside of hiding pet tanking
                //Participants.Remove(pet);

                // it's probably better to clear the damage on the pet but keep it for tanking stats
                pet.OutboundHitCount = 0;
                pet.OutboundHitSum = 0;
                pet.OutboundMissCount = 0;
                pet.AttackTypes.Clear();
                pet.Spells.Clear();
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

        /// <summary>
        /// Replace real player names with fake names and return a decoder dictionary that can be
        /// used to undo the anonymization.
        /// </summary>
        public Dictionary<string, string> Anonymize()
        {
            // todo: don't anonymize charm pet names
            var names = new Dictionary<string, string>();

            var i = 1;
            foreach (var p in Participants)
            {
                i++;
                var alias = p.PetOwner == null ? "Player" + i : "Pet" + i;
                names[p.Name] = alias;
                p.Name = alias;
            }

            foreach (var p in Participants.Where(x => x.PetOwner != null))
            {
                if (names.TryGetValue(p.PetOwner, out string alias))
                {
                    p.PetOwner = alias;
                }
                else
                {
                    // this shouldn't happen
                    p.PetOwner = "Unknown";
                }
            }

            foreach (var d in Deaths)
            {
                if (names.TryGetValue(d.Name, out string alias))
                {
                    d.Name = alias;
                }
                else
                {
                    // this shouldn't happen
                    d.Name = "Unknown";
                }

            }

            // invert the dictionary: alias=>real
            return names.ToDictionary(x => x.Value, x => x.Key);
        }

    }
}
