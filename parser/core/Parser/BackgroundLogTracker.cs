using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;


namespace EQLogParser
{
    /// <summary>
    /// Combines all the trackers into a single class that can safely run in a background task and save data to 
    /// concurrent queues for sharing with other threads.
    /// </summary>
    public class BackgroundLogTracker
    {
        private readonly FightTracker fightTracker;
        private readonly LootTracker lootTracker;
        //private readonly CharTracker charTracker;
        private readonly ConcurrentQueue<FightInfo> fights;
        private readonly ConcurrentQueue<LootInfo> loot;

        public ConcurrentQueue<FightInfo> Fights => fights;
        public ConcurrentQueue<LootInfo> Loot => loot;

        public BackgroundLogTracker(SpellParser spells)
        {
            fights = new ConcurrentQueue<FightInfo>();
            fightTracker = new FightTracker(spells);
            fightTracker.OnFightFinished += x => fights.Enqueue(x);
            fightTracker.AddTemplateFromResource();
            loot = new ConcurrentQueue<LootInfo>();
            lootTracker = new LootTracker();
            lootTracker.OnLoot += x => loot.Enqueue(x);
            //charTracker = new CharTracker();
        }

        public void HandleEvent(LogEvent e)
        {
            if (e is LogOpenEvent)
            {
                // set parser player...
            }

            fightTracker.HandleEvent(e);
            lootTracker.HandleEvent(e);
            //charTracker.HandleEvent(e);
        }

        private void HandleLine(string line)
        {
            throw new NotImplementedException();
        }
    }
}
