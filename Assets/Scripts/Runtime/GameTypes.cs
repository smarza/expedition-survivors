using System;
using UnityEngine;

namespace ProjectExpedition
{
    public enum RunState
    {
        MainMenu,
        CharacterSelect,
        MapSelect,
        Playing,
        LevelUp,
        BuildDetails,
        Paused,
        GameOver,
        Victory,
        Settings
    }

    public enum UpgradeId
    {
        None,
        AxeDamage,
        AxeSpeed,
        ExtraAxe,
        AxePierce,
        MoveSpeed,
        MaxHealth,
        Armor,
        Magnet,
        ShieldPulse,
        ShieldDamage,
        CriticalRunes,
        UltimateCooldown,
        UltimateDamage,
        Heal,
        ShieldDamageAndSpeed,
        OrbitDamage,
        OrbitSpeed,
        ExtraOrbit,
        RadialDamage,
        RadialSpeed,
        ExtraRadial,
        SapRegen,
        SiegeKnockback,
        ReturningProjectile,
        PersistentHealAura
    }

    [Serializable]
    public sealed class UpgradeChoice
    {
        public UpgradeId Id;
        public string Name;
        public string Description;
        public Color Accent;

        public UpgradeChoice(UpgradeId id, string name, string description, Color accent)
        {
            Id = id;
            Name = name;
            Description = description;
            Accent = accent;
        }
    }

    [Serializable]
    public sealed class MetaProgress
    {
        public int TotalRenown;
        public int SpentRenown;
        public int RunsCompleted;
        public int BestKills;
        public float BestTime;
        public int HaldorMastery;
        public int SylvaMastery;
        public int MaraMastery;
        public int EiraMastery;
        public int BrenMastery;
        public int RexMastery;
        public string LastCampLeaderId;
        public string LastCoopPartnerId;
        public string[] RelicsCollected = new string[0];
        public string[] UnlockedContentIds = new string[0];
        public string[] UnlockedMutatorIds = new string[0];
        public string[] DiscoveredCodexIds = new string[0];
        public bool CampOnboardingComplete;
        public bool ChallengeOnboardingComplete;
    }
}
