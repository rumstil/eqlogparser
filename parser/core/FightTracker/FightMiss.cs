
// riposte/strikethrough discussion
// https://forums.daybreakgames.com/eq/index.php?threads/riposte-change.259558/page-4


namespace EQLogParser
{
    /// <summary>
    /// An aggregate of melee defense checks: riposte, parry, dodge, block, miss, etc..
    /// </summary>
    public class FightMiss
    {
        //public static readonly string[] MissOrder = new[] { "invul", "riposte", "parry", "dodge", "block", "miss", "rune" }; // the way it was for 20 years
        public static readonly string[] MissOrder = new[] { "miss", "invul", "riposte", "parry", "dodge", "block", "rune" }; // lets change it up for shits and giggles

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
