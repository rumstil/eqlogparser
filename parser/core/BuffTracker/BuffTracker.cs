using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EQLogParser.Events;


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
        public DateTime LandedOn;
        public int DurationTicks; // base duration prior to focus/aa
    }

    /// <summary>
    /// Watches for spell emotes and uses them to track player buffs.
    /// </summary>
    public class BuffTracker
    {
        private readonly ISpellLookup Spells;

        private readonly List<BuffInfo> Buffs = new List<BuffInfo>();

        //public readonly List<string> Ignored = new List<string>();

        public BuffTracker(ISpellLookup spells)
        {
            Spells = spells;
        }

        public void HandleEvent(LogEvent e)
        {
            if (e is LogDeathEvent death)
            {
                Buffs.RemoveAll(x => x.CharName == death.Name);
            }

            if (e is LogRawEvent raw)
            {
                // there are two problems loading buffs from spell emotes
                // 1. some buffs are irrelevant. e.g. promised heals
                // 2. some emotes are not unique 
                var spell = Spells.GetSpellFromEmote(raw.Text);
                if (spell != null && spell.DurationTicks > 1)
                {
                    var name = raw.Player;
                    if (raw.Text != spell.LandSelf)
                        name = raw.Text.Substring(0, raw.Text.Length - spell.LandOthers.Length);

                    
                    var buff = new BuffInfo()
                    {
                        CharName = name,
                        SpellName = spell.Name,
                        LandedOn = e.Timestamp,
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
            Buffs.RemoveAll(x => x.LandedOn < ts);
        }

        /// <summary>
        /// Get all buffs for a single character since the given timestamp.
        /// </summary>
        public IEnumerable<FightBuff> Get(string name, DateTime ts)
        {
            return Buffs
                .Where(x => x.CharName == name && x.LandedOn >= ts)
                .Select(x => new FightBuff { Name = x.SpellName, LandedOn = x.LandedOn });
        }


    }
}
