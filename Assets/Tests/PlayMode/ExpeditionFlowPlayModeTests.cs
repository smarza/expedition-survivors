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
        public IEnumerator PresentationFoundation_InitializesAndFollowsRunState()
        {
            yield return ClearDirectors();
            var director = CreateDirector();

            Assert.That(director.Presentation, Is.Not.Null);
            Assert.That(director.Presentation.MusicState, Is.EqualTo(PresentationMusicState.Menu));

            director.BeginRunSetup(1);
            director.ConfirmCharacters(0, 1);
            director.SelectMapAndStart(0);
            yield return null;

            Assert.That(director.Presentation.MusicState, Is.EqualTo(PresentationMusicState.Expedition));
            Assert.That(director.Player.GetComponent<HeroPresentation>(), Is.Not.Null);
            Assert.That(director.RunRoot.GetComponentInChildren<FrostboundAmbience>(), Is.Not.Null);

            yield return DestroyDirector(director);
        }

        [UnityTest]
        public IEnumerator PresentationVfx_ReusesPoolAndSettingsReturnToTheirOwnerState()
        {
            yield return ClearDirectors();
            var director = CreateDirector();

            director.OpenSettings();
            Assert.That(director.State, Is.EqualTo(RunState.Settings));
            Assert.That(Time.timeScale, Is.Zero);
            director.CloseSettings();
            Assert.That(director.State, Is.EqualTo(RunState.MainMenu));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            director.Present(PresentationCue.Impact, Vector2.zero, Color.cyan);
            Assert.That(director.Presentation.ActiveVfx, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(director.Presentation.ActiveVfx, Is.Zero);

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
        public IEnumerator EnemyAdapter_ProjectsSharedEnemyStateAndDamage()
        {
            yield return ClearDirectors();
            var director = CreateDirector();
            var enemyObject = new GameObject("Shared Enemy Adapter Test");
            enemyObject.transform.SetParent(director.transform, false);
            enemyObject.transform.position = new Vector3(3f, -2f, 0f);
            var enemy = enemyObject.AddComponent<Enemy>();

            enemy.Initialize(director, 2f, false);
            var initialHealth = EnemyCatalog.Draugr.BaseHealth +
                2f * EnemyCatalog.Draugr.HealthPerDifficulty;

            Assert.That(enemy.Alive, Is.True);
            Assert.That(enemy.Boss, Is.False);
            Assert.That(enemy.Position, Is.EqualTo(new Vector2(3f, -2f)));
            Assert.That(enemy.Health, Is.EqualTo(initialHealth));
            Assert.That(enemy.Speed, Is.InRange(
                EnemyCatalog.Draugr.MinimumSpeed + 2f * EnemyCatalog.Draugr.SpeedPerDifficulty,
                EnemyCatalog.Draugr.MaximumSpeed + 2f * EnemyCatalog.Draugr.SpeedPerDifficulty));
            Assert.That(enemy.ExperienceValue, Is.InRange(
                EnemyCatalog.Draugr.MinimumExperience,
                EnemyCatalog.Draugr.MaximumExperienceExclusive - 1));

            var damageResult = enemy.TakeDamage(5f, 0f, enemy.Position);
            Assert.That((damageResult & EnemyDamageResult.Damaged) != 0, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(initialHealth - 5f));
            Assert.That(enemy.Alive, Is.True);

            yield return DestroyDirector(director);
        }

        [UnityTest]
        public IEnumerator RewardPipeline_ProjectsSharedWeaponUpgrade()
        {
            yield return ClearDirectors();
            var director = CreateDirector();
            director.BeginRunSetup(1);
            director.ConfirmCharacters(0, 1);
            director.SelectMapAndStart(0);
            var player = director.Player;
            var initialDamage = player.Weapons.AxeDamage;

            var applied = RewardEffects.Apply(player, new RewardOption
            {
                Item = ItemCatalog.FrostAxe,
                TargetPlayerIndex = 0
            });

            Assert.That(applied, Is.True);
            Assert.That(player.Build.Find(ItemCatalog.FrostAxe.Id).Level, Is.EqualTo(2));
            Assert.That(player.Weapons.AxeDamage,
                Is.EqualTo(initialDamage * 1.26f).Within(0.0001f));

            for (var level = 3; level <= ItemCatalog.FrostAxe.MaxLevel; level++)
                Assert.That(RewardEffects.Apply(player, new RewardOption
                {
                    Item = ItemCatalog.FrostAxe,
                    TargetPlayerIndex = 0
                }), Is.True);

            Assert.That(player.Build.Find(ItemCatalog.FrostAxe.Id).Level, Is.EqualTo(8));
            Assert.That(player.Weapons.AxeCount, Is.EqualTo(2));
            Assert.That(player.Weapons.AxePierce, Is.EqualTo(2));
            Assert.That(player.Weapons.AxeCooldown,
                Is.EqualTo(0.82f * Mathf.Pow(0.86f, 2f)).Within(0.0001f));

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
