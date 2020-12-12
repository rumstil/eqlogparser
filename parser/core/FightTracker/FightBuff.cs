using System;


namespace EQLogParser
{
    /// <summary>
    /// Any buff or debuff that landed on a character during a fight.
    /// </summary>
    public class FightBuff
    {
        public string Name { get; set; }
        public DateTime LandedOn { get; set; }
        //public List<(int Start, int End)> Times = new List<(int, int)>();
    }

}
