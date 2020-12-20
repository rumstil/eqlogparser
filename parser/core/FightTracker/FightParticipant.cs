using System;
using System.Collections.Generic;
using System.Linq;


namespace EQLogParser
{
    /// <summary>
    /// A summary of activity for one player or mob in a single fight.
    /// </summary>
    public class FightParticipant
    {
        public string Name { get; set; }
        public string PetOwner { get; set; }
        public string Class { get; set; }
        public int Level { get; set; }

        public DateTime? FirstAction;
        public DateTime? LastAction;

        /// <summary>
        /// Active duration in seconds. Can be zero if someone was AFK for a fight.
        /// </summary>
        public int Duration { get; set; } // => FirstAction.HasValue && LastAction.HasValue ? (int)(LastAction.Value - FirstAction.Value).TotalSeconds + 1 : 0;

        public int OutboundMissCount { get; set; }
        public int OutboundHitCount { get; set; } // includes all damage
        public long OutboundHitSum { get; set; }
        public int OutboundStrikeCount { get; set; }

        public int InboundMissCount { get; set; }
        public int InboundHitCount { get; set; }
        public long InboundHitSum { get; set; }
        // keep separate melee stats for tanking summary
        public int InboundMeleeCount { get; set; }
        public long InboundMeleeSum { get; set; }
        public int InboundRiposteSum { get; set; }
        //public int InboundSpellCount { get; set; }
        //public int InboundSpellSum { get; set; }

        public int OutboundHealSum { get; set; }
        public int InboundHealSum { get; set; }

        public int DeathCount { get; set; }

        // store damage, tanking, healing summaries at fixed intervals rather than storing every data point
        // e.g. storing 6 seconds worth of hits as 1 integer takes a lot less space than 30 hits
        public List<int> DPS { get; set; } = new List<int>();
        public List<int> HPS { get; set; } = new List<int>();
        public List<int> TankDPS { get; set; } = new List<int>();

        public List<FightHit> AttackTypes { get; set; } = new List<FightHit>();
        public List<FightMiss> DefenseTypes { get; set; } = new List<FightMiss>();
        // a list of healing targets
        public List<FightHeal> Heals { get; set; } = new List<FightHeal>();
        // a list of outgoing dd, dot, and heals
        public List<FightSpell> Spells { get; set; } = new List<FightSpell>();
        // a list of buffs received
        public List<FightBuff> Buffs { get; set; } = new List<FightBuff>();

        public override string ToString()
        {
            if (PetOwner != null)
                return String.Format("{0} - Pet ({1})", Name, PetOwner);

            if (Class != null)
                return String.Format("{0} - {1}", Name, Class);

            return Name;
        }

        public FightParticipant()
        {
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
                    //OutboundSpellCount += 1;
                    //OutboundSpellSum += hit.Amount;

                    var spell = AddSpell(hit.Spell, "hit");
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
                    //OutboundMeleeCount += 1;
                    //OutboundMeleeSum += hit.Amount;
                }

                var type = hit.Type;

                // alter the attack type on some special hits
                if (hit.Mod.HasFlag(LogEventMod.Finishing_Blow))
                    type = "finishing";
                else if (hit.Mod.HasFlag(LogEventMod.Headshot))
                    type = "headshot";
                else if (hit.Mod.HasFlag(LogEventMod.Assassinate))
                    type = "assassinate";
                else if (hit.Mod.HasFlag(LogEventMod.Decapitate))
                    type = "decapitate";
                else if (hit.Mod.HasFlag(LogEventMod.Slay_Undead))
                    type = "slay";
                //else if (hit.Mod.HasFlag(LogEventMod.Special))
                //    type += ":special";
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

            if (miss.Spell != null)
            {
                if (miss.Source == Name)
                {
                    var spell = AddSpell(miss.Spell, "hit");
                    spell.ResistCount += 1;
                }
                // don't count spell resist/invul in defense stats
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

                // todo: should we ignore heals that land for 0?
                var h = Heals.FirstOrDefault(x => x.Target == heal.Target);
                if (h == null)
                {
                    h = new FightHeal();
                    h.Target = heal.Target;
                    Heals.Add(h);
                }
                h.HitCount += 1;
                h.HitSum += heal.Amount;

                if (heal.Spell != null)
                {
                    var spell = AddSpell(heal.Spell, "heal");
                    spell.HitCount += 1;
                    spell.HitSum += heal.Amount;
                    //spell.HealCount += 1;
                    //spell.HealSum += heal.Amount;
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
            //var spell = AddSpell(cast.Spell);
            //spell.Times.Add(time);
        }

        private FightSpell AddSpell(string name, string type)
        {
            var spell = Spells.FirstOrDefault(x => x.Name == name && x.Type == type);
            if (spell == null)
            {
                spell = new FightSpell() { Name = name, Type = type };
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

        /// <summary>
        /// Merge data from another participant into this participant.
        /// </summary>
        public void Merge(FightParticipant p, int intervalOffset = 0, int timeOffset = 0)
        {
            OutboundMissCount += p.OutboundMissCount;
            OutboundHitCount += p.OutboundHitCount;
            OutboundHitSum += p.OutboundHitSum;
            OutboundStrikeCount += p.OutboundStrikeCount;

            InboundMissCount += p.InboundMissCount;
            InboundHitCount += p.InboundHitCount;
            InboundHitSum += p.InboundHitSum;
            InboundMeleeCount += p.InboundMeleeCount;
            InboundMeleeSum += p.InboundMeleeSum; 
            InboundRiposteSum += p.InboundRiposteSum;
            //InboundSpellCount += p.InboundSpellCount;
            //InboundSpellSum += p.InboundSpellSum;

            OutboundHealSum += p.OutboundHealSum;
            InboundHealSum += p.InboundHealSum;

            DeathCount += p.DeathCount;

            // combine the total of each fight into one value
            // todo: this doesn't probably handle fights where the participant wasn't active at all (that must be handled from outside this function)
            //DPS.Add((int)p.OutboundHitSum);
            //TankDPS.Add((int)p.InboundHitSum);
            //HPS.Add((int)p.OutboundHealSum);

            // merge intervals starting at 'interval' base
            for (var i = 0; i < p.DPS.Count; i++)
            {
                while (DPS.Count <= intervalOffset + i)
                    DPS.Add(0);
                DPS[intervalOffset + i] += p.DPS[i];
            }

            for (var i = 0; i < p.TankDPS.Count; i++)
            {
                while (TankDPS.Count <= intervalOffset + i)
                    TankDPS.Add(0);
                TankDPS[intervalOffset + i] += p.TankDPS[i];
            }

            for (var i = 0; i < p.HPS.Count; i++)
            {
                while (HPS.Count <= intervalOffset + i)
                    HPS.Add(0);
                HPS[intervalOffset + i] += p.HPS[i];
            }


            foreach (var at in p.AttackTypes)
            {
                var _at = AttackTypes.FirstOrDefault(x => x.Type == at.Type);
                if (_at == null)
                {
                    _at = new FightHit();
                    _at.Type = at.Type;
                    AttackTypes.Add(_at);
                }
                _at.Merge(at);
            }

            foreach (var dt in p.DefenseTypes)
            {
                var _dt = DefenseTypes.FirstOrDefault(x => x.Type == dt.Type);
                if (_dt == null)
                {
                    _dt = new FightMiss();
                    _dt.Type = dt.Type;
                    DefenseTypes.Add(_dt);
                }
                _dt.Merge(dt);
            }

            foreach (var h in p.Heals)
            {
                var _h = Heals.FirstOrDefault(x => x.Target == h.Target);
                if (_h == null)
                {
                    _h = new FightHeal();
                    _h.Target = h.Target;
                    Heals.Add(_h);
                }
                _h.Merge(h);
            }

            foreach (var s in p.Spells)
            {
                var _s = Spells.FirstOrDefault(x => x.Name == s.Name && x.Type == s.Type);
                if (_s == null)
                {
                    _s = new FightSpell();
                    _s.Type = s.Type;
                    _s.Name = s.Name;
                    //_s.Times =  // todo
                    Spells.Add(_s);
                }
                _s.Merge(s);
            }

            // >= 0 avoids any pre fight buffs
            foreach (var b in p.Buffs.Where(x => x.Time >= 0))
            {
                if (timeOffset == 0)
                    Buffs.Add(b);
                else
                    Buffs.Add(new FightBuff { Name = b.Name, Time = b.Time + timeOffset });
            }

        }
    
    }


}
