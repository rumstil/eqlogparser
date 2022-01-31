using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
Things to keep in mind:

Landing emotes are shared between:
-self/group versions of spells. e.g. (Group) Guardian of the Forest
-unrelated spells. e.g. Empowered Blades, Acromancy, Valorous Rage
-different tiers of the same spell line

There are no 3rd party "wearing off" emotes.

Is landing emote vs casting reporting range different?

Perhaps I should simplify this class a bit by not using casting events to record a buff?
Instead, a landing message could be combined with the last cast to generate a more specific spell name.
This would allow me to remove duplicate spell line references in the constructor and to remove the duplicate avoidance code.

*/

namespace EQLogParser
{
    /// <summary>
    /// Watches casting and landing text emotes to track player buffs.
    /// This is limited to tracking when a buff starts.
    /// </summary>
    public class BuffTracker
    {
        public const string DEATH = "*Died";

        private readonly ISpellLookup Spells;
        //private readonly ICharLookup Chars;

        private readonly List<SpellInfo> TrackedSpells = new List<SpellInfo>();
        private Dictionary<string, SpellInfo> TrackedSpellsByName = new Dictionary<string, SpellInfo>();
        private readonly List<BuffInfo> Buffs = new List<BuffInfo>();
        private readonly Dictionary<string, SpellInfo> LastCast = new Dictionary<string, SpellInfo>();


        public BuffTracker(ISpellLookup spells)
        {
            Spells = spells;

            // we only need to add rank 1 of most spells lines because...
            // -group spells: they the use same emote
            // -self spells with ranks: we match with any spell name minus the rank
            // the exception is...
            // -self spells with different names across levels

            // specials
            AddSpell("Glyph of Destruction I");
            AddSpell("Glyph of Dragon Scales I");
            AddSpell("Intensity of the Resolute");

            // archtype
            AddSpell("Improved Twincast I"); // aa, clr, dru, wiz, mag, enc
            AddSpell("Killing Spree I"); // war, mnk, rog, ber killshot proc (don't think it gets logged)

            // bard
            AddSpell("Spirit of Vesagran"); // epic
            AddSpell("Fierce Eye I");
            AddSpell("Quick Time I");
            AddSpell("Dance of Blades I");

            // beastlord
            AddSpell("Group Bestial Alignment I"); // aa, self/group emotes are the same
            AddSpell("Bestial Alignment I"); // aa
            AddSpell("Bloodlust I"); // aa
            AddSpell("Merciless Ferocity"); // L97, T9, single
            AddSpell("Ruaabri's Fury"); // L98, T4, group
            AddSpell("Savage Fury"); // L94, T3
            AddSpell("Savage Rage"); // L99, T3
            AddSpell("Savage Rancor"); // L104, T3
            AddSpell("Taste of Blood I"); // killshot proc

            // berserker
            AddSpell("Frenzied Resolve Discipline");
            AddSpell("Cleaving Rage Discipline"); // L54, T4, probably not used
            AddSpell("Blind Rage Discipline");// L58, T4
            AddSpell("Berserking Discipline"); // L75, T4
            AddSpell("Sundering Discipline"); // L95, T4
            AddSpell("Brutal Discipline"); // L100, T4
            AddSpell("Cry Havoc");
            AddSpell("Battle Cry of the Mastruq");

            // cleric
            AddSpell("Flurry of Life I"); // aa
            AddSpell("Healing Frenzy I"); // aa

            // druid
            AddSpell("Group Spirit of the Great Wolf I"); // aa, self/group emotes are the same
            AddSpell("Spirit of the Great Wolf I");
            AddSpell("Spire of Nature I"); // aa

            // enchanter
            AddSpell("Illusions of Grandeur I"); // aa
            AddSpell("Chromatic Haze I"); // aa

            // magician
            AddSpell("Frenzied Burnout I"); // aa, pet
            AddSpell("Heart of Skyfire I"); // aa
            AddSpell("Spire of the Elements I"); // aa

            // monk
            // https://forums.daybreakgames.com/eq/index.php?threads/monk-burns.242877/
            AddSpell("Ashenhand Discipline"); // L60, T4
            AddSpell("Scaledfist Discipline"); // L74, T4
            AddSpell("Ironfist Discipline"); // L88, T4
            AddSpell("Stonestance Discipline"); // L51, T2 (mitigation)
            AddSpell("Earthwalk Discipline"); // L65, T2
            AddSpell("Impenetrable Discipline"); // L72, T2
            AddSpell("Whirlwind Discipline"); // L53, T2 (riposte)
            AddSpell("Voiddance Discipline"); // L54, T2 (avoidance)
            AddSpell("Earthforce Discipline"); // L96, T2
            AddSpell("Speed Focus Discipline"); // T3 (weapon delay)
            AddSpell("Innerflame Discipline"); // L57, T3
            AddSpell("Crystalpalm Discipline"); // L79, T3
            AddSpell("Diamondpalm Discipline"); // L90, T3
            AddSpell("Terrorpalm Discipline"); // L99, T3
            AddSpell("Hundred Fists Discipline"); // L57, T3
            AddSpell("Rapid Kick Discipline"); // L70, T6
            AddSpell("Heel of Kanji"); // L70, T6
            AddSpell("Heel of Kai"); // L90, T6
            AddSpell("Heel of Kojai"); // L95, T6
            AddSpell("Heel of Zagali"); // L100, T6
            AddSpell("Zan Fi's Whistle I"); // aa
            AddSpell("Destructive Force I"); // aa
            AddSpell("Focused Destructive Force I"); // aa
            AddSpell("Infusion of Thunder I"); // aa
            AddSpell("Spire of the Sensei I"); // aa

            // necromancer
            AddSpell("Curse of Muram"); // anguish robe
            AddSpell("Spire of Necromancy I");
            AddSpell("Hand of Death I");
            AddSpell("Heretic's Twincast I");
            AddSpell("Funeral Pyre I");

            // paladin
            AddSpell("Pureforge Discipline");
            AddSpell("Holyforge Discipline");
            AddSpell("Blessing of the Faithful I"); // killshot proc

            // ranger
            AddSpell("Auspice of the Hunter I"); // aa
            AddSpell("Scarlet Cheetah Fang I"); // aa
            AddSpell("Group Guardian of the Forest I"); // aa, self/group emotes are the same
            AddSpell("Guardian of the Forest I");
            AddSpell("Empowered Blades I"); // aa, same emote as Acromancy and Valorous Rage
            AddSpell("Outrider's Accuracy I"); // aa
            AddSpell("Bosquestalker's Discipline"); // L100, T2 (melee)
            AddSpell("Copsestalker's Discipline"); // L105, T2
            AddSpell("Wildstalker's Discipline"); // L110, T2
            AddSpell("Arbor Stalker's Discipline"); // L115, T2
            AddSpell("Dusksage Stalker's Discipline"); // L120, T2
            AddSpell("Trueshot Discipline"); // L55, T2 (archery)
            AddSpell("Aimshot Discipline"); // L80, T2
            AddSpell("Sureshot Discipline"); // L85, T2
            AddSpell("Bullseye Discipline"); // L90, T2
            AddSpell("Pureshot Discipline"); // L100, T2
            AddSpell("Imbued Ferocity I"); // aa
            AddSpell("Hunter's Fury I"); // aa

            // rogue
            // https://forums.daybreakgames.com/eq/index.php?threads/sneak-attack-rogue-disciplines.243116/#post-3983165
            AddSpell("Deceiver's Blight"); // epic
            AddSpell("Frenzied Stabbing Discipline");
            AddSpell("Knifeplay Discipline"); // L98, T16
            AddSpell("Blinding Speed Discipline"); // L58, T4
            AddSpell("Shrouding Speed Discipline"); // L102, T4
            AddSpell("Cloaking Speed Discipline"); // L112, T4
            AddSpell("Twisted Chance Discipline"); // L65, T4
            AddSpell("Rogue's Fury I");
            AddSpell("Dissident Weapons");
            //AddSpell("Thief's Vengeance"); // L52, T9 (one hit only)
            AddSpell("Rake's Rampage I");
            AddSpell("Rake's Focused Rampage I");
            AddSpell("Spire of the Rake I"); // aa

            // shaman
            AddSpell("Prophet's Gift of the Ruchu"); // epic
            AddSpell("Spire of Ancestors I");

            // shadowknight
            AddSpell("Lich Sting"); // epic/group
            AddSpell("Unholy Aura Discipline");
            AddSpell("Visage of Death I"); // aa
            AddSpell("Visage of Decay I"); // aa
            AddSpell("Spire of the Reavers I"); // aa
            AddSpell("Mortal Coil I"); // killshot proc

            // warrior
            AddSpell("Heroic Rage I"); // aa
            AddSpell("Savage Onslaught Discipline"); // L68, T6
            AddSpell("Brutal Onslaught Discipline"); // L74, T6
            AddSpell("Brightfeld's Onslaught Discipline"); // L117, T6
            AddSpell("Mighty Strike Discipline"); // T4
            AddSpell("Fellstrike Discipline"); // T4
            AddSpell("Defensive Discipline"); // L55, T2
            AddSpell("Stonewall Discipline"); // L65, T2
            AddSpell("Final Stand Discipline"); // L72, T2
            AddSpell("Last Stand Discipline"); // L98, T2
            AddSpell("Culminating Stand Discipline"); // L108, T2
            AddSpell("Ultimate Stand Discipline"); // L113, T2
            AddSpell("Resolute Stand"); // L118, T2
            AddSpell("Evasive Discipline"); // T2
            AddSpell("Offensive Discipline"); // L97, T2
            AddSpell("Furious Discipline"); // L56, T3
            AddSpell("Fortitude Discipline"); // L59, T3

            // wizard
            AddSpell("Frenzied Devastation I");
            AddSpell("Arcane Fury I");
            AddSpell("Arcane Destruction I");
            AddSpell("Arcane Overkill I"); // killshot proc
            AddSpell("Fury of the Gods I"); 

            TrackedSpellsByName = TrackedSpells.ToDictionary(x => SpellParser.StripRank(x.Name));
        }

        public void HandleEvent(LogEvent e)
        {
            // refresh the lookup cache if needed
            if (TrackedSpellsByName.Count == 0)
                TrackedSpellsByName = TrackedSpells.ToDictionary(x => SpellParser.StripRank(x.Name));

            if (e is LogDeathEvent death)
            {
                // disabling this because it's still useful to track buffs before death
                //Buffs.RemoveAll(x => x.CharName == death.Name);
                // death is just a debuff
                Buffs.Add(new BuffInfo { Target = death.Name, SpellName = DEATH, Timestamp = death.Timestamp });
            }

            if (e is LogCastingEvent cast)
            {
                // casting can be used to safely track self-only buffs since they will only land on the caster 
                // this is better than tracking by emote since we can determine exactly which spell was cast (unless interrupted)
                var name = SpellParser.StripRank(cast.Spell);
                TrackedSpellsByName.TryGetValue(name, out SpellInfo spell);
                if (spell != null && (spell.Target == (int)SpellTarget.Self || spell.Target == (int)SpellTarget.Pet))
                {
                    var buff = new BuffInfo()
                    {
                        Timestamp = e.Timestamp,
                        Target = cast.Source,
                        SpellName = cast.Spell,
                        DurationTicks = spell.DurationTicks
                    };
                    Buffs.Add(buff);
                }
                // if this spell isn't a buff then last cast will be null 
                LastCast[cast.Source] = spell;
            }

            if (e is LogShieldEvent shield)
            {
                Buffs.Add(new BuffInfo { Target = shield.Target, SpellName = "*Shielded by " + shield.Source, Timestamp = shield.Timestamp });
            }

            if (e is LogRescuedEvent rescue)
            {
                Buffs.Add(new BuffInfo { Target = rescue.Target, SpellName = "*Rescued by DI", Timestamp = rescue.Timestamp });
            }

            if (e is LogRawEvent raw)
            {
                // strip name from start of string to compare LandsOther version of emote
                // todo: this doesn't handle names with spaces properly (mob/merc/pet)
                var otherText = Regex.Replace(raw.Text, @"^\w+", "");
                var otherTarget = raw.Text.Substring(0, raw.Text.Length - otherText.Length);

                foreach (var spell in TrackedSpells)
                {
                    // lands on self?
                    if (spell.LandSelf == raw.Text && spell.Target != (int)SpellTarget.Self)
                    {
                        // group spells may share an emote with a self spell -- which already get captured during the casting handler
                        // so make sure we aren't double counting
                        LastCast.TryGetValue(raw.Player, out SpellInfo last);
                        if (last?.LandSelf == spell.LandSelf)
                            break;

                        var buff = new BuffInfo()
                        {
                            Timestamp = e.Timestamp,
                            Target = raw.Player,
                            // all spells in a set use the same emote we won't actually know which rank landed
                            SpellName = FixName(spell.Name),
                            DurationTicks = spell.DurationTicks
                        };
                        Buffs.Add(buff);
                        break;
                    }

                    // if this is a self-only spell then we can discard comparisons with spells the player can't cast
                    //if (spell.Target == (int)SpellTarget.Self && Chars.Get(target)?.Class != spell.ClassesNames)
                    //    continue;

                    // lands on others?
                    if (spell.LandOthers == otherText && spell.Target != (int)SpellTarget.Self)
                    {
                        // group spells may share an emote with a self spell -- which already get captured during the casting handler
                        // so make sure we aren't double counting
                        LastCast.TryGetValue(otherTarget, out SpellInfo last);
                        if (last?.LandOthers == spell.LandOthers)
                            break;

                        var buff = new BuffInfo()
                        {
                            Timestamp = e.Timestamp,
                            Target = otherTarget,
                            // all spells in a set use the same emote we won't actually know which rank landed
                            // but don't modify custom effects like "Rescued by DI"
                            SpellName = spell.Name.StartsWith("*") ? spell.Name : FixName(spell.Name),
                            DurationTicks = spell.DurationTicks,
                        };
                        Buffs.Add(buff);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Since spells in the same spell line share emotes, the buff tracker can't be sure which one actually landed based on the emote.
        /// Instead we just normalize the name so that they are all consistent.
        /// </summary>
        private string FixName(string name)
        {
            //return SpellParser.StripRank(name.Replace("Group ", ""));
            return SpellParser.StripRank(name);
        }

        /// <summary>
        /// Remove all buffs that landed before the given timestamp.
        /// If the list of buffs grows too long the class will slow down.
        /// </summary>
        public void PurgeStale(DateTime ts)
        {
            if (Buffs.Count > 1000)
                Buffs.RemoveAll(x => x.Timestamp < ts);
        }

        /// <summary>
        /// Get all buffs for a single character occuring in the requested time span.
        /// </summary>
        /// <param name="backtrack">Optional number of seconds to backtrack for pre-fight buffs.</param>
        public List<FightBuff> Get(string name, DateTime from, DateTime to, int backtrack = 0)
        {
            if (backtrack > 0)
                throw new ArgumentException("Backtrack time must be negative or zero.");

            return Buffs
                .Where(x => x.Target == name && x.Timestamp >= from.AddSeconds(backtrack) && x.Timestamp <= to)
                .Select(x => new FightBuff { Name = x.SpellName, Time = (int)(x.Timestamp - from).TotalSeconds })
                .ToList();
        }

        /// <summary>
        /// Add buff tracking for a spell.
        /// </summary>
        public void AddSpell(SpellInfo s)
        {
            TrackedSpells.Add(s);
            TrackedSpellsByName.Clear();
        }

        /// <summary>
        /// Add buff tracking for a spell.
        /// </summary>
        public void AddSpell(string name)
        {
            var s = Spells.GetSpell(name);
            if (s != null)
            {
                TrackedSpells.Add(s);
                TrackedSpellsByName.Clear();
            }
            //Tracking.Add(new SpellInfo { Name = FixName(s.Name), LandOthers = s.LandOthers, LandSelf = s.LandSelf, Target = s.Target });
        }


    }

    public class BuffInfo
    {
        public string Target;
        public string SpellName;
        public DateTime Timestamp;
        public int DurationTicks; // base duration prior to focus/aa

        public override string ToString()
        {
            return $"Buff: {SpellName} => ${Target}";
        }
    }

}
