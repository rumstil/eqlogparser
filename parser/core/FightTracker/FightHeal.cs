namespace EQLogParser
{
    /// <summary>
    /// An aggregate of heals that landed on a player.
    /// </summary>
    public class FightHeal
    {
        public string Target { get; set; }
        public int HitCount { get; set; }
        public int HitSum { get; set; }
        //public int CritCount { get; set; }
        //public int CritSum { get; set; }
        //public int HitMin;
        //public int HitMax;

        public void Add(FightHeal x)
        {
            HitSum += x.HitSum;
            HitCount += x.HitCount;
            //CritSum += x.CritSum;
            //CritCount += x.CritCount;
            //if (HitMin > x.HitMin || HitMin == 0)
            //    HitMin = x.HitMin;
            //if (HitMax < x.HitMax)
            //    HitMax = x.HitMax;
        }
    }
}
