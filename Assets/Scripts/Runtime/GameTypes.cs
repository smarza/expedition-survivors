using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public enum RunState
    {
        TitleScreen,
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

    public enum RunEndPresentationPhase
    {
        Beat,
        Summary
    }

    public enum RunEndCause
    {
        VictoryExtraction,
        VictoryTimeout,
        DefeatPartyWiped
    }

    public enum ExtractionCompletionKind
    {
        None,
        BeaconHold,
        Timeout
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

    public enum PlayerHurtHitKind : byte
    {
        Contact,
        BossContact,
        BossSlam
    }

    public readonly struct PlayerHurtSeverity
    {
        public readonly float Trauma;
        public readonly float VfxScale;
        public readonly float Pitch;
        public readonly float VignetteStrength;
        public readonly float HapticLow;
        public readonly float HapticHigh;
        public readonly float HapticDuration;

        public PlayerHurtSeverity(
            float trauma,
            float vfxScale,
            float pitch,
            float vignetteStrength,
            float hapticLow,
            float hapticHigh,
            float hapticDuration)
        {
            Trauma = trauma;
            VfxScale = vfxScale;
            Pitch = pitch;
            VignetteStrength = vignetteStrength;
            HapticLow = hapticLow;
            HapticHigh = hapticHigh;
            HapticDuration = hapticDuration;
        }

        public static PlayerHurtSeverity Resolve(float damageRatio, PlayerHurtHitKind hitKind)
        {
            var ratio = Mathf.Clamp01(damageRatio);
            var traumaBase = DevelopmentTuningResolver.PlayerHurtTraumaBase;
            var traumaHeavy = DevelopmentTuningResolver.PlayerHurtTraumaHeavyBonus;
            var vignetteScale = DevelopmentTuningResolver.PlayerHurtVignetteScale;

            switch (hitKind)
            {
                case PlayerHurtHitKind.BossSlam:
                    return new PlayerHurtSeverity(
                        traumaBase + traumaHeavy + ratio * 0.12f,
                        Mathf.Lerp(0.85f, 1.35f, ratio),
                        Mathf.Lerp(0.82f, 0.68f, ratio),
                        Mathf.Clamp01((0.55f + ratio * 0.35f) * vignetteScale),
                        0.35f,
                        0.85f,
                        0.3f);
                case PlayerHurtHitKind.BossContact:
                    return new PlayerHurtSeverity(
                        traumaBase + traumaHeavy * 0.55f + ratio * 0.08f,
                        Mathf.Lerp(0.65f, 1.05f, ratio),
                        Mathf.Lerp(0.9f, 0.78f, ratio),
                        Mathf.Clamp01((0.38f + ratio * 0.28f) * vignetteScale),
                        0.42f,
                        0.62f,
                        0.22f);
                default:
                    return new PlayerHurtSeverity(
                        traumaBase + ratio * 0.06f,
                        Mathf.Lerp(0.48f, 0.82f, ratio),
                        Mathf.Lerp(0.96f, 0.86f, ratio),
                        Mathf.Clamp01((0.22f + ratio * 0.32f) * vignetteScale),
                        0.28f,
                        0.18f,
                        0.15f);
            }
        }
    }

    public sealed class PlayerHurtFeedbackTracker
    {
        public const int MaxTrackedPlayers = 2;

        private readonly float[] _vignetteStrength = new float[MaxTrackedPlayers];
        private readonly float[] _healthBarPulse = new float[MaxTrackedPlayers];
        private readonly float[] _ghostHealthFraction = new float[MaxTrackedPlayers];

        public float ResolveVignetteStrength(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxTrackedPlayers)
            {
                return 0f;
            }

            return _vignetteStrength[playerIndex];
        }

        public float ResolveHealthBarPulse(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxTrackedPlayers)
            {
                return 0f;
            }

            return _healthBarPulse[playerIndex];
        }

        public float ResolveGhostHealthFraction(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxTrackedPlayers)
            {
                return 0f;
            }

            return _ghostHealthFraction[playerIndex];
        }

        public void RegisterHit(int playerIndex, float healthFractionBeforeDamage, float damageRatio, PlayerHurtSeverity severity)
        {
            if (playerIndex < 0 || playerIndex >= MaxTrackedPlayers)
            {
                return;
            }

            _vignetteStrength[playerIndex] = severity.VignetteStrength;
            _healthBarPulse[playerIndex] = 1f;
            _ghostHealthFraction[playerIndex] = Mathf.Max(
                _ghostHealthFraction[playerIndex],
                healthFractionBeforeDamage);
        }

        public void Advance(float deltaTime, IReadOnlyList<PlayerController> players)
        {
            if (deltaTime <= 0f || players == null)
            {
                return;
            }

            var vignetteDecay = deltaTime / 0.35f;
            var pulseDecay = deltaTime / 0.2f;
            var ghostDecay = deltaTime * 1.8f;

            for (var i = 0; i < MaxTrackedPlayers; i++)
            {
                _vignetteStrength[i] = Mathf.Max(0f, _vignetteStrength[i] - vignetteDecay);
                _healthBarPulse[i] = Mathf.Max(0f, _healthBarPulse[i] - pulseDecay);

                if (i >= players.Count || players[i] == null)
                {
                    _ghostHealthFraction[i] = 0f;
                    continue;
                }

                var currentFraction = players[i].Health / Mathf.Max(1f, players[i].MaxHealth);
                _ghostHealthFraction[i] = Mathf.MoveTowards(_ghostHealthFraction[i], currentFraction, ghostDecay);
            }
        }

        public void Reset()
        {
            for (var i = 0; i < MaxTrackedPlayers; i++)
            {
                _vignetteStrength[i] = 0f;
                _healthBarPulse[i] = 0f;
                _ghostHealthFraction[i] = 0f;
            }
        }
    }
}
