using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQLogParser
{
    /// <summary>
    /// A mask of class types. 
    /// These should be the same literal text as returned by /who (with underscores converted to spaces).
    /// </summary>
    [Flags]
    enum ClassesMask
    {
        Warrior = 1, Cleric = 2, Paladin = 4, Ranger = 8, Shadow_Knight = 16, Druid = 32, Monk = 64, Bard = 128, Rogue = 256,
        Shaman = 512, Necromancer = 1024, Wizard = 2048, Magician = 4096, Enchanter = 8192, Beastlord = 16384, Berserker = 32768
    }

}
