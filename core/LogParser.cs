using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace EQLogParser
{
    public sealed class LogLine : RawLogEvent
    {
        //public DateTime Timestamp;
        //public string Text;
        //public int Num;
        //public LogEvent Event;
    }

    /// <summary>
    /// This is an experiment to call parsers via attrib registration rather than explicitly calling each function.
    /// </summary>
    //[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    //public class EventParserAttribute : Attribute
    //{
    //}

    /// <summary>
    /// This is an event based log parser with minimal state tracking. 
    /// All personal log references (e.g. you/your) are converted to the character name.
    /// </summary>
    public class LogParser : ILogStream
    {
        private const string CorpseSuffix = "'s corpse";

        public readonly PlayerFoundEvent Player;
        //public readonly string Server;
        public DateTime MinDate;
        public DateTime MaxDate;
        public List<string> Ignore = new List<string>();
        //public List<string> Targets;

        private int Count;
        //private LogEvent LastEvent;
        //private LogEvent Event; 

        public event RawLogEventHandler OnBeforeEvent;
        public event RawLogEventHandler OnAfterEvent;

        public event ZoneEventHandler OnZone;
        public event LocationEventHandler OnLocation;
        public event PlayerFoundEventHandler OnPlayerFound;
        public event PetFoundEventHandler OnPetFound;
        public event FightCritEventHandler OnFightCrit;
        public event FightHitEventHandler OnFightHit;
        public event FightMissEventHandler OnFightMiss;
        public event HealCritEventHandler OnHealCrit;
        public event HealEventHandler OnHeal;
        public event DeathEventHandler OnDeath;
        public event SpellCastingEventHandler OnSpellCasting;
        public event SpellFadeEventHandler OnSpellFade;
        public event ChatEventHandler OnChat;
        public event ItemLootedEventHandler OnItemLooted;
        public event ItemCraftedEventHandler OnItemCrafted;
        public event FactionEventHandler OnFaction;
        public event SkillEventHandler OnSkill;


        /*
        static LogParser()
        {
            // Collect parsers that are defined in the class
            var methods = typeof(Parser).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parsers = methods.Where(x => x.GetCustomAttributes(false).OfType<EventParserAttribute>().Any());
        }
        */

        public LogParser(string player)
        {
            Player = new PlayerFoundEvent { Timestamp = DateTime.MinValue, Name = player };
            MinDate = DateTime.MinValue;
            MaxDate = DateTime.MaxValue;

            Ignore.Add("You haven't recovered yet...");
            Ignore.Add("You can't use that command right now...");
            Ignore.Add("Your bow shot did double dmg.");
            Ignore.Add("You unleash a flurry of attacks.");
            Ignore.Add("Your target is too far away, get closer!");
            //Ignore.Add("You strike through your opponent's defenses!");
            //Ignore.Add("Your opponent strikes through your defenses!");
            Ignore.Add("You can use the ability"); // [Thu May 19 11:10:40 2016] You can use the ability Entropy of Nature again in 0 minute(s) 6 seconds.

        }

        public void LoadFromFile(StreamReader file)
        {
            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                if (line == null)
                    return;
                ParseLine(line);
            }
        }

        // [Tue Nov 03 21:41:50 2015] Welcome to EverQuest!
        private static readonly Regex LinePartsRegex = new Regex(@"^\[\w{3} (.{20})\] (.+)$", RegexOptions.Compiled);

        /// <summary>
        /// Process a single line from the log file and fire corresponding events.
        /// </summary>
        public void ParseLine(string text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            var m = LinePartsRegex.Match(text);
            if (!m.Success)
                return;

            DateTime ts;
            if (!DateTime.TryParseExact(m.Groups[1].Value, "MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out ts))
                return;

            /*
            // an attempt to make date parsing faster - doesn't seem to matter much
            // 01234567890123456789
            // MMM dd HH:mm:ss yyyy
            var d = m.Groups[1].Value;
            var mm = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            ts = new DateTime(
                Int32.Parse(d.Substring(16, 4)),
                Array.IndexOf(mm, d.Substring(0, 3)) + 1,
                Int32.Parse(d.Substring(4, 2)),
                Int32.Parse(d.Substring(7, 2)),
                Int32.Parse(d.Substring(10, 2)),
                Int32.Parse(d.Substring(13, 2))
                );
            */

            // skip parsing if outside of requested date range
            if (ts < MinDate || ts > MaxDate)
                return;

            // ignore junk to skip slow regex checks
            text = m.Groups[2].Value;
            //if (Ignore.Contains(text))
            //    return;

            var line = new LogLine { Timestamp = ts, RawText = text };
            ParseLine(line);
            //LastLine = line;
        }

        /// <summary>
        /// Process a single line from the log file and fire corresponding events.
        /// </summary>
        public void ParseLine(LogLine line)
        {
            Count += 1;

            if (OnBeforeEvent != null)
                OnBeforeEvent(line);

            CheckFightCrit(line);
            CheckFightHit(line);
            CheckFightMiss(line);
            CheckHealCrit(line);
            CheckHeal(line);
            CheckPlayerFound(line);
            CheckPetFound(line);
            CheckZone(line);
            CheckSpellCasting(line);
            CheckSpellFade(line);
            CheckDeath(line);
            CheckChat(line);
            CheckItemLooted(line);
            CheckItemCrafted(line);
            CheckLocation(line);
            CheckFaction(line);
            CheckSkill(line);

            if (OnAfterEvent != null)
                OnAfterEvent(line);
        }

        /*
        /// <summary>
        /// Send an event to listeners.
        /// </summary>
        public void Dispatch(LogEvent e)
        {
            throw new NotImplementedException();

            //if (e is PlayerFound && OnPlayerFound != null) OnPlayerFound((PlayerFound)e);
            // etc...
        }
        */

        public void GetOwner()
        {
            if (OnPlayerFound != null)
                OnPlayerFound(Player);
        }

        /// <summary>
        /// Return the first successful regex capture.
        /// </summary>
        private string Coalesce(params Group[] groups)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i].Success)
                    return groups[i].Value;
            }
            return null;
        }

        /// <summary>
        /// Normalize player names.
        /// </summary>
        private string FixName(string name)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            if (name.Equals("you", StringComparison.InvariantCultureIgnoreCase) ||
                name.Equals("your", StringComparison.InvariantCultureIgnoreCase))
                return Player.Name;

            // a few log messages can reference a corpse if they occur after the pc/npc died
            if (name.EndsWith(CorpseSuffix))
                name = name.Substring(0, name.Length - CorpseSuffix.Length);

            // many log messages will uppercase the first letter in a mob's name
            // so we will normalize the names to always start with an uppercased char
            if (Char.IsLower(name[0]))
                name = Char.ToUpper(name[0]) + name.Substring(1);

            // mob names are repeated so often it makes sense to use string interning if we are keeping copies of events around
            //return String.Intern(name);
            return name;
        }

        // [Thu May 19 13:37:35 2016] [ANONYMOUS] Rumstil 
        // [Thu May 19 13:39:00 2016] [105 Huntmaster (Ranger)] Rumstil (Halfling) ZONE: kattacastrumb  
        // [Thu May 19 13:55:55 2016] [1 Cleric] Test (Froglok)  ZONE: bazaar  
        // [Thu May 19 13:57:50 2016] OFFLINE MODE[1 Shadow Knight] Test (Dark Elf) ZONE: bazaar  
        private static readonly Regex WhoRegex = new Regex(@"^\[(?:(ANONYMOUS)|(\d+) ([\w\s]+)|(\d+) .+? \(([\w\s]+)\))\] (\w+)", RegexOptions.Compiled);

        //private static readonly Regex PartyRegex = new Regex(@"^(\w+) (?:has|have|have been) (joined|left|removed) .+? (group|raid)\.$", RegexOptions.Compiled);

        // [Thu May 19 10:02:40 2016] You have joined the group.
        // [Thu May 19 10:02:40 2016] Rumstil has joined the group.
        // [Sat Mar 19 20:48:12 2016] You have been removed from the group.
        // [Sat May 07 20:44:12 2016] You remove Fourier from the party.
        // [Thu May 19 10:20:40 2016] You notify Rumstil that you agree to join the group.
        // [Sat May 21 19:42:50 2016] You will now auto-follow Rumstil.
        // [Thu May 19 10:36:24 2016] You are no longer auto-following Rumstil.
        //private static readonly Regex PartyRegex = new Regex(@"^(.+?) (?:have|has) (joined|left|been removed from) the (group|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyJoinedRegex = new Regex(@"^(.+?) (?:have|has) joined the (group|raid)\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PartyJoinedInviteeRegex = new Regex(@"^You notify (\w+) that you agree to join the (group|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyLeftRegex = new Regex(@"^(.+?) (?:have been|has been|has|were) (?:left|removed from) the (group|raid)\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PartyKickRegex = new Regex(@"^You remove (.+?) from the (group|party|raid)\.$", RegexOptions.Compiled);
        private static readonly Regex PartyFollowRegex = new Regex(@"^(?:You will now auto-follow|You are no longer auto-following) (\w+)\.$", RegexOptions.Compiled);


        private void CheckPlayerFound(LogLine line)
        {
            if (OnPlayerFound == null)
                return;

            var m = WhoRegex.Match(line.RawText);
            if (m.Success)
            {
                var classtype = Coalesce(m.Groups[1], m.Groups[3], m.Groups[5]);
                if (classtype == "ANONYMOUS")
                    classtype = null;
                var level = Int32.Parse(Coalesce(m.Groups[2], m.Groups[4]) ?? "0");
                OnPlayerFound(new PlayerFoundEvent { Timestamp = line.Timestamp, Name = m.Groups[6].Value, Class = classtype, Level = level });
                return;
            }

            m = PartyJoinedRegex.Match(line.RawText);
            if (m.Success)
            {
                var status = m.Groups[2].Value == "raid" ? PlayerPartyStatus.JoinedRaid : PlayerPartyStatus.JoinedGroup;
                OnPlayerFound(new PlayerFoundEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value), Status = status });
                return;
            }

            m = PartyLeftRegex.Match(line.RawText);
            if (m.Success)
            {
                var status = m.Groups[2].Value == "raid" ? PlayerPartyStatus.LeftRaid : PlayerPartyStatus.LeftGroup;
                OnPlayerFound(new PlayerFoundEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value), Status = status });
                return;
            }

            m = PartyKickRegex.Match(line.RawText);
            if (m.Success)
            {
                var status = m.Groups[2].Value == "raid" ? PlayerPartyStatus.LeftRaid : PlayerPartyStatus.LeftGroup;
                OnPlayerFound(new PlayerFoundEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value), Status = status });
                return;
            }

            m = PartyFollowRegex.Match(line.RawText);
            if (m.Success)
            {
                OnPlayerFound(new PlayerFoundEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value) });
                return;
            }

        }


        // [Wed Apr 27 09:46:18 2016] Goner says 'My leader is Fourier.'
        // [Fri May 20 16:32:03 2016] Jekn says 'As you wish, oh great one.'
        // [Fri May 20 16:32:03 2016] Jekn says 'Sorry, Master... calming down.'
        private static readonly Regex PetOwnerRegex = new Regex(@"^(.+) says 'My leader is (\w+)\.'$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PetChatRegex = new Regex(@"^(.+) says '(As you wish, oh great one|Sorry, Master\.\.\. calming down|Following you, Master)\.'$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private void CheckPetFound(LogLine line)
        {
            if (OnPetFound == null)
                return;

            var m = PetOwnerRegex.Match(line.RawText);
            if (m.Success)
            {
                OnPetFound(new PetFoundEvent { Timestamp = line.Timestamp, Name = m.Groups[1].Value, Owner = m.Groups[2].Value });
                return;
            }

            m = PetChatRegex.Match(line.RawText);
            if (m.Success)
            {
                OnPetFound(new PetFoundEvent { Timestamp = line.Timestamp, Name = m.Groups[1].Value, Owner = Player.Name });
                return;
            }
        }


        // [Tue Nov 03 21:41:54 2015] You have entered Plane of Knowledge.
        private static readonly Regex ZoneChangedRegex = new Regex(@"^You have entered (.+)\.$", RegexOptions.Compiled);

        private void CheckZone(LogLine line)
        {
            if (OnZone == null)
                return;

            var m = ZoneChangedRegex.Match(line.RawText);
            if (m.Success)
            {
                var zone = m.Groups[1].Value;
                if (zone == "an area where levitation effects do not function" ||
                    zone == "an Arena (PvP) area" ||
                    zone == "the Drunken Monkey stance adequately")
                    return;

                OnZone(new ZoneEvent { Timestamp = line.Timestamp, Name = zone });
                return;
            }
        }


        // melee critical hits are always in 3rd person 
        // [Tue Nov 03 22:09:18 2015] Rumstil scores a critical hit! (8786) -- shown before actual hit, always 3rd person
        // spell criticals are in both first and 3rd person (if others criticals option is enabled)
        // [Tue Nov 03 22:11:19 2015] You deliver a critical blast! (16956) -- shown after actual spell
        private static readonly Regex MeleeCriticalRegex = new Regex(@"^(.+?)(?: scores a critical hit! | lands a Crippling Blow!| scores a Deadly Strike!|'s holy blade cleanses h\w\w target!)\((\d+)\)$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex SpellCriticalRegex = new Regex(@"^(.+?) delivers? a critical blast! \((\d+)\)$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private void CheckFightCrit(LogLine line)
        {
            if (OnFightCrit == null)
                return;

            var m = MeleeCriticalRegex.Match(line.RawText);
            if (m.Success)
            {
                OnFightCrit(new FightCritEvent { Timestamp = line.Timestamp, Source = FixName(m.Groups[1].Value), Amount = Int32.Parse(m.Groups[2].Value), Sequence = FightCritEventSequence.BeforeHit });
                return;
            }

            m = SpellCriticalRegex.Match(line.RawText);
            if (m.Success)
            {
                // if others crits are on, the logger will produce 2 versions of the critical message
                // we can ignore one of them (the 3rd party one)
                if (m.Groups[1].Value != Player.Name)
                    OnFightCrit(new FightCritEvent { Timestamp = line.Timestamp, Source = FixName(m.Groups[1].Value), Amount = Int32.Parse(m.Groups[2].Value), Sequence = FightCritEventSequence.AfterHit });
                return;
            }
        }


        private static readonly Regex UnknownHitRegex = new Regex(@"(\d+).+?damage");

        // non-melee skill hits like archery appear are mostly duplicate messages of plain hits.
        // Old damage shield messages used this format.

        // [Thu May 19 10:37:29 2016] Rumstil scores a Finishing Blow!!
        // [Thu May 19 10:37:29 2016] a fright funnel was hit by non-melee for 186844 points of damage.
        // [Thu May 19 10:37:29 2016] You gain party experience!!
        // [Thu May 19 10:37:29 2016] You hit a fright funnel for 186844 points of damage.
        //private static readonly Regex MiscHitRegex = new Regex(@"^(.+?) was hit by non-melee for (\d+) points of damage\.$", RegexOptions.Compiled);

        // [Wed Apr 20 18:36:59 2016] You pierce a skeletal minion for 1424 points of damage.
        // [Wed Apr 27 09:46:20 2016] A ghoul hits YOU for 3551 points of damage.
        // [Sun May 08 20:13:09 2016] Jonekab slashes an aggressive corpse for 429 points of damage.
        private static readonly Regex MeleeHitRegex = new Regex(@"^(.+?) (hit|kick|slash|crush|pierce|bash|slam|strike|punch|backstab|bite|claw|smash|slice|gore|maul|rend|burn|sting|frenzy on|frenzies on)e?s? (?!by non-melee)(.+?) for (\d+) points of damage\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Fri May 20 19:49:50 2016] Xasarn slashes a Degmar guardian for 1464 points of damage. (Wild Rampage)
        private static readonly Regex RampageRegex = new Regex(@"^(.+?) \w+ (.+?) for (\d+) points of damage\. \((Rampage|Wild Rampage)\)$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Wed Apr 27 09:46:20 2016] A ghoul is pierced by YOUR thorns for 614 points of non-melee damage.
        private static readonly Regex DamageShieldRegex = new Regex(@"^(.+?) is (?:\w+) by YOUR (?:\w+) for (\d+) points of non-melee damage\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu May 12 17:11:52 2016] Rumstil hit a singedbones skeleton for 37469 points of non-melee damage.
        // [Thu May 12 17:11:52 2016] You deliver a critical blast! (37469)
        // [Thu May 12 17:11:52 2016] A singedbones skeleton is caught in a hot summer's storm.
        private static readonly Regex NukeDamageRegex = new Regex(@"^(.+?) hit (.+?) for (\d+) points of non-melee damage\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun Nov 08 21:50:16 2015] A vicious shadow bites at your soul.  You have taken 1138 points of damage.
        private static readonly Regex IncNukeDamageRegex = new Regex(@"^(.*?)  You have taken (\d+) points of damage\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // 3rd party DoT
        // [Wed Apr 20 18:36:41 2016] A skeletal minion has taken 90882 damage from Fourier by Mind Storm.
        // 3rd party DoT from dead caster or trap
        // [Wed Apr 20 18:36:41 2016] A skeletal minion has taken 90882 damage by Mind Storm.
        // personal DoT
        // [Sun May 08 20:13:09 2016] An aggressive corpse has taken 212933 damage from your Mind Storm.
        private static readonly Regex DoTDamageRegex = new Regex(@"^(.+?) has taken (\d+) damage (?:from (your) |from (.+?) by |by )(.+?)\.$", RegexOptions.Compiled);

        // [Wed Nov 04 20:26:51 2015] You have taken 3251 damage from Deadly Screech by The Cliknar Queen.
        // [Wed Nov 04 20:26:51 2015] You have taken 3251 damage from Deadly Screech.
        private static readonly Regex IncDoTDamageRegex = new Regex(@"^You have taken (\d+) damage from (.+?)(?: by (.+))?$", RegexOptions.Compiled);

        // runes are currently handled by FightMiss but perhaps they should also be hits for 0 damage?
        //private static readonly Regex RuneRegex = new Regex(@"^(.+?) (?:try|tries) to (\w+) (.+?), but .*? magical skin absorbs the blow!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        //[EventParser]
        private void CheckFightHit(LogLine line)
        {
            if (OnFightHit == null)
                return;

            var m = MeleeHitRegex.Match(line.RawText);
            if (m.Success)
            {
                var type = m.Groups[2].Value;
                if (type == "frenzy on" || type == "frenzies on")
                    type = "frenzy";

                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Source = FixName(m.Groups[1].Value),
                    Type = type,
                    Target = FixName(m.Groups[3].Value),
                    Amount = Int32.Parse(m.Groups[4].Value)
                };
                OnFightHit(hit);
                return;
            }

            m = RampageRegex.Match(line.RawText);
            if (m.Success)
            {
                var type = m.Groups[4].Value;
                if (type == "Rampage")
                    type = "rampage";
                if (type == "Wild Rampage")
                    type = "wildramp";

                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Source = FixName(m.Groups[1].Value),
                    Target = FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    Type = type
                };
                OnFightHit(hit);
                return;
            }


            /*
            m = MiscHitRegex.Match(line.Text);
            if (m.Success)
            {
                var hit = new FightHit()
                {
                    Timestamp = line.Timestamp,
                    Target = FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Type = "hit",
                };
                OnFightHit(hit);
                return;
            }
            */

            m = NukeDamageRegex.Match(line.RawText);
            if (m.Success)
            {
                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Source = FixName(m.Groups[1].Value),
                    Target = FixName(m.Groups[2].Value),
                    Amount = Int32.Parse(m.Groups[3].Value),
                    Type = "nuke"
                };
                OnFightHit(hit);
                return;
            }

            m = IncNukeDamageRegex.Match(line.RawText);
            if (m.Success)
            {
                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Target = Player.Name,
                    Spell = m.Groups[1].Value,
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Type = "nuke"
                };
                OnFightHit(hit);
                return;
            }

            m = DoTDamageRegex.Match(line.RawText);
            if (m.Success)
            {
                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Target = FixName(m.Groups[1].Value),
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Source = FixName(Coalesce(m.Groups[3], m.Groups[4])),
                    SourceIsCorpse = m.Groups[4].Value.EndsWith(CorpseSuffix),
                    Spell = m.Groups[5].Value,
                    Type = "dot"
                };
                OnFightHit(hit);
                return;
            }
            
            m = IncDoTDamageRegex.Match(line.RawText);
            if (m.Success)
            {
                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Target = Player.Name,
                    Amount = Int32.Parse(m.Groups[1].Value),
                    Spell = m.Groups[2].Value,
                    Source = FixName(m.Groups[3].Value),
                    SourceIsCorpse = m.Groups[3].Value.EndsWith(CorpseSuffix),
                    Type = "dot"
                };
                OnFightHit(hit);
                return;
            }

            m = DamageShieldRegex.Match(line.RawText);
            if (m.Success)
            {
                var hit = new FightHitEvent()
                {
                    Timestamp = line.Timestamp,
                    Source = Player.Name,
                    Target = m.Groups[1].Value,
                    Amount = Int32.Parse(m.Groups[2].Value),
                    Type = "dmgshield"
                };
                OnFightHit(hit);
                return;
            }

#if DEBUG
            //m = UnknownHitRegex.Match(line.RawText);
            //if (m.Success)
            //{
            //    Console.Error.WriteLine(line.RawText);
            //    OnFightHit(new FightHitEvent { Timestamp = line.Timestamp, Target = "N/A", Source = "N/A", Type = line.RawText });
            //}
#endif
        }


        // [Thu Apr 21 20:56:17 2016] Commander Alast Degmar tries to punch YOU, but misses!
        // [Thu May 19 10:30:15 2016] A darkmud watcher tries to hit Rumstil, but misses!
        // [Thu May 19 15:32:30 2016] You try to pierce an ocean serpent, but miss!
        // [Thu May 19 15:32:23 2016] An ocean serpent tries to hit YOU, but YOU parry!
        // [Thu May 19 15:31:33 2016] A sea naga stormcaller tries to hit Fourier, but Fourier's magical skin absorbs the blow!
        //private static readonly Regex MeleeMissRegex = new Regex(@"^(.+?)(?: try to | tries to )(\w+)(?: on)? (.+?), but .*?(miss|riposte|parry|parries|dodge|block|magical skin absorbs the blow)e?s?!$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex MeleeMissRegex = new Regex(@"^(.+?) tr(?:y|ies) to (\w+)(?: on)? (.+?), but .*?(miss|riposte|parry|parries|dodge|block|INVULNERABLE|magical skin absorbs the blow)e?s?!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu May 19 10:36:29 2016] Your target is immune to root spells.
        // [Thu May 26 11:08:19 2016] Your target is immune to the stun portion of this effect.
        // [Thu May 19 10:30:37 2016] Your target resisted the Summer's Cyclone Splash II spell.
        //private static readonly Regex SpellResistRegex = new Regex(@"^Your target resisted the (.+?) spell\.$", RegexOptions.Compiled);

        private void CheckFightMiss(LogLine line)
        {
            if (OnFightMiss == null)
                return;

            var m = MeleeMissRegex.Match(line.RawText);
            if (m.Success)
            {
                var type = m.Groups[4].Value;
                if (type == "parries")
                    type = "parry";
                if (type == "magical skin absorbs the blow")
                    type = "rune";
                if (type == "INVULNERABLE")
                    type = "invul";

                OnFightMiss(new FightMissEvent { Timestamp = line.Timestamp, Source = FixName(m.Groups[1].Value), Target = FixName(m.Groups[3].Value), Type = type });
                return;
            }
        }


        // [Sun May 22 11:02:53 2016] Rumstil performs an exceptional heal! (52008)
        // [Sun May 22 11:02:53 2016] You perform an exceptional heal! (52008)
        private static readonly Regex HealCriticalRegex = new Regex(@"^(.+?) performs? an exceptional heal! \((\d+)\)$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private void CheckHealCrit(LogLine line)
        {
            if (OnHealCrit == null)
                return;

            var m = HealCriticalRegex.Match(line.RawText);
            if (m.Success)
            {
                OnHealCrit(new HealCritEvent { Timestamp = line.Timestamp, Source = FixName(m.Groups[1].Value), Amount = Int32.Parse(m.Groups[2].Value) });
                return;
            }
        }

        // [Sun May 22 11:04:28 2016] You are bathed in a holy light. You have been healed for 46620 points. -- attempted heal (actual amount healed may be lower or even 0)
        // [Thu May 19 10:37:34 2016] Cleric has healed you for 61780 points. -- player heal (only shown if 1+ points are healed)
        // [Sun May 22 11:04:19 2016] Cleric healed you for 7853 hit points by Ardent Elixir Rk. II.  -- player heal (HoT)
        // [Sun May 22 11:40:18 2016] You have been healed for 10000 hit points by your Nature's Reprieve III.
        // [Sun May 22 11:02:53 2016] You have healed a lizard hireling for 15802 points. -- actual amount (only shown if 1+ points are healed)
        // promised heals show up in log as if they were cast on the target by themselves (i.e. You have healed SELF for...)
        private static readonly Regex HealRegex = new Regex(@"^(?:You have healed (.+?)|(.+?) has healed you|(.+?) healed you|You have been healed) for (\d+) (?:hit )?points", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private void CheckHeal(LogLine line)
        {
            if (OnHeal == null)
                return;

            var m = HealRegex.Match(line.RawText);
            if (m.Success)
            {
                var source = Player.Name;
                var target = Player.Name;
                if (m.Groups[1].Success)
                    target = m.Groups[1].Value;
                if (m.Groups[2].Success)
                    source = m.Groups[2].Value;
                if (m.Groups[3].Success)
                    source = m.Groups[3].Value;

                OnHeal(new HealEvent { Timestamp = line.Timestamp, Source = FixName(source), Target = target, Amount = Int32.Parse(m.Groups[4].Value) });
                return;
            }
        }


        // [Tue Nov 03 22:34:34 2015] You have been slain by a sneaky escort!
        // [Tue Nov 03 22:34:38 2015] Rumstil has been slain by a supply guardian!
        private static readonly Regex DeathRegex = new Regex(@"^(.+?) (?:have|has) been slain by (.+?)!$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Thu May 12 17:16:25 2016] You have slain a slag golem!
        private static readonly Regex DeathRegex2 = new Regex(@"^You have slain (.+?)!$", RegexOptions.Compiled);

        // [Thu May 26 14:09:39 2016] a loyal reaver died.
        private static readonly Regex DeathRegex3 = new Regex(@"^(.+?) died\.$", RegexOptions.Compiled | RegexOptions.RightToLeft);


        private void CheckDeath(LogLine line)
        {
            if (OnDeath == null)
                return;

            var m = DeathRegex.Match(line.RawText);
            if (m.Success)
            {
                OnDeath(new DeathEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value), KillShot = FixName(m.Groups[2].Value) });
                return;
            }

            m = DeathRegex2.Match(line.RawText);
            if (m.Success)
            {
                OnDeath(new DeathEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value), KillShot = Player.Name });
                return;
            }

            m = DeathRegex3.Match(line.RawText);
            if (m.Success)
            {
                OnDeath(new DeathEvent { Timestamp = line.Timestamp, Name = FixName(m.Groups[1].Value) });
                return;
            }
        }


        // [Sun May 01 08:44:56 2016] a woundhealer goblin begins to cast a spell. <Inner Fire>
        private static readonly Regex OtherCastRegex = new Regex(@"^(.+?) begins to cast a spell\. <(.+)>$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        // [Sun May 01 08:44:59 2016] You begin casting Group Perfected Invisibility.
        private static readonly Regex SelfCastRegex = new Regex(@"^You begin casting (.+?)\.$", RegexOptions.Compiled);

        private void CheckSpellCasting(LogLine line)
        {
            if (OnSpellCasting == null)
                return;

            var m = SelfCastRegex.Match(line.RawText);
            if (m.Success)
            {
                OnSpellCasting(new SpellCastingEvent { Timestamp = line.Timestamp, Source = Player.Name, Spell = m.Groups[1].Value });
                return;
            }

            m = OtherCastRegex.Match(line.RawText);
            if (m.Success)
            {
                OnSpellCasting(new SpellCastingEvent { Timestamp = line.Timestamp, Source = FixName(m.Groups[1].Value), Spell = m.Groups[2].Value });
                return;
            }
        }

        // [Sat Dec 03 06:27:20 2011] Your Vinelash Cascade Rk. II spell has worn off of a tower sentry.
        private static readonly Regex FadeRegex = new Regex(@"^Your (.+?) spell has worn off of (.+?)\.$", RegexOptions.Compiled);

        // [Thu May 19 14:41:38 2016] Your Fury of the Forest spell did not take hold. (Blocked by Hunter's Fury XI.)
        // [Fri May 20 20:12:33 2016] Your Strength of the Copsestalker Rk. II spell did not take hold on AfkInThroneRoom. (Blocked by Spiritual Vivification Rk. III.)
        //private static readonly Regex BlockedRegex = new Regex(@"^Your (.+?) spell did not take hold on (.+?)\. \(Blocked by (.+?)\.\)$", RegexOptions.Compiled);

        private void CheckSpellFade(LogLine line)
        {
            if (OnSpellFade == null)
                return;

            var m = FadeRegex.Match(line.RawText);
            if (m.Success)
            {
                OnSpellFade(new SpellFadeEvent { Timestamp = line.Timestamp, Target = FixName(m.Groups[2].Value), Spell = m.Groups[1].Value });
                return;
            }
        }


        // [Tue Nov 03 21:52:01 2015] Dude tells the guild, 'hello?'
        // [Sun May 08 20:33:17 2016] You say to your guild, 'congrats'
        // using \w+ is a lot faster than .+? but will miss messages from NPCs with spaces in their names
        private static readonly Regex PrivateChatRegex = new Regex(@"^(.+?) (?:say to your|told|tell your|tells the|tells?) (.+?),\s(?:in .+, )?\s?'(.+)'$", RegexOptions.Compiled | RegexOptions.RightToLeft);
        private static readonly Regex PublicChatRegex = new Regex(@"^(.+?) (says? out of channel|says?|shouts?|auctions?),\s(?:in .+, )?'(.+)'$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private void CheckChat(LogLine line)
        {
            if (OnChat == null)
                return;

            var m = PrivateChatRegex.Match(line.RawText);
            if (m.Success)
            {
                var source = FixName(m.Groups[1].Value);
                var channel = m.Groups[2].Value;
                if (channel == "you" || line.RawText.StartsWith("You told"))
                    channel = "tell";
                if (channel == "party")
                    channel = "group";
                channel = Regex.Replace(channel, @":\d+$", "");

                OnChat(new ChatEvent { Timestamp = line.Timestamp, Source = source, Channel = channel, Message = m.Groups[3].Value });
                return;
            }

            m = PublicChatRegex.Match(line.RawText);
            if (m.Success)
            {
                var source = FixName(m.Groups[1].Value);
                var channel = m.Groups[2].Value.TrimEnd('s');
                if (channel == "says out of channel" || channel == "say out of channel")
                    channel = "ooc";

                OnChat(new ChatEvent { Timestamp = line.Timestamp, Source = source, Channel = channel, Message = m.Groups[3].Value });
                return;
            }
        }


        // [Tue Apr 26 20:17:58 2016] --Rumstil has looted a Alluring Flower.--
        // [Tue Apr 26 20:26:20 2016] --You have looted a Bixie Chitin Sword.--
        private static readonly Regex ItemLootedRegex = new Regex(@"^--(\w+) (?:has|have) looted (.+?)\.--$", RegexOptions.Compiled);

        private void CheckItemLooted(LogLine line)
        {
            if (OnItemLooted == null)
                return;

            var m = ItemLootedRegex.Match(line.RawText);
            if (m.Success)
            {
                OnItemLooted(new ItemLootedEvent { Timestamp = line.Timestamp, Looter = FixName(m.Groups[1].Value), Item = m.Groups[2].Value });
                return;
            }
        }


        // [Fri Jun 10 08:39:54 2016] You can no longer advance your skill from making this item.
        // [Fri Jun 10 08:39:54 2016] You lacked the skills to fashion the items together.
        // [Fri Jun 10 08:39:54 2016] You have fashioned the items together to create something new: Magi-potent Crystal.
        private static readonly Regex ItemCraftedRegex = new Regex(@"^You have fashioned the items together to create .+?: (.+?)\.$", RegexOptions.Compiled);
        //private static readonly Regex ItemCrafted2Regex = new Regex(@"^(\w+) has fashioned (.+?)\.$", RegexOptions.Compiled);
        //private static readonly Regex ItemCrafted3Regex = new Regex(@"^(\w+) was not successful in making .+?, but made (.+?) (?as an alternate product!|unexpectedly!)$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private void CheckItemCrafted(LogLine line)
        {
            if (OnItemCrafted == null)
                return;

            var m = ItemCraftedRegex.Match(line.RawText);
            if (m.Success)
            {
                OnItemCrafted(new ItemCraftedEvent { Timestamp = line.Timestamp, Crafter = Player.Name, Item = m.Groups[1].Value });
                return;
            }

            //if (line.RawText == "You lacked the skills to fashion the items together")
            //{
            //    OnItemCrafted(new ItemCraftedEvent { Timestamp = line.Timestamp, Crafter = Player.Name, Item = null });
            //    return;
            //}
        }


        // [Sat Dec 03 06:25:38 2011] You have become better at Archery! (401)
        private static readonly Regex SkillRegex = new Regex(@"^You have become better at (.+?)! \((\d+)\)$", RegexOptions.Compiled);

        private void CheckSkill(LogLine line)
        {
            if (OnSkill == null)
                return;

            var m = SkillRegex.Match(line.RawText);
            if (m.Success)
            {
                OnSkill(new SkillEvent { Timestamp = line.Timestamp, Name = m.Groups[1].Value, Level = Int32.Parse(m.Groups[2].Value) });
                return;
            }

        }


        // [Mon Mar 21 21:44:57 2016] Your Location is 1131.16, 1089.94, 162.74
        private static readonly Regex LocationRegex = new Regex(@"^Your Location is (-?\d+).+?, (-?\d+).+?, (-?\d+)", RegexOptions.Compiled);

        private void CheckLocation(LogLine line)
        {
            if (OnLocation == null)
                return;

            var m = LocationRegex.Match(line.RawText);
            if (m.Success)
            {
                OnLocation(new LocationEvent { Timestamp = line.Timestamp, Y = Int32.Parse(m.Groups[1].Value), X = Int32.Parse(m.Groups[2].Value), Z = Int32.Parse(m.Groups[3].Value) });
            }
        }


        // [Tue Nov 03 22:09:49 2015] Your faction standing with Stone Hive Bixies has been adjusted by -2.
        // [Sat Sep 11 21:48:46 2010] Your faction standing with Underfoot Autarchs got worse. -- this is the old message
        private static readonly Regex FactionRegex = new Regex(@"^Your faction standing with (.+?) has been adjusted by (-?\d+)\.$", RegexOptions.Compiled);
        private static readonly Regex FactionCapRegex = new Regex(@"^Your faction standing with (.+?) could not possibly get any (better|worse)\.$", RegexOptions.Compiled);

        private void CheckFaction(LogLine line)
        {
            if (OnFaction == null)
                return;

            var m = FactionRegex.Match(line.RawText);
            if (m.Success)
            {
                OnFaction(new FactionEvent { Timestamp = line.Timestamp, Name = m.Groups[1].Value, Change = Int32.Parse(m.Groups[2].Value) });
                return;
            }

            m = FactionCapRegex.Match(line.RawText);
            if (m.Success)
            {
                OnFaction(new FactionEvent { Timestamp = line.Timestamp, Name = m.Groups[1].Value, Change = 0 });
                return;
            }
        }

    }
}
