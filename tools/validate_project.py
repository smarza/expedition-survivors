#!/usr/bin/env python3
"""Fast repository checks that do not require a Unity installation."""

from __future__ import annotations

import json
import re
import struct
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    print(f"ERROR: {message}")
    raise SystemExit(1)


def strip_csharp_literals(source: str) -> str:
    source = re.sub(r"//.*?$", "", source, flags=re.MULTILINE)
    source = re.sub(r"/\*.*?\*/", "", source, flags=re.DOTALL)
    source = re.sub(r'@"(?:""|[^"])*"', '""', source)
    source = re.sub(r'\$?"(?:\\.|[^"\\])*"', '""', source)
    source = re.sub(r"'(?:\\.|[^'\\])'", "''", source)
    return source


def validate_csharp(path: Path) -> None:
    raw = path.read_text(encoding="utf-8")
    clean = strip_csharp_literals(raw)
    if path.name != "AssemblyInfo.cs" and "namespace ProjectExpedition" not in clean:
        fail(f"{path.relative_to(ROOT)} is outside the project namespace")
    for opening, closing in [("{", "}"), ("(", ")"), ("[", "]")]:
        depth = 0
        for char in clean:
            if char == opening:
                depth += 1
            elif char == closing:
                depth -= 1
            if depth < 0:
                fail(f"{path.relative_to(ROOT)} has an early {closing}")
        if depth != 0:
            fail(f"{path.relative_to(ROOT)} has unbalanced {opening}{closing}")
    if re.search(r"\b(TODO|FIXME|NotImplementedException)\b", raw):
        fail(f"{path.relative_to(ROOT)} contains unfinished implementation markers")
    obsolete_unity_65_apis = ("GetInstanceID(", "FindFirstObjectByType", "FindObjectsSortMode")
    obsolete = [token for token in obsolete_unity_65_apis if token in clean]
    if obsolete:
        fail(f"{path.relative_to(ROOT)} uses Unity 6000.5 obsolete APIs: " + ", ".join(obsolete))


def main() -> int:
    required = [
        ROOT / "Assets/Scenes/Bootstrap.unity",
        ROOT / "Assets/Scripts/Runtime/GameBootstrap.cs",
        ROOT / "Assets/Scripts/Runtime/GameDirector.cs",
        ROOT / "Assets/Scripts/Runtime/GameHUD.cs",
        ROOT / "Assets/Scripts/Runtime/ContentDefinitions.cs",
        ROOT / "Assets/Scripts/Runtime/BuildSystem.cs",
        ROOT / "Assets/Scripts/Runtime/ContentAssets.cs",
        ROOT / "Assets/Scripts/Runtime/ProductionContentDatabase.cs",
        ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs",
        ROOT / "Assets/Scripts/Runtime/SharedRunModel.cs",
        ROOT / "Assets/Scripts/Runtime/SharedProjectileModel.cs",
        ROOT / "Assets/Scripts/Runtime/SharedPlayerModel.cs",
        ROOT / "Assets/Scripts/Runtime/SharedEnemyModel.cs",
        ROOT / "Assets/Scripts/Runtime/SharedSpawnModel.cs",
        ROOT / "Assets/Scripts/Runtime/SharedEffectModel.cs",
        ROOT / "Assets/Scripts/Runtime/ProjectExpedition.Runtime.asmdef",
        ROOT / "Assets/Scripts/Runtime/LocalInputRouter.cs",
        ROOT / "Assets/Scripts/Runtime/PresentationSettings.cs",
        ROOT / "Assets/Scripts/Runtime/PresentationServices.cs",
        ROOT / "Assets/Scripts/Runtime/HeroPresentation.cs",
        ROOT / "Assets/Tests/EditMode/ProjectExpedition.EditModeTests.asmdef",
        ROOT / "Assets/Tests/EditMode/DeterministicFoundationTests.cs",
        ROOT / "Assets/Tests/EditMode/BuildAndRewardTests.cs",
        ROOT / "Assets/Tests/EditMode/SaveMigrationTests.cs",
        ROOT / "Assets/Tests/EditMode/SharedRunModelTests.cs",
        ROOT / "Assets/Tests/EditMode/SharedPlayerModelTests.cs",
        ROOT / "Assets/Tests/EditMode/SharedEnemyModelTests.cs",
        ROOT / "Assets/Tests/EditMode/SharedSpawnModelTests.cs",
        ROOT / "Assets/Tests/EditMode/SharedEffectModelTests.cs",
        ROOT / "Assets/Tests/EditMode/PresentationFoundationTests.cs",
        ROOT / "Assets/Tests/Shared/ProjectExpedition.TestSupport.asmdef",
        ROOT / "Assets/Tests/Shared/PoolProbe.cs",
        ROOT / "Assets/Tests/PlayMode/ProjectExpedition.PlayModeTests.asmdef",
        ROOT / "Assets/Tests/PlayMode/ExpeditionFlowPlayModeTests.cs",
        ROOT / "Assets/Resources/Art/Haldor_Stormborn_KeyArt.png",
        ROOT / "Assets/Resources/Content/ProductionContent.asset",
        ROOT / "docs/ONLINE_EXPEDITION.md",
        ROOT / "docs/BUILD_AND_REWARD_0.6.md",
        ROOT / "docs/BUILD_AND_CONTENT_REFERENCE.md",
        ROOT / "docs/PRODUCTION_FOUNDATION_0.7.md",
        ROOT / "docs/PROJECT_MASTER_PLAN.md",
        ROOT / "docs/TESTING_0.8.md",
        ROOT / "docs/CONTINUOUS_INTEGRATION.md",
        ROOT / "docs/PRESENTATION_FOUNDATION_0.9.md",
        ROOT / "docs/TESTING_0.9.md",
        ROOT / ".github/workflows/unity-ci.yml",
        ROOT / "Packages/manifest.json",
        ROOT / "ProjectSettings/ProjectSettings.asset",
        ROOT / "ProjectSettings/ProjectVersion.txt",
    ]
    missing = [str(path.relative_to(ROOT)) for path in required if not path.is_file()]
    if missing:
        fail("missing required files: " + ", ".join(missing))

    manifest = json.loads((ROOT / "Packages/manifest.json").read_text(encoding="utf-8"))
    if "dependencies" not in manifest:
        fail("Packages/manifest.json has no dependencies object")
    if "com.unity.inputsystem" not in manifest["dependencies"]:
        fail("Unity Input System package is required by the co-op input router")
    if "com.unity.netcode.gameobjects" in manifest["dependencies"]:
        fail("Online Co-op is deferred; Netcode for GameObjects must not ship in the active runtime")
    if manifest["dependencies"].get("com.unity.test-framework") != "1.4.6":
        fail("Unity Test Framework 1.4.6 is required by the 0.8 automated suites")
    expected_unity_65_packages = {
        "com.unity.ide.rider": "3.0.40",
        "com.unity.ide.visualstudio": "2.0.27",
        "com.unity.inputsystem": "1.19.0",
    }
    mismatched_packages = [
        f"{package}={manifest['dependencies'].get(package)} (expected {version})"
        for package, version in expected_unity_65_packages.items()
        if manifest["dependencies"].get(package) != version
    ]
    if mismatched_packages:
        fail("Unity 6000.5 package matrix mismatch: " + ", ".join(mismatched_packages))
    for core_package in ("com.unity.collections", "com.unity.transport"):
        if core_package in manifest["dependencies"]:
            fail(f"{core_package} is a Unity 6000.5 core/transitive package and must not be pinned directly")
    for module in ("com.unity.modules.animation", "com.unity.modules.physics", "com.unity.modules.physics2d"):
        if module not in manifest["dependencies"]:
            fail(f"Unity runtime module is missing: {module}")
    project_settings = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8")
    if "activeInputHandler: 1" not in project_settings and "activeInputHandler: 2" not in project_settings:
        fail("Project Settings must enable the Unity Input System")
    if "m_BuildTarget: Standalone" not in project_settings or "m_DynamicBatching: 0" not in project_settings:
        fail("Dynamic Batching is deprecated in Unity 6000.5 and must be disabled for Standalone")
    if "webGLDecompressionFallback: 1" not in project_settings:
        fail("Web decompression fallback is required for static GitHub Pages hosting")

    project_version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
    editor_version = re.search(r"m_EditorVersion:\s*(\d+)\.(\d+)\.(\d+)f(\d+)", project_version)
    if not editor_version:
        fail("ProjectVersion.txt does not contain a supported final Unity Editor version")
    editor_tuple = tuple(int(part) for part in editor_version.groups())
    if editor_tuple < (6000, 0, 58, 2):
        fail("Unity Editor version is vulnerable to CVE-2025-59489; use 6000.0.58f2 or newer")

    scene_meta = (ROOT / "Assets/Scenes/Bootstrap.unity.meta").read_text(encoding="utf-8")
    build_settings = (ROOT / "ProjectSettings/EditorBuildSettings.asset").read_text(encoding="utf-8")
    match = re.search(r"^guid:\s*([a-f0-9]{32})$", scene_meta, flags=re.MULTILINE)
    if not match or match.group(1) not in build_settings:
        fail("bootstrap scene GUID does not match build settings")

    scripts = sorted((ROOT / "Assets").rglob("*.cs"))
    if len(scripts) < 8:
        fail("unexpectedly small runtime script set")
    for script in scripts:
        validate_csharp(script)

    runtime_assembly = json.loads((ROOT / "Assets/Scripts/Runtime/ProjectExpedition.Runtime.asmdef").read_text(encoding="utf-8"))
    runtime_references = set(runtime_assembly.get("references", []))
    required_runtime_references = {"Unity.InputSystem"}
    if runtime_assembly.get("name") != "ProjectExpedition.Runtime" or not required_runtime_references.issubset(runtime_references):
        fail("runtime assembly definition is missing its production package references")
    if any(reference.startswith("Unity.Netcode") or reference == "Unity.Networking.Transport" for reference in runtime_references):
        fail("deferred Online assemblies must not be referenced by the active runtime")

    edit_assembly = json.loads((ROOT / "Assets/Tests/EditMode/ProjectExpedition.EditModeTests.asmdef").read_text(encoding="utf-8"))
    edit_references = set(edit_assembly.get("references", []))
    if "Editor" not in edit_assembly.get("includePlatforms", []) or not {
        "ProjectExpedition.Runtime", "ProjectExpedition.TestSupport"
    }.issubset(edit_references):
        fail("EditMode test assembly must be Editor-only and reference runtime plus player-compatible test support")

    support_assembly = json.loads((ROOT / "Assets/Tests/Shared/ProjectExpedition.TestSupport.asmdef").read_text(encoding="utf-8"))
    if support_assembly.get("includePlatforms") or "ProjectExpedition.Runtime" not in support_assembly.get("references", []):
        fail("test support must remain player-compatible and reference the runtime assembly")

    play_assembly = json.loads((ROOT / "Assets/Tests/PlayMode/ProjectExpedition.PlayModeTests.asmdef").read_text(encoding="utf-8"))
    if play_assembly.get("includePlatforms") or "ProjectExpedition.Runtime" not in play_assembly.get("references", []):
        fail("PlayMode test assembly must reference the runtime assembly and remain player-compatible")

    edit_tests = "\n".join(path.read_text(encoding="utf-8")
                           for path in sorted((ROOT / "Assets/Tests/EditMode").glob("*.cs")))
    edit_test_requirements = ("RunRandom_SameSeedProducesSameSequence", "SpatialGrid_UpdatesAndRemovesMembership",
                              "ComponentPool_ReusesReleasedInstances", "PlayerBuild_EvolutionRequiresMaximumLevelAndCatalyst",
                              "RewardFactory_SameSeedProducesSameRecipientsAndItems", "LegacyV1Save_MigratesWithoutProgressLoss",
                              "UpgradeDescriptions_MatchExactWeaponLevelEffects",
                              "Begin_InitializesDeterministicProgressionState", "Advance_TriggersBossExactlyOnceAtConfiguredTime",
                              "AddExperience_CarriesOverflowAndWaitsForRewardResolution", "RewardTurn_AlternatesAcrossTwoPlayers",
                              "Complete_IsIdempotentAndResetReturnsToIdle",
                              "SharedProjectile_TravelsAndConsumesTheSamePierceBudgetForEveryAdapter",
                              "Begin_InitializesCharacterStatisticsAndPartialUltimateCharge",
                              "AdvanceAndMovement_UseTheSameTimingAndStatisticsForEveryAdapter",
                              "Ultimate_RequiresChargeAndStartsCooldownAndInvulnerability",
                              "Damage_AppliesArmorMinimumDamageAndKnockdownExactlyOnce",
                              "Revival_RequiresNearbyRescuerAndRestoresProtectedHealth",
                              "Upgrades_UpdateDerivedStatisticsAndPreserveUltimateChargeRatio",
                              "Begin_DerivesStatisticsFromDefinitionDifficultyAndExplicitRolls",
                              "AdvanceTowards_MovesAndAppliesTheSharedContactInterval",
                              "TakeDamage_AppliesKnockbackAndReportsDeathExactlyOnce",
                              "Boss_UsesTheSameDifficultyScalingAndStateBoundary",
                              "SameCommands_ProduceIdenticalEnemyStateForEveryAdapter",
                              "Advance_UsesInitialDelayAndMapDifficultyRamp",
                              "Advance_GrowsGroupsAndClampsSpawnInterval",
                              "Advance_RespectsActiveCapButNeverSuppressesBoss",
                              "CalculateSpawnPosition_UsesSharedRingBounds",
                              "Advance_PreservesAutomaticWeaponCadence",
                              "UpgradesAndEvolutions_PreserveExistingBalanceValues",
                              "FrostAxeLevelTable_ReachesTwoProjectilesAtMaximumLevel",
                              "RavenGuardLevelTable_PreservesDamageAndAddsPromisedFrequency",
                              "AxeVolley_ProducesSharedProjectileEffectsAndDirections",
                              "RavenGuard_ProducesSharedAreaEffectAndBoundedHealing",
                              "EffectPipeline_AppliesPlayerWeaponAndEvolutionState",
                              "UltimateAndEvolutionExplosion_UseSharedAreaRequests",
                              "Preferences_RoundTripAccessibilityAudioAndBindings",
                              "Preferences_ClampUnsafePresentationValues",
                              "Layout_PreservesReferenceAspectInsideDesktopAndSteamDeckSafeAreas",
                              "Glyphs_ExposeKeyboardAndControllerSpecificPrompts",
                              "AudioMix_UsesMasterBusAndProtectsImportantVoices",
                              "MusicRouting_FollowsMenuRunBossRewardAndResultStates")
    if any(requirement not in edit_tests for requirement in edit_test_requirements):
        fail("EditMode deterministic foundation coverage is incomplete")

    play_tests = (ROOT / "Assets/Tests/PlayMode/ExpeditionFlowPlayModeTests.cs").read_text(encoding="utf-8")
    play_test_requirements = ("GameDirector_InitializesProductionFoundation",
                              "SoloRun_LevelUpOffersFourRewardsAndResumes", "ReplayRun_PreservesSeedAndRestartsProgress",
                              "RunOutcome_TransitionsOnceWithoutTouchingDisk",
                              "PlayerController_ProjectsSharedPlayerStateAndUpgrades",
                              "EnemyAdapter_ProjectsSharedEnemyStateAndDamage", "RunSimulationPhase.Reward",
                              "RunSimulationPhase.Completed",
                              "PresentationFoundation_InitializesAndFollowsRunState",
                              "PresentationVfx_ReusesPoolAndSettingsReturnToTheirOwnerState")
    if any(requirement not in play_tests for requirement in play_test_requirements):
        fail("PlayMode expedition flow coverage is incomplete")

    axe_source = (ROOT / "Assets/Scripts/Runtime/AxeProjectile.cs").read_text(encoding="utf-8")
    if "SharedProjectileModel" not in axe_source:
        fail("Local Frost Axe must use the shared travelling-projectile model")

    player_source = (ROOT / "Assets/Scripts/Runtime/PlayerController.cs").read_text(encoding="utf-8")
    shared_player_source = (ROOT / "Assets/Scripts/Runtime/SharedPlayerModel.cs").read_text(encoding="utf-8")
    shared_player_requirements = (
        "class SharedPlayerModel",
        "PlayerDamageResult TakeDamage",
        "bool AdvanceRevival",
        "Vector2 CalculateRequestedPosition",
        "bool TryActivateUltimate",
    )
    if any(requirement not in shared_player_source for requirement in shared_player_requirements) or any(
        requirement not in player_source for requirement in (
            "readonly SharedPlayerModel _model", "_model.TakeDamage", "_model.AdvanceRevival",
            "_model.CalculateRequestedPosition", "_model.TryActivateUltimate")):
        fail("PlayerController must project the shared player state instead of owning duplicate player rules")

    enemy_source = (ROOT / "Assets/Scripts/Runtime/Enemy.cs").read_text(encoding="utf-8")
    shared_enemy_source = (ROOT / "Assets/Scripts/Runtime/SharedEnemyModel.cs").read_text(encoding="utf-8")
    shared_spawn_source = (ROOT / "Assets/Scripts/Runtime/SharedSpawnModel.cs").read_text(encoding="utf-8")
    shared_effect_source = (ROOT / "Assets/Scripts/Runtime/SharedEffectModel.cs").read_text(encoding="utf-8")
    build_source = (ROOT / "Assets/Scripts/Runtime/BuildSystem.cs").read_text(encoding="utf-8")
    shared_enemy_requirements = (
        "class SharedEnemyModel",
        "EnemyAdvanceResult AdvanceTowards",
        "EnemyDamageResult TakeDamage",
        "ContactCooldownRemaining",
        "EnemyDefinition definition",
    )
    if any(requirement not in shared_enemy_source for requirement in shared_enemy_requirements) or any(
        requirement not in enemy_source for requirement in (
            "readonly SharedEnemyModel _model", "_model.Begin", "_model.AdvanceTowards",
            "_model.TakeDamage", "_model.Stop")):
        fail("Enemy must project shared enemy state instead of owning duplicate movement and combat rules")
    if any(requirement not in shared_spawn_source for requirement in (
        "class SharedSpawnModel", "SpawnAdvanceResult Advance", "CalculateSpawnPosition")) or any(
        requirement not in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8")
        for requirement in ("_spawnModel.Advance", "_spawnModel.Begin", "SharedSpawnModel.CalculateSpawnPosition")):
        fail("GameDirector must project the shared spawn scheduler instead of owning wave formulas")
    weapon_source = (ROOT / "Assets/Scripts/Runtime/WeaponSystem.cs").read_text(encoding="utf-8")
    if any(requirement not in shared_effect_source for requirement in (
        "class SharedWeaponModel", "struct SharedEffectRequest", "class SharedEffectPipeline",
        "WeaponAdvanceResult Advance", "CreateUltimate", "CreateJotunnCleaverExplosion")) or any(
        requirement not in weapon_source for requirement in (
            "internal SharedWeaponModel Model", "Model.Advance", "Model.CreateAxeEffect",
            "Model.CreateRavenGuardEffect")):
        fail("WeaponSystem must adapt the shared weapon/effect pipeline instead of owning combat rules")
    if "switch (effect)" in build_source or "player.Weapons.Apply" in build_source:
        fail("RewardEffects must route upgrades through the shared effect pipeline")
    play_mode_source = (ROOT / "Assets/Tests/PlayMode/ExpeditionFlowPlayModeTests.cs").read_text(encoding="utf-8")
    if "RewardPipeline_ProjectsSharedWeaponUpgrade" not in play_mode_source:
        fail("PlayMode tests must cover the reward-to-shared-weapon adapter boundary")

    content_source = (ROOT / "Assets/Scripts/Runtime/ContentDefinitions.cs").read_text(encoding="utf-8")
    input_source = (ROOT / "Assets/Scripts/Runtime/LocalInputRouter.cs").read_text(encoding="utf-8")
    hud_source = (ROOT / "Assets/Scripts/Runtime/GameHUD.cs").read_text(encoding="utf-8")
    game_types_source = (ROOT / "Assets/Scripts/Runtime/GameTypes.cs").read_text(encoding="utf-8")
    database_source = (ROOT / "Assets/Scripts/Runtime/ProductionContentDatabase.cs").read_text(encoding="utf-8")
    online_runtime = ROOT / "Assets/Scripts/Runtime/OnlineCoopSpike.cs"
    if online_runtime.exists() or "OnlineSpike" in game_types_source or "ONLINE CO-OP" in hud_source:
        fail("Online Co-op is deferred; its duplicate runtime, state and menu entry must remain outside the active product")
    if "Wrap(_mainSelection + direction, 3)" not in hud_source or '"SETTINGS"' not in hud_source:
        fail("the active main menu must expose Solo, Local Co-op and presentation settings")

    workflow_source = (ROOT / ".github/workflows/unity-ci.yml").read_text(encoding="utf-8")
    workflow_requirements = (
        "game-ci/unity-test-runner@",
        "testMode: All",
        "game-ci/unity-builder@",
        "targetPlatform: WebGL",
        "actions/upload-pages-artifact@",
        "actions/deploy-pages@",
        "github-pages-preview",
        '      - "agent/**"',
        "targetPlatform: StandaloneWindows64",
        "inputs.build_windows",
        "github.event.head_commit.message",
        "python3 tools/validate_project.py",
        "UNITY_LICENSE:",
        "UNITY_SERIAL:",
        "cancel-in-progress: true",
    )
    if any(requirement not in workflow_source for requirement in workflow_requirements):
        fail("Unity CI workflow is missing a required validation, test, build or license boundary")
    production_requirements = {
        "content catalog": "class CharacterDefinition" in content_source and "class MapDefinition" in content_source,
        "ultimate balance": "UltimateCooldown" in content_source and "Mathf.Max(28f" in content_source,
        "slower XP curve": "ExperienceToNext" in content_source and "1.35f" in content_source,
        "gamepad ownership": "AssignedDeviceIds" in input_source and "RefreshAssignments" in input_source,
        "gamepad menus": "AnyMenuSubmitPressed" in input_source and "MenuHorizontalPressed" in input_source,
        "selection flow": "DrawCharacterSelect" in hud_source and "DrawMapSelect" in hud_source,
        "four reward cards": "new List<RewardOption>(4)" in build_source and "CurrentRewards" in hud_source,
        "map build slots": "WeaponSlots" in content_source and "GearSlots" in content_source,
        "targeted co-op rewards": "TargetPlayerIndex" in build_source and "Shared" in build_source,
        "behavioral evolutions": "JotunnCleaver" in build_source and "StormAegis" in build_source,
        "build HUD": "DrawBuildTray" in hud_source and "DrawBuildDetails" in hud_source,
        "proportional UI canvas": "PresentationLayout.Calculate" in hud_source and "DrawLetterbox" in hud_source,
        "stable label hover": "SetAllTextColors" in hud_source and "style.hover.textColor = color" in hud_source,
        "opaque modal hierarchy": "case RunState.LevelUp: DrawLevelUp();" in hud_source and "case RunState.BuildDetails: DrawBuildDetails();" in hud_source,
        "readable item grid": "visibleIndex % 3" in hud_source and "row * 78" in hud_source,
        "complete build statistics": all(token in hud_source for token in (
            '"SURVIVOR"', '"FROST AXE"', '"RAVEN GUARD"', '"INTERVAL"',
            '"PROJECTILES"', '"ULT RADIUS"')),
        "exact reward preview": "RewardEffectPreview" in hud_source and
            "EffectDescriptionAtLevel" in hud_source,
        "fully clickable rewards": "GUI.Button(rect, GUIContent.none, GUIStyle.none)" in hud_source,
        "separated character controls": "var ultimateRect" in hud_source and "rect.y + 592" in hud_source,
        "responsive map titles": "_mapTitle" in hud_source and "rect.y + 190, rect.width - 80, 78" in hud_source,
        "compact combat hint": "Prompt(BindingAction.Ultimate)" in hud_source and "Prompt(BindingAction.Pause)" in hud_source,
        "aligned local statistics": "DrawStatColumn" in hud_source and "_statValue" in hud_source,
        "safe result summary": "var summary = new Rect" in hud_source and "_director.SelectedMap.Name.ToUpperInvariant()" in hud_source,
        "component pooling": "class ComponentPool" in (ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs").read_text(encoding="utf-8") and "ReleasePooledSimulation" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "spatial partitioning": "class SpatialHashGrid" in (ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs").read_text(encoding="utf-8") and "GetEnemiesInRadius" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "deterministic run seed": "class RunRandom" in (ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs").read_text(encoding="utf-8") and "ReplayRun" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "shared run progression": "class SharedRunModel" in (ROOT / "Assets/Scripts/Runtime/SharedRunModel.cs").read_text(encoding="utf-8") and "_runModel.AddExperience" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8") and "_runModel.TryTriggerBoss" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "shared player state": "class SharedPlayerModel" in shared_player_source and "readonly SharedPlayerModel _model" in player_source,
        "shared enemy state": "class SharedEnemyModel" in shared_enemy_source and "readonly SharedEnemyModel _model" in enemy_source,
        "shared spawning": "class SharedSpawnModel" in shared_spawn_source and "_spawnModel.Advance" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "shared effects": "class SharedEffectPipeline" in shared_effect_source and "Model.CreateRavenGuardEffect" in weapon_source,
        "scriptable content database": "class ProductionContentDatabase : ScriptableObject" in database_source and "Resources.Load<ProductionContentDatabase>" in (ROOT / "Assets/Scripts/Runtime/ContentAssets.cs").read_text(encoding="utf-8"),
        "gamepad movement deadzone": "MovementDeadzone" in input_source and "ApplyMovementDeadzone" in input_source,
        "visible replay confirmation": "REPLAYING SEED" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "runtime foundation checks": "ProductionFoundationChecks.Run" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "performance metrics": "DrawPerformancePanel" in hud_source and "MetricsPressed" in input_source,
        "presentation preferences": "PresentationPreferences.Data" in hud_source and "PresentationPreferences.Save" in input_source,
        "keyboard rebinding": "PollRebind" in input_source and "RebindableActions" in hud_source,
        "active device glyphs": "InputGlyphs.Prompt" in hud_source and "CurrentPromptDevice" in input_source,
        "audio buses and priorities": "class PresentationAudioMixer" in (ROOT / "Assets/Scripts/Runtime/PresentationServices.cs").read_text(encoding="utf-8") and "PresentationMix.Priority" in (ROOT / "Assets/Scripts/Runtime/PresentationServices.cs").read_text(encoding="utf-8"),
        "pooled presentation effects": "class PresentationVfxPool" in (ROOT / "Assets/Scripts/Runtime/PresentationServices.cs").read_text(encoding="utf-8") and "ComponentPool<PresentationBurst>" in (ROOT / "Assets/Scripts/Runtime/PresentationServices.cs").read_text(encoding="utf-8"),
        "hero presentation": "class HeroPresentation" in (ROOT / "Assets/Scripts/Runtime/HeroPresentation.cs").read_text(encoding="utf-8") and "BuildHaldorSilhouette" in (ROOT / "Assets/Scripts/Runtime/HeroPresentation.cs").read_text(encoding="utf-8"),
        "deterministic ambience": "class FrostboundAmbience" in (ROOT / "Assets/Scripts/Runtime/PresentationServices.cs").read_text(encoding="utf-8") and "Hash01" in (ROOT / "Assets/Scripts/Runtime/PresentationServices.cs").read_text(encoding="utf-8"),
    }
    incomplete = [name for name, passed in production_requirements.items() if not passed]
    if incomplete:
        fail("production core is incomplete: " + ", ".join(incomplete))

    content_asset = (ROOT / "Assets/Resources/Content/ProductionContent.asset").read_text(encoding="utf-8")
    content_reference = (ROOT / "docs/BUILD_AND_CONTENT_REFERENCE.md").read_text(encoding="utf-8")
    reference_requirements = (
        "The most important level rule", "Haldor Stormborn", "Eira Raven-Sworn",
        "Frost Axe", "Raven Guard", "Jotunn Cleaver", "Storm Aegis",
        "Reward cards show the exact next-level effect", "Required template for future content",
    )
    if any(requirement not in content_reference for requirement in reference_requirements):
        fail("build and content reference is missing required player or authoring rules")
    weapon_level_tables = {
        "weapon.frost_axe": [0, 1, 2, 4, 1, 3, 2, 1],
        "weapon.raven_guard": [0, 10, 7, 10, 15, 7, 10, 15],
    }
    for item_id, expected_effects in weapon_level_tables.items():
        record = re.search(
            rf"- id: {re.escape(item_id)}\n.*?levelEffects:\n((?:\s+- \d+\n)+)",
            content_asset,
            flags=re.DOTALL,
        )
        effects = [int(value) for value in re.findall(r"- (\d+)", record.group(1))] if record else []
        if effects != expected_effects:
            fail(f"{item_id} production level table differs from its tested 0.8.0 contract")
    if "Heal,\n        ShieldDamageAndSpeed" not in game_types_source:
        fail("new serialized UpgradeId values must be appended without renumbering existing content")
    content_ids = re.findall(r"^\s+- id:\s*([^\s]+)\s*$", content_asset, flags=re.MULTILINE)
    required_content_ids = {
        "ravenbound.haldor", "ravenbound.eira", "frostbound.scout", "frostbound.saga",
        "weapon.frost_axe", "weapon.raven_guard", "evolution.jotunn_cleaver",
        "evolution.storm_aegis", "boon.field_rations", "enemy.draugr_raider",
        "enemy.jotunn_warlord",
    }
    if len(content_ids) != len(set(content_ids)):
        fail("ProductionContent.asset contains duplicate stable IDs")
    if not required_content_ids.issubset(content_ids):
        fail("ProductionContent.asset is missing required production content IDs")

    foundation_meta = (ROOT / "Assets/Scripts/Runtime/ProductionContentDatabase.cs.meta").read_text(encoding="utf-8")
    content_guid = re.search(r"^guid:\s*([a-f0-9]{32})$", foundation_meta, flags=re.MULTILINE)
    if not content_guid or content_guid.group(1) not in content_asset:
        fail("ProductionContent.asset does not reference the ProductionContentDatabase MonoScript")

    steady_state_sources = [
        ROOT / "Assets/Scripts/Runtime/Enemy.cs",
        ROOT / "Assets/Scripts/Runtime/AxeProjectile.cs",
        ROOT / "Assets/Scripts/Runtime/ExperienceGem.cs",
        ROOT / "Assets/Scripts/Runtime/WeaponSystem.cs",
    ]
    for source_path in steady_state_sources:
        source = source_path.read_text(encoding="utf-8")
        if "Destroy(gameObject)" in source:
            fail(f"{source_path.name} still destroys steady-state combat objects")
    weapon_source = (ROOT / "Assets/Scripts/Runtime/WeaponSystem.cs").read_text(encoding="utf-8")
    if "new GameObject" in weapon_source:
        fail("WeaponSystem still allocates projectile GameObjects during combat")

    art = ROOT / "Assets/Resources/Art/Haldor_Stormborn_KeyArt.png"
    header = art.read_bytes()[:24]
    if len(header) < 24 or header[:8] != b"\x89PNG\r\n\x1a\n" or header[12:16] != b"IHDR":
        fail("Haldor key art must be a valid PNG")
    width, height = struct.unpack(">II", header[16:24])
    if width < 1024 or height < 1024:
        fail("Haldor key art must be at least 1024×1024")

    print(f"OK: {len(scripts)} C# files, bootstrap scene, package manifest and Haldor key art validated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
