using System;

namespace ProjectExpedition
{
    public enum RunSimulationPhase : byte
    {
        Idle,
        Playing,
        Reward,
        Completed
    }

    public enum RunOutcome : byte
    {
        None,
        Victory,
        Defeat
    }

    /// <summary>
    /// Deterministic progression state shared by every run adapter. It owns no
    /// GameObjects, input, UI, persistence or network transport.
    /// </summary>
    public sealed class SharedRunModel
    {
        private Func<int, int, int> _experienceRequirement;
        private int _rewardTurnCounter;

        public RunSimulationPhase Phase { get; private set; } = RunSimulationPhase.Idle;
        public RunOutcome Outcome { get; private set; } = RunOutcome.None;
        public int PlayerCount { get; private set; } = 1;
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int ExperienceToNext { get; private set; } = 1;
        public int RewardTurnPlayerIndex { get; private set; }
        public float Elapsed { get; private set; }
        public bool BossTriggered { get; private set; }

        public void Begin(int playerCount, Func<int, int, int> experienceRequirement)
        {
            _experienceRequirement = experienceRequirement ??
                throw new ArgumentNullException(nameof(experienceRequirement));
            PlayerCount = Math.Max(1, Math.Min(2, playerCount));
            Level = 1;
            Experience = 0;
            ExperienceToNext = RequirementFor(Level);
            RewardTurnPlayerIndex = 0;
            Elapsed = 0f;
            BossTriggered = false;
            Outcome = RunOutcome.None;
            _rewardTurnCounter = 0;
            Phase = RunSimulationPhase.Playing;
        }

        public void Advance(float deltaTime)
        {
            if (Phase != RunSimulationPhase.Playing || deltaTime <= 0f) return;
            Elapsed += deltaTime;
        }

        public bool TryTriggerBoss(float triggerTime)
        {
            if (Phase != RunSimulationPhase.Playing || BossTriggered ||
                Elapsed < Math.Max(0f, triggerTime)) return false;
            BossTriggered = true;
            return true;
        }

        public bool AddExperience(int amount)
        {
            if (Phase != RunSimulationPhase.Playing || amount <= 0) return false;
            Experience += amount;
            if (Experience < ExperienceToNext) return false;

            Experience -= ExperienceToNext;
            Level++;
            ExperienceToNext = RequirementFor(Level);
            RewardTurnPlayerIndex = _rewardTurnCounter % PlayerCount;
            Phase = RunSimulationPhase.Reward;
            return true;
        }

        public bool CompleteReward()
        {
            if (Phase != RunSimulationPhase.Reward) return false;
            _rewardTurnCounter++;
            Phase = RunSimulationPhase.Playing;
            return true;
        }

        public bool Complete(bool victory)
        {
            if (Phase == RunSimulationPhase.Idle || Phase == RunSimulationPhase.Completed) return false;
            Outcome = victory ? RunOutcome.Victory : RunOutcome.Defeat;
            Phase = RunSimulationPhase.Completed;
            return true;
        }

        public void Reset()
        {
            Phase = RunSimulationPhase.Idle;
            Outcome = RunOutcome.None;
            PlayerCount = 1;
            Level = 1;
            Experience = 0;
            ExperienceToNext = 1;
            RewardTurnPlayerIndex = 0;
            Elapsed = 0f;
            BossTriggered = false;
            _rewardTurnCounter = 0;
            _experienceRequirement = null;
        }

        private int RequirementFor(int level) => Math.Max(1, _experienceRequirement(level, PlayerCount));
    }
}
