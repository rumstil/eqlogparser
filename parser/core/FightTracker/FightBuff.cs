using System;


namespace EQLogParser
{
    /// <summary>
    /// Any buff or debuff that landed on a character during a fight.
    /// </summary>
    public class FightBuff
    {
        public string Name { get; set; }
        
        /// <summary>
        /// Number of seconds offset from start of fight.
        /// </summary>
        public int Time { get; set; }
    }

}
