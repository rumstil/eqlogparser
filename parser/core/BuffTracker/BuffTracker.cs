using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
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

    /// <summary>
    /// Watches for spell emotes and uses them to track player buffs.
    /// This only tracks when a buff lands. We cannot track when most buffs wear off since third party wearing off messages
    /// are not logged by the game.
    /// </summary>
    public class BuffTracker
    {
        public const string DEATH = "*Died";

        private readonly ISpellLookup Spells;
        private readonly ICharLookup Chars;

        private readonly List<BuffInfo> Buffs = new List<BuffInfo>();
        private readonly List<SpellInfo> Tracking = new List<SpellInfo>();


        public BuffTracker(ISpellLookup spells, ICharLookup chars)
        {
            Chars = chars;
            Spells = spells;

            // we only need to add rank 1 of most spells lines because all the other ranks 
            // share the same emote and will be detected as well

            // specials
            AddSpell("Glyph of Destruction I");
            AddSpell("Glyph of Dragon Scales I");
            AddSpell("Intensity of the Resolute");

            // bard
            AddSpell("Fierce Eye I");
            AddSpell("Quick Time I");
            AddSpell("Dance of Blades I");
            AddSpell("Spirit of Vesagran"); // epic

            // beastlord
            AddSpell("Group Bestial Alignment I"); // self/group emotes are the same
            AddSpell("Bloodlust I");
            AddSpell("Merciless Ferocity"); // different names across levels
            AddSpell("Ruaabri's Fury");
            AddSpell("Savage Fury"); // different names across levels

            // berserker
            AddSpell("Cry Havoc");
            AddSpell("Battle Cry of the Mastruq");

            // cleric
            AddSpell("Divine Intervention");
            AddSpell("Rescued by DI", " has been rescued by divine intervention!", null);

            // druid
            AddSpell("Group Spirit of the Great Wolf I"); // self/group emotes are the same

            // enchanter
            AddSpell("Illusions of Grandeur I");

            // necromancer
            AddSpell("Curse of Muram"); // anguish robe

            // ranger
            AddSpell("Auspice of the Hunter I");
            AddSpell("Scarlet Cheetah Fang I");
            AddSpell("Group Guardian of the Forest I"); // self/group emotes are the same
            AddSpell("Empowered Blades I"); // same emote as Acromancy and Valorous Rage
            AddSpell("Outrider's Accuracy I");
            AddSpell("Bosquestalker's Discipline");
            AddSpell("Trueshot Discipline");
            AddSpell("Imbued Ferocity I");

            // shaman
            AddSpell("Prophet's Gift of the Ruchu"); // epic
            AddSpell("Spire of Ancestors I");

            // warrior
            AddSpell("Heroic Rage I");

        }

        public void HandleEvent(LogEvent e)
        {
            if (e is LogDeathEvent death)
            {
                // disabling this because it's still useful to track buffs before death
                //Buffs.RemoveAll(x => x.CharName == death.Name);
                // death is just a debuff
                Buffs.Add(new BuffInfo { Target = death.Name, SpellName = DEATH, Timestamp = death.Timestamp });
            }

            if (e is LogCastingEvent cast)
            {


            }

            if (e is LogShieldEvent shield)
            {
                Buffs.Add(new BuffInfo { Target = shield.Target, SpellName = "Shielding from " + shield.Source, Timestamp = shield.Timestamp });
            }

            if (e is LogRawEvent raw)
            {
                // strip name from start of string to compare LandsOther version of emote
                // todo: this doesn't handle names with spaces properly (mob/merc/pet)
                var other = Regex.Replace(raw.Text, @"^\w+", "");
                var target = raw.Text.Substring(0, raw.Text.Length - other.Length);

                foreach (var spell in Tracking)
                {
                    // lands on self?
                    if (spell.LandSelf == raw.Text)
                    {
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

                    // if this is a self-only spell then we can discard comparisons with spells the class can't cast
                    if (spell.Target == (int)SpellTarget.Self && Chars.Get(target)?.Class != spell.ClassesNames)
                        continue;

                    // lands on others?
                    if (spell.LandOthers == other)
                    {
                        var buff = new BuffInfo()
                        {
                            Timestamp = e.Timestamp,
                            Target = target,
                            // all spells in a set use the same emote we won't actually know which rank landed
                            SpellName = FixName(spell.Name),
                            DurationTicks = spell.DurationTicks
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
            return SpellParser.StripRank(name.Replace("Group ", ""));
        }

        /// <summary>
        /// Remove all buffs that landed before the given timestamp.
        /// This is useful since we can't track when spells wear off another player.
        /// </summary>
        public void Purge(DateTime ts)
        {
            Buffs.RemoveAll(x => x.Timestamp < ts);
        }

        /// <summary>
        /// Get all buffs for a single character occuring in the requested time span.
        /// </summary>
        public IEnumerable<FightBuff> Get(string name, DateTime from, DateTime to, int offset = 0)
        {
            return Buffs
                .Where(x => x.Target == name && x.Timestamp >= from && x.Timestamp <= to)
                //.Select(x => new FightBuff { Name = x.SpellName, LandedOn = x.Timestamp });
                .Select(x => new FightBuff { Name = x.SpellName, Time = (int)(x.Timestamp - from).TotalSeconds + offset });
        }

        /// <summary>
        /// Add buff tracking for a spell using the spell's "landing text" emotes.
        /// </summary>
        private void AddSpell(string name)
        {
            var s = Spells.GetSpell(name);
            if (s != null)
                Tracking.Add(s);
        }

        /// <summary>
        /// Add buff tracking for a spell using custom "landing text" emote.
        /// </summary>
        private void AddSpell(string name, string others, string self)
        {
            Tracking.Add(new SpellInfo { Name = name, LandOthers = others, LandSelf = self });
        }

    }
}
