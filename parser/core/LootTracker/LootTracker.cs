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
        /// Mob will be null if item was crafted.
        /// </summary>
        public string Mob { get; set; }

        /// <summary>
        /// Zone will be null if item was crafted.
        /// </summary>
        public string Zone { get; set; }

        /// <summary>
        /// Server this was looted on.
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
            "Bloodstone",
            "Blue Diamond",
            "Bonded Loam",
            "Bone Chips",
            "Breath of Ro",
            "Brick of Ethereal Energy",
            "Burnt Out Golem Animation Essence",
            "Chronal Resonance Dust",
            "Chunk of Broken Ancient Stone Worker",
            "Chunk of Discordian Rock",
            "Chunk of Meat",
            "Cloth Bolt",
            "Cobalt Ore",
            "Complex Gold Gem Dusted Rune",
            "Complex Platinum Gem Dusted Rune",
            "Complex Velium Embossed Rune",
            "Complex Velium Gem Dusted Rune",
            "Conflagrant Diamond",
            "Crude Animal Pelt",
            "Crude Silk",
            "Crystallized Sulfur",
            "Curzon",
            "Diamond",
            "Dirty Runic Papyrus",
            "Dirty Runic Spell Scroll",
            "Distilled Grade A Gormar Venom",
            "Distilled Grade A Nigriventer Venom",
            "Distilled Grade C Gormar Venom",
            "Distilled Grade C Nigriventer Venom",
            "Dream Dust",
            "Dream Meat",
            "Dream Sapphire",
            "Dweric Powder",
            "Emerald Ring",
            "Emerald",
            "Eternal Grove Chain Chest Ornament",
            "Eternal Grove Chain Feet Ornament",
            "Eternal Grove Chain Hands Ornament",
            "Eternal Grove Chain Helm Ornament",
            "Eternal Grove Chain Legs Ornament",
            "Eternal Grove Chain Wrist Ornament",
            "Eternal Grove Cloth Arms Ornament",
            "Eternal Grove Cloth Chest Ornament",
            "Eternal Grove Cloth Hands Ornament",
            "Eternal Grove Cloth Helm Ornament",
            "Eternal Grove Cloth Legs Ornament",
            "Eternal Grove Cloth Robe Ornament",
            "Eternal Grove Cloth Wrist Ornament",
            "Eternal Grove Leather Feet Ornament",
            "Eternal Grove Plate Arms Ornament",
            "Eternal Grove Plate Chest Ornament",
            "Eternal Grove Plate Feet Ornament",
            "Eternal Grove Plate Hands Ornament",
            "Eternal Grove Plate Helm Ornament",
            "Eternal Grove Plate Legs Ornament",
            "Eternal Grove Plate Wrist Ornament",
            "Excellent Animal Pelt",
            "Excellent Silk",
            "Exquisite Animal Pelt",
            "Exquisite Embossed Rune",
            "Exquisite Gem Dusted Rune",
            "Exquisite Gold Gem Dusted Rune",
            "Exquisite Marrow",
            "Exquisite Platinum Embossed Rune",
            "Exquisite Platinum Gem Dusted Rune",
            "Exquisite Silk",
            "Exquisite Velium Embossed Rune",
            "Exquisite Velium Gem Dusted Rune",
            "Extra Planar Potential Shard",
            "Extruded Underfoot Diamond",
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
            "Fire Emerald Ring",
            "Fire Emerald",
            "Fire Opal",
            "Flame of Vox",
            "Flawless Animal Pelt",
            "Flawless Larkspur",
            "Flawless Silk",
            "Flawless Spinneret Fluid",
            "Fresh Meat",
            "Fulginate Ore",
            "Fused Loam",
            "Glob of Fine Gelatinous Ooze",
            "Glob of Fine Viscous Ooze",
            "Glove of Rallos Zek",
            "Glowgem",
            "Glowrock",
            "Glowstone",
            "Gold Gem Dusted Rune",
            "Golden Pendant",
            "Golem Animation Essence",
            "Grimy Fine Vellum Parchment",
            "Grimy Papyrus",
            "Grimy Spell Scroll",
            "Grubby Crude Spell Scroll",
            "Grubby Fine Papyrus",
            "Grubby Fine Parchment",
            "Grubby Fine Vellum",
            "Harmonagate",
            "Ice of Velious",
            "Immaculate Animal Pelt",
            "Immaculate Caladium",
            "Immaculate Delphinium",
            "Immaculate Laburnum",
            "Immaculate Larkspur",
            "Immaculate Muscimol",
            "Immaculate Oleander",
            "Immaculate Privet",
            "Immaculate Silk",
            "Immaculate Spinneret Fluid",
            "Indium Ore",
            "Intricate Binding Powder",
            "Iridium Ore",
            "Jacinth",
            "Jade Earring",
            "Jade Ring",
            "Jade",
            "Leather Roll",
            "Lucidem",
            "Lumber Plank",
            "Malachite",
            "Mastruq's Clawed Finger",
            "Medicinal Herbs",
            "Mithril Amulet",
            "Natural Silk",
            "Natural Spices",
            "Nightmare Ruby",
            "Ooze Crystal",
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
            "Pristine Animal Pelt",
            "Pristine Caladium",
            "Pristine Delphinium",
            "Pristine Laburnum",
            "Pristine Larkspur",
            "Pristine Muscimol",
            "Pristine Oleander",
            "Pristine Privet",
            "Pristine Silk",
            "Pure Diamond Trade Gem",
            "Pure Emerald Trade Gem",
            "Pure Sapphire Trade Gem",
            "Purified Grade AA Gormar Venom",
            "Purified Grade AA Nigriventer Venom",
            "Raw Amber Nihilite",
            "Raw Crimson Nihilite",
            "Raw Diamond",
            "Raw Faycite Crystal",
            "Raw Fine Runic Hide",
            "Raw Fine Supple Runic Hide",
            "Raw Hide",
            "Raw Indigo Nihilite",
            "Raw Infused Dark Matter",
            "Raw Runic Hide",
            "Raw Shimmering Nihilite",
            "Raw Supple Hide",
            "Raw Supple Runic Hide",
            "Rhenium Ore",
            "Rotting Fang",
            "Rough Animal Pelt",
            "Rubicite Ore",
            "Ruby Crown",
            "Ruby",
            "Rune Binding Powder",
            "Rune of Al`Kabor",
            "Rune of Ap`Sagor",
            "Rune of Concussion",
            "Rune of Crippling",
            "Rune of Frost",
            "Rune of Impetus",
            "Rune of Impulse",
            "Rune of Rathe",
            "Rune of the Astral",
            "Rune of the Helix",
            "Rusty Broad Sword",
            "Rusty Long Sword",
            "Rusty Short Sword",
            "Saltpeter",
            "Sample of Highland Sludge",
            "Sapphire Necklace",
            "Sapphire",
            "Scale Ore",
            "Shabby Fine Spell Scroll",
            "Shabby Rough Spell Scroll",
            "Shabby Vellum Parchment",
            "Shimmering Aligned Ore",
            "Sibilisan Viridian Pigment",
            "Skeleton Parts",
            "Smudged Rough Papyrus",
            "Smudged Rough Sortilege Sheet",
            "Smudged Runic Parchment",
            "Smudged Runic Sortilege Sheet",
            "Soluble Loam",
            "Sooty Fine Runic Papyrus",
            "Sooty Fine Sortilege Sheet",
            "Spectral Parchment",
            "Spider Legs",
            "Spider Silk",
            "Spider Venom Sac",
            "Spongy Loam",
            "Stained Fine Runic Sortilege Sheet",
            "Stained Fine Runic Spell Scroll",
            "Star Ruby Earring",
            "Star Ruby",
            "Staurolite",
            "Steel Ingot",
            "Stone of Tranquility",
            "Strand of Ether",
            "Sunshard Dust",
            "Sunshard Ore",
            "Sunshard Pebble",
            "Sunshard Powder",
            "Superb Animal Pelt",
            "Superb Marrow",
            "Superb Silk",
            "Superb Spinneret Fluid",
            "Taaffeite",
            "Tacky Silk",
            "Taelosian Mountain Tea Leaves",
            "Taelosian Mountain Wheat",
            "Tainted Caladium",
            "Tainted Delphinium",
            "Tainted Laburnum",
            "Tainted Larkspur",
            "Tainted Muscimol",
            "Tainted Oleander",
            "Tainted Privet",
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
            "Velium Laced Choresine Sample",
            "Velium Laced Gormar Venom",
            "Velium Laced Mamba Venom",
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
            "Zombie Skin",
        };

        private string zone;
        private string server;

        public event LootTrackerEvent OnLoot;

        public void HandleEvent(LogEvent e)
        {
            if (e is LogOpenEvent open)
            {
                server = open.Server;
            }

            if (e is LogZoneEvent z)
            {
                zone = z.Name;
            }

            if (e is LogLootEvent loot)
            {
                if (!Ignore.Contains(loot.Item) && zone != null && loot.Source != null)
                    OnLoot?.Invoke(new LootInfo { Item = loot.Item, Mob = loot.Source, Zone = zone, Date = e.Timestamp, Server = server });
            }

            if (e is LogRotEvent rot)
            {
                if (!Ignore.Contains(rot.Item) && zone != null && rot.Source != null)
                    OnLoot?.Invoke(new LootInfo { Item = rot.Item, Mob = rot.Source, Zone = zone, Date = e.Timestamp, Server = server });
            }

            //if (e is LogCraftEvent craft)
            //{
            //    OnLoot?.Invoke(new LootInfo { Item = craft.Item, Mob = "Crafted", Date = e.Timestamp });
            //}

        }

        /// <summary>
        /// Does the server follow standard loot rules?
        /// Beta server may have items that don't exist or haven't been named yet.
        /// Mischief, Thornblade, Teek servers use randomized loot.
        /// </summary>
        public static bool IsStandardServer(string server)
        {
            return server != null && server != "beta" && server != "mischief" && server != "thornblade" && server != "teek";
        }
    }
}
