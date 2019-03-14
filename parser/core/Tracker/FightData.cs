using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    /// <summary>
    /// Any kind of damage that occurs in a fight: melee hit, ranged hit, spell, proc.
    /// </summary>
    public class FightHit
    {
        public string Type;
        public int HitCount;
        public int HitSum;
        public int HitMin;
        public int HitMax;
        public int CritCount;
        public int CritSum;

        //public Dictionary<int, int> HitBuckets = new Dictionary<int, int>();
    }

    /// <summary>
    /// Any kind of defense that occurs in a fight: invulnerability, riposte, parry, dodge, block, miss, resist etc..
    /// </summary>
    public class FightMiss
    {
        public static readonly string[] MissOrder = new [] { "invul", "riposte", "parry", "dodge", "block", "miss", "rune" };
        
        public string Type;
        public int Count;
        public int Attempts;
    }

    public class FightSpell
    {
        public string Name;
        public int ResistCount;
        public int HitCount;
        public int HitSum;
        public int HitMin;
        public int HitMax;
        public int CritCount;
        public int CritSum;
        public int HealCount;
        public int HealSum;
        public int HealGross;
        //public int HealMin; // heal min is going to be 0 often
        public int HealMax;

        /// <summary>
        /// Each time the spell is cast an entry is added with the # seconds from start of fight
        /// </summary>
        public List<int> Times = new List<int>();  
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
        public int OutboundHitCount;
        public int OutboundHitSum;

        public int InboundMissCount;
        public int InboundHitCount;
        public int InboundHitSum;

        public int OutboundHealSum;
        public int InboundHealSum;
        public int SelfHealSum;
        //public FightHitEvent LastHit;

        // store damage, tanking, healing summaries at fixed intervals rather than storing every data point
        // e.g. storing 6 seconds worth of hits as 1 integer takes a lot less space than 30 hits
        public List<int> DPS = new List<int>();
        public List<int> HPS = new List<int>();
        public List<int> TankDPS = new List<int>();

        /// <summary>
        /// A list of successful attacks that the participant has landed.
        /// </summary>
        public List<FightHit> AttackTypes = new List<FightHit>();

        /// <summary>
        /// A list of successful defenses that the partipant has used.
        /// </summary>
        public List<FightMiss> DefenseTypes = new List<FightMiss>();

        /// <summary>
        /// A list of spells that the participant has cast.
        /// </summary>
        public List<FightSpell> Spells = new List<FightSpell>();

        public override string ToString()
        {
            if (PetOwner != null)
                return String.Format("{0} ({1} Pet)", Name, PetOwner);

            if (Class != null)
                return String.Format("{0} ({1})", Name, Class);

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

            if (hit.Type == "resist")
            {
                return;
            }

            if (hit.Source == Name)
            {
                OutboundHitCount += 1;
                OutboundHitSum += hit.Amount;

                var at = AttackTypes.FirstOrDefault(x => x.Type == hit.Type);
                if (at == null)
                {
                    at = new FightHit();
                    at.Type = hit.Type;
                    AttackTypes.Add(at);
                }

                at.HitCount += 1;
                at.HitSum += hit.Amount;
                if (hit.Amount > at.HitMax)
                    at.HitMax = hit.Amount;
                if (hit.Amount < at.HitMin || at.HitMin == 0)
                    at.HitMin = hit.Amount;

                if (hit.Special != null && hit.Special.Contains("critical"))
                {
                    at.CritCount += 1;
                    at.CritSum += hit.Amount;
                }

                while (DPS.Count <= interval)
                    DPS.Add(0);
                DPS[interval] += hit.Amount;

                if (hit.Spell != null)
                {
                    var spell = AddSpell(hit.Spell);
                    spell.HitCount += 1;
                    spell.HitSum += hit.Amount;
                    if (hit.Amount > spell.HitMax)
                        spell.HitMax = hit.Amount;
                    if (hit.Amount < spell.HitMin || spell.HitMin == 0)
                        spell.HitMin = hit.Amount;

                    if (hit.Special != null && hit.Special.Contains("critical"))
                    {
                        spell.CritCount += 1;
                        spell.CritSum += hit.Amount;
                    }
                }

            }
            else if (hit.Target == Name)
            {
                InboundHitCount += 1;
                InboundHitSum += hit.Amount;

                if (hit.Spell == null)
                {
                    while (TankDPS.Count <= interval)
                        TankDPS.Add(0);
                    TankDPS[interval] += hit.Amount;

                    //TankHits.TryGetValue(hit.Amount, out int count);
                    //TankHits[hit.Amount] = count + 1;
                }
            }
        }

        public void AddMiss(LogMissEvent miss, int interval)
        {
            if (FirstAction == null)
                FirstAction = miss.Timestamp;
            LastAction = miss.Timestamp;

            if (miss.Source == Name)
            {
                OutboundMissCount += 1;

                if (miss.Spell != null)
                {
                    var spell = AddSpell(miss.Spell);
                    spell.ResistCount += 1;
                }
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
            // promised heals appear as self heals 
            // we may want to ignore them for self healing stats
            //var promised = heal.Source == Name && heal.Target == Name && heal.Spell != null && heal.Spell.StartsWith("Promised");

            if (heal.Source == Name && heal.Target == Name)
            {
                SelfHealSum += heal.Amount;
            }

            if (heal.Source == Name)
            {
                OutboundHealSum += heal.Amount;

                if (heal.Spell != null)
                {
                    var spell = AddSpell(heal.Spell);
                    spell.HealCount += 1;
                    spell.HealSum += heal.Amount;
                    spell.HealGross += heal.GrossAmount;
                    if (heal.Amount > spell.HealMax)
                        spell.HealMax = heal.Amount;
                    //if (heal.Amount < spell.HealMin || spell.HealMin == 0)
                    //    spell.HealMin = heal.Amount;
                }

                while (HPS.Count <= interval)
                    HPS.Add(0);
                HPS[interval] += heal.Amount;
            }
            else if (heal.Target == Name)
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


    }

    /// <summary>
    /// A summary of activity for a single fight.
    /// </summary>
    public class Fight
    {
        public string Version = "1";
        public string OwnerID; // secret - cleared after upload
        public string ID;
        public DateTime Started;
        public DateTime Updated;
        public DateTime? Finished;
        public DateTime? Expires;
        public string Player;
        public string Server;
        public string Zone;
        public string Name;
        public string Party;
        public bool Rare;
        public int Deaths;
        //public int PullSize;
        //public FightHitEvent LastHit;
        //public FightHitEvent LastSpell;
        
        public int HP => Target.InboundHitSum;

        /// <summary>
        /// Fight duration in seconds. Always at least 1.
        /// </summary>
        public int Duration => (int)(Updated - Started).TotalSeconds + 1;

        //public int Interval => Duration / 6;

        public FightParticipant Target;

        public List<FightParticipant> Participants = new List<FightParticipant>();
        
        private FightParticipant AddParticipant(string name)
        {
            if (Target.Name == name)
                return Target;

            var p = Participants.FirstOrDefault(x => x.Name == name);
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
            return (int)(e.Timestamp - Started).TotalSeconds / 6;
        }

        public void AddHit(LogHitEvent hit)
        {
            var interval = GetInterval(hit);
            AddParticipant(hit.Source).AddHit(hit, interval);
            AddParticipant(hit.Target).AddHit(hit, interval);
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
            AddParticipant(heal.Source).AddHeal(heal, interval);
        }

        public void AddCasting(LogCastingEvent cast)
        {
            AddParticipant(cast.Source).AddCasting(cast, Duration - 1);
        }

        public override string ToString()
        {
            return String.Format("{0} ({1}) - {2}", Target.Name, Zone, Started);
        }

        /// <summary>
        /// Finalize fight info when it is finished.
        /// </summary>
        public void Finish()
        {
            Finished = Updated;
            var ticks = Duration / 6;

            // sort by most damage to least damage
            Participants.Sort((a, b) => b.OutboundHitSum - a.OutboundHitSum);
            foreach (var p in Participants)
            {
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
        }

        /// <summary>
        /// Merge all pet damage under their owner's damage types.
        /// </summary>
        public void MergePets()
        {
            var pets = Participants.Where(x => x.PetOwner != null).ToList();

            foreach (var pet in pets)
            {
                Participants.Remove(pet);

                var owner = AddParticipant(pet.PetOwner);

                foreach (var hit in pet.AttackTypes)
                {
                    hit.Type = "pet:" + hit.Type;
                    owner.AttackTypes.Add(hit);
                }

                foreach (var spell in pet.Spells)
                {
                    spell.Name = "pet:" + spell.Name;
                    owner.Spells.Add(spell);
                }

                owner.OutboundHitCount += pet.OutboundHitCount;
                owner.OutboundHitSum += pet.OutboundHitSum;
                owner.OutboundMissCount += pet.OutboundMissCount;
            }
        }

        /// <summary>
        /// Replace character names with fake names.
        /// </summary>
        public void Anonymize()
        {
            var i = 1;
            foreach (var p in Participants)
            {
                p.Name = (p.Class ?? "Player") + (i++);
            }
        }

    }
}
