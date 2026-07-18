using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class SharedRunModelTests
    {
        [Test]
        public void Begin_InitializesDeterministicProgressionState()
        {
            var model = new SharedRunModel();

            model.Begin(2, (level, players) => level * 10 + players);

            Assert.That(model.Phase, Is.EqualTo(RunSimulationPhase.Playing));
            Assert.That(model.Outcome, Is.EqualTo(RunOutcome.None));
            Assert.That(model.PlayerCount, Is.EqualTo(2));
            Assert.That(model.Level, Is.EqualTo(1));
            Assert.That(model.Experience, Is.Zero);
            Assert.That(model.ExperienceToNext, Is.EqualTo(12));
            Assert.That(model.RewardTurnPlayerIndex, Is.Zero);
            Assert.That(model.Elapsed, Is.Zero);
            Assert.That(model.BossTriggered, Is.False);
        }

        [Test]
        public void Advance_TriggersBossExactlyOnceAtConfiguredTime()
        {
            var model = StartedModel(1, 20);

            model.Advance(-1f);
            model.Advance(9f);
            Assert.That(model.Elapsed, Is.EqualTo(9f));
            Assert.That(model.TryTriggerBoss(10f), Is.False);

            model.Advance(1.5f);
            Assert.That(model.TryTriggerBoss(10f), Is.True);
            Assert.That(model.BossTriggered, Is.True);
            Assert.That(model.TryTriggerBoss(10f), Is.False);
        }

        [Test]
        public void AddExperience_CarriesOverflowAndWaitsForRewardResolution()
        {
            var model = StartedModel(1, 10);

            Assert.That(model.AddExperience(7), Is.False);
            Assert.That(model.AddExperience(5), Is.True);
            Assert.That(model.Phase, Is.EqualTo(RunSimulationPhase.Reward));
            Assert.That(model.Level, Is.EqualTo(2));
            Assert.That(model.Experience, Is.EqualTo(2));
            Assert.That(model.ExperienceToNext, Is.EqualTo(20));

            Assert.That(model.AddExperience(100), Is.False);
            Assert.That(model.Experience, Is.EqualTo(2));
            Assert.That(model.CompleteReward(), Is.True);
            Assert.That(model.Phase, Is.EqualTo(RunSimulationPhase.Playing));
        }

        [Test]
        public void RewardTurn_AlternatesAcrossTwoPlayers()
        {
            var model = StartedModel(2, 1);

            Assert.That(model.AddExperience(1), Is.True);
            Assert.That(model.RewardTurnPlayerIndex, Is.Zero);
            Assert.That(model.CompleteReward(), Is.True);

            Assert.That(model.AddExperience(2), Is.True);
            Assert.That(model.RewardTurnPlayerIndex, Is.EqualTo(1));
            Assert.That(model.CompleteReward(), Is.True);

            Assert.That(model.AddExperience(3), Is.True);
            Assert.That(model.RewardTurnPlayerIndex, Is.Zero);
        }

        [Test]
        public void Complete_IsIdempotentAndResetReturnsToIdle()
        {
            var model = StartedModel(1, 10);
            model.Advance(4f);

            Assert.That(model.Complete(true), Is.True);
            Assert.That(model.Phase, Is.EqualTo(RunSimulationPhase.Completed));
            Assert.That(model.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(model.Complete(false), Is.False);
            model.Advance(5f);
            Assert.That(model.Elapsed, Is.EqualTo(4f));

            model.Reset();
            Assert.That(model.Phase, Is.EqualTo(RunSimulationPhase.Idle));
            Assert.That(model.Outcome, Is.EqualTo(RunOutcome.None));
        }

        private static SharedRunModel StartedModel(int players, int baseRequirement)
        {
            var model = new SharedRunModel();
            model.Begin(players, (level, _) => level * baseRequirement);
            return model;
        }
    }
}
