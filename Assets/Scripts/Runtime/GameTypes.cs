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
        OnlineSpike
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
        Heal
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
    }
}
