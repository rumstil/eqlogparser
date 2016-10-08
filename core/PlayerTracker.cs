using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EQLogParser
{
    public class PlayerInfo
    {
        public string Name;
        public string Class;
        public int Level;
        public string Owner;

        public override string ToString()
        {
            return String.Format("{0} {1}", Name, Class);
        }
    }

    /// <summary>
    /// This class consumes parser events to track player, merc and pet names. (i.e. Anyone who isn't a killable mob).
    /// </summary>
    public class PlayerTracker : IEnumerable<PlayerInfo>
    {
        private readonly Dictionary<string, PlayerInfo> Players = new Dictionary<string, PlayerInfo>(StringComparer.InvariantCultureIgnoreCase);


        public PlayerTracker()
        {

        }

        public PlayerTracker(LogParser parser) : base()
        {
            Subscribe(parser);
        }

        public void Subscribe(LogParser parser)
        {
            parser.OnPlayerFound += TrackPlayer;
            parser.OnPetFound += TrackPetOwner;
            parser.OnChat += TrackChat;
            parser.OnItemLooted += TrackItemDrop;
            parser.OnFightCrit += TrackFightCrit;
            parser.OnHeal += TrackHeal;
            parser.GetOwner();
        }

        public void Unsubscribe(LogParser parser)
        {
            parser.OnPlayerFound -= TrackPlayer;
            parser.OnPetFound -= TrackPetOwner;
            parser.OnChat -= TrackChat;
            parser.OnItemLooted -= TrackItemDrop;
            parser.OnFightCrit -= TrackFightCrit;
            parser.OnHeal -= TrackHeal;            
        }

        private void TrackPlayer(PlayerFoundEvent player)
        {
            Add(player.Name);
            if (player.Class != null)
                Players[player.Name].Class = player.Class;
        }

        private void TrackPetOwner(PetFoundEvent pet)
        {
            Add(pet.Name);
            Add(pet.Owner);
            Players[pet.Name].Owner = pet.Owner;
        }

        private void TrackChat(ChatEvent chat)
        {
            // use chat messages to identify players
            // NPCs use say, tell and shout
            // pets use tell
            // mercs send group chat (maybe all starting with "Casting"?)
            if (chat.Channel != "tell" && chat.Channel != "shout" && chat.Channel != "say")
                Add(chat.Source);
        }

        private void TrackItemDrop(ItemLootedEvent drop)
        {
            Add(drop.Looter);
        }

        private void TrackFightCrit(FightCritEvent crit)
        {
            // pretty sure NPCs never have critical hits 
            Add(crit.Source);
        }

        private void TrackHeal(HealEvent heal)
        {
            Add(heal.Source);
            Add(heal.Target);
        }

        /// <summary>
        /// Add a name to the player list. This function will check for duplicates.
        /// </summary>
        public void Add(string name)
        {
            if (!String.IsNullOrEmpty(name) && !Players.ContainsKey(name))
                Players.Add(name, new PlayerInfo { Name = name });
        }

        /// <summary>
        /// Remove a name from the player list.
        /// </summary>
        public bool Remove(string name)
        {
            return Players.Remove(name);
        }

        /// <summary>
        /// Get a player by name.
        /// </summary>
        public PlayerInfo Get(string name)
        {
            PlayerInfo player = null;
            if (Players.TryGetValue(name, out player))
                return player;
            return null;
        }

        /// <summary>
        /// Check if a player name has been registered.
        /// </summary>
        public bool Contains(string name)
        {
            if (String.IsNullOrEmpty(name))
                return false;
            return Players.ContainsKey(name);
        }

        /// <summary>
        /// Return the owner of a pet or null if this doesn't appear to be an owned pet.
        /// </summary>
        public string GetOwner(string name)
        {
            if (name.EndsWith("`s warder"))
                return name.Substring(0, name.Length - 9);
            
            if (name.EndsWith("`s pet"))
                return name.Substring(0, name.Length - 6);
            
            var player = Get(name);
            if (player != null && player.Owner != null)
                return player.Owner;

            return null;
        }


        public IEnumerator<PlayerInfo> GetEnumerator()
        {
            return Players.Values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
