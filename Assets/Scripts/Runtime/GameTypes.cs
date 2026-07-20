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
        ExtraRadial
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
        public int RunsCompleted;
        public int BestKills;
        public float BestTime;
        public int HaldorMastery;
        public string LastCampLeaderId;
        public string LastCoopPartnerId;
        public string[] RelicsCollected = new string[0];
    }
}
