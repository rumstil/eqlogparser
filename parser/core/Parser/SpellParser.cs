using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EQLogParser
{
    public class SpellInfo
    {
        public int Id;
        public string Name;
        public int ClassesMask;
        public int ClassesCount;
        public string LandSelf;
        public string LandOthers;

        public string ClassName => ((ClassesMask)ClassesMask).ToString().Replace('_', ' ');

        public override string ToString()
        {
            return String.Format("{0} {1}", Id, Name);
        }
    }
 
    /// <summary>
    /// A rudimentary spell data parser that loads just enough information to help with log parsing.
    /// </summary>
    public class SpellParser
    {
        private readonly Dictionary<int, SpellInfo> LookupById = new Dictionary<int, SpellInfo>(10000);
        private readonly Dictionary<string, SpellInfo> LookupByName = new Dictionary<string, SpellInfo>(10000);

        /*
        public event LogEventHandler OnEvent;

        public void HandleEvent(LogEvent e)
        {
            if (e is LogCastingEvent cast)
            {
                var s = GetSpell(cast.Spell);
                if (s != null && !String.IsNullOrEmpty(s.LandSelf) && !String.IsNullOrEmpty(s.LandOthers))
                {
                    var extcast = new LogCastingEventWithSpellInfo
                    {
                        Id = cast.Id,
                        Timestamp = cast.Timestamp,
                        Source = cast.Source,
                        Spell = cast.Spell,
                        ClassName = s.ClassName,
                        LandOthers = s.LandOthers,
                        LandSelf = s.LandSelf
                    };

                    OnEvent(extcast);
                    return;
                }
            }

            // pass the original event along if it wasn't handled yet
            OnEvent(e);
        }
        */

        /// <summary>
        /// Load spell data.
        /// </summary>
        /// <param name="path">Path to spells_us.txt file.</param>
        public void Load(string path)
        {
            if (!File.Exists(path))
                return;

            LookupById.Clear();
            LookupByName.Clear();

            using (var f = File.OpenText(path))
            {
                while (true)
                {
                    var line = f.ReadLine();
                    if (line == null)
                        break;

                    var fields = line.Split('^');
                    if (line.StartsWith("#"))
                        continue;

                    var spell = new SpellInfo();

                    // 0 SPELLINDEX
                    spell.Id = Convert.ToInt32(fields[0]);

                    // 1 SPELLNAME
                    spell.Name = fields[1].Trim();

                    // 38 WARRIORMIN .. BERSERKERMIN
                    for (int i = 0; i < 16; i++)
                    {
                        var level = fields[38 + i];
                        // 255 for uncastable
                        // 254 for AA
                        if (level != "255")
                        {
                            spell.ClassesCount += 1;
                            spell.ClassesMask |= 1 << i;
                        }
                    }

                    // we could store all spells but that would take a lot of memory - so instead we will:
                    // only keep spells that are player castable
                    // only keep one rank since all ranks are equivalent in terms of castable class and landing text
                    //if (spell.ClassesCount > 0 && spell.Name == StripRank(spell.Name)) // rank 1 AA usually end with " I"
                    if (spell.ClassesCount > 0 && !LookupByName.ContainsKey(StripRank(spell.Name)))
                    {
                        LookupById[spell.Id] = spell;
                        LookupByName[StripRank(spell.Name)] = spell;
                    }

                    //Console.WriteLine("{0} {1} {2}", spell.Id, spell.Name, (SpellClassesMaskLong)spell.ClassesMask);
                }

                //Console.WriteLine("Loaded {0} spells", lookupByName.Count);
            }

            // load spell string file (starting 2018-2-14)
            // *SPELLINDEX^CASTERMETXT^CASTEROTHERTXT^CASTEDMETXT^CASTEDOTHERTXT^SPELLGONE^
            var strpath = path.Replace("spells_us", "spells_us_str");
            using (var f = File.OpenText(strpath))
            {
                while (true)
                {
                    var line = f.ReadLine();
                    if (line == null)
                        break;

                    var fields = line.Split('^');
                    if (fields.Length < 4 || line.StartsWith("#"))
                        continue;

                    var id = Convert.ToInt32(fields[0]);
                    if (LookupById.TryGetValue(id, out SpellInfo s))
                    {
                        s.LandSelf = fields[3];
                        s.LandOthers = fields[4];
                    }
                }
            }
        }

        /// <summary>
        /// Lookup a spell by name.
        /// </summary>
        public SpellInfo GetSpell(string name)
        {
            if (LookupByName.TryGetValue(StripRank(name), out SpellInfo s))
                return s;
            return null;
        }

        /// <summary>
        /// Get the class of a character based on a spell they cast.
        /// TODO: Are any proc buffs that can be cast on other players ever tagged with a class?
        /// </summary>
        public string GetClass(string name)
        {
            if (LookupByName.TryGetValue(StripRank(name), out SpellInfo s) && s.ClassesCount == 1)
                return s.ClassName;
            return null;
        }

        /// <summary>
        /// Get the name of the spell for the given landed text.
        /// </summary>
        //public string GetLandedSpell(string landed)
        //{
        //    return null;
        //}

        /// <summary>
        /// Strip any digit or roman numeral rank from the end of a spell name.
        /// </summary>
        public static string StripRank(string name)
        {
            name = Regex.Replace(name, @"\s\(?\d+\)?$", ""); // (3) 
            name = Regex.Replace(name, @"\s(Rk\.\s)?[IVX]+$", ""); // Rk. III
            return name;
        }
    }
}
