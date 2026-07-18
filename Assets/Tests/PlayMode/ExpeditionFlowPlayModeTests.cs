using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectExpedition.Tests
{
    public sealed class ExpeditionFlowPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            SaveService.PersistenceEnabled = true;
            var directors = Object.FindObjectsByType<GameDirector>();
            for (var i = 0; i < directors.Length; i++)
                if (directors[i] != null) Object.Destroy(directors[i].gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameDirector_InitializesProductionFoundation()
        {
            yield return ClearDirectors();
            var director = CreateDirector();

            Assert.That(director.State, Is.EqualTo(RunState.MainMenu));
            Assert.That(director.SimulationPhase, Is.EqualTo(RunSimulationPhase.Idle));
            Assert.That(director.FoundationStatus, Does.StartWith("READY"));
            Assert.That(director.CreatedPooledObjects, Is.GreaterThan(0));

            yield return DestroyDirector(director);
        }

        [UnityTest]
        public IEnumerator SoloRun_LevelUpOffersFourRewardsAndResumes()
        {
            yield return ClearDirectors();
            var director = CreateDirector();
            director.BeginRunSetup(1);
            director.ConfirmCharacters(0, 1);
            director.SelectMapAndStart(0);

            Assert.That(director.State, Is.EqualTo(RunState.Playing));
            Assert.That(director.SimulationPhase, Is.EqualTo(RunSimulationPhase.Playing));
            Assert.That(director.Players.Count, Is.EqualTo(1));
            Assert.That(director.Player.HeroName, Is.EqualTo("Haldor Stormborn"));
            Assert.That(director.ExperienceToNext, Is.GreaterThan(0));

            director.AddExperience(director.ExperienceToNext);
            Assert.That(director.State, Is.EqualTo(RunState.LevelUp));
            Assert.That(director.SimulationPhase, Is.EqualTo(RunSimulationPhase.Reward));
            Assert.That(director.CurrentRewards.Count, Is.EqualTo(4));
            Assert.That(director.RewardTurnPlayerIndex, Is.Zero);
            Assert.That(Time.timeScale, Is.Zero);

            director.ChooseReward(0);
            Assert.That(director.State, Is.EqualTo(RunState.Playing));
            Assert.That(director.SimulationPhase, Is.EqualTo(RunSimulationPhase.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            yield return DestroyDirector(director);
        }

        [UnityTest]
        public IEnumerator PlayerController_ProjectsSharedPlayerStateAndUpgrades()
        {
            yield return ClearDirectors();
            var director = CreateDirector();
            director.BeginRunSetup(1);
            director.ConfirmCharacters(0, 1);
            director.SelectMapAndStart(0);
            var player = director.Player;

            Assert.That(player.MaxHealth, Is.EqualTo(150f));
            Assert.That(player.Health, Is.EqualTo(150f));
            Assert.That(player.MoveSpeed, Is.EqualTo(4.45f));
            Assert.That(player.Armor, Is.EqualTo(2f));
            Assert.That(player.UltimateCooldown, Is.EqualTo(60f));
            Assert.That(player.UltimateRemaining, Is.EqualTo(18f));

            player.TakeDamage(10f);
            player.AddMoveSpeed(0.46f);
            player.AddArmor(1f);
            player.AddMagnet(0.6f);
            player.AddMaxHealth(24f);
            player.ImproveUltimateCooldown();
            player.ImproveUltimateDamage();

            Assert.That(player.Health, Is.EqualTo(166f));
            Assert.That(player.MaxHealth, Is.EqualTo(174f));
            Assert.That(player.MoveSpeed, Is.EqualTo(4.91f).Within(0.0001f));
            Assert.That(player.Armor, Is.EqualTo(3f));
            Assert.That(player.MagnetRadius, Is.EqualTo(2.3f).Within(0.0001f));
            Assert.That(player.UltimateCooldown, Is.EqualTo(54f).Within(0.0001f));
            Assert.That(player.UltimateDamage, Is.EqualTo(188.5f).Within(0.0001f));

            yield return DestroyDirector(director);
        }

        [UnityTest]
        public IEnumerator ReplayRun_PreservesSeedAndRestartsProgress()
        {
            yield return ClearDirectors();
            var director = CreateDirector();
            director.BeginRunSetup(1);
            director.ConfirmCharacters(0, 1);
            director.SelectMapAndStart(0);
            var seed = director.RunSeed;

            director.AddExperience(10);
            director.ReplayRun();

            Assert.That(director.RunSeed, Is.EqualTo(seed));
            Assert.That(director.State, Is.EqualTo(RunState.Playing));
            Assert.That(director.SimulationPhase, Is.EqualTo(RunSimulationPhase.Playing));
            Assert.That(director.Level, Is.EqualTo(1));
            Assert.That(director.Experience, Is.Zero);
            Assert.That(director.Players.Count, Is.EqualTo(1));

            yield return DestroyDirector(director);
        }

        [UnityTest]
        public IEnumerator RunOutcome_TransitionsOnceWithoutTouchingDisk()
        {
            yield return ClearDirectors();
            var director = CreateDirector();
            director.BeginRunSetup(1);
            director.ConfirmCharacters(0, 1);
            director.SelectMapAndStart(0);

            director.EndRun(true);
            Assert.That(director.State, Is.EqualTo(RunState.Victory));
            Assert.That(director.SimulationPhase, Is.EqualTo(RunSimulationPhase.Completed));
            Assert.That(director.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(SaveService.Data.RunsCompleted, Is.EqualTo(1));
            Assert.That(SaveService.Data.TotalRenown, Is.GreaterThanOrEqualTo(50));

            director.EndRun(true);
            Assert.That(SaveService.Data.RunsCompleted, Is.EqualTo(1));

            yield return DestroyDirector(director);
        }

        private static GameDirector CreateDirector()
        {
            Time.timeScale = 1f;
            SaveService.PersistenceEnabled = false;
            SaveService.ResetForTests();
            return new GameObject("Expedition Test Runtime").AddComponent<GameDirector>();
        }

        private static IEnumerator ClearDirectors()
        {
            Time.timeScale = 1f;
            var directors = Object.FindObjectsByType<GameDirector>();
            for (var i = 0; i < directors.Length; i++)
                if (directors[i] != null) Object.Destroy(directors[i].gameObject);
            yield return null;
        }

        private static IEnumerator DestroyDirector(GameDirector director)
        {
            Time.timeScale = 1f;
            if (director != null) Object.Destroy(director.gameObject);
            yield return null;
            SaveService.PersistenceEnabled = true;
        }
    }
}
