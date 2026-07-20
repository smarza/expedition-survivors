using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum ItemCategory : byte
    {
        Weapon,
        Gear,
        Boon,
        Evolution
    }

    public sealed class ItemDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string ShortName;
        public readonly string Description;
        public readonly ItemCategory Category;
        public readonly int MaxLevel;
        public readonly Color Color;
        public readonly UpgradeId[] LevelEffects;
        public readonly string EvolutionOf;
        public readonly string CatalystId;

        public bool IsEvolution => Category == ItemCategory.Evolution;

        public ItemDefinition(
            string id, string name, string shortName, string description, ItemCategory category,
            int maxLevel, Color color, UpgradeId[] levelEffects, string evolutionOf = null, string catalystId = null)
        {
            Id = id;
            Name = name;
            ShortName = shortName;
            Description = description;
            Category = category;
            MaxLevel = maxLevel;
            Color = color;
            LevelEffects = levelEffects ?? new UpgradeId[0];
            EvolutionOf = evolutionOf;
            CatalystId = catalystId;
        }

        public UpgradeId EffectAtLevel(int level)
        {
            var index = level - 1;
            return index >= 0 && index < LevelEffects.Length ? LevelEffects[index] : UpgradeId.None;
        }

        public string EffectDescriptionAtLevel(int level)
        {
            var catalystDescription = UpgradeDescriptions.ForEvolutionCatalyst(Id);
            if (catalystDescription != null)
            {
                return catalystDescription;
            }

            return UpgradeDescriptions.For(EffectAtLevel(level), Id);
        }
    }

    public static class UpgradeDescriptions
    {
        public static string For(UpgradeId id, string itemId = null)
        {
            var weaponLabel = ResolveWeaponLabel(itemId);
            var pulseLabel = ResolvePulseLabel(itemId);
            var orbitLabel = ResolveOrbitLabel(itemId);
            var radialLabel = ResolveRadialLabel(itemId);

            switch (id)
            {
                case UpgradeId.AxeDamage:
                    return weaponLabel != null
                        ? $"+26% {weaponLabel} damage"
                        : "+26% Frost Axe damage";
                case UpgradeId.AxeSpeed:
                    return weaponLabel != null
                        ? $"-14% {weaponLabel} interval"
                        : "-14% Frost Axe interval";
                case UpgradeId.ExtraAxe:
                    return weaponLabel != null
                        ? $"+1 projectile per {weaponLabel} volley"
                        : "+1 projectile per Frost Axe volley";
                case UpgradeId.AxePierce:
                    return weaponLabel != null
                        ? $"+1 {weaponLabel} pierce"
                        : "+1 Frost Axe pierce";
                case UpgradeId.MoveSpeed: return "+0.46 movement speed";
                case UpgradeId.MaxHealth: return "+24 maximum and current health";
                case UpgradeId.Armor: return "+1 armor";
                case UpgradeId.Magnet: return "+0.55 XP pickup radius";
                case UpgradeId.ShieldPulse:
                    return pulseLabel != null
                        ? $"Activate {pulseLabel}"
                        : "Activate Raven Guard";
                case UpgradeId.ShieldDamage:
                    return pulseLabel != null
                        ? $"+42% {pulseLabel} damage"
                        : "+42% Raven Guard damage";
                case UpgradeId.CriticalRunes:
                    return weaponLabel != null
                        ? $"+9 percentage points {weaponLabel} critical chance"
                        : "+9 percentage points critical chance";
                case UpgradeId.UltimateCooldown: return "-10% base Ultimate cooldown";
                case UpgradeId.UltimateDamage: return "+30% Ultimate damage and derived area";
                case UpgradeId.Heal: return "Restore 24 health immediately";
                case UpgradeId.ShieldDamageAndSpeed:
                    return pulseLabel != null
                        ? $"+42% {pulseLabel} damage and -14% interval"
                        : "+42% Raven Guard damage and -14% interval";
                case UpgradeId.OrbitDamage:
                    return orbitLabel != null
                        ? $"+30% {orbitLabel} blade damage"
                        : "+30% orbit blade damage";
                case UpgradeId.OrbitSpeed:
                    return orbitLabel != null
                        ? $"+20% {orbitLabel} speed and -14% interval"
                        : "+20% orbit speed and -14% interval";
                case UpgradeId.ExtraOrbit:
                    return orbitLabel != null
                        ? $"+1 {orbitLabel} blade"
                        : "+1 orbit blade";
                case UpgradeId.RadialDamage:
                    return radialLabel != null
                        ? $"+28% {radialLabel} burst damage"
                        : "+28% radial burst damage";
                case UpgradeId.RadialSpeed:
                    return radialLabel != null
                        ? $"-14% {radialLabel} interval"
                        : "-14% radial burst interval";
                case UpgradeId.ExtraRadial:
                    return radialLabel != null
                        ? $"+1 projectile per {radialLabel} burst"
                        : "+1 projectile per radial burst";
                default: return "Base item level; no additional modifier";
            }
        }

        public static string ForEvolutionCatalyst(string itemId)
        {
            switch (itemId)
            {
                case "gear.jotunn_rune":
                    return "+1 Frost Axe pierce";
                case "gear.flare_core":
                    return "+1 Signal Flare projectile per volley";
                case "gear.grove_seed":
                    return "+1 Canopy Vortex burst projectile";
                case "gear.oath_band":
                    return "+1 Oath Ring blade";
                default:
                    return null;
            }
        }

        private static string ResolveWeaponLabel(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            switch (itemId)
            {
                case "weapon.frost_axe": return "Frost Axe";
                case "weapon.north_wind_spear": return "North Wind Spear";
                case "weapon.rune_bolt": return "Rune Bolt";
                case "weapon.signal_flare": return "Signal Flare";
                case "weapon.tide_caller": return "Tide Caller";
                default: return null;
            }
        }

        private static string ResolvePulseLabel(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            switch (itemId)
            {
                case "weapon.raven_guard": return "Raven Guard";
                case "weapon.grove_thorn_lash": return "Grove Thorn Lash";
                case "weapon.supply_pulse": return "Supply Pulse";
                case "weapon.iron_beacon": return "Iron Beacon";
                default: return null;
            }
        }

        private static string ResolveOrbitLabel(string itemId)
        {
            if (itemId == "weapon.oath_ring")
            {
                return "Oath Ring";
            }

            return null;
        }

        private static string ResolveRadialLabel(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            switch (itemId)
            {
                case "weapon.canopy_vortex": return "Canopy Vortex";
                case "weapon.driftwood_staff": return "Driftwood Staff";
                default: return null;
            }
        }

        public static string Progression(ItemDefinition item)
        {
            if (item == null || item.LevelEffects == null || item.LevelEffects.Length == 0)
                return string.Empty;
            var parts = new string[item.LevelEffects.Length];
            for (var level = 1; level <= item.LevelEffects.Length; level++)
                parts[level - 1] = $"L{level}: {item.EffectDescriptionAtLevel(level)}";
            return string.Join("  •  ", parts);
        }
    }

    public sealed class ItemState
    {
        public string ItemId;
        public int Level;
        public string EvolutionId;
        public bool IsEvolved => !string.IsNullOrEmpty(EvolutionId);
    }

    public sealed class BuildApplyResult
    {
        public ItemDefinition Item;
        public int NewLevel;
        public bool Evolution;
    }

    public sealed class PlayerBuild
    {
        private readonly List<ItemState> _items = new List<ItemState>(14);
        public IReadOnlyList<ItemState> Items => _items;
        public int WeaponSlots { get; private set; }
        public int GearSlots { get; private set; }

        public void Initialize(int weaponSlots, int gearSlots, string[] starterWeaponIds = null)
        {
            WeaponSlots = Mathf.Max(1, weaponSlots);
            GearSlots = Mathf.Max(1, gearSlots);
            _items.Clear();

            if (starterWeaponIds == null || starterWeaponIds.Length == 0)
            {
                AddStarter(ItemCatalog.FrostAxe.Id);
                AddStarter(ItemCatalog.RavenGuard.Id);
                return;
            }

            for (var i = 0; i < starterWeaponIds.Length; i++)
            {
                if (string.IsNullOrEmpty(starterWeaponIds[i]))
                {
                    continue;
                }

                AddStarter(starterWeaponIds[i]);
            }
        }

        private void AddStarter(string itemId) => _items.Add(new ItemState { ItemId = itemId, Level = 1 });

        public ItemState Find(string itemId)
        {
            for (var i = 0; i < _items.Count; i++)
                if (_items[i].ItemId == itemId) return _items[i];
            return null;
        }

        public int CountCategory(ItemCategory category)
        {
            var count = 0;
            for (var i = 0; i < _items.Count; i++)
            {
                var definition = ItemCatalog.Find(_items[i].ItemId);
                if (definition != null && definition.Category == category) count++;
            }
            return count;
        }

        public bool CanAcquire(ItemDefinition item)
        {
            if (item == null) return false;
            if (item.IsEvolution) return CanEvolve(item);
            var existing = Find(item.Id);
            if (existing != null) return item.Category == ItemCategory.Boon || existing.Level < item.MaxLevel;
            if (item.Category == ItemCategory.Weapon) return CountCategory(ItemCategory.Weapon) < WeaponSlots;
            if (item.Category == ItemCategory.Gear) return CountCategory(ItemCategory.Gear) < GearSlots;
            return true;
        }

        public bool CanEvolve(ItemDefinition evolution)
        {
            if (evolution == null || !evolution.IsEvolution) return false;
            var baseItem = Find(evolution.EvolutionOf);
            var baseDefinition = ItemCatalog.Find(evolution.EvolutionOf);
            return baseItem != null && baseDefinition != null && baseItem.Level >= baseDefinition.MaxLevel &&
                   !baseItem.IsEvolved && Find(evolution.CatalystId) != null;
        }

        public BuildApplyResult Acquire(ItemDefinition item)
        {
            if (!CanAcquire(item)) return null;
            if (item.IsEvolution)
            {
                var baseItem = Find(item.EvolutionOf);
                baseItem.EvolutionId = item.Id;
                return new BuildApplyResult { Item = item, NewLevel = baseItem.Level, Evolution = true };
            }
            var state = Find(item.Id);
            if (state == null)
            {
                state = new ItemState { ItemId = item.Id, Level = 1 };
                _items.Add(state);
            }
            else if (item.Category != ItemCategory.Boon)
            {
                state.Level = Mathf.Min(item.MaxLevel, state.Level + 1);
            }
            return new BuildApplyResult { Item = item, NewLevel = state.Level, Evolution = false };
        }

        public string NextLabel(ItemDefinition item)
        {
            if (item == null) return string.Empty;
            if (item.IsEvolution) return "EVOLUTION";
            var state = Find(item.Id);
            if (state == null) return item.Category == ItemCategory.Boon ? "INSTANT" : "NEW ITEM";
            return item.Category == ItemCategory.Boon ? "INSTANT" : $"LEVEL {Mathf.Min(item.MaxLevel, state.Level + 1)}";
        }

        public void LoadSnapshot(int weaponSlots, int gearSlots, List<ItemState> states)
        {
            WeaponSlots = weaponSlots;
            GearSlots = gearSlots;
            _items.Clear();
            for (var i = 0; i < states.Count; i++)
                _items.Add(new ItemState { ItemId = states[i].ItemId, Level = states[i].Level, EvolutionId = states[i].EvolutionId });
        }
    }

    public sealed class RewardOption
    {
        public ItemDefinition Item;
        public int TargetPlayerIndex;
        public bool Shared;
    }

    public static class ItemCatalog
    {
        private static UpgradeId[] Effects(params UpgradeId[] effects) => effects;

        public static ItemDefinition FrostAxe { get; private set; } = new ItemDefinition(
            "weapon.frost_axe", "Frost Axe", "AXE", "Automatic rune axe. Each level grants the listed damage, interval, pierce or projectile modifier.",
            ItemCategory.Weapon, 8, new Color(0.35f, 0.88f, 1f), Effects(
                UpgradeId.None, UpgradeId.AxeDamage, UpgradeId.AxeSpeed, UpgradeId.AxePierce,
                UpgradeId.AxeDamage, UpgradeId.ExtraAxe, UpgradeId.AxeSpeed, UpgradeId.AxeDamage));

        public static ItemDefinition RavenGuard { get; private set; } = new ItemDefinition(
            "weapon.raven_guard", "Raven Guard", "GUARD", "Automatic defensive shockwave. Each level grants the listed damage, armor or interval modifier.",
            ItemCategory.Weapon, 8, new Color(0.55f, 0.72f, 0.9f), Effects(
                UpgradeId.None, UpgradeId.ShieldDamage, UpgradeId.Armor, UpgradeId.ShieldDamage,
                UpgradeId.ShieldDamageAndSpeed, UpgradeId.Armor, UpgradeId.ShieldDamage,
                UpgradeId.ShieldDamageAndSpeed));

        public static ItemDefinition NorthWindSpear { get; private set; } = new ItemDefinition(
            "weapon.north_wind_spear", "North Wind Spear", "SPEAR", "Directed spear projectile. Each level grants the listed damage, interval or pierce modifier.",
            ItemCategory.Weapon, 8, new Color(0.75f, 0.88f, 0.95f), Effects(
                UpgradeId.None, UpgradeId.AxeDamage, UpgradeId.AxeSpeed, UpgradeId.AxePierce,
                UpgradeId.AxeDamage, UpgradeId.AxePierce, UpgradeId.AxeSpeed, UpgradeId.AxeDamage));

        public static ItemDefinition RuneBolt { get; private set; } = new ItemDefinition(
            "weapon.rune_bolt", "Rune Bolt", "BOLT", "Fast rune projectile. Each level grants the listed damage, interval or projectile modifier.",
            ItemCategory.Weapon, 8, new Color(0.65f, 0.45f, 0.92f), Effects(
                UpgradeId.None, UpgradeId.AxeSpeed, UpgradeId.AxeDamage, UpgradeId.AxeSpeed,
                UpgradeId.ExtraAxe, UpgradeId.AxeDamage, UpgradeId.AxeSpeed, UpgradeId.AxeDamage));

        public static ItemDefinition OathRing { get; private set; } = new ItemDefinition(
            "weapon.oath_ring", "Oath Ring", "RING", "Orbiting oath blades. Each level grants the listed damage, speed or blade modifier.",
            ItemCategory.Weapon, 8, new Color(0.55f, 0.82f, 0.45f), Effects(
                UpgradeId.None, UpgradeId.OrbitDamage, UpgradeId.OrbitSpeed, UpgradeId.OrbitDamage,
                UpgradeId.ExtraOrbit, UpgradeId.OrbitDamage, UpgradeId.OrbitSpeed, UpgradeId.ExtraOrbit));

        public static ItemDefinition GroveThornLash { get; private set; } = new ItemDefinition(
            "weapon.grove_thorn_lash", "Grove Thorn Lash", "LASH", "Fast thorn pulse centered on its owner. Each level grants the listed damage or interval modifier.",
            ItemCategory.Weapon, 8, new Color(0.42f, 0.78f, 0.38f), Effects(
                UpgradeId.None, UpgradeId.ShieldDamage, UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldDamage,
                UpgradeId.ShieldDamage, UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldDamage,
                UpgradeId.ShieldDamageAndSpeed));

        public static ItemDefinition CanopyVortex { get; private set; } = new ItemDefinition(
            "weapon.canopy_vortex", "Canopy Vortex", "VORTEX", "Radial canopy burst. Each level grants the listed damage, interval or projectile modifier.",
            ItemCategory.Weapon, 8, new Color(0.38f, 0.85f, 0.72f), Effects(
                UpgradeId.None, UpgradeId.RadialDamage, UpgradeId.RadialSpeed, UpgradeId.RadialDamage,
                UpgradeId.ExtraRadial, UpgradeId.RadialDamage, UpgradeId.RadialSpeed, UpgradeId.ExtraRadial));

        public static ItemDefinition DriftwoodStaff { get; private set; } = new ItemDefinition(
            "weapon.driftwood_staff", "Driftwood Staff", "STAFF", "Slow radial driftwood burst. Each level grants the listed damage, interval or projectile modifier.",
            ItemCategory.Weapon, 8, new Color(0.55f, 0.48f, 0.35f), Effects(
                UpgradeId.None, UpgradeId.RadialDamage, UpgradeId.RadialDamage, UpgradeId.ExtraRadial,
                UpgradeId.RadialDamage, UpgradeId.RadialSpeed, UpgradeId.RadialDamage, UpgradeId.ExtraRadial));

        public static ItemDefinition SignalFlare { get; private set; } = new ItemDefinition(
            "weapon.signal_flare", "Signal Flare", "FLARE", "Explosive signal projectile. Each level grants the listed damage, interval or projectile modifier.",
            ItemCategory.Weapon, 8, new Color(0.95f, 0.55f, 0.22f), Effects(
                UpgradeId.None, UpgradeId.AxeDamage, UpgradeId.AxeDamage, UpgradeId.AxeSpeed,
                UpgradeId.AxeDamage, UpgradeId.ExtraAxe, UpgradeId.AxeSpeed, UpgradeId.AxeDamage));

        public static ItemDefinition SupplyPulse { get; private set; } = new ItemDefinition(
            "weapon.supply_pulse", "Supply Pulse", "SUPPLY", "Periodic heal pulse centered on its owner. Each level grants the listed interval or potency modifier.",
            ItemCategory.Weapon, 8, new Color(0.48f, 0.82f, 0.55f), Effects(
                UpgradeId.None, UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldPulse,
                UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldDamageAndSpeed,
                UpgradeId.ShieldDamageAndSpeed));

        public static ItemDefinition IronBeacon { get; private set; } = new ItemDefinition(
            "weapon.iron_beacon", "Iron Beacon", "BEACON", "Large defensive pulse centered on its owner. Each level grants the listed damage, armor or interval modifier.",
            ItemCategory.Weapon, 8, new Color(0.58f, 0.65f, 0.78f), Effects(
                UpgradeId.None, UpgradeId.ShieldDamage, UpgradeId.ShieldDamage, UpgradeId.ShieldDamageAndSpeed,
                UpgradeId.ShieldDamage, UpgradeId.Armor, UpgradeId.ShieldDamageAndSpeed, UpgradeId.ShieldDamage));

        public static ItemDefinition TideCaller { get; private set; } = new ItemDefinition(
            "weapon.tide_caller", "Tide Caller", "TIDE", "Wide shore projectile volley. Each level grants the listed damage, interval or projectile modifier.",
            ItemCategory.Weapon, 8, new Color(0.35f, 0.72f, 0.88f), Effects(
                UpgradeId.None, UpgradeId.AxeDamage, UpgradeId.ExtraAxe, UpgradeId.AxeSpeed,
                UpgradeId.AxeDamage, UpgradeId.ExtraAxe, UpgradeId.AxeSpeed, UpgradeId.AxeDamage));

        public static ItemDefinition LongshipBoots { get; private set; } = Gear(
            "gear.longship_boots", "Longship Boots", "BOOTS", "Increases movement speed.", 5,
            new Color(0.4f, 0.84f, 0.62f), UpgradeId.MoveSpeed);

        public static ItemDefinition BearBlooded { get; private set; } = Gear(
            "gear.bear_blooded", "Bear-Blooded", "BEAR", "Increases maximum health and heals the same amount.", 5,
            new Color(0.82f, 0.32f, 0.3f), UpgradeId.MaxHealth);

        public static ItemDefinition RavenArmor { get; private set; } = Gear(
            "gear.raven_armor", "Raven Armor", "ARMOR", "Reduces every contact hit.", 5,
            new Color(0.62f, 0.68f, 0.76f), UpgradeId.Armor);

        public static ItemDefinition SagaCarver { get; private set; } = Gear(
            "gear.saga_carver", "Saga Carver", "CRIT", "Increases the chance of golden critical axes.", 5,
            new Color(1f, 0.64f, 0.2f), UpgradeId.CriticalRunes);

        public static ItemDefinition RavenHourglass { get; private set; } = Gear(
            "gear.raven_hourglass", "Raven Hourglass", "HOUR", "Ultimate recharges 10% faster per level.", 5,
            new Color(0.62f, 0.42f, 0.92f), UpgradeId.UltimateCooldown);

        public static ItemDefinition FinalVerse { get; private set; } = Gear(
            "gear.final_verse", "Final Verse", "ULT", "Increases Ultimate damage and impact area.", 5,
            new Color(0.95f, 0.45f, 0.25f), UpgradeId.UltimateDamage);

        public static ItemDefinition WindsweptCloak { get; private set; } = Gear(
            "gear.windswept_cloak", "Windswept Cloak", "CLOAK", "Increases movement speed.", 5,
            new Color(0.62f, 0.72f, 0.82f), UpgradeId.MoveSpeed);

        public static ItemDefinition HollowGourds { get; private set; } = Gear(
            "gear.hollow_gourds", "Hollow Gourds", "GOURD", "Increases maximum health and heals the same amount.", 5,
            new Color(0.78f, 0.62f, 0.28f), UpgradeId.MaxHealth);

        public static ItemDefinition OathFeather { get; private set; } = Gear(
            "gear.oath_feather", "Oath Feather", "FEATHER", "Reduces every contact hit.", 5,
            new Color(0.82f, 0.92f, 0.78f), UpgradeId.Armor);

        public static ItemDefinition SignalMagnet { get; private set; } = Gear(
            "gear.signal_magnet", "Signal Magnet", "MAGNET", "Increases XP pickup radius.", 5,
            new Color(0.92f, 0.82f, 0.35f), UpgradeId.Magnet);

        public static ItemDefinition FieldManual { get; private set; } = Gear(
            "gear.field_manual", "Field Manual", "MANUAL", "Ultimate recharges 10% faster per level.", 5,
            new Color(0.72f, 0.65f, 0.48f), UpgradeId.UltimateCooldown);

        public static ItemDefinition JotunnRune { get; private set; } = new ItemDefinition(
            "gear.jotunn_rune", "Jotunn Rune", "RUNE", "A rare catalyst that prepares Frost Axe for evolution.",
            ItemCategory.Gear, 1, new Color(0.75f, 0.35f, 0.85f), Effects(UpgradeId.AxePierce));

        public static ItemDefinition GroveSeed { get; private set; } = new ItemDefinition(
            "gear.grove_seed", "Grove Seed", "SEED", "A rare catalyst that prepares Grove Thorn Lash for evolution.",
            ItemCategory.Gear, 1, new Color(0.48f, 0.68f, 0.32f), Effects(UpgradeId.ExtraRadial));

        public static ItemDefinition FlareCore { get; private set; } = new ItemDefinition(
            "gear.flare_core", "Flare Core", "CORE", "A rare catalyst that prepares Signal Flare for evolution.",
            ItemCategory.Gear, 1, new Color(0.98f, 0.62f, 0.18f), Effects(UpgradeId.ExtraAxe));

        public static ItemDefinition OathBand { get; private set; } = new ItemDefinition(
            "gear.oath_band", "Oath Band", "BAND", "A rare catalyst that prepares Oath Ring for evolution.",
            ItemCategory.Gear, 1, new Color(0.55f, 0.75f, 0.62f), Effects(UpgradeId.ExtraOrbit));

        public static ItemDefinition JotunnCleaver { get; private set; } = new ItemDefinition(
            "evolution.jotunn_cleaver", "Jotunn Cleaver", "CLEAVER", "Frost axes explode and split their damage across clustered enemies.",
            ItemCategory.Evolution, 1, new Color(0.95f, 0.45f, 0.22f), Effects(UpgradeId.None), FrostAxe.Id, JotunnRune.Id);

        public static ItemDefinition StormAegis { get; private set; } = new ItemDefinition(
            "evolution.storm_aegis", "Storm Aegis", "AEGIS", "Raven Guard becomes larger and restores health whenever it erupts.",
            ItemCategory.Evolution, 1, new Color(0.35f, 0.95f, 0.8f), Effects(UpgradeId.None), RavenGuard.Id, BearBlooded.Id);

        public static ItemDefinition GroveCrown { get; private set; } = new ItemDefinition(
            "evolution.grove_crown", "Grove Crown", "CROWN", "Grove Thorn Lash leaves damaging thorn patches where it strikes.",
            ItemCategory.Evolution, 1, new Color(0.55f, 0.88f, 0.42f), Effects(UpgradeId.None), GroveThornLash.Id, GroveSeed.Id);

        public static ItemDefinition SignalStorm { get; private set; } = new ItemDefinition(
            "evolution.signal_storm", "Signal Storm", "STORM", "Signal Flares chain to two nearby enemies after impact.",
            ItemCategory.Evolution, 1, new Color(0.98f, 0.48f, 0.15f), Effects(UpgradeId.None), SignalFlare.Id, FlareCore.Id);

        public static ItemDefinition OathMaelstrom { get; private set; } = new ItemDefinition(
            "evolution.oath_maelstrom", "Oath Maelstrom", "MAEL", "Oath Ring gains an extra blade and a wider orbit radius.",
            ItemCategory.Evolution, 1, new Color(0.42f, 0.92f, 0.55f), Effects(UpgradeId.None), OathRing.Id, OathBand.Id);

        public static ItemDefinition IronSanctuary { get; private set; } = new ItemDefinition(
            "evolution.iron_sanctuary", "Iron Sanctuary", "SANCT", "Iron Beacon persists as a moving shield zone after each pulse.",
            ItemCategory.Evolution, 1, new Color(0.52f, 0.58f, 0.72f), Effects(UpgradeId.None), IronBeacon.Id, FieldManual.Id);

        public static ItemDefinition FieldRations { get; private set; } = new ItemDefinition(
            "boon.field_rations", "Field Rations", "HEAL", "Immediately restores health. Does not occupy a build slot.",
            ItemCategory.Boon, 99, new Color(0.45f, 0.85f, 0.48f), Effects(UpgradeId.Heal));

        public static ItemDefinition[] All { get; private set; } = new[]
        {
            FrostAxe, RavenGuard, NorthWindSpear, RuneBolt, OathRing, GroveThornLash, CanopyVortex,
            DriftwoodStaff, SignalFlare, SupplyPulse, IronBeacon, TideCaller,
            LongshipBoots, BearBlooded, RavenArmor, SagaCarver, RavenHourglass, FinalVerse,
            WindsweptCloak, HollowGourds, OathFeather, SignalMagnet, FieldManual,
            JotunnRune, GroveSeed, FlareCore, OathBand,
            JotunnCleaver, StormAegis, GroveCrown, SignalStorm, OathMaelstrom, IronSanctuary,
            FieldRations
        };

        private static ItemDefinition Gear(string id, string name, string shortName, string description, int maxLevel, Color color, UpgradeId effect)
        {
            var effects = new UpgradeId[maxLevel];
            for (var i = 0; i < effects.Length; i++) effects[i] = effect;
            return new ItemDefinition(id, name, shortName, description, ItemCategory.Gear, maxLevel, color, effects);
        }

        public static ItemDefinition Find(string id)
        {
            for (var i = 0; i < All.Length; i++) if (All[i].Id == id) return All[i];
            return null;
        }

        public static int IndexOf(string id)
        {
            for (var i = 0; i < All.Length; i++) if (All[i].Id == id) return i;
            return -1;
        }

        public static ItemDefinition At(int index) => index >= 0 && index < All.Length ? All[index] : null;

        public static void Apply(ProductionContentDatabase database)
        {
            if (database == null || database.items == null || database.items.Length == 0) return;
            var definitions = new List<ItemDefinition>(database.items.Length);
            for (var i = 0; i < database.items.Length; i++)
            {
                var record = database.items[i];
                if (record == null || string.IsNullOrWhiteSpace(record.id)) continue;
                definitions.Add(record.Build());
            }
            if (definitions.Count == 0) return;

            var frostAxe = Find(definitions, "weapon.frost_axe");
            var ravenGuard = Find(definitions, "weapon.raven_guard");
            var fieldRations = Find(definitions, "boon.field_rations");
            if (frostAxe == null || ravenGuard == null || fieldRations == null) return;

            All = definitions.ToArray();
            FrostAxe = frostAxe;
            RavenGuard = ravenGuard;
            FieldRations = fieldRations;
            LongshipBoots = Find(definitions, "gear.longship_boots") ?? LongshipBoots;
            BearBlooded = Find(definitions, "gear.bear_blooded") ?? BearBlooded;
            RavenArmor = Find(definitions, "gear.raven_armor") ?? RavenArmor;
            SagaCarver = Find(definitions, "gear.saga_carver") ?? SagaCarver;
            RavenHourglass = Find(definitions, "gear.raven_hourglass") ?? RavenHourglass;
            FinalVerse = Find(definitions, "gear.final_verse") ?? FinalVerse;
            JotunnRune = Find(definitions, "gear.jotunn_rune") ?? JotunnRune;
            GroveSeed = Find(definitions, "gear.grove_seed") ?? GroveSeed;
            FlareCore = Find(definitions, "gear.flare_core") ?? FlareCore;
            OathBand = Find(definitions, "gear.oath_band") ?? OathBand;
            JotunnCleaver = Find(definitions, "evolution.jotunn_cleaver") ?? JotunnCleaver;
            StormAegis = Find(definitions, "evolution.storm_aegis") ?? StormAegis;
            GroveCrown = Find(definitions, "evolution.grove_crown") ?? GroveCrown;
            SignalStorm = Find(definitions, "evolution.signal_storm") ?? SignalStorm;
            OathMaelstrom = Find(definitions, "evolution.oath_maelstrom") ?? OathMaelstrom;
            IronSanctuary = Find(definitions, "evolution.iron_sanctuary") ?? IronSanctuary;
            NorthWindSpear = Find(definitions, "weapon.north_wind_spear") ?? NorthWindSpear;
            RuneBolt = Find(definitions, "weapon.rune_bolt") ?? RuneBolt;
            OathRing = Find(definitions, "weapon.oath_ring") ?? OathRing;
            GroveThornLash = Find(definitions, "weapon.grove_thorn_lash") ?? GroveThornLash;
            CanopyVortex = Find(definitions, "weapon.canopy_vortex") ?? CanopyVortex;
            DriftwoodStaff = Find(definitions, "weapon.driftwood_staff") ?? DriftwoodStaff;
            SignalFlare = Find(definitions, "weapon.signal_flare") ?? SignalFlare;
            SupplyPulse = Find(definitions, "weapon.supply_pulse") ?? SupplyPulse;
            IronBeacon = Find(definitions, "weapon.iron_beacon") ?? IronBeacon;
            TideCaller = Find(definitions, "weapon.tide_caller") ?? TideCaller;
            WindsweptCloak = Find(definitions, "gear.windswept_cloak") ?? WindsweptCloak;
            HollowGourds = Find(definitions, "gear.hollow_gourds") ?? HollowGourds;
            OathFeather = Find(definitions, "gear.oath_feather") ?? OathFeather;
            SignalMagnet = Find(definitions, "gear.signal_magnet") ?? SignalMagnet;
            FieldManual = Find(definitions, "gear.field_manual") ?? FieldManual;
        }

        private static ItemDefinition Find(List<ItemDefinition> definitions, string id)
        {
            for (var i = 0; i < definitions.Count; i++) if (definitions[i].Id == id) return definitions[i];
            return null;
        }
    }

    public static class RewardFactory
    {
        public static List<RewardOption> Generate(PlayerBuild[] builds, int owner, int playerCount, RunRandom random = null)
        {
            var options = new List<RewardOption>(4);
            var used = new HashSet<string>();
            AddForTargetOrRations(options, used, builds, owner, random);
            AddForTargetOrRations(options, used, builds, owner, random);

            if (playerCount > 1 && Chance(random, 0.6f))
                AddForTargetOrRations(options, used, builds, 1 - owner, random);
            else
                AddForTargetOrRations(options, used, builds, owner, random);

            if (playerCount > 1 && Chance(random, 0.35f))
                AddShared(options, used, builds, playerCount, owner, random);
            else
                AddForTargetOrRations(options, used, builds, owner, random);

            while (options.Count < 4)
                options.Add(new RewardOption { Item = ItemCatalog.FieldRations, TargetPlayerIndex = owner });
            return options;
        }

        private static void AddForTargetOrRations(
            List<RewardOption> options, HashSet<string> used, PlayerBuild[] builds, int target, RunRandom random)
        {
            if (!AddForTarget(options, used, builds, target, random))
                options.Add(new RewardOption { Item = ItemCatalog.FieldRations, TargetPlayerIndex = target });
        }

        private static bool AddForTarget(
            List<RewardOption> options, HashSet<string> used, PlayerBuild[] builds, int target, RunRandom random)
        {
            var candidates = Eligible(builds[target]);
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                if (used.Contains($"{target}:{candidates[i].Id}")) candidates.RemoveAt(i);
            }
            if (candidates.Count == 0) return false;
            var item = candidates[PickIndex(random, candidates.Count)];
            used.Add($"{target}:{item.Id}");
            options.Add(new RewardOption { Item = item, TargetPlayerIndex = target });
            return true;
        }

        private static void AddShared(
            List<RewardOption> options, HashSet<string> used, PlayerBuild[] builds, int playerCount, int fallbackTarget, RunRandom random)
        {
            var candidates = Eligible(builds[0]);
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var item = candidates[i];
                if (item.IsEvolution || item.Category == ItemCategory.Boon ||
                    used.Contains($"0:{item.Id}") || used.Contains($"1:{item.Id}"))
                {
                    candidates.RemoveAt(i);
                    continue;
                }
                for (var player = 1; player < playerCount; player++)
                    if (!builds[player].CanAcquire(item)) { candidates.RemoveAt(i); break; }
            }
            if (candidates.Count == 0)
            {
                AddForTargetOrRations(options, used, builds, fallbackTarget, random);
                return;
            }
            var selected = candidates[PickIndex(random, candidates.Count)];
            used.Add($"team:{selected.Id}");
            options.Add(new RewardOption { Item = selected, TargetPlayerIndex = 0, Shared = true });
        }

        private static List<ItemDefinition> Eligible(PlayerBuild build)
        {
            var result = new List<ItemDefinition>();
            for (var i = 0; i < ItemCatalog.All.Length; i++)
            {
                var item = ItemCatalog.All[i];
                if (item == ItemCatalog.FieldRations) continue;
                if (build.CanAcquire(item)) result.Add(item);
            }
            // Completed recipes should be noticeably more likely than another numeric level.
            var originalCount = result.Count;
            for (var i = 0; i < originalCount; i++)
                if (result[i].IsEvolution) { result.Add(result[i]); result.Add(result[i]); }
            if (result.Count == 0) result.Add(ItemCatalog.FieldRations);
            return result;
        }

        private static bool Chance(RunRandom random, float probability) =>
            random != null ? random.Chance(probability) : Random.value < probability;

        private static int PickIndex(RunRandom random, int count) =>
            random != null ? random.Range(0, count) : Random.Range(0, count);
    }

    public static class RewardEffects
    {
        public static bool Apply(PlayerController player, RewardOption option)
        {
            if (player == null || option == null || option.Item == null) return false;
            var result = player.Build.Acquire(option.Item);
            if (result == null) return false;
            Apply(player, result);
            return true;
        }

        public static void Apply(PlayerController player, BuildApplyResult result)
        {
            if (player == null || result == null) return;
            player.ApplyBuildResult(result);
        }
    }
}
