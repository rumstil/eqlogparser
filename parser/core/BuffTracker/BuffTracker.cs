using System;
using System.Collections.Generic;
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
        public string CharName;
        public string SpellName;
        public DateTime Timestamp;
        public int DurationTicks; // base duration prior to focus/aa

        public override string ToString()
        {
            return $"Buff: {SpellName} => ${CharName}";
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
            AddSpell("Spirit of Vesagran"); // epic

            // beastlord
            AddSpell("Group Bestial Alignment I");

            // cleric
            AddSpell("Divine Intervention");
            AddSpell("Divine Intervention Trigger", "has been rescued by divine intervention!");

            // druid
            AddSpell("Group Spirit of the Great Wolf I");

            // enchanter
            AddSpell("Illusions of Grandeur I");

            // ranger
            AddSpell("Auspice of the Hunter I");
            AddSpell("Scarlet Cheetah Fang I");
            AddSpell("Guardian of the Forest I");
            AddSpell("Group Guardian of the Forest I");
            AddSpell("Empowered Blades I");
            AddSpell("Outrider's Accuracy I");
            AddSpell("Bosquestalker's Discipline");
            AddSpell("Trueshot Discipline");
            AddSpell("Imbued Ferocity");

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
                Buffs.Add(new BuffInfo { CharName = death.Name, SpellName = DEATH, Timestamp = death.Timestamp });
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
                        CharName = name,
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
                .Where(x => x.CharName == name && x.Timestamp >= from && x.Timestamp <= to)
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
            // Nielar's instincts are sharpened by the auspice of the hunter.
            // Fred flees in terror.
            // todo: this doesn't handle mob/merc/pet names with spaces properly
            text = Regex.Replace(text, @"^\w+", "");

            Emotes.TryGetValue(text, out SpellInfo s);
            return s;
        }


    }
}
