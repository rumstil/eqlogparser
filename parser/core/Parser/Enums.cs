using System;

namespace EQLogParser
{
    /// <summary>
    /// A mask of class types. 
    /// These should be the same literal text as returned by /who (with underscores converted to spaces).
    /// </summary>
    [Flags]
    public enum ClassesMaskLong
    {
        Warrior = 1, Cleric = 2, Paladin = 4, Ranger = 8, Shadow_Knight = 16, Druid = 32, Monk = 64, Bard = 128, Rogue = 256,
        Shaman = 512, Necromancer = 1024, Wizard = 2048, Magician = 4096, Enchanter = 8192, Beastlord = 16384, Berserker = 32768
    }

    [Flags]
    public enum ClassesMaskShort
    {
        WAR = 1, CLR = 2, PAL = 4, RNG = 8, SHD = 16, DRU = 32, MNK = 64, BRD = 128, ROG = 256,
        SHM = 512, NEC = 1024, WIZ = 2048, MAG = 4096, ENC = 8192, BST = 16384, BER = 32768
        //ALL = 65535
    }

}
