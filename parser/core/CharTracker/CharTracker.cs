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
        public bool IsPlayer;
        public DateTime? PlayerAggro; // last time a player hit the mob
        public CharType Type;

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", Name, Class, Type);
        }
    }

    internal class PetBuffEmote
    {
        public DateTime Timestamp;
        public string Name;
        public string Source;
        public string Emote;

        public override string ToString()
        {
            return String.Format("{0} {1} ({2})", Name, Emote, Source);
        }
    }

    /// <summary>
    /// Tracks PC, NPC, merc, and pet activity to determine who is a friend/foe and what class they are.
    /// Players are always considered friends. Obviously this won't work on a PvP server.
    /// NPCs are always considered foes.
    /// </summary>
    public class CharTracker
    {
        private string Player = null;

        // # https://forums.daybreakgames.com/eq/index.php?threads/collecting-pet-names.249684/#post-3671490
        private static readonly Regex PetNameRegex = new Regex(@"^[GJKLVXZ]([aeio][bknrs]){0,2}(ab|er|n|tik)$", RegexOptions.Compiled);
        private static readonly Regex PetOwnerRegex = new Regex(@"^My leader is (\w+)\.$", RegexOptions.Compiled);
        private static readonly Regex PetTellOwnerRegex = new Regex(@"^(Attacking .+? Master|Sorry, Master\.\.\. calming down|Following you, Master|By your command, master|I live again\.\.)\.$", RegexOptions.Compiled);

        private readonly Dictionary<string, CharInfo> CharsByName = new Dictionary<string, CharInfo>(); // StringComparer.InvariantCultureIgnoreCase);

        private readonly List<PetBuffEmote> PetBuffs = new List<PetBuffEmote>();

        private readonly ISpellLookup Spells;

        public CharTracker()
        {
        }

        public CharTracker(ISpellLookup spells)
        {
            Spells = spells;
        }

        public void HandleEvent(LogEvent e)
        {
            if (e is LogOpenEvent open)
            {
                Player = open.Player;
                var c = GetOrAdd(open.Player);
                c.IsPlayer = true;
                c.Type = CharType.Friend;
            }

            if (e is LogWhoEvent who)
            {
                // who command only shows players
                var c = GetOrAdd(who.Name);
                c.IsPlayer = true;
                c.Owner = null;
                c.Type = CharType.Friend;
                if (who.Level > 0)
                    c.Level = who.Level;
                if (who.Class != null)
                    c.Class = who.Class;
            }

            if (e is LogConEvent con)
            {
                var c = GetOrAdd(con.Name);
                if (con.Level > 0)
                    c.Level = con.Level;
                // your own pet is aimable
                // other players, pets and mercs are indifferent
                if (con.Faction != "regards you indifferently" && con.Faction != "judges you amiably")
                    c.Type = CharType.Foe;
            }

            if (e is LogPartyEvent party)
            {
                var c = GetOrAdd(party.Name);
                c.IsPlayer = true;
                c.Owner = null;
                c.Type = CharType.Friend;
            }

            if (e is LogChatEvent chat)
            {
                var c = GetOrAdd(chat.Source);

                // NPCs use say, tell and shout
                // pets use say, tell
                // mercs use group chat (maybe all starting with "Casting"?)
                if (chat.Channel != "tell" && chat.Channel != "shout" && chat.Channel != "say")
                {
                    c.IsPlayer = true;
                    c.Type = CharType.Friend;
                }
                else if (IsPetName(chat.Source))
                {
                    c.Type = CharType.Friend;
                }

                var m = PetOwnerRegex.Match(chat.Message);
                if (m.Success && !c.IsPlayer)
                {
                    c.Owner = m.Groups[1].Value;
                    c.Type = CharType.Friend; // can you /pet leader a NPC pet?
                    GetOrAdd(c.Owner);
                }

                m = PetTellOwnerRegex.Match(chat.Message);
                if (m.Success && !c.IsPlayer)
                {
                    c.Type = CharType.Friend;
                    c.Owner = Player;
                }
            }

            if (e is LogHealEvent heal)
            {
                // mobs can heal mobs so we can't tag healers without more info
                var target = GetOrAdd(heal.Target);

                // some heals are pet only spells
                // however, they may have a recourse heal with the same name... e.g. Growl of the Jaguar
                // todo: do any have a group heal/lifetap recourse with the same name?
                if (target.Owner == null && !target.IsPlayer && heal.Spell != null && heal.Source != null && heal.Source != heal.Target)
                {
                    var spell = Spells?.GetSpell(heal.Spell);
                    if (spell?.IsPetTarget == true)
                        target.Owner = heal.Source;
                }

                if (heal.Source != null)
                {
                    // seems a DD can result in a heal... what is this? some kind of reverse spell DS?
                    // e.g. "You healed Zlandicar for 2 hit points by Summer's Sleet Rk. III."
                    // which means it isn't safe to use heals to determine friend/foe status
                    var source = GetOrAdd(heal.Source);
                    if (target.IsPlayer)
                        source.Type = CharType.Friend;

                    // not sure this is safe since some heals may misflag
                    // e.g. [18286/4217] Mark of the Unsullied Rk. II
                    //if (source.Class == null && heal.Spell != null)
                    //{
                    //    var s = Spells?.GetSpell(heal.Spell);
                    //    if (s?.ClassesCount == 1)
                    //        source.Class = s.ClassesNames;
                    //}
                }
            }

            if (e is LogLootEvent loot)
            {
                var c = GetOrAdd(loot.Char);
                c.IsPlayer = true;
                c.Type = CharType.Friend;
            }

            if (e is LogCastingEvent cast)
            {
                var c = GetOrAdd(cast.Source);

                if (cast.Type == CastingType.Disc)
                {
                    c.IsPlayer = true;
                    c.Type = CharType.Friend;
                }

                var spell = Spells?.GetSpell(cast.Spell);

                if (spell?.ClassesCount == 1)
                {
                    // as of 2021-1-12 there are 321 player spells that are also item clicks/procs
                    // however, ancients and rank 2/3 spells are never procs/clicks
                    if (cast.Type == CastingType.Disc || cast.Type == CastingType.Song
                        || spell.Name.StartsWith("Ancient:")
                        || Regex.IsMatch(spell.Name, @"Rk\.\s?I?II$", RegexOptions.RightToLeft)
                        || (Regex.IsMatch(spell.Name, @"\s[XVI]{1,4}$", RegexOptions.RightToLeft) && spell.DurationTicks > 0))
                        c.Class = spell.ClassesNames;
                }

                if (spell?.LandPet != null)
                {
                    PetBuffs.Add(new PetBuffEmote() { Timestamp = e.Timestamp, Source = cast.Source, Name = spell.Name, Emote = spell.LandPet });
                }
            }

            if (e is LogRawEvent raw)
            {
                // we only need to keep track of buffs for a few seconds between casting time and landing time
                if (PetBuffs.Count > 0)
                    PetBuffs.RemoveAll(x => x.Timestamp < e.Timestamp.AddSeconds(-5));

                // search recently cast pet buffs to see if a spell emote matches and use it to tag a pet owner
                // this could possibly misattribute the pet owner if two people cast the same buff at nearly the same time
                for (var i = 0; i < PetBuffs.Count; i++)
                {
                    var pb = PetBuffs[i];
                    if (raw.Text.EndsWith(pb.Emote))
                    {
                        var name = raw.Text.Substring(0, raw.Text.Length - pb.Emote.Length);
                        var c = Get(name);
                        // since this isn't 100% accurate we only set the owner if one isn't already present
                        // todo: maybe keep track of a few guesses and only set owner after a few matches?
                        if (c != null && c.Owner == null)
                        {
                            //Console.WriteLine("owner: {0} => {1}", name, pb.Source);
                            c.Owner = pb.Source;
                        }
                        PetBuffs.RemoveAt(i);
                        break;
                    }
                }
            }

            if (e is LogHitEvent hit)
            {
                var target = GetOrAdd(hit.Target);
                var source = GetOrAdd(hit.Source);
                if (target.IsPlayer)
                {
                    source.PlayerAggro = hit.Timestamp;
                }

                // backstab detection for rogue mercs and low level rogues
                if (hit.Type == "backstab")
                    source.Class = ClassesMaskShort.ROG.ToString();

                // frenzy detection for low level berserkers
                if (hit.Type == "frenzy")
                    source.Class = ClassesMaskShort.BER.ToString();

                if (hit.Spell != null)
                {
                    var spell = Spells?.GetSpell(hit.Spell);

                    // procs/clicks could misidentify the class
                    // however, rank 2/3 spells are never procs/clicks
                    if (spell?.ClassesCount == 1 && Regex.IsMatch(spell.Name, @"Rk\.\s?I?II$", RegexOptions.RightToLeft))
                    {
                        source.Class = spell.ClassesNames;
                    }

                    // some spells will damage pets to provide their owner a benefit in return
                    if (spell?.IsPetTarget == true)
                    {
                        target.Owner = source.Name;
                    }
                }

                // only owners can damage pets? this should already be covered by detecting pet spells
                //if (source.IsPlayer && IsPetName(target.Name))
                //{
                //    target.Owner = source.Name;
                //}

                GetFoe(hit.Source, hit.Target);
            }

            if (e is LogMissEvent miss)
            {
                var c = GetOrAdd(miss.Target);

                if (c.IsPlayer)
                {
                    c = GetOrAdd(miss.Source);
                    c.PlayerAggro = miss.Timestamp;
                }

                GetFoe(miss.Source, miss.Target);
            }

            if (e is LogDeathEvent death)
            {
                var c = GetOrAdd(death.Name);
                c.PlayerAggro = null;
                if (!c.IsPlayer)
                    c.Type = CharType.Unknown;
            }

            if (e is LogShieldEvent shield)
            {
                // NPCs also use this ability
                var c = GetOrAdd(shield.Source);
                c.Class = ClassesMaskShort.WAR.ToString();
            }

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
            var c = GetOrAdd(name);
            if (c.Type != CharType.Unknown)
                return c.Type;

            if (c.Owner != null)
            {
                c = GetOrAdd(c.Owner);
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
        /// Get existing player or NPC from the list of tracked entities. 
        /// </summary>
        public CharInfo Get(string name)
        {
            CharsByName.TryGetValue(name, out CharInfo c);
            return c;
        }

        /// <summary>
        /// Get or add a player or NPC to the list of tracked entities.
        /// </summary>
        public CharInfo GetOrAdd(string name)
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

                if (name.EndsWith("`s ward") || name.EndsWith("'s ward"))
                    c.Owner = name.Substring(0, name.Length - 7);

                if (name.EndsWith("`s pet") || name.EndsWith("'s pet"))
                    c.Owner = name.Substring(0, name.Length - 6);

                if (name.StartsWith("Eye of "))
                    c.Owner = name.Substring(7);
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
            var n1 = GetOrAdd(name1);
            var n2 = GetOrAdd(name2);

            if (name1 == name2)
            {
                // ignore hitting self
                return null;
            }

            if (n1.Owner == name2 || n2.Owner == name1)
            {
                // ignore pets being damaged by their owners e.g. Spectral Symbiosis
                return null;
            }

            if (n1.IsPlayer && n2.IsPlayer)
            {
                // ignore duels and charmed players
                return null;
            }

            if ((n1.Type == CharType.Friend && n2.Type == CharType.Friend) || (n1.Type == CharType.Foe && n2.Type == CharType.Foe))
            {
                // if they're both foes, one may be newly charmed and should be reverted
                // if they're both friends, one may be a charm break and should be reverted
                if ((!n1.IsPlayer && n2.IsPlayer) || (n1.PlayerAggro > n2.PlayerAggro) || (n1.PlayerAggro != null && n2.PlayerAggro == null))
                {
                    n1.Type = CharType.Foe;
                    n2.Type = CharType.Friend;
                    //Console.WriteLine("*** Pick {0} as foe", name1);
                    return name1;
                }
                else if ((n1.IsPlayer && !n2.IsPlayer) || (n2.PlayerAggro > n1.PlayerAggro) || (n1.PlayerAggro == null && n2.PlayerAggro != null))
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

            else if (IsPetName(n1.Name))
            {
                n1.Type = CharType.Friend;
                n2.Type = CharType.Foe;
                return name2;
            }
            else if (IsPetName(n2.Name))
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
            var c = GetOrAdd(name);
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
