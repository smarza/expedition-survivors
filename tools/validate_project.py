#!/usr/bin/env python3
"""Fast repository checks that do not require a Unity installation."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

from PIL import Image


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
        ROOT / "Assets/Scripts/Runtime/ProjectExpedition.Runtime.asmdef",
        ROOT / "Assets/Scripts/Runtime/LocalInputRouter.cs",
        ROOT / "Assets/Scripts/Runtime/OnlineCoopSpike.cs",
        ROOT / "Assets/Tests/EditMode/ProjectExpedition.EditModeTests.asmdef",
        ROOT / "Assets/Tests/EditMode/DeterministicFoundationTests.cs",
        ROOT / "Assets/Tests/EditMode/BuildAndRewardTests.cs",
        ROOT / "Assets/Tests/EditMode/SaveMigrationTests.cs",
        ROOT / "Assets/Tests/EditMode/SharedRunModelTests.cs",
        ROOT / "Assets/Tests/Shared/ProjectExpedition.TestSupport.asmdef",
        ROOT / "Assets/Tests/Shared/PoolProbe.cs",
        ROOT / "Assets/Tests/PlayMode/ProjectExpedition.PlayModeTests.asmdef",
        ROOT / "Assets/Tests/PlayMode/ExpeditionFlowPlayModeTests.cs",
        ROOT / "Assets/Resources/Art/Haldor_Stormborn_KeyArt.png",
        ROOT / "Assets/Resources/Content/ProductionContent.asset",
        ROOT / "docs/ONLINE_EXPEDITION.md",
        ROOT / "docs/BUILD_AND_REWARD_0.6.md",
        ROOT / "docs/PRODUCTION_FOUNDATION_0.7.md",
        ROOT / "docs/PROJECT_MASTER_PLAN.md",
        ROOT / "docs/TESTING_0.8.md",
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
    if "com.unity.netcode.gameobjects" not in manifest["dependencies"]:
        fail("Netcode for GameObjects is required by the online spike")
    if manifest["dependencies"].get("com.unity.test-framework") != "1.4.6":
        fail("Unity Test Framework 1.4.6 is required by the 0.8 automated suites")
    expected_unity_65_packages = {
        "com.unity.ide.rider": "3.0.40",
        "com.unity.ide.visualstudio": "2.0.27",
        "com.unity.inputsystem": "1.19.0",
        "com.unity.netcode.gameobjects": "2.13.0",
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
            fail(f"Netcode compile dependency is missing: {module}")
    project_settings = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8")
    if "activeInputHandler: 1" not in project_settings and "activeInputHandler: 2" not in project_settings:
        fail("Project Settings must enable the Unity Input System")
    if "m_BuildTarget: Standalone" not in project_settings or "m_DynamicBatching: 0" not in project_settings:
        fail("Dynamic Batching is deprecated in Unity 6000.5 and must be disabled for Standalone")

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
    required_runtime_references = {
        "Unity.Collections",
        "Unity.InputSystem",
        "Unity.Netcode.Runtime",
        "Unity.Networking.Transport",
    }
    if runtime_assembly.get("name") != "ProjectExpedition.Runtime" or not required_runtime_references.issubset(runtime_references):
        fail("runtime assembly definition is missing its production package references")

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

    edit_tests = "\n".join((ROOT / "Assets/Tests/EditMode" / name).read_text(encoding="utf-8") for name in (
        "DeterministicFoundationTests.cs", "BuildAndRewardTests.cs", "SaveMigrationTests.cs", "SharedRunModelTests.cs"))
    edit_test_requirements = ("RunRandom_SameSeedProducesSameSequence", "SpatialGrid_UpdatesAndRemovesMembership",
                              "ComponentPool_ReusesReleasedInstances", "PlayerBuild_EvolutionRequiresMaximumLevelAndCatalyst",
                              "RewardFactory_SameSeedProducesSameRecipientsAndItems", "LegacyV1Save_MigratesWithoutProgressLoss",
                              "Begin_InitializesDeterministicProgressionState", "Advance_TriggersBossExactlyOnceAtConfiguredTime",
                              "AddExperience_CarriesOverflowAndWaitsForRewardResolution", "RewardTurn_AlternatesAcrossTwoPlayers",
                              "Complete_IsIdempotentAndResetReturnsToIdle",
                              "OnlinePhaseProjection_PreservesSnapshotWireValues")
    if any(requirement not in edit_tests for requirement in edit_test_requirements):
        fail("EditMode deterministic foundation coverage is incomplete")

    play_tests = (ROOT / "Assets/Tests/PlayMode/ExpeditionFlowPlayModeTests.cs").read_text(encoding="utf-8")
    play_test_requirements = ("GameDirector_InitializesProductionFoundation",
                              "SoloRun_LevelUpOffersFourRewardsAndResumes", "ReplayRun_PreservesSeedAndRestartsProgress",
                              "RunOutcome_TransitionsOnceWithoutTouchingDisk", "RunSimulationPhase.Reward",
                              "RunSimulationPhase.Completed")
    if any(requirement not in play_tests for requirement in play_test_requirements):
        fail("PlayMode expedition flow coverage is incomplete")

    online_source = (ROOT / "Assets/Scripts/Runtime/OnlineCoopSpike.cs").read_text(encoding="utf-8")
    if "_networkObject.transform.SetParent" in online_source:
        fail("NetworkManager must be created on a root GameObject")
    if "NetworkConfig = new NetworkConfig()" not in online_source:
        fail("Runtime NetworkManager must initialize NetworkConfig explicitly")
    if "ConnectionApproval = true" not in online_source or "ConnectedClientsIds.Count < 2" not in online_source:
        fail("Online POC must enforce its two-player connection limit")
    if "Destroy(managerObject, 0.5f)" not in online_source:
        fail("Network transport must receive a graceful shutdown window")
    online_requirements = [
        "OnlinePhase.LevelUp",
        "BeginExpedition()",
        "SimulateRun(Time.unscaledDeltaTime)",
        "DamageEnemiesInRadius",
        "OfferLevelUp()",
        "ResolveUpgrade",
        "BossSpawnTime",
        "MaximumEnemies = 96",
        "enemy.Position.x * 100f",
        "PulseMessage",
    ]
    missing_online = [token for token in online_requirements if token not in online_source]
    if missing_online:
        fail("networked gameplay slice is incomplete: " + ", ".join(missing_online))

    online_shared_requirements = ("_onlineRunModel.Begin", "_onlineRunModel.Advance",
                                  "_onlineRunModel.AddExperience", "_onlineRunModel.CompleteReward",
                                  "_onlineRunModel.Complete", "SyncHostRunProjection")
    if any(requirement not in online_source for requirement in online_shared_requirements):
        fail("Online host does not project the shared run model completely")

    snapshot_start = online_source.index("private void SendSnapshot()")
    snapshot_end = online_source.index("private void ReceiveSnapshot", snapshot_start)
    snapshot_source = online_source[snapshot_start:snapshot_end]
    snapshot_wire_order = ("writer.WriteValueSafe((byte)_phase)", "writer.WriteValueSafe((byte)_mapIndex)",
                           "writer.WriteValueSafe(_elapsed)", "writer.WriteValueSafe(_level)",
                           "writer.WriteValueSafe(_experience)", "writer.WriteValueSafe(_experienceToNext)",
                           "writer.WriteValueSafe(_kills)", "writer.WriteValueSafe(_renown)",
                           "writer.WriteValueSafe(_bossSpawned)", "writer.WriteValueSafe((byte)_rewardTurnPlayerIndex)")
    cursor = -1
    for field in snapshot_wire_order:
        position = snapshot_source.find(field, cursor + 1)
        if position < 0:
            fail(f"snapshot v2 header field is missing or reordered: {field}")
        cursor = position

    content_source = (ROOT / "Assets/Scripts/Runtime/ContentDefinitions.cs").read_text(encoding="utf-8")
    input_source = (ROOT / "Assets/Scripts/Runtime/LocalInputRouter.cs").read_text(encoding="utf-8")
    hud_source = (ROOT / "Assets/Scripts/Runtime/GameHUD.cs").read_text(encoding="utf-8")
    build_source = (ROOT / "Assets/Scripts/Runtime/BuildSystem.cs").read_text(encoding="utf-8")
    database_source = (ROOT / "Assets/Scripts/Runtime/ProductionContentDatabase.cs").read_text(encoding="utf-8")
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
        "online build synchronization": "LoadSnapshot" in online_source and "_rewardTurnPlayerIndex" in online_source,
        "proportional UI canvas": "Mathf.Min(Screen.width / 1920f" in hud_source and "DrawLetterbox" in hud_source,
        "stable label hover": "SetAllTextColors" in hud_source and "style.hover.textColor = color" in hud_source,
        "opaque modal hierarchy": "case RunState.LevelUp: DrawLevelUp();" in hud_source and "case RunState.BuildDetails: DrawBuildDetails();" in hud_source,
        "readable item grid": "visibleIndex % 3" in hud_source and "row * 94" in hud_source,
        "fully clickable rewards": "GUI.Button(rect, GUIContent.none, GUIStyle.none)" in hud_source,
        "online UI parity": "DrawLetterbox" in online_source and "SetAllTextColors" in online_source,
        "separated character controls": "var ultimateRect" in hud_source and "rect.y + 592" in hud_source,
        "responsive map titles": "_mapTitle" in hud_source and "rect.y + 190, rect.width - 80, 78" in hud_source,
        "compact combat hint": "ULTIMATE  SPACE / RT" in hud_source and "PAUSE  ESC / START" in hud_source,
        "aligned local statistics": "DrawStatColumn" in hud_source and "_statValue" in hud_source,
        "safe result summary": "var summary = new Rect" in hud_source and "_director.SelectedMap.Name.ToUpperInvariant()" in hud_source,
        "aligned online statistics": "DrawOnlineStatColumn" in online_source and "statValue" in online_source,
        "component pooling": "class ComponentPool" in (ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs").read_text(encoding="utf-8") and "ReleasePooledSimulation" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "spatial partitioning": "class SpatialHashGrid" in (ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs").read_text(encoding="utf-8") and "GetEnemiesInRadius" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "deterministic run seed": "class RunRandom" in (ROOT / "Assets/Scripts/Runtime/ProductionFoundation.cs").read_text(encoding="utf-8") and "ReplayRun" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "shared run progression": "class SharedRunModel" in (ROOT / "Assets/Scripts/Runtime/SharedRunModel.cs").read_text(encoding="utf-8") and "_runModel.AddExperience" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8") and "_runModel.TryTriggerBoss" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "scriptable content database": "class ProductionContentDatabase : ScriptableObject" in database_source and "Resources.Load<ProductionContentDatabase>" in (ROOT / "Assets/Scripts/Runtime/ContentAssets.cs").read_text(encoding="utf-8"),
        "gamepad movement deadzone": "MovementDeadzone" in input_source and "ApplyMovementDeadzone" in input_source,
        "readable online lobby buttons": "var readable = new Color(0.88f, 0.94f, 0.96f)" in online_source and "var hostRect" in online_source,
        "visible replay confirmation": "REPLAYING SEED" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "runtime foundation checks": "ProductionFoundationChecks.Run" in (ROOT / "Assets/Scripts/Runtime/GameDirector.cs").read_text(encoding="utf-8"),
        "performance metrics": "DrawPerformancePanel" in hud_source and "MetricsPressed" in input_source,
        "online pooled views": "ComponentPool<OnlineEnemyView>" in online_source and "_onlineEnemyGrid" in online_source,
    }
    incomplete = [name for name, passed in production_requirements.items() if not passed]
    if incomplete:
        fail("production core is incomplete: " + ", ".join(incomplete))

    content_asset = (ROOT / "Assets/Resources/Content/ProductionContent.asset").read_text(encoding="utf-8")
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
    with Image.open(art) as image:
        if image.format != "PNG" or image.width < 1024 or image.height < 1024:
            fail("Haldor key art must be a PNG of at least 1024×1024")

    print(f"OK: {len(scripts)} C# files, bootstrap scene, package manifest and Haldor key art validated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
