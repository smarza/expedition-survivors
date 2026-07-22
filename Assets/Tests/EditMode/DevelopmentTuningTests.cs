using NUnit.Framework;

namespace ProjectExpedition.Tests
{
    public sealed class DevelopmentTuningTests
    {
        [SetUp]
        public void SetUp()
        {
            DevelopmentTuningService.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            DevelopmentTuningService.ResetToDefaults();
        }

        [Test]
        public void ResetToDefaults_RestoresLootDropChanceFromCatalog()
        {
            var profile = DevelopmentTuningService.Active;
            profile.BaseDropChance = 0.75f;
            profile.MinimumDropChance = 0.5f;
            DevelopmentTuningService.NotifyChanged();

            DevelopmentTuningService.ResetToDefaults();

            var resolved = DevelopmentTuningResolver.ResolveLootDefinition();
            var catalog = LootEffectCatalog.HealingEmbers;
            Assert.That(resolved.BaseDropChance, Is.EqualTo(catalog.BaseDropChance).Within(0.0001f));
            Assert.That(resolved.MinimumDropChance, Is.EqualTo(catalog.MinimumDropChance).Within(0.0001f));
        }

        [Test]
        public void ResolveLootDefinition_AppliesPerLootOverrideWithoutAffectingOthers()
        {
            var profile = DevelopmentTuningService.Active;
            profile.LootOverrides = new[]
            {
                new LootOverrideEntry
                {
                    Id = LootEffectCatalog.CriticalFlare.Id,
                    OverrideEffectDuration = true,
                    EffectDuration = 12f,
                    OverrideEffectIntensity = true,
                    EffectIntensity = 0.4f
                }
            };
            DevelopmentTuningService.NotifyChanged();

            var critical = DevelopmentTuningResolver.ResolveLootDefinition(LootEffectCatalog.CriticalFlare);
            var healing = DevelopmentTuningResolver.ResolveLootDefinition(LootEffectCatalog.HealingEmbers);

            Assert.That(critical.EffectDuration, Is.EqualTo(12f).Within(0.0001f));
            Assert.That(critical.EffectIntensity, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(healing.EffectDuration, Is.EqualTo(LootEffectCatalog.HealingEmbers.EffectDuration).Within(0.0001f));
            Assert.That(healing.EffectIntensity, Is.EqualTo(LootEffectCatalog.HealingEmbers.EffectIntensity).Within(0.0001f));
        }

        [Test]
        public void Resolver_AppliesCharacterOverrideWithoutAffectingBaseCatalog()
        {
            var hero = ContentCatalog.FindCharacter("ravenbound.haldor");
            var baseHealth = hero.MaxHealth;
            var profile = DevelopmentTuningService.Active;
            profile.CharacterOverrides = new[]
            {
                new CharacterOverrideEntry
                {
                    Id = hero.Id,
                    OverrideMaxHealth = true,
                    MaxHealth = baseHealth + 42f
                }
            };
            DevelopmentTuningService.NotifyChanged();

            var resolved = DevelopmentTuningResolver.ResolveCharacter(hero);
            Assert.That(resolved.MaxHealth, Is.EqualTo(baseHealth + 42f).Within(0.0001f));
            Assert.That(ContentCatalog.FindCharacter(hero.Id).MaxHealth, Is.EqualTo(baseHealth).Within(0.0001f));
        }

        [Test]
        public void SaveAndLoad_PersistsOverridesSeparatelyFromSaveService()
        {
            SaveService.Load();
            var renownBefore = SaveService.Data.TotalRenown;
            var profile = DevelopmentTuningService.Active;
            profile.BaseDropChance = 0.33f;
            DevelopmentTuningService.Save();
            DevelopmentTuningService.Load();

            Assert.That(DevelopmentTuningService.Active.BaseDropChance, Is.EqualTo(0.33f).Within(0.0001f));
            Assert.That(SaveService.Data.TotalRenown, Is.EqualTo(renownBefore));
        }

        [Test]
        public void WeaponOverride_ChangesResolvedProfile()
        {
            WeaponProfile.TryGetBase("weapon.frost_axe", out var baseProfile);
            var profile = DevelopmentTuningService.Active;
            profile.WeaponOverrides = new[]
            {
                new WeaponOverrideEntry
                {
                    Id = baseProfile.Id,
                    OverrideBaseDamage = true,
                    BaseDamage = baseProfile.BaseDamage + 15f
                }
            };
            DevelopmentTuningService.NotifyChanged();

            Assert.That(DevelopmentTuningResolver.TryResolveWeapon(baseProfile.Id, out var resolved), Is.True);
            Assert.That(resolved.BaseDamage, Is.EqualTo(baseProfile.BaseDamage + 15f).Within(0.0001f));
            WeaponProfile.TryGetBase(baseProfile.Id, out var unchanged);
            Assert.That(unchanged.BaseDamage, Is.EqualTo(baseProfile.BaseDamage).Within(0.0001f));
        }

        [Test]
        public void SerializeActiveProfile_IncludesAdjustedValues()
        {
            DevelopmentTuningService.Active.BaseDropChance = 0.33f;
            DevelopmentTuningService.NotifyChanged();

            var json = DevelopmentTuningService.SerializeActiveProfile();

            Assert.That(json, Does.Contain("\"BaseDropChance\": 0.33"));
        }

        [Test]
        public void ExportToClipboard_DoesNotChangeActiveProfile()
        {
            DevelopmentTuningService.Active.RequiredCount = 17;
            DevelopmentTuningService.NotifyChanged();

            Assert.That(DevelopmentTuningService.ExportToClipboard(), Is.True);
            Assert.That(DevelopmentTuningService.Active.RequiredCount, Is.EqualTo(17));
        }

        [Test]
        public void ForceLootDropChance_CausesTryRollDropToAlwaysSucceed()
        {
            var progress = new SharedLootProgressModel();
            progress.Begin();
            var random = new RunRandom(42);
            DevelopmentTuningService.Active.ForceLootDropChance = true;
            DevelopmentTuningService.NotifyChanged();
            progress.Begin();

            Assert.That(progress.TryRollDrop(12, 500, 1, random, out _), Is.True);
        }
    }
}
