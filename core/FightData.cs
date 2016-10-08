using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EQLogParser
{
    /// <summary>
    /// A summary of log activity within an defined segment like a fight, raid event, or time period.
    /// </summary>
    /*
    public class LogSummary
    {
        List<FightHit> Hits = new List<FightHit>();

        public void AddHit(FightHit hit)
        {
        }
    }
    */

    /// <summary>
    /// A summary of log activity for a single damage type.
    /// </summary>
    public class CombatantHit
    {
        public string Type;
        public int NormalHitCount;
        public int NormalHitSum;
        public int NormalMinHit;
        public int NormalMaxHit;
        public int CritHitCount;
        public int CritHitSum;
        public int CritMinHit;
        public int CritMaxHit;

        public override string ToString()
        {
            return Type;
        }
    }

    public class CombatantMiss
    {
        public string Type;
        public int Count;
    }

    /// <summary>
    /// A summary of activity for a single player or mob.
    /// </summary>
    public class Combatant //: LogSummary
    {
        public readonly string Name;
        public int SourceMissCount;
        public int SourceHitCount;
        public int SourceHitSum;
        public int TargetMissCount;
        public int TargetHitCount;
        public int TargetHitSum;
        //public int TargetHealCount;
        //public int TargetHealSum;
        //public FightHitEvent LastHit;
        public List<CombatantHit> AttackTypes = new List<CombatantHit>();
        //public List<CombatantHit> AttackSpells = new List<CombatantHit>();
        public List<CombatantMiss> DefenseTypes = new List<CombatantMiss>();
        public List<SpellCastingEvent> Casting = new List<SpellCastingEvent>();

        public override string ToString()
        {
            return Name;
        }

        public Combatant(string name)
        {
            Name = name;
        }

        public void AddHit(FightHitEvent hit)
        {
            if (hit.Source == Name)
            {
                SourceHitCount += 1;
                SourceHitSum += hit.Amount;

                var at = AttackTypes.FirstOrDefault(x => x.Type == hit.Type);
                if (at == null)
                {
                    at = new CombatantHit();
                    at.Type = hit.Type;
                    AttackTypes.Add(at);
                }
                at.NormalHitCount += 1;
                at.NormalHitSum += hit.Amount;

                //if (hit.Amount > ht.MaxHit)
                //    ht.MaxHit = hit.Amount;
            }
            else if (hit.Target == Name)
            {
                TargetHitCount += 1;
                TargetHitSum += hit.Amount;
            }
        }

        public void AddMiss(FightMissEvent miss)
        {
            if (miss.Source == Name)
            {
                SourceMissCount += 1;

            }
            else if (miss.Target == Name)
            {
                TargetMissCount += 1;

                var dt = DefenseTypes.FirstOrDefault(x => x.Type == miss.Type);
                if (dt == null)
                {
                    dt = new CombatantMiss();
                    dt.Type = miss.Type;
                    DefenseTypes.Add(dt);
                }
                dt.Count += 1;

            }
        }

        public void AddHeal(HealEvent heal)
        {

        }
    }

    /// <summary>
    /// A summary of activity for a single fight.
    /// </summary>
    public class Fight //: LogSummary
    {
        public DateTime Started;
        public DateTime LastActive;
        public DateTime? Finished;
        public string Server;
        public string Zone;
        public Combatant Opponent;
        //public FightHitEvent LastHit;
        //public FightHitEvent LastMelee;
        //public FightHitEvent LastSpell;

        /// <summary>
        /// A list of combatants in the fight. The opponent will be the first entry.
        /// </summary>
        public List<Combatant> Participants = new List<Combatant>();

        public override string ToString()
        {
            return String.Format("{0} ({1}) - {2}", Opponent.Name, Zone, Started);
        }
    }
}
