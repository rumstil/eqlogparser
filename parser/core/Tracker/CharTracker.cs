using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    public enum CharType
    {
        Unknown, Friend, Foe
    }

    public class CharInfo
    {
        public string Name;
        public string Class;
        public int Level;
        public string Owner;
        public bool Player;
        public DateTime? PlayerAggro; // last time a player hit the mob
        public CharType Type;

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", Name, Class, Type);
        }
    }

    /// <summary>
    /// Tracks PC, NPC, merc, and pets activity to determine who is a friend/foe and what class they are.
    /// Players are always considered friends. Obviously this won't work on a PvP server.
    /// NPCs are always considered foes.
    /// </summary>
    public class CharTracker
    {
        // # https://forums.daybreakgames.com/eq/index.php?threads/collecting-pet-names.249684/#post-3671490
        private static readonly Regex PetNameRegex = new Regex(@"^[GJKLVXZ]([aeio][bknrs]){0,2}(ab|er|n|tik)$", RegexOptions.Compiled);
        private static readonly Regex PetOwnerRegex = new Regex(@"^My leader is (\w+)\.$", RegexOptions.Compiled);
        private static readonly Regex PetTellOwnerRegex = new Regex(@"^(Attacking .+? Master|Sorry, Master\.\.\. calming down|Following you, Master|By your command, master|I live again\.\.)\.$", RegexOptions.Compiled);

        private readonly Dictionary<string, CharInfo> CharsByName = new Dictionary<string, CharInfo>(); // StringComparer.InvariantCultureIgnoreCase);

        private readonly Func<string, string> GetSpellClass;
        private readonly Func<string, SpellInfo> GetSpell;

        public CharTracker()
        {
            GetSpell = SpellParser.Default.GetSpell;
            GetSpellClass = SpellParser.Default.GetClass;
        }

        public void HandleEvent(LogEvent e)
        {
            if (e is LogWhoEvent who)
            {
                // who command only shows players
                var c = Add(who.Name);
                c.Player = true;
                c.Type = CharType.Friend;
                if (who.Level > 0)
                    c.Level = who.Level;
                if (who.Class != null)
                    c.Class = who.Class;
            }

            if (e is LogConEvent con)
            {
                var c = Add(con.Name);
                if (con.Level > 0)
                    c.Level = con.Level;
                // your own pet is aimable
                // other players, pets and mercs are indifferent
                if (con.Faction != "regards you indifferently" && con.Faction != "judges you amiably")
                    c.Type = CharType.Foe;
            }

            if (e is LogPartyEvent party)
            {
                var c = Add(party.Name);
                c.Player = true;
                c.Type = CharType.Friend;
            }

            if (e is LogChatEvent chat)
            {
                // NPCs use say, tell and shout
                // pets use say, tell
                // mercs use group chat (maybe all starting with "Casting"?)
                if (chat.Channel != "tell" && chat.Channel != "shout" && chat.Channel != "say")
                {
                    var c = Add(chat.Source);
                    c.Player = true;
                    c.Type = CharType.Friend;
                }
                else if (IsPetName(chat.Source))
                {
                    Add(chat.Source).Type = CharType.Friend;
                }

                var m = PetOwnerRegex.Match(chat.Message);
                if (m.Success)
                {
                    var c = Add(chat.Source);
                    c.Owner = m.Groups[1].Value;
                    c.Type = CharType.Friend; // can you /pet leader a NPC pet?
                    c = Add(c.Owner);
                    c.Type = CharType.Friend;
                    c.Player = true;
                }

                m = PetTellOwnerRegex.Match(chat.Message);
                if (m.Success)
                {
                    var c = Add(chat.Source);
                    c.Type = CharType.Friend;
                }
            }

            if (e is LogHealEvent heal)
            {
                // is it safe tag targets as friends? some raids involve healing NPCs
                var c = Add(heal.Target);
                c.Type = CharType.Friend;

                // some heals are pet only spells
                if (c.Owner == null && !c.Player && heal.Spell != null && heal.Source != null)
                {
                    var s = GetSpell(heal.Spell);
                    if (s != null && s.Target == (int)SpellTarget.Pet)
                        c.Owner = heal.Source;
                }

                if (heal.Source != null)
                {
                    Add(heal.Source).Type = CharType.Friend;                    
                }
            }

            if (e is LogLootEvent loot)
            {
                var c = Add(loot.Char);
                c.Player = true;
                c.Type = CharType.Friend;
            }

            if (e is LogCastingEvent cast)
            {
                var c = Add(cast.Source);
                
                if (c.Class == null && GetSpellClass != null)
                {
                    var cls = GetSpellClass(cast.Spell);
                    if (cls != null)
                    {
                        c.Class = cls;
                        //Console.WriteLine("*** {0} ... {1} {2}", cast.Source, cast.Spell, cls);
                        //Console.ReadLine();
                    }
                }

            }

            if (e is LogHitEvent hit)
            {
                var c = Add(hit.Target);
                if (c.Player)
                {
                    c = Add(hit.Source);
                    c.PlayerAggro = hit.Timestamp;
                }

                // this works, but it's probably unnecessary given spell and /who id
                //c = Add(hit.Source);
                //if (c.Class == null)
                //{
                //    if (hit.Type == "backstab")
                //        c.Class = ClassesMask.Rogue.ToString();
                //    if (hit.Type == "frenzy")
                //        c.Class = ClassesMask.Berserker.ToString();
                //}
            }

            if (e is LogMissEvent miss)
            {
                var c = Add(miss.Target);

                if (c.Player)
                {
                    c = Add(miss.Source);
                    c.PlayerAggro = miss.Timestamp;
                }
            }

            if (e is LogDeathEvent death)
            {
                var c = Add(death.Name);
                c.PlayerAggro = null;
                if (!c.Player)
                    c.Type = CharType.Unknown;
            }

            //var test = Add("Saity");
            //if (test.Class != null)
            //    Console.WriteLine(e);
        }

        /// <summary>
        /// Get info for a character.
        /// </summary>
        //public CharInfo Get(string name)
        //{
        //    CharsByName.TryGetValue(name, out CharInfo c);
        //    return c;
        //}

        /// <summary>
        /// Get friend/foe type for a character.
        /// </summary>
        public CharType GetType(string name)
        {
            var c = Add(name);
            if (c.Type != CharType.Unknown)
                return c.Type;

            if (c.Owner != null)
            {
                c = Add(c.Owner);
                return c.Type;
            }

            return CharType.Unknown;
        }

        /// <summary>
        /// Get class type for a character.
        /// </summary>
        public string GetClass(string name)
        {
            if (CharsByName.TryGetValue(name, out CharInfo c))
                return c.Class;
            return null;
        }

        /// <summary>
        /// Add a character. Will perform a dupe check.
        /// </summary>
        public CharInfo Add(string name)
        {
            if (name.EndsWith("'corpse"))
            {
                name = name.Substring(0, name.Length - 9);
            }

            if (!CharsByName.TryGetValue(name, out CharInfo c))
            {
                CharsByName[name] = c = new CharInfo { Name = name };

                if (name.EndsWith("`s warder") || name.EndsWith("'s warder"))
                    c.Owner = name.Substring(0, name.Length - 9);

                if (name.EndsWith("`s pet") || name.EndsWith("'s pet"))
                    c.Owner = name.Substring(0, name.Length - 6);
            }

            //if (type != CharType.Unknown)
            //    c.Type = type;

            return c;
        }

        /// <summary>
        /// Determine who the foe is for a combat event involving two characters.
        /// Will return a null if foe cannot be determined.
        /// This function is not idempotent! It will alter chars to friend/foe if the other target is a known type.
        /// </summary>
        public string GetFoe(string name1, string name2)
        {
            var n1 = Add(name1);
            var n2 = Add(name2);

            if (name1 == name2)
            {
                // ignore hitting self
                return null;
            }

            else if ((n1.Type == CharType.Friend && n2.Type == CharType.Friend) || (n1.Type == CharType.Foe && n2.Type == CharType.Foe))
            {
                // if they're both foes, one may be newly charmed and should be reverted
                // if they're both friends, one may be a charm break and should be reverted
                if ((!n1.Player && n2.Player) || (n1.PlayerAggro > n2.PlayerAggro) || (n1.PlayerAggro != null && n2.PlayerAggro == null))
                {
                    n1.Type = CharType.Foe;
                    n2.Type = CharType.Friend;
                    //Console.WriteLine("*** Pick {0} as foe", name1);
                    return name1;
                }
                else if ((n1.Player && !n2.Player) || (n2.PlayerAggro > n1.PlayerAggro) || (n1.PlayerAggro == null && n2.PlayerAggro != null))
                {
                    n1.Type = CharType.Friend;
                    n2.Type = CharType.Foe;
                    //Console.WriteLine("*** Pick {0} as foe", name2);
                    return name2;
                }
            }

            else if (n1.Type == CharType.Friend || n2.Type == CharType.Foe)
            {
                n1.Type = CharType.Friend;
                n2.Type = CharType.Foe;
                return name2;
            }

            else if (n1.Type == CharType.Foe || n2.Type == CharType.Friend)
            {
                n1.Type = CharType.Foe;
                n2.Type = CharType.Friend;
                return name1;
            }

            return null;
        }

        /// <summary>
        /// Return the owner of a pet or null if this doesn't appear to be an owned pet.
        /// </summary>
        public string GetOwner(string name)
        {
            var c = Add(name);
            return c.Owner;
        }

        /// <summary>
        /// Is this an auto generated pet name?
        /// </summary>
        public static bool IsPetName(string name)
        {
            return PetNameRegex.IsMatch(name);
        }

    }
}
