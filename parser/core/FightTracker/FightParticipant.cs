﻿using System;
using System.Collections.Generic;
using System.Linq;


namespace EQLogParser
{
    /// <summary>
    /// A summary of activity for one player or mob.
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
        public int InboundStrikeCount { get; set; }
        public int InboundStrikeProneCount { get; set; }
        public int InboundRiposteSum { get; set; }
        //public int InboundSpellCount { get; set; }
        //public int InboundSpellSum { get; set; }

        public int OutboundHealSum { get; set; }
        public int OutboundFullHealSum { get; set; }
        public int InboundHealSum { get; set; }
        public int InboundFullHealSum { get; set; }
        //public int InboundHealCount { get; set; }
        //public int InboundNullHealCount { get; set; }

        public int DeathCount { get; set; }

        // store damage, tanking, healing summaries at fixed intervals rather than storing every data point
        // e.g. storing 6 seconds worth of hits as 1 integer takes a lot less space than 30 hits
        public List<int> DPS { get; set; } = new List<int>();
        public List<int> HPS { get; set; } = new List<int>();
        public List<int> TankDPS { get; set; } = new List<int>();
        public List<int> InboundHPS { get; set; } = new List<int>();

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

        public void AddHit(LogHitEvent hit, int interval = -1)
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
                    if (hit.Amount > spell.HitMax)
                        spell.HitMax = hit.Amount;

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
                    //if (hit.Amount > OutboundMaxHit)
                    //{
                    //    OutboundMaxHit = hit.Amount;
                    //}
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
                else if (hit.Mod.HasFlag(LogEventMod.Slay_Undead))
                    type = "slay";
                //else if (hit.Mod.HasFlag(LogEventMod.Special))
                //    type += ":special";
                else if (hit.Mod.HasFlag(LogEventMod.Riposte))
                    type = "riposte";

                var at = AddAttack(type);
                at.HitCount += 1;
                at.HitSum += hit.Amount;
                if (hit.Amount > at.HitMax)
                    at.HitMax = hit.Amount;

                if (hit.Mod.HasFlag(LogEventMod.Critical))
                {
                    at.CritCount += 1;
                    at.CritSum += hit.Amount;
                }

                if (hit.Mod.HasFlag(LogEventMod.Strikethrough))
                {
                    OutboundStrikeCount += 1;
                }

                if (interval >= 0)
                {
                    while (DPS.Count <= interval)
                        DPS.Add(0);
                    DPS[interval] += hit.Amount;
                }

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

                    if (interval >= 0)
                    {
                        while (TankDPS.Count <= interval)
                            TankDPS.Add(0);
                        TankDPS[interval] += hit.Amount;
                    }

                    //TankHits.TryGetValue(hit.Amount, out int count);
                    //TankHits[hit.Amount] = count + 1;
                }

                if (hit.Mod.HasFlag(LogEventMod.Riposte))
                {
                    InboundRiposteSum += hit.Amount;
                }

                if (hit.Mod.HasFlag(LogEventMod.Strikethrough))
                {
                    InboundStrikeCount += 1;

                    // strikethroughs are reported on hits and riposte, but other defenses do get not reported on a strikethrough
                    // in order to properly count these defenses we should log a mystery defense that was never reported
                    // i.e. for riposte the log will show this:
                    // [Wed Jan 19 20:54:58 2022] A shadowbone tries to bash YOU, but YOU riposte!(Strikethrough)
                    // [Wed Jan 19 20:54:58 2022] A shadowbone bashes YOU for 5625 points of damage. (Riposte Strikethrough)
                    // but for dodge/parry/etc.. it will only show this:
                    // [Wed Jan 19 20:54:58 2022] A shadowbone bashes YOU for 5625 points of damage. (Strikethrough)
                    if (!hit.Mod.HasFlag(LogEventMod.Riposte))
                        AddMiss(new LogMissEvent { Timestamp = hit.Timestamp, Source = hit.Source, Target = hit.Target, Type = "unknown" });
                }

            }
        }

        public void AddMiss(LogMissEvent miss, int interval = -1)
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

        public void AddHeal(LogHealEvent heal, int interval = -1)
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
                OutboundFullHealSum += heal.FullAmount;

                var h = Heals.FirstOrDefault(x => x.Target == heal.Target);
                if (h == null)
                {
                    h = new FightHeal();
                    h.Target = heal.Target;
                    Heals.Add(h);
                }
                h.HitCount += 1;
                h.HitSum += heal.Amount;
                h.FullHitSum += heal.FullAmount;

                // should we label unknown heals as lifetap or unknown?
                var spell = AddSpell(heal.Spell ?? "Lifetap", "heal");
                spell.HitCount += 1;
                spell.HitSum += heal.Amount;
                spell.FullHitSum += heal.FullAmount;

                if (heal.Mod.HasFlag(LogEventMod.Critical))
                {
                    spell.CritCount += 1;
                    spell.CritSum += heal.Amount;
                }

                if (heal.Mod.HasFlag(LogEventMod.Twincast))
                {
                    spell.TwinCount += 1;
                }

                if (interval >= 0)
                {
                    while (HPS.Count <= interval)
                        HPS.Add(0);
                    HPS[interval] += heal.Amount;
                }
            }

            if (heal.Target == Name)
            {
                InboundHealSum += heal.Amount;
                InboundFullHealSum += heal.FullAmount;

                if (interval >= 0)
                {
                    while (InboundHPS.Count <= interval)
                        InboundHPS.Add(0);
                    InboundHPS[interval] += heal.Amount;
                }
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
            InboundStrikeCount += p.InboundStrikeCount;

            OutboundHealSum += p.OutboundHealSum;
            InboundHealSum += p.InboundHealSum;
            InboundFullHealSum += p.InboundFullHealSum;

            DeathCount += p.DeathCount;

            // merge intervals starting at 'intervalOffset' base
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

            for (var i = 0; i < p.InboundHPS.Count; i++)
            {
                while (InboundHPS.Count <= intervalOffset + i)
                    InboundHPS.Add(0);
                InboundHPS[intervalOffset + i] += p.InboundHPS[i];
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

            // disabled - merging buffs will create duplicates if fights overlap and include the same buff
            // it would be better to recreate buffs after merging
            p.Buffs.Clear();

            // >= 0 avoids any pre fight buffs
            //foreach (var b in p.Buffs.Where(x => x.Time >= 0))
            //{
            //    if (timeOffset == 0)
            //        Buffs.Add(b);
            //    else
            //        Buffs.Add(new FightBuff { Name = b.Name, Time = b.Time + timeOffset });
            //}
        }
    
        /// <summary>
        /// Merges damage, heals and spells from a pet into this participant.
        /// Tanking is not merged.
        /// All data will be prefixed with "pet:" to distinguish it from the owner. e.g. "pet:slash" vs "slash"
        /// </summary>
        public void MergePet(FightParticipant pet)
        {
            foreach (var at in pet.AttackTypes)
            {
                at.Type = "pet:" + at.Type;
                //owner.AttackTypes.Add(hit);
                var match = AttackTypes.FirstOrDefault(x => x.Type == at.Type);
                if (match != null)
                {
                    match.Merge(at);
                }
                else
                {
                    AttackTypes.Add(at);
                }
            }

            foreach (var spell in pet.Spells.Where(x => x.Type == "hit"))
            {
                spell.Name = "pet:" + spell.Name;
                var match = Spells.FirstOrDefault(x => x.Type == spell.Type && x.Name == spell.Name);
                if (match != null)
                {
                    match.Merge(spell);
                }
                else
                {
                    Spells.Add(spell);
                }
            }

            OutboundHitCount += pet.OutboundHitCount;
            OutboundHitSum += pet.OutboundHitSum;
            OutboundMissCount += pet.OutboundMissCount;

            // merges DPS and HPS intervals (but not TankDPS or InboundHPS)
            for (var i = 0; i < pet.DPS.Count; i++)
            {
                while (DPS.Count <= i)
                    DPS.Add(0);
                DPS[i] += pet.DPS[i];
            }

            for (var i = 0; i < pet.HPS.Count; i++)
            {
                while (HPS.Count <= i)
                    HPS.Add(0);
                HPS[i] += pet.HPS[i];
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
        }

    }


}
