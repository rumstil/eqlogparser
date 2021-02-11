using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    public enum SpellTarget
    {
        Pet = 14,
        Pet2 = 38,
    }

    public class SpellInfo
    {
        public int Id;
        public string Name;
        public int Target;
        public int DurationTicks;
        public int ClassesMask;
        public int ClassesCount;
        public string LandSelf;
        public string LandOthers;
        public string LandPet;
        public string WearsOff;
        //public bool IsCombatSkill;

        public string ClassesNames => ((ClassesMaskShort)ClassesMask).ToString().Replace('_', ' ');

        public bool IsPetTarget => Target == (int)SpellTarget.Pet || Target == (int)SpellTarget.Pet2;

        public override string ToString()
        {
            return String.Format("[{0}] {1}", Id, Name);
        }
    }

    public interface ISpellLookup
    {
        //SpellInfo GetSpell(int id);
        SpellInfo GetSpell(string name);
        //string GetSpellClass(string name);
    }

    /// <summary>
    /// A minimalist spell_us.txt parser that loads just enough information to help with log parsing.
    /// </summary>
    public class SpellParser : ISpellLookup
    {
        //public const int MAX_LEVEL = 115;

        private IReadOnlyList<SpellInfo> List = new List<SpellInfo>();
        //private IReadOnlyDictionary<int, SpellInfo> LookupById = new Dictionary<int, SpellInfo>();
        private IReadOnlyDictionary<string, SpellInfo> LookupByName = new Dictionary<string, SpellInfo>();

        public bool IsReady => List.Count > 0;

        /// <summary>
        /// Load spell data.
        /// </summary>
        /// <param name="path">Path to spells_us.txt file.</param>
        public void Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            var list = new List<SpellInfo>(30000);
            var lookupById = new Dictionary<int, SpellInfo>(list.Count);
            var lookupByName = new Dictionary<string, SpellInfo>(list.Count);

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

                    // 11 DURATIONBASE
                    // 12 DURATIONCAP
                    spell.DurationTicks = CalcDuration(Convert.ToInt32(fields[11]), Convert.ToInt32(fields[12]));
                    //spell.Duration = Convert.ToInt32(fields[11]);

                    // 30 BENEFICIAL
                    //spell.IsBeneficial = fields[30] != "0";

                    // 32 TYPENUMBER
                    spell.Target = Convert.ToInt32(fields[32]);

                    // 100 IS_SKILL
                    //spell.IsCombatSkill = fields[100] != "0";

                    // 38 WARRIORMIN .. BERSERKERMIN
                    // determine which classes can use this spell
                    for (int i = 0; i < 16; i++)
                    {
                        var level = Int32.Parse(fields[38 + i]);
                        if (level < 255)
                        {
                            spell.ClassesMask |= 1 << i;
                            spell.ClassesCount += 1;
                        }
                    }

                    // handle spell name collisions. 
                    // e.g. Merciful Light is both PAL/CLR
                    // e.g. Inspire Fear is CLR and Inspire Fear II is None
                    if (lookupByName.TryGetValue(spell.Name, out SpellInfo match))
                    {
                        match.ClassesMask |= spell.ClassesMask;
                        match.ClassesCount = BitOperations.PopCount((uint)match.ClassesMask);
                    }
                    else
                    {
                        lookupByName.Add(spell.Name, spell);
                    }

                    list.Add(spell);
                    lookupById[spell.Id] = spell;

                    //Console.WriteLine("{0} {1} {2}", spell.Id, spell.Name, (SpellClassesMaskLong)spell.ClassesMask);
                }

                //Console.WriteLine("Loaded {0} spells", lookupByName.Count);
            }

            // load spell string file (starting 2018-2-14)
            // *SPELLINDEX^CASTERMETXT^CASTEROTHERTXT^CASTEDMETXT^CASTEDOTHERTXT^SPELLGONE^
            var strpath = path.Replace("spells_us", "spells_us_str");
            if (!File.Exists(strpath))
            {
                //LookupById = lookupById;
                LookupByName = lookupByName;
                return;
            }

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
                    if (lookupById.TryGetValue(id, out SpellInfo s))
                    {
                        s.LandSelf = fields[3];
                        s.LandOthers = fields[4];
                        s.WearsOff = fields[5];

                        // todo: emotes that aren't unique will misidentify spells
                        // e.g. "eyes gleam."
                        //if (!String.IsNullOrEmpty(s.LandSelf))
                        //    lookupByEmote.TryAdd(s.LandSelf, s);
                        //if (!String.IsNullOrEmpty(s.LandOthers))
                        //    lookupByEmote.TryAdd(s.LandOthers, s);
                    }
                }

                // set LandPet emote on pet spells if the emote is only used by pet spells
                // e.g. "shrinks" is used by both pet and non pet spells
                var nonPet = list.Where(x => !x.IsPetTarget).Select(x => x.LandOthers).ToHashSet();
                foreach (var spell in list.Where(x => x.IsPetTarget))
                {
                    if (!nonPet.Contains(spell.LandOthers))
                        spell.LandPet = spell.LandOthers;
                }
            }

            //LookupById = lookupById;
            LookupByName = lookupByName;
            List = list;
        }

        /// <summary>
        /// Lookup a spell by id. Need to store all ranks if doing this.
        /// </summary>
        public SpellInfo GetSpell(int id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Lookup a spell by name. 
        /// </summary>
        public SpellInfo GetSpell(string name)
        {
            LookupByName.TryGetValue(name, out SpellInfo s);
            return s;
        }

        /// <summary>
        /// Lookup a spell casting classes by name. 
        /// </summary>
        private string GetSpellClass(string name)
        {
            if (!LookupByName.TryGetValue(name, out SpellInfo s))
                return null;

            if (s.ClassesCount != 1)
                return null;

            return s?.ClassesNames;
        }


        /*
        /// <summary>
        /// Lookup a spell by emote.
        /// </summary>
        public SpellInfo GetSpellFromEmote(string text)
        {
            // there are two problems loading buffs from spell emotes
            // 1. some buffs are irrelevant. e.g. promised heals
            // 2. some emotes are not unique 

            // slow
            //foreach (var spell in List)
            //{
            //    if (text.EndsWith(spell.LandOthers, StringComparison.OrdinalIgnoreCase) || text.EndsWith(spell.LandSelf, StringComparison.OrdinalIgnoreCase))
            //        return spell;
            //}

            // Nielar's instincts are sharpened by the auspice of the hunter.
            // Fred flees in terror.
            // todo: this doesn't handle mob/pet names with spaces properly
            text = Regex.Replace(text, @"^\w+", "");

            LookupByEmote.TryGetValue(text, out SpellInfo s);
            return s;
        }
        */

        /// <summary>
        /// Strip any digit or roman numeral rank from the end of a spell name.
        /// </summary>
        public static string StripRank(string name)
        {
            name = Regex.Replace(name, @"\s\(?\d+\)?$", ""); // (3) 
            name = Regex.Replace(name, @"\s(Rk\.\s)?[IVX]+$", ""); // Rk. III
            return name;
        }

        /// <summary>
        /// Calculate a duration.
        /// </summary>
        /// <returns>Numbers of ticks (6 second units)</returns>
        private static int CalcDuration(int calc, int max, int level = 254)
        {
            int value = 0;

            switch (calc)
            {
                case 0:
                    value = 0;
                    break;
                case 1:
                    value = level / 2;
                    if (value < 1)
                        value = 1;
                    break;
                case 2:
                    value = (level / 2) + 5;
                    if (value < 6)
                        value = 6;
                    break;
                case 3:
                    value = level * 30;
                    break;
                case 4:
                    value = 50;
                    break;
                case 5:
                    value = 2;
                    break;
                case 6:
                    value = level / 2;
                    break;
                case 7:
                    value = level;
                    break;
                case 8:
                    value = level + 10;
                    break;
                case 9:
                    value = level * 2 + 10;
                    break;
                case 10:
                    value = level * 30 + 10;
                    break;
                case 11:
                    value = (level + 3) * 30;
                    break;
                case 12:
                    value = level / 2;
                    if (value < 1)
                        value = 1;
                    break;
                case 13:
                    value = level * 4 + 10;
                    break;
                case 14:
                    value = level * 5 + 10;
                    break;
                case 15:
                    value = (level * 5 + 50) * 2;
                    break;
                case 50:
                    value = 72000;
                    break;
                case 3600:
                    value = 3600;
                    break;
                default:
                    value = max;
                    break;
            }

            if (max > 0 && value > max)
                value = max;

            return value;
        }


    }
}
