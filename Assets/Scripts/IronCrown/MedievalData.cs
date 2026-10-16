using System;
using System.Collections.Generic;
using UnityEngine;

namespace IronCrown
{
    public enum LobbySize
    {
        Lobby25 = 25,
        Lobby50 = 50,
        Lobby100 = 100
    }

    public enum BotDifficulty
    {
        Squire = 0,
        Knight = 1,
        Warlord = 2
    }

    public enum ControlLayoutMode
    {
        Auto = 0,
        PCKeyboardMouse = 1,
        MobileTouch = 2
    }

    public enum MatchPhase
    {
        Menu = 0,
        AirshipDrop = 1,
        InProgress = 2,
        Victory = 3,
        Defeat = 4
    }

    public enum DropState
    {
        AboardAirship = 0,
        Freefall = 1,
        Gliding = 2,
        Grounded = 3
    }

    public enum ItemSlotType
    {
        Melee = 0,
        Ranged = 1,
        Throwable = 2,
        Alchemy = 3,
        ArmorUpgrade = 4,
        ArrowBundle = 5
    }

    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Legendary = 3
    }

    public enum WeaponId
    {
        None = 0,
        // Slot 1: Melee
        OakenClub = 1,
        ArmingSword = 2,
        RoyalHalberd = 3,
        SunforgedGreatsword = 4,
        // Slot 2: Ranged
        HuntingShortbow = 10,
        WarLongbow = 11,
        HeavyArbalest = 12,
        DwarvenRepeater = 13,
        // Slot 3: Throwable
        FranciscaAxes = 20,
        AlchemicalFireFlask = 21,
        // Slot 4: Alchemy / Healing
        LinenBandage = 30,
        ApothecaryVial = 31,
        RoyalElixir = 32,
        // Ground-only pickups
        BoiledLeatherArmor = 40,
        ChainmailHauberk = 41,
        GildedPlateArmor = 42,
        BodkinArrowQuiver = 50
    }

    [Serializable]
    public class WeaponSpec
    {
        public WeaponId id;
        public string displayName;
        public string shortCode;
        public ItemSlotType slotType;
        public ItemRarity rarity;
        public float baseDamage;
        public float attackCooldown;
        public float rangeOrVelocity;
        public float projectileGravity;
        public float cleaveAngle;
        public int armorTierValue;
        public int healAmount;
        public int armorRestoreAmount;
        public int defaultCount;
        public Color themeColor;
        public string description;
    }

    [Serializable]
    public struct LandmarkPOI
    {
        public string name;
        public string subtitle;
        public Vector2 worldXZ;
        public float radius;
        public int tier; // 1 = Village, 2 = Outpost/Abbey, 3 = Major Citadel
    }

    [Serializable]
    public struct KillFeedEntry
    {
        public string killerName;
        public string victimName;
        public string weaponName;
        public float distance;
        public float timestamp;
        public bool killerIsPlayer;
        public bool victimIsPlayer;
    }

    [Serializable]
    public struct FloatingDamageText
    {
        public Vector3 worldPos;
        public int amount;
        public bool isHeadshot;
        public bool isArmorHit;
        public float spawnTime;
    }

    public static class MedievalCatalog
    {
        public const float IslandHalfExtent = 580f; // 1160m x 1160m playable island
        public const float WaterLevel = 4.5f;

        private static Dictionary<WeaponId, WeaponSpec> _specs;

        public static readonly LandmarkPOI[] Landmarks = new LandmarkPOI[]
        {
            new LandmarkPOI
            {
                name = "HIGHKEEP CITADEL",
                subtitle = "Royal Bastion & Great Keep",
                worldXZ = new Vector2(0f, 95f),
                radius = 95f,
                tier = 3
            },
            new LandmarkPOI
            {
                name = "OAKHAVEN HAMLET",
                subtitle = "Millers' Village & Granary",
                worldXZ = new Vector2(-235f, 35f),
                radius = 78f,
                tier = 2
            },
            new LandmarkPOI
            {
                name = "IRONFORGE WATCH",
                subtitle = "Eastern Dwarven Garrison",
                worldXZ = new Vector2(245f, 80f),
                radius = 80f,
                tier = 2
            },
            new LandmarkPOI
            {
                name = "RAVENMIRE ABBEY",
                subtitle = "Sunken Cloister & Crypts",
                worldXZ = new Vector2(-190f, -210f),
                radius = 72f,
                tier = 2
            },
            new LandmarkPOI
            {
                name = "KINGSPORT DOCKS",
                subtitle = "Southern Siege Harbor",
                worldXZ = new Vector2(65f, -275f),
                radius = 85f,
                tier = 2
            },
            new LandmarkPOI
            {
                name = "SUNKEN STONES",
                subtitle = "Elder Runestone Ring",
                worldXZ = new Vector2(-210f, 255f),
                radius = 65f,
                tier = 1
            }
        };

        public static readonly string[] BotNames = new string[]
        {
            "Sir Vance", "Baron Kael", "Lady Isolde", "Lord Godfrey", "Rowan the Bold",
            "Aldric Ironhand", "Freya Bloodoak", "Garrick Vane", "Cedric of Ash", "Thane Ulric",
            "Dame Brienne", "Oswin Fletcher", "Brother Malakor", "Halvar Storm", "Seraphina",
            "Beric Thorn", "Viktor Black", "Elowen Swift", "Korgan Pike", "Tormund Graves",
            "Cassian Rook", "Leoric Crown", "Wulfric Frost", "Alaric Cross", "Maeve Shadow",
            "Dorian Lance", "Gideon Wall", "Ragnar Red", "Soren Hound", "Valen Creed"
        };

        public static readonly Color[] HeraldicColors = new Color[]
        {
            new Color(0.68f, 0.18f, 0.18f), // Crimson
            new Color(0.18f, 0.36f, 0.62f), // Royal Cobalt
            new Color(0.22f, 0.48f, 0.26f), // Forest Warden
            new Color(0.76f, 0.56f, 0.18f), // Golden Lion
            new Color(0.45f, 0.28f, 0.16f), // Boar Umber
            new Color(0.28f, 0.45f, 0.48f), // Teal Wyvern
            new Color(0.58f, 0.32f, 0.20f), // Rust Forge
            new Color(0.32f, 0.32f, 0.36f)  // Iron Wolf
        };

        public static void EnsureInitialized()
        {
            if (_specs != null) return;
            _specs = new Dictionary<WeaponId, WeaponSpec>();

            // MELEE WEAPONS (SLOT 0)
            Register(new WeaponSpec
            {
                id = WeaponId.OakenClub,
                displayName = "Oaken Cudgel",
                shortCode = "CLUB",
                slotType = ItemSlotType.Melee,
                rarity = ItemRarity.Common,
                baseDamage = 24f,
                attackCooldown = 0.52f,
                rangeOrVelocity = 3.1f,
                cleaveAngle = 65f,
                themeColor = new Color(0.55f, 0.42f, 0.28f),
                description = "Crude hardwood bludgeon for close scraps."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.ArmingSword,
                displayName = "Knight's Arming Sword",
                shortCode = "SWORD",
                slotType = ItemSlotType.Melee,
                rarity = ItemRarity.Uncommon,
                baseDamage = 38f,
                attackCooldown = 0.56f,
                rangeOrVelocity = 3.6f,
                cleaveAngle = 80f,
                themeColor = new Color(0.30f, 0.65f, 0.36f),
                description = "Tempered steel blade paired with a heater shield."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.RoyalHalberd,
                displayName = "Royal Guard Halberd",
                shortCode = "HALBERD",
                slotType = ItemSlotType.Melee,
                rarity = ItemRarity.Rare,
                baseDamage = 54f,
                attackCooldown = 0.78f,
                rangeOrVelocity = 4.6f,
                cleaveAngle = 95f,
                themeColor = new Color(0.25f, 0.55f, 0.85f),
                description = "Long-reach poleaxe that cleaves through shields."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.SunforgedGreatsword,
                displayName = "Sunforged Greatsword",
                shortCode = "CLAYMORE",
                slotType = ItemSlotType.Melee,
                rarity = ItemRarity.Legendary,
                baseDamage = 74f,
                attackCooldown = 0.82f,
                rangeOrVelocity = 4.4f,
                cleaveAngle = 115f,
                themeColor = new Color(0.88f, 0.66f, 0.20f),
                description = "Relic claymore of Highkeep. Devastating sweeping arcs."
            });

            // RANGED WEAPONS (SLOT 1)
            Register(new WeaponSpec
            {
                id = WeaponId.HuntingShortbow,
                displayName = "Yew Hunting Bow",
                shortCode = "SHORTBOW",
                slotType = ItemSlotType.Ranged,
                rarity = ItemRarity.Common,
                baseDamage = 34f,
                attackCooldown = 0.72f,
                rangeOrVelocity = 56f,
                projectileGravity = 14.5f,
                themeColor = new Color(0.65f, 0.58f, 0.45f),
                description = "Nimble shortbow for skirmishing on the move."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.WarLongbow,
                displayName = "English War Longbow",
                shortCode = "LONGBOW",
                slotType = ItemSlotType.Ranged,
                rarity = ItemRarity.Rare,
                baseDamage = 56f,
                attackCooldown = 0.98f,
                rangeOrVelocity = 78f,
                projectileGravity = 11.5f,
                themeColor = new Color(0.28f, 0.58f, 0.86f),
                description = "Heavy draw yew longbow. Lethal precision at long range."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.HeavyArbalest,
                displayName = "Siege Arbalest Crossbow",
                shortCode = "ARBALEST",
                slotType = ItemSlotType.Ranged,
                rarity = ItemRarity.Rare,
                baseDamage = 72f,
                attackCooldown = 1.45f,
                rangeOrVelocity = 92f,
                projectileGravity = 8.5f,
                themeColor = new Color(0.32f, 0.62f, 0.88f),
                description = "Steel-prodded crossbow that punches through plate armor."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.DwarvenRepeater,
                displayName = "Dwarven Chu-Ko-Nu Repeater",
                shortCode = "REPEATER",
                slotType = ItemSlotType.Ranged,
                rarity = ItemRarity.Legendary,
                baseDamage = 36f,
                attackCooldown = 0.30f,
                rangeOrVelocity = 72f,
                projectileGravity = 11.0f,
                themeColor = new Color(0.90f, 0.68f, 0.22f),
                description = "Lever-fed magazine crossbow firing rapid volleys of bolts."
            });

            // THROWABLES / TACTICAL (SLOT 2)
            Register(new WeaponSpec
            {
                id = WeaponId.FranciscaAxes,
                displayName = "Francisca Throwing Axes",
                shortCode = "THROW AXE",
                slotType = ItemSlotType.Throwable,
                rarity = ItemRarity.Uncommon,
                baseDamage = 58f,
                attackCooldown = 0.85f,
                rangeOrVelocity = 42f,
                projectileGravity = 18f,
                defaultCount = 3,
                themeColor = new Color(0.35f, 0.68f, 0.40f),
                description = "Heavy balanced iron axes that shatter enemy guards."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.AlchemicalFireFlask,
                displayName = "Wildfire Alchemical Flask",
                shortCode = "WILDFIRE",
                slotType = ItemSlotType.Throwable,
                rarity = ItemRarity.Rare,
                baseDamage = 28f,
                attackCooldown = 1.15f,
                rangeOrVelocity = 34f,
                projectileGravity = 19f,
                defaultCount = 2,
                themeColor = new Color(0.88f, 0.42f, 0.16f),
                description = "Shatters on impact, igniting a 6m pool of Greek wildfire."
            });

            // ALCHEMY / HEALING (SLOT 3)
            Register(new WeaponSpec
            {
                id = WeaponId.LinenBandage,
                displayName = "Herbal Linen Poultice",
                shortCode = "POULTICE",
                slotType = ItemSlotType.Alchemy,
                rarity = ItemRarity.Common,
                attackCooldown = 1.0f,
                healAmount = 30,
                defaultCount = 3,
                themeColor = new Color(0.62f, 0.66f, 0.56f),
                description = "Field dressing that restores +30 Vitality."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.ApothecaryVial,
                displayName = "Crimson Apothecary Draught",
                shortCode = "DRAUGHT",
                slotType = ItemSlotType.Alchemy,
                rarity = ItemRarity.Uncommon,
                attackCooldown = 1.2f,
                healAmount = 65,
                 armorRestoreAmount = 15,
                defaultCount = 2,
                themeColor = new Color(0.78f, 0.22f, 0.24f),
                description = "Alchemical tonic restoring +65 Vitality & +15 Armor."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.RoyalElixir,
                displayName = "Royal Elixir of Ironblood",
                shortCode = "ELIXIR",
                slotType = ItemSlotType.Alchemy,
                rarity = ItemRarity.Legendary,
                attackCooldown = 1.3f,
                healAmount = 100,
                armorRestoreAmount = 75,
                defaultCount = 1,
                themeColor = new Color(0.90f, 0.68f, 0.22f),
                description = "Restores full Vitality (+100) and reinforces +75 Armor."
            });

            // ARMOR UPGRADES (AUTO EQUIP)
            Register(new WeaponSpec
            {
                id = WeaponId.BoiledLeatherArmor,
                displayName = "Boiled Leather Cuirass (Tier I)",
                shortCode = "LEATHER I",
                slotType = ItemSlotType.ArmorUpgrade,
                rarity = ItemRarity.Common,
                armorTierValue = 1,
                themeColor = new Color(0.55f, 0.38f, 0.22f),
                description = "Grants 50 Max Armor & 22% Damage Reduction."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.ChainmailHauberk,
                displayName = "Riveted Chainmail Hauberk (Tier II)",
                shortCode = "CHAIN II",
                slotType = ItemSlotType.ArmorUpgrade,
                rarity = ItemRarity.Rare,
                armorTierValue = 2,
                themeColor = new Color(0.32f, 0.60f, 0.85f),
                description = "Grants 100 Max Armor & 38% Damage Reduction."
            });
            Register(new WeaponSpec
            {
                id = WeaponId.GildedPlateArmor,
                displayName = "Gilded Full Plate Harness (Tier III)",
                shortCode = "PLATE III",
                slotType = ItemSlotType.ArmorUpgrade,
                rarity = ItemRarity.Legendary,
                armorTierValue = 3,
                themeColor = new Color(0.90f, 0.68f, 0.22f),
                description = "Grants 150 Max Armor & 52% Damage Reduction."
            });

            // AMMO
            Register(new WeaponSpec
            {
                id = WeaponId.BodkinArrowQuiver,
                displayName = "Sheaf of Bodkin Arrows (x18)",
                shortCode = "ARROWS",
                slotType = ItemSlotType.ArrowBundle,
                rarity = ItemRarity.Common,
                defaultCount = 18,
                themeColor = new Color(0.75f, 0.68f, 0.52f),
                description = "Broadhead & bodkin shafts for all bows and crossbows."
            });
        }

        private static void Register(WeaponSpec spec)
        {
            _specs[spec.id] = spec;
        }

        public static WeaponSpec Get(WeaponId id)
        {
            EnsureInitialized();
            if (_specs.TryGetValue(id, out var s)) return s;
            return _specs[WeaponId.OakenClub];
        }

        public static int GetMaxArmorForTier(int tier)
        {
            switch (tier)
            {
                case 1: return 50;
                case 2: return 100;
                case 3: return 150;
                default: return 0;
            }
        }

        public static float GetDamageReductionForTier(int tier)
        {
            switch (tier)
            {
                case 1: return 0.22f;
                case 2: return 0.38f;
                case 3: return 0.52f;
                default: return 0.0f;
            }
        }

        public static string GetArmorTierName(int tier)
        {
            switch (tier)
            {
                case 1: return "Tier I Leather";
                case 2: return "Tier II Chainmail";
                case 3: return "Tier III Gilded Plate";
                default: return "Peasant Tunic";
            }
        }

        public static Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return new Color(0.32f, 0.68f, 0.38f);
                case ItemRarity.Rare: return new Color(0.28f, 0.58f, 0.88f);
                case ItemRarity.Legendary: return new Color(0.90f, 0.68f, 0.20f);
                default: return new Color(0.75f, 0.70f, 0.62f);
            }
        }
    }
}
