using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EQLogParser
{
    public abstract class LogEvent
    {
        public DateTime Timestamp;
    }

    public class RawLogEvent : LogEvent
    {
        public string RawText;
    }

    /// <summary>
    /// Generated when a player zones.
    /// </summary>
    public class ZoneEvent : LogEvent
    {
        public string Name;

        public override string ToString()
        {
            return String.Format("Zone: {0}", Name);
        }
    }

    public enum PlayerPartyStatus
    {
        None,
        JoinedGroup,
        LeftGroup,
        JoinedRaid,
        LeftRaid
    }

    /// <summary>
    /// Generated when a /who player list is encountered or a player changes group/raid status. 
    /// The class and level fields may be null.
    /// </summary>
    public class PlayerFoundEvent : LogEvent
    {
        public string Name;
        public string Class;
        public int Level;
        public PlayerPartyStatus Status;

        public override string ToString()
        {
            return String.Format("Player: {0} {1}", Name, Class);
        }
    }

    /// <summary>
    /// Generated when a pet announces it's owner.
    /// </summary>
    public class PetFoundEvent : LogEvent
    {
        public string Name;
        public string Owner;

        public override string ToString()
        {
            return String.Format("Pet: {0} belongs to {1}", Name, Owner);
        }
    }

    /// <summary>
    /// Generated when someone dies (can be player, pet or NPC)
    /// </summary>
    public class DeathEvent : LogEvent
    {
        public string Name;
        public string KillShot;

        public override string ToString()
        {
            return String.Format("Died: {0}", Name);
        }
    }

    /// <summary>
    /// This abstract class is the common root for both hits and misses.
    /// </summary>
    public abstract class FightHitAttemptEvent : LogEvent
    {
        public string Source;
        public bool SourceIsCorpse;
        public string Target;
        //public bool TargetIsCorpse;
    }

    /// <summary>
    /// Generated when a successful fight hit lands and causes damage (can be melee or spell damage).
    /// </summary>
    public class FightHitEvent : FightHitAttemptEvent
    {
        public int Amount;
        public string Type;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Hit: {0} => {1} ({2}) {3} {4}", Source, Target, Amount, Type, Spell);
        }
    }

    public enum FightCritEventSequence { BeforeHit, AfterHit };

    /// <summary>
    /// Generated when a damage hit has a critical success.
    /// These are always shown alongside the action that caused them.
    /// Melee crits are shown before the hit.
    /// Nuke crits are shown after the hit.
    /// </summary>
    public class FightCritEvent : LogEvent
    {
        public string Source;
        //public string Type;
        public int Amount;
        public FightCritEventSequence Sequence;

        public override string ToString()
        {
            return String.Format("HitCrit: {0} => {1}", Source, Amount);
        }
    }

    /// <summary>
    /// Generated when a damage attempt fails and does no damage (can be a miss, defense, or spell resist).
    /// </summary>
    public class FightMissEvent : FightHitAttemptEvent
    {
        //public string AttemptType;
        public string Type;

        public override string ToString()
        {
            return String.Format("Miss: {0} => {1} {2}", Source, Target, Type);
        }
    }

    /// <summary>
    /// Generated when someone heals you or you heal someone else.
    /// Only the 2 players involved in a heal are notified. EQ doesn't send notifications for 3rd party heals.
    /// </summary>
    public class HealEvent : LogEvent
    {
        public string Source;
        public string Target;
        public int Amount;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Heal: {0} => {1} ({2})", Source, Target, Amount);
        }
    }

    public class HealCritEvent : LogEvent
    {
        public string Source;
        public int Amount;

        public override string ToString()
        {
            return String.Format("HealCrit: {0} => {1}", Source, Amount);
        }

    }

    /// <summary>
    /// Generated when a player or NPC starts to cast a spell.
    /// </summary>
    public class SpellCastingEvent : LogEvent
    {
        public string Source;
        public string Spell;
        //public string Cancelled;

        public override string ToString()
        {
            return String.Format("Spell: {0} casting {1}", Source, Spell);
        }
    }

    /// <summary>
    /// Generated when a spell wears off or is dispelled.
    /// </summary>
    public class SpellFadeEvent : LogEvent
    {
        public string Target;
        public string Spell;

        public override string ToString()
        {
            return String.Format("Spell: {0} lost {1}", Target, Spell);
        }
    }

    /// <summary>
    /// Generated when a chat message is received.
    /// </summary>
    public class ChatEvent : LogEvent
    {
        public string Source;
        public string Channel;
        public string Message;

        public override string ToString()
        {
            return String.Format("Chat: {0} tells {1} - {2}", Source, Channel, Message);
        }
    }

    /// <summary>
    /// Generated when an item is looted.
    /// </summary>
    public class ItemLootedEvent : LogEvent
    {
        public string Item;
        public string Looter;
        //public string Type;

        public override string ToString()
        {
            return String.Format("Item: {0} looted {1}", Looter, Item);
        }
    }

    /// <summary>
    /// Generated when an item is crafted.
    /// </summary>
    public class ItemCraftedEvent : LogEvent
    {
        public string Item;
        public string Crafter;

        public override string ToString()
        {
            return String.Format("Item: {0} crafted {1}", Crafter, Item);
        }
    }

    /// <summary>
    /// Generated when a faction hit is received. Change = 0 for faction hits that says "could get no better/worse".
    /// </summary>
    public class FactionEvent : LogEvent
    {
        public string Name;
        public int Change;

        public override string ToString()
        {
            return String.Format("Faction: {0} {1}", Name, Change);
        }
    }

    /// <summary>
    /// Generated when a /loc update is received.
    /// EQ displays coordinates in Y, X, Z order.
    /// </summary>
    public class LocationEvent : LogEvent
    {
        public int X;
        public int Y;
        public int Z;

        public override string ToString()
        {
            return String.Format("Loc: {0}, {1}, {2}", Y, X, Z);
        }
    }

    /// <summary>
    /// Generated when a skill is improved/learned.
    /// </summary>
    public class SkillEvent : LogEvent
    {
        public string Name;
        public int Level;

        public override string ToString()
        {
            return String.Format("Skill: {0} {1}", Name, Level);
        }
    }


    public delegate void RawLogEventHandler(RawLogEvent args);

    public delegate void ZoneEventHandler(ZoneEvent args);
    public delegate void LocationEventHandler(LocationEvent args);
    public delegate void PlayerFoundEventHandler(PlayerFoundEvent args);
    public delegate void PetFoundEventHandler(PetFoundEvent args);
    public delegate void ItemLootedEventHandler(ItemLootedEvent args);
    public delegate void ItemCraftedEventHandler(ItemCraftedEvent args);
    public delegate void FightCritEventHandler(FightCritEvent args);
    public delegate void FightHitEventHandler(FightHitEvent args);
    public delegate void FightMissEventHandler(FightMissEvent args);
    public delegate void HealCritEventHandler(HealCritEvent args);
    public delegate void HealEventHandler(HealEvent args);
    public delegate void DeathEventHandler(DeathEvent args);
    public delegate void SpellCastingEventHandler(SpellCastingEvent args);
    public delegate void SpellFadeEventHandler(SpellFadeEvent args);
    public delegate void ChatEventHandler(ChatEvent args);
    public delegate void FactionEventHandler(FactionEvent args);
    public delegate void SkillEventHandler(SkillEvent args);

    public interface ILogStream
    {
        event RawLogEventHandler OnBeforeEvent;
        event RawLogEventHandler OnAfterEvent;

        //event ZoneEventHandler OnZone;
        //event LocationEventHandler OnLocation;
        //event PlayerFoundEventHandler OnPlayerFound;
        //event PetFoundEventHandler OnPetFound;
        //event FightCritEventHandler OnFightCrit;
        //event FightHitEventHandler OnFightHit;
        //event FightMissEventHandler OnFightMiss;
        //event HealCritEventHandler OnHealCrit;
        //event HealEventHandler OnHeal;
        //event DeathEventHandler OnDeath;
        //event SpellCastingEventHandler OnSpellCasting;
        //event SpellFadeEventHandler OnSpellFade;
        //event ChatEventHandler OnChat;
        //event ItemLootedEventHandler OnItemLooted;
        //event ItemCraftedEventHandler OnItemCrafted;
        //event FactionEventHandler OnFaction;
        //event SkillEventHandler OnSkill;
    }
}


