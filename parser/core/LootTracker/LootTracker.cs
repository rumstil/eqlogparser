using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace EQLogParser
{
    public delegate void LootTrackerEvent(LootInfo args);

    public class LootInfo
    {
        public string Item { get; set; }

        /// <summary>
        /// Mob will be "Unknown" if item rotted or came from an unknown mob.
        /// Mob will be "Crafted" if item was a crafted tradeskill item.
        /// </summary>
        public string Mob { get; set; }

        /// <summary>
        /// Zone will be null if item was crafted.
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Server this was looted on. Mostly interesting if test/beta.
        /// </summary>
        public string Server { get; set; }

        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Track loot drops.
    /// </summary>
    public class LootTracker
    {
        /// <summary>
        /// A list of global or common drops that can be ignored.
        /// </summary>
        public readonly HashSet<string> Ignore = new HashSet<string>(StringComparer.Ordinal)
        {
            "Aderirse Bur",
            "Aged Muramite Etched Scales",
            "Alkalai Loam",
            "Amber",
            "Animal Venom",
            "Animating Force",
            "Black Pearl",
            "Black Powder Pouch",
            "Black Sapphire",
            "Blue Diamond",
            "Bonded Loam",
            "Bone Chips",
            "Breath of Ro",
            "Burnt Out Golem Animation Essence",
            "Chronal Resonance Dust",
            "Chunk of Broken Ancient Stone Worker",
            "Chunk of Discordian Rock",
            "Cloth Bolt",
            "Cobalt Ore",
            "Complex Gold Gem Dusted Rune",
            "Complex Platinum Gem Dusted Rune",
            "Complex Velium Gem Dusted Rune",
            "Crude Silk",
            "Crystallized Sulfur",
            "Diamond",
            "Dirty Runic Papyrus",
            "Distilled Grade C Gormar Venom",
            "Dream Dust",
            "Dream Sapphire",
            "Dweric Powder",
            "Emerald Ring",
            "Emerald",
            "Excellent Animal Pelt",
            "Excellent Silk",
            "Exquisite Animal Pelt",
            "Exquisite Embossed Rune",
            "Exquisite Gem Dusted Rune",
            "Exquisite Gold Gem Dusted Rune",
            "Exquisite Platinum Gem Dusted Rune",
            "Exquisite Silk",
            "Exquisite Velium Gem Dusted Rune",
            "Extra Planar Potential Shard",
            "Fantastic Animal Pelt",
            "Fantastic Silk",
            "Fine Animal Pelt",
            "Fine Feathers",
            "Fine Silk",
            "Fine Steel Dagger",
            "Fine Steel Great Staff",
            "Fine Steel Long Sword",
            "Fine Steel Morning Star",
            "Fine Steel Rapier",
            "Fine Steel Scimitar",
            "Fine Steel Short Sword",
            "Fine Steel Spear",
            "Fine Steel Two Handed Sword",
            "Fine Steel Warhammer",
            "Fire Emerald",
            "Fire Opal",
            "Flame of Vox",
            "Flawless Animal Pelt",
            "Flawless Silk",
            "Fresh Meat",
            "Fulginate Ore",
            "Glob of Fine Viscous Ooze",
            "Glove of Rallos Zek",
            "Gold Gem Dusted Rune",
            "Golem Animation Essence",
            "Grimy Fine Vellum Parchment",
            "Grimy Papyrus",
            "Grimy Spell Scroll",
            "Grubby Crude Spell Scroll",
            "Grubby Fine Papyrus",
            "Grubby Fine Parchment",
            "Harmonagate",
            "Ice of Velious",
            "Immaculate Animal Pelt",
            "Immaculate Silk",
            "Immaculate Spinneret Fluid",
            "Indium Ore",
            "Iridium Ore",
            "Jacinth",
            "Jade",
            "Leather Roll",
            "Lumber Plank",
            "Malachite",
            "Medicinal Herbs",
            "Mithril Amulet",
            "Natural Silk",
            "Natural Spices",
            "Nightmare Ruby",
            "Opal Bracelet",
            "Opal",
            "Osmium Ore",
            "Pearl Necklace",
            "Pearl",
            "Peridot",
            "Phosphorous Powder",
            "Platinum Gem Dusted Rune",
            "Porous Loam",
            "Prestidigitase",
            "Pristine Caladium",
            "Pristine Delphinium",
            "Pristine Laburnum",
            "Pristine Larkspur",
            "Pristine Muscimol",
            "Pristine Oleander",
            "Pristine Silk",
            "Pure Diamond Trade Gem",
            "Purified Grade AA Gormar Venom",
            "Purified Grade AA Nigriventer Venom",
            "Raw Amber Nihilite",
            "Raw Crimson Nihilite",
            "Raw Diamond",
            "Raw Faycite Crystal",
            "Raw Fine Runic Hide",
            "Raw Fine Supple Runic Hide",
            "Raw Indigo Nihilite",
            "Raw Shimmering Nihilite",
            "Raw Supple Runic Hide",
            "Rhenium Ore",
            "Rotting Fang",
            "Rough Animal Pelt",
            "Rubicite Ore",
            "Ruby Crown",
            "Rune Binding Powder",
            "Rune of Al`Kabor",
            "Rune of Concussion",
            "Rune of Crippling",
            "Rune of Frost",
            "Rune of Impetus",
            "Rune of Impulse",
            "Rune of Rathe",
            "Rune of the Astral",
            "Rusty Long Sword",
            "Saltpeter",
            "Sapphire Necklace",
            "Scale Ore",
            "Shabby Rough Spell Scroll",
            "Shabby Vellum Parchment",
            "Sibilisan Viridian Pigment",
            "Smudged Rough Papyrus",
            "Smudged Runic Parchment",
            "Soluble Loam",
            "Sooty Fine Runic Papyrus",
            "Spider Legs",
            "Spider Silk",
            "Spider Venom Sac",
            "Spongy Loam",
            "Stained Fine Runic Spell Scroll",
            "Staurolite",
            "Steel Ingot",
            "Stone of Tranquility",
            "Sunshard Dust",
            "Sunshard Ore",
            "Sunshard Pebble",
            "Sunshard Powder",
            "Superb Animal Pelt",
            "Superb Silk",
            "Taaffeite",
            "Taelosian Mountain Tea Leaves",
            "Taelosian Mountain Wheat",
            "Tainted Larkspur",
            "Tainted Oleander",
            "Tantalum Ore",
            "Tears of Prexus",
            "Thalium Ore",
            "Thick Silk",
            "Titanium Ore",
            "Topaz",
            "Tungsten Ore",
            "Ukun Hide",
            "Uncut Alexandrite",
            "Uncut Amethyst",
            "Uncut Black Sapphire",
            "Uncut Combine Star",
            "Uncut Demantoid",
            "Uncut Goshenite",
            "Uncut Jacinth",
            "Uncut Morganite",
            "Uncut Rubellite",
            "Urticaceae",
            "Used Parchment",
            "Vanadium Ore",
            "Velium Embossed Rune",
            "Velium Gem Dusted Rune",
            "Velium Gemmed Rune",
            "Velium Laced Gormar Venom",
            "Velium Laced Taipan Venom",
            "Velium Silvered Rune",
            "Versluierd Fungus",
            "Wing of Xegony",
            "Words of Acquisition (Beza)",
            "Words of Bondage",
            "Words of Cazic-Thule",
            "Words of Crippling Force",
            "Words of Grappling",
            "Words of Incarceration",
            "Words of Obliteration",
            "Words of Odus",
            "Words of Possession",
            "Words of Requisition",
            "Words of the Ethereal",
            "Words of the Suffering",
            "Writing Ink",
        };

        private string zone;
        private string server;

        public event LootTrackerEvent OnLoot;

        public void HandleEvent(LogEvent e)
        {
            if (e is LogOpenEvent open)
            {
                server = open.Server;
                if (server != "test" && server != "beta")
                    server = null;
            }

            if (e is LogLootEvent loot)
            {
                if (!Ignore.Contains(loot.Item) && zone != null && loot.Source != null)
                    OnLoot?.Invoke(new LootInfo { Item = loot.Item, Mob = loot.Source ?? "Unknown", Zone = zone, Date = e.Timestamp });
            }

            if (e is LogRotEvent rot)
            {
                if (!Ignore.Contains(rot.Item) && zone != null && rot.Source != null)
                    OnLoot?.Invoke(new LootInfo { Item = rot.Item, Mob = rot.Source ?? "Unknown", Zone = zone, Date = e.Timestamp });
            }

            //if (e is LogCraftEvent craft)
            //{
            //    OnLoot?.Invoke(new LootInfo { Item = craft.Item, Mob = "Crafted", Date = e.Timestamp });
            //}

            if (e is LogZoneEvent z)
            {
                zone = z.Name;
            }
        }

    }
}
