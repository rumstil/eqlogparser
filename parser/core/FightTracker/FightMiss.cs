
// riposte/strikethrough discussion
// https://forums.daybreakgames.com/eq/index.php?threads/riposte-change.259558/page-4


namespace EQLogParser
{
    /// <summary>
    /// An aggregate of melee defense checks: riposte, parry, dodge, block, miss, etc..
    /// </summary>
    public class FightMiss
    {
        public static readonly string[] MissOrder = new[] { "miss", "invul", "block", "riposte", "parry", "dodge", "shield", "rune" }; 

        public string Type { get; set; }
        public int Count { get; set; }
        public int Attempts { get; set; }

        public void Merge(FightMiss x)
        {
            Count += x.Count;
            Attempts += x.Attempts;
        }
    }
}
