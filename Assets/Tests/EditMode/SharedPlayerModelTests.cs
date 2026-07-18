using NUnit.Framework;
using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class SharedPlayerModelTests
    {
        [Test]
        public void Begin_InitializesCharacterStatisticsAndPartialUltimateCharge()
        {
            var model = StartedModel();

            Assert.That(model.MaxHealth, Is.EqualTo(150f));
            Assert.That(model.Health, Is.EqualTo(150f));
            Assert.That(model.MoveSpeed, Is.EqualTo(4.45f));
            Assert.That(model.Armor, Is.EqualTo(2f));
            Assert.That(model.MagnetRadius, Is.EqualTo(1.7f));
            Assert.That(model.IsAlive, Is.True);
            Assert.That(model.IsDowned, Is.False);
            Assert.That(model.UltimateCooldown, Is.EqualTo(60f));
            Assert.That(model.UltimateRemaining, Is.EqualTo(18f));
            Assert.That(model.UltimateDamage, Is.EqualTo(145f));
            Assert.That(model.UltimateRadius, Is.EqualTo(6.8f));
        }

        [Test]
        public void AdvanceAndMovement_UseTheSameTimingAndStatisticsForEveryAdapter()
        {
            var model = StartedModel();

            model.Advance(3f);
            var requested = model.CalculateRequestedPosition(new Vector2(2f, -1f), Vector2.right, 2f);

            Assert.That(model.UltimateRemaining, Is.EqualTo(15f));
            Assert.That(requested.x, Is.EqualTo(10.9f).Within(0.0001f));
            Assert.That(requested.y, Is.EqualTo(-1f).Within(0.0001f));
            model.Advance(-10f);
            Assert.That(model.UltimateRemaining, Is.EqualTo(15f));
        }

        [Test]
        public void Ultimate_RequiresChargeAndStartsCooldownAndInvulnerability()
        {
            var model = StartedModel();

            Assert.That(model.TryActivateUltimate(), Is.False);
            model.Advance(18f);
            Assert.That(model.UltimateReady, Is.True);
            Assert.That(model.TryActivateUltimate(), Is.True);
            Assert.That(model.UltimateRemaining, Is.EqualTo(60f));
            Assert.That(model.InvulnerabilityRemaining, Is.EqualTo(1.25f));
            Assert.That(model.TryActivateUltimate(), Is.False);
            Assert.That(model.TakeDamage(1000f), Is.EqualTo(PlayerDamageResult.Ignored));
        }

        [Test]
        public void Damage_AppliesArmorMinimumDamageAndKnockdownExactlyOnce()
        {
            var model = StartedModel();

            Assert.That(model.TakeDamage(1f), Is.EqualTo(PlayerDamageResult.Damaged));
            Assert.That(model.Health, Is.EqualTo(149f));
            Assert.That(model.TakeDamage(1000f), Is.EqualTo(PlayerDamageResult.Ignored));
            model.Advance(0.22f);
            Assert.That(model.TakeDamage(1000f), Is.EqualTo(PlayerDamageResult.Downed));
            Assert.That(model.Health, Is.Zero);
            Assert.That(model.IsDowned, Is.True);
            Assert.That(model.IsAlive, Is.False);
            Assert.That(model.TakeDamage(1000f), Is.EqualTo(PlayerDamageResult.Ignored));
        }

        [Test]
        public void Revival_RequiresNearbyRescuerAndRestoresProtectedHealth()
        {
            var model = StartedModel();
            model.TakeDamage(1000f);

            Assert.That(model.AdvanceRevival(true, 1.25f), Is.False);
            Assert.That(model.ReviveProgress, Is.EqualTo(0.5f));
            Assert.That(model.AdvanceRevival(false, 1f), Is.False);
            Assert.That(model.ReviveProgress, Is.EqualTo(0.36f).Within(0.0001f));
            Assert.That(model.AdvanceRevival(true, 1.6f), Is.True);
            Assert.That(model.IsDowned, Is.False);
            Assert.That(model.Health, Is.EqualTo(63f));
            Assert.That(model.ReviveProgress, Is.Zero);
            Assert.That(model.InvulnerabilityRemaining, Is.EqualTo(1f));
        }

        [Test]
        public void Upgrades_UpdateDerivedStatisticsAndPreserveUltimateChargeRatio()
        {
            var model = StartedModel();

            model.AddMoveSpeed(0.46f);
            model.AddArmor(1f);
            model.AddMagnet(0.6f);
            model.AddMaxHealth(24f);
            model.ImproveUltimateCooldown();
            model.ImproveUltimateDamage();

            Assert.That(model.MoveSpeed, Is.EqualTo(4.91f).Within(0.0001f));
            Assert.That(model.Armor, Is.EqualTo(3f));
            Assert.That(model.MagnetRadius, Is.EqualTo(2.3f).Within(0.0001f));
            Assert.That(model.MaxHealth, Is.EqualTo(174f));
            Assert.That(model.Health, Is.EqualTo(174f));
            Assert.That(model.UltimateCooldown, Is.EqualTo(54f).Within(0.0001f));
            Assert.That(model.UltimateRemaining, Is.EqualTo(16.2f).Within(0.0001f));
            Assert.That(model.UltimateDamage, Is.EqualTo(188.5f).Within(0.0001f));
            Assert.That(model.UltimateRadius, Is.EqualTo(7.514f).Within(0.0001f));
        }

        private static SharedPlayerModel StartedModel()
        {
            var model = new SharedPlayerModel();
            model.Begin(150f, 4.45f, 2f, 60f, 145f, 6.8f,
                BalanceRules.UltimateCooldown);
            return model;
        }
    }
}
