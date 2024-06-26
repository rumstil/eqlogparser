﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;


namespace EQLogParser
{
    public delegate void FightTrackerEvent(FightInfo args);

    /// <summary>
    /// Tracks combat events and assembles them into individual fight summaries.
    /// </summary>
    public class FightTracker
    {
        private readonly ICharLookup Chars;
        private readonly BuffTracker Buffs;
        private readonly List<LogHitEvent> PendingHits = new List<LogHitEvent>();
        private DateTime Timestamp;
        private DateTime LastTimeoutCheck;
        //private LogHitEvent LastHit;
        private FightInfo LastFight; // the fight that the previous event was assigned to

        public string Server { get; private set; }
        public string Player { get; private set; }
        public string Zone { get; private set; }
        public string Party { get; private set; }

        public List<RaidTemplate> Templates { get; } = new List<RaidTemplate>();
        public List<FightInfo> ActiveFights { get; } = new List<FightInfo>();
        public List<RaidFightInfo> ActiveRaids { get; } = new List<RaidFightInfo>();

        /// <summary>
        /// Finish a fight after this duration of no activity is detected.
        /// This should be as least as long as mez or root.
        /// Changing my mind: this should be shorter than a mez/root because we don't want parked mobs to count as engaged and to have an incorrect duration.
        /// </summary>
        //public TimeSpan FightTimeout = TimeSpan.FromSeconds(90);
        public TimeSpan GroupFightTimeout = TimeSpan.FromSeconds(15);
        public TimeSpan RaidFightTimeout = TimeSpan.FromMinutes(2);

        public event FightTrackerEvent OnFightStarted;
        public event FightTrackerEvent OnFightFinished;

        public FightTracker(ISpellLookup spells, ICharLookup chars)
        {
            Chars = chars;
            Buffs = new BuffTracker(spells);
        }

        /// <summary>
        /// Add a raid template.
        /// </summary>
        public void AddTemplate(RaidTemplate temp)
        {
            if (temp == null || temp.Name == null || temp.Zone == null || temp.Mobs == null)
                throw new ArgumentNullException();
            if (temp.Mobs.Length == 0)
                temp.Mobs = new[] { temp.Name };
            Templates.Add(temp);
        }

        /// <summary>
        /// Add raid templates from an XML file.
        /// </summary>
        public void AddTemplateFromFile(Stream stream)
        {
            var xml = XElement.Load(stream, LoadOptions.None);
            var items = xml.Elements();
            foreach (var item in items)
            {
                var temp = new RaidTemplate()
                {
                    Zone = item.Element("Zone")?.Value,
                    Name = item.Element("Name")?.Value,
                    Mobs = item.Elements("Mob").Select(x => x.Value).ToArray(),
                    EndsOnDeath = item.Elements("Mob").Where(x => x.Attribute("EndsOnDeath")?.Value == "true").Select(x => x.Value).ToArray(),
                    //EndsOnDeath = item.Elements("EndsOnDeath").Select(x => x.Value).ToArray(),
                };

                if (!String.IsNullOrEmpty(temp.Zone) && !String.IsNullOrEmpty(temp.Name))
                    AddTemplate(temp);
            }
        }

        /// <summary>
        /// Add raid templates from the Raids.xml resource (it's compiled into the assembly).
        /// </summary>
        public void AddTemplateFromResource()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("EQLogParser.FightTracker.Raids.xml");
            AddTemplateFromFile(stream);
        }

        public void HandleEvent(LogEvent e)
        {
            Buffs.HandleEvent(e);

            Timestamp = e.Timestamp;

            // todo: log open should reset the tracker's state
            if (e is LogOpenEvent open)
            {
                Player = open.Player;
                Server = open.Server;
                Zone = null;
                Party = null;
                LastTimeoutCheck = DateTime.MinValue;
                ActiveFights.Clear();
                ActiveRaids.Clear();
                PendingHits.Clear();
            }

            // no need to check for timeouts more often than every few seconds
            // however timestamp may jump back if we are handling external events like LogWhoEvent from a roster
            if (LastTimeoutCheck + TimeSpan.FromSeconds(5) <= Timestamp || LastTimeoutCheck > Timestamp)
            {
                CheckFightTimeouts();
                LastTimeoutCheck = Timestamp;
            }

            if (e is LogHitEvent hit)
            {
                TrackHit(hit);
            }

            if (e is LogMissEvent miss)
            {
                TrackMiss(miss);
            }

            if (e is LogHealEvent heal)
            {
                TrackHeal(heal);
            }

            if (e is LogCastingEvent cast)
            {
                TrackCasting(cast);
            }

            if (e is LogDeathEvent death)
            {
                TrackDeath(death);
            }

            if (e is LogZoneEvent zone)
            {
                TrackZone(zone);
            }

            if (e is LogXPEvent xp)
            {
                TrackXP(xp);
            }

            if (e is LogPartyEvent party)
            {
                TrackParty(party);
            }

            if (e is LogChatEvent chat)
            {
                TrackChat(chat);
            }

            if (e is LogRawEvent raw)
            {
                TrackMisc(raw);
            }

        }

        private void TrackZone(LogZoneEvent zone)
        {
            Zone = zone.Name;
        }

        private void TrackHit(LogHitEvent hit)
        {
            // DoTs from dead casters.. perhaps we could attribute the last caster to have died
            if (String.IsNullOrEmpty(hit.Source))
                return;

            var foe = Chars.GetFoe(hit.Source, hit.Target);
            if (foe == null)
            {
                //Console.WriteLine("*** " + hit);
                if (hit.Source != hit.Target)
                    PendingHits.Add(hit);
                return;
            }

            if (hit.Source.EndsWith("'s corpse"))
            {
                if (hit.Target == foe)
                {
                    // rename source to track damage from dead player
                    hit = new LogHitEvent
                    {
                        Timestamp = hit.Timestamp,
                        Type = hit.Type,
                        Source = hit.Source.Substring(0, hit.Source.Length - 9),
                        Target = hit.Target,
                        Amount = hit.Amount,
                        Mod = hit.Mod,
                        Spell = hit.Spell
                    };
                }
                else
                {
                    // do not track damage from a dead mob
                    return;
                }

            }

            var f = GetOrAddFight(foe);
            if (f == null)
                return;

            f.AddHit(hit);
            //LastHit = hit;
            LastFight = f;
        }

        private void TrackMiss(LogMissEvent miss)
        {
            var foe = Chars.GetFoe(miss.Source, miss.Target);
            if (foe == null)
                return;

            var f = GetOrAddFight(foe);
            if (f == null)
                return;

            f.AddMiss(miss);
            LastFight = f;
        }

        private void TrackHeal(LogHealEvent heal)
        {
            // rather than treating ward heals as a separte caster, attribute them to the owner
            if (heal.Source != null && heal.Source.EndsWith("`s ward"))
            {
                heal.Source = heal.Source.Substring(0, heal.Source.Length - 7);
                //heal.Spell = "ward:" + heal.Spell;
            }

            // heals are complicated because they aren't easily attributable to a fight (especially if several fights are in progress)
            // the best compromise is probably to attribute to the most recent active fight (at least for player cast heals)
            // what if a cleric heals someone between fights? (ignore it for now)
            // what about if A,B are fighting but C heals C or D? (check all active fights)
            if (LastFight != null && LastFight.Status == FightStatus.Active)
            {
                for (int i = 0; i < LastFight.Participants.Count; i++)
                    if (LastFight.Participants[i].Name == heal.Target || LastFight.Participants[i].Name == heal.Source)
                    {
                        LastFight.AddHeal(heal);
                        return;
                    }
            }

            foreach (var f in ActiveFights)
            {
                if (f == LastFight)
                    continue;

                //for (int i = 0; i < f.Participants.Count; i++)
                for (int i = f.Participants.Count - 1; i >= 0; i--)
                    if (f.Participants[i].Name == heal.Target || f.Participants[i].Name == heal.Source)
                    {
                        f.AddHeal(heal);
                        return;
                    }
            }

            // finally just attribute it to an active fight if it is cast by a friend
            if (heal.Source != null && Chars.GetType(heal.Source) == CharType.Friend && ActiveFights.Count > 0)
            {
                var f = ActiveFights[^1];
                f.AddHeal(heal);
                return;
            }

            //if (LastFight == null || LastFight.Status != FightStatus.Active)
            //    LastFight.AddHeal(heal);
        }

        private void TrackCasting(LogCastingEvent cast)
        {
            // attribute spell to most recent fight - this is probably the best compromise
            if (LastFight != null && LastFight.Status == FightStatus.Active)
            {
                // only include casting by the target and friends because if we if we pull a group of mobs, 
                // we don't want mob A appearing as a participant in a fight with mob B just because it cast a spell
                if (cast.Source == LastFight.Name || Chars.GetType(cast.Source) == CharType.Friend)
                    LastFight.AddCasting(cast);
            }
        }

        private void TrackDeath(LogDeathEvent death)
        {
            if (LastFight != null && LastFight.Status == FightStatus.Active && death.Name != LastFight.Name)
            {
                // todo: use killshot name instead of lastfight
                //LastFight.DeathCount += 1;
                //LastFight.AddDeath(death);
            }

            var f = GetFight(death.Name);
            if (f != null)
            {
                f.Status = FightStatus.Killed;
                f.UpdatedOn = death.Timestamp;
                f.CohortCount = ActiveFights.Count - 1;
                FinishFight(f);
            }
            else
            {
                if (death.KillShot != null)
                {
                    f = GetFight(death.KillShot);
                    if (f != null)
                        f.AddDeath(death);
                }
                else if (LastFight != null && LastFight.Status == FightStatus.Active)
                {
                    LastFight.AddDeath(death);
                }
            }
        }

        private void TrackXP(LogXPEvent xp)
        {
            if (xp.Type == XPType.RaidXP)
                Party = "Raid";

            if (xp.Type == XPType.GroupXP)
                Party = "Group";

            if (xp.Type == XPType.SoloXP)
                Party = "Solo";
        }

        private void TrackParty(LogPartyEvent party)
        {
            if (party.Status == PartyStatus.RaidJoined)
                Party = "Raid";

            // may be group or solo -- lets just use group until an XP message says otherwise
            if (party.Status == PartyStatus.RaidLeft && party.Name == Player)
                Party = "Group";

            if (party.Status == PartyStatus.GroupLeft && Party == "Group" && party.Name == Player)
                Party = "Solo";
        }

        private void TrackChat(LogChatEvent chat)
        {
            if (chat.Channel == "raid")
                Party = "Raid";

            if (chat.Channel == "group" && Party == "Solo")
                Party = "Group";
        }

        private void TrackMisc(LogRawEvent raw)
        {


        }

        /// <summary>
        /// Run a check to see if any fights have timed out.
        /// </summary>
        public void CheckFightTimeouts()
        {
            //Console.WriteLine("Checking {0} fights for timeout", ActiveFights.Count);

            var cohorts = ActiveFights.Count - 1;

            var timeout = Party == "Raid" ? RaidFightTimeout : GroupFightTimeout;

            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];

                if (f.UpdatedOn + timeout <= Timestamp)
                {
                    ActiveFights.Remove(f);

                    // ignore fights without any damage activity
                    // e.g. miss messages arriving after death message
                    // e.g. mob casting but never engaged
                    if (f.Target.InboundHitSum > 0)
                    {
                        f.Status = FightStatus.Timeout;
                        f.CohortCount = cohorts;
                        FinishFight(f);
                    }
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Force all active fights to time out.
        /// If the optional "mobs" parameter is used this will only time out mobs matching the given list.
        /// </summary>
        public void ForceFightTimeouts(string[] mobs = null)
        {
            var cohorts = ActiveFights.Count - 1;

            int i = 0;
            while (i < ActiveFights.Count)
            {
                var f = ActiveFights[i];
                if (mobs != null && !mobs.Contains(f.Name))
                {
                    i += 1;
                    continue;
                }

                ActiveFights.Remove(f);

                // ignore fights without any damage activity
                // e.g. miss messages arriving after death message
                // e.g. mob casting but never engaged
                if (f.Target.InboundHitSum > 0)
                {
                    f.Status = FightStatus.Timeout;
                    f.CohortCount = cohorts;
                    FinishFight(f);
                }
            }
        }

        /// <summary>
        /// Finish an active fight and pass it off to the OnFightFinished delegate.
        /// </summary>
        private void FinishFight(FightInfo f)
        {
            ActiveFights.Remove(f);

            // players and pets may end up as fights before we know their status or if they are charmed
            if (Chars.GetType(f.Name) == CharType.Friend)
                return;

            f.Party = Party ?? "Solo";
            f.Player = Player;
            f.Server = Server;

            // update info from trackers
            //Buffs.PurgeStale(f.StartedOn.AddMinutes(-5));
            foreach (var p in f.Participants)
            {
                p.Class = Chars.GetClass(p.Name);
                p.PetOwner = Chars.GetOwner(p.Name);
                // go back a few seconds to include buffs cast in preparation for the fight
                p.Buffs = Buffs.Get(p.Name, f.StartedOn, f.UpdatedOn, -10);
            }

            f.Finish();

            // fight may have no participants if only a miss was registered
            if (f.Participants.Count == 0)
                return;

            // see if the fight is part of a raid
            var raid = GetRaid(f);
            if (raid != null)
            {
                raid.Merge(f);

                // end the raid on boss death 
                // todo: what about trash that's still alive after the boss dies?
                // finishing a raid will force timeouts on related mobs and those timeouts can recursively call 
                // this function again -- we use the status check to prevent infinite recursion
                if (raid.Template.EndsOnDeath.Contains(f.Name) && f.Status == FightStatus.Killed)
                {
                    FinishRaid(raid);
                }
            }

            // after a fight is passed to the OnFightFinished delegate it should never be modified
            // and probably not even accessed in case code from another thread updates it for some reason
            OnFightFinished?.Invoke(f);
            LastFight = null;
        }

        /// <summary>
        /// Finish an active raid and pass it off to the OnFightFinished delegate.
        /// </summary>
        private void FinishRaid(RaidFightInfo raid)
        {
            ForceFightTimeouts(raid.Template.Mobs);

            // this raid removal must occur after forcing timeouts otherwise the timeouts will create duplicate raids
            ActiveRaids.Remove(raid);

            raid.Finish();

            OnFightFinished?.Invoke(raid);
        }

        /// <summary>
        /// Find the active fight involving the given foe name or return null if it doesn't exist.
        /// </summary>
        private FightInfo GetFight(string name)
        {
            // the fight list can get pretty long so we limit our check to the tail
            //for (var i = ActiveFights.Count - 1; i >= 0 && i > ActiveFights.Count - 20; i--)
            for (var i = ActiveFights.Count - 1; i >= 0; i--)
            {
                if (ActiveFights[i].Name == name)
                {
                    ActiveFights[i].UpdatedOn = Timestamp;
                    return ActiveFights[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Find the active fight involving the given foe name or create a new fight if it doesn't exist.
        /// This will return a null if it the name is a friend.
        /// </summary>
        private FightInfo GetOrAddFight(string name)
        {
            var f = GetFight(name);
            if (f != null)
                return f;

            // fights are always "foe" focused so we need to return a null if the name is a friend
            var type = Chars.GetType(name);
            if (type == CharType.Friend)
                return null;

            // start a new fight
            f = new FightInfo();
            //f.Id = name.Replace(' ', '-') + "-" + Environment.TickCount.ToString();
            f.Zone = Zone;
            f.Name = name;
            f.Target = new FightParticipant() { Name = name };
            // todo: always start fights at multiple of 6s for better alignment of data when merging fights?
            f.StartedOn = f.UpdatedOn = Timestamp;

            // are there any pending hits that couldn't be assigned to a fight?
            // since this doesn't rollback the StartedOn time, all pending hits will be placed in the first interval
            if (PendingHits.Count > 0)
            {
                PendingHits.RemoveAll(x => x.Timestamp < Timestamp.AddMinutes(-1));
                foreach (var hit in PendingHits.Where(x => x.Source == name || x.Target == name))
                {
                    f.AddHit(hit);
                }
                PendingHits.RemoveAll(x => x.Source == name || x.Target == name);
            }

            ActiveFights.Add(f);
            if (OnFightStarted != null)
                OnFightStarted(f);

            return f;
        }

        /// <summary>
        /// Find the active raid involving the given foe or create a new raid if it doesn't exist.
        /// Will return a null if there is no raid template defined for this mob.
        /// </summary>
        private RaidFightInfo GetRaid(FightInfo f)
        {
            if (f.Party != "Raid")
                return null;

            // remove stale raids
            ActiveRaids.RemoveAll(x => x.StartedOn < f.StartedOn.AddHours(-1));

            // search active raids
            var raid = ActiveRaids.FirstOrDefault(x => x.Template.Zone == f.Zone && x.Template.Mobs.Contains(f.Name));

            // if none found, check if this is a new raid
            if (raid == null)
            {
                var temp = Templates.FirstOrDefault(x => x.Zone == f.Zone && x.Mobs.Contains(f.Name));
                if (temp == null)
                    return null;

                raid = new RaidFightInfo();
                raid.Template = temp;
                ActiveRaids.Add(raid);
            }

            return raid;
        }



        //private string StripCorpse(string name)
        //{
        //    if (name.EndsWith("'s corpse"))
        //        name = name.Substring(0, name.Length - 9);
        //    return name;
        //}
    }
}
