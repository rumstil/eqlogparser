using System.Collections.Generic;


namespace EQLogParser
{
    /// <summary>
    /// A short history of activity that occured before a death.
    /// </summary>
    public class FightDeath
    {
        public string Name { get; set; }
        public string Class { get; set; }
        //public DateTime Timestamp;
        public int Time { get; set; }
        public List<string> Replay = new List<string>();
    }


}
