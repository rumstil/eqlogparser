using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    public class BuffEmote
    {
        public string Name;
        public string LandSelf;
        public string LandOthers;
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

    /// <summary>
    /// Watches for spell emotes and uses them to track player buffs.
    /// This only tracks when a buff lands. We cannot track when most buffs wear off since third party wearing off messages
    /// are not logged by the game.
    /// </summary>
    public class BuffTracker
    {
        public const string DEATH = "*Died";

        private readonly ISpellLookup Spells;

        private readonly List<BuffInfo> Buffs = new List<BuffInfo>();
        private readonly Dictionary<string, SpellInfo> Emotes = new Dictionary<string, SpellInfo>();


        public BuffTracker(ISpellLookup spells)
        {
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
            AddSpell("Bestial Alignment I"); // same emote as group version
            AddSpell("Bloodlust I");

            // berserker
            AddSpell("Cry Havoc");

            // cleric
            AddSpell("Divine Intervention");
            AddSpell("Divine Intervention Trigger", " has been rescued by divine intervention!");

            // druid
            AddSpell("Spirit of the Great Wolf I"); // same emote as group version

            // enchanter
            AddSpell("Illusions of Grandeur I");

            // necromancer
            AddSpell("Curse of Muram"); // anguish robe

            // ranger
            AddSpell("Auspice of the Hunter I");
            AddSpell("Scarlet Cheetah Fang I");
            AddSpell("Guardian of the Forest I"); // same emote as group version
            AddSpell("Empowered Blades I");
            AddSpell("Outrider's Accuracy I");
            AddSpell("Bosquestalker's Discipline");
            AddSpell("Trueshot Discipline");
            AddSpell("Imbued Ferocity I");

            // shaman
            AddSpell("Prophet's Gift of the Ruchu"); // epic

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

            if (e is LogRawEvent raw)
            {
                var spell = GetSpellFromEmote(raw.Text);
                if (spell != null)
                {
                    var name = raw.Player;
                    if (raw.Text != spell.LandSelf)
                        name = raw.Text.Substring(0, raw.Text.Length - spell.LandOthers.Length);

                    var buff = new BuffInfo()
                    {
                        Target = name,
                        // all spells in a set use the same emote we won't actually know which rank landed
                        SpellName = SpellParser.StripRank(spell.Name),
                        Timestamp = e.Timestamp,
                        DurationTicks = spell.DurationTicks
                    };
                    Buffs.Add(buff);
                }
            }
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
            {
                if (!String.IsNullOrEmpty(s.LandSelf))
                    Emotes[s.LandSelf] = s;
                if (!String.IsNullOrEmpty(s.LandOthers))
                    Emotes[s.LandOthers] = s;
            }
        }

        /// <summary>
        /// Add buff tracking for a spell using a fake "landing text" emote.
        /// </summary>
        private void AddSpell(string name, string emote)
        {
            Emotes[emote] = new SpellInfo { Name = name, LandOthers = emote };
        }

        /// <summary>
        /// Check text to see if it matches a tracked spell emote.
        /// </summary>
        private SpellInfo GetSpellFromEmote(string text)
        {
            // lands on self?
            Emotes.TryGetValue(text, out SpellInfo s);
            if (s != null)
                return s;

            // lands on others?
            // strip name from start of string
            // todo: this doesn't handle names with spaces properly (mob/merc/pet)
            text = Regex.Replace(text, @"^\w+", "");
            Emotes.TryGetValue(text, out s);
            return s;
        }


    }
}
