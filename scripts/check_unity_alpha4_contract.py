#!/usr/bin/env python3
from pathlib import Path
import json
import sys

ROOT = Path(__file__).resolve().parents[1]
UNITY = ROOT / "UnityProject"

errors = []

def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)

def text(path: str) -> str:
    target = ROOT / path
    require(target.exists(), f"Missing required file: {path}")
    if not target.exists():
        return ""
    return target.read_text(encoding="utf-8")

project_version = text("UnityProject/ProjectSettings/ProjectVersion.txt")
require(
    "m_EditorVersion: 6000.0.65f1" in project_version,
    "Unity editor baseline drifted from 6000.0.65f1",
)

manifest_text = text("UnityProject/Packages/manifest.json")
try:
    manifest = json.loads(manifest_text)
    deps = manifest.get("dependencies", {})
except Exception as exc:
    deps = {}
    errors.append(f"Invalid Unity package manifest JSON: {exc}")

expected_packages = {
    "com.unity.render-pipelines.universal": "17.0.4",
    "com.unity.test-framework": "1.4.6",
    "com.unity.inputsystem": "1.17.0",
    "com.unity.netcode.gameobjects": "2.7.0",
}
for package, version in expected_packages.items():
    require(
        deps.get(package) == version,
        f"Package mismatch {package}: expected {version}, got {deps.get(package)!r}",
    )

runtime_asm = text(
    "UnityProject/Assets/Crows/Scripts/DungeonsCrows.Runtime.asmdef"
)
editor_asm = text(
    "UnityProject/Assets/Crows/Editor/DungeonsCrows.Editor.asmdef"
)
tests_asm = text(
    "UnityProject/Assets/Crows/Tests/Editor/DungeonsCrows.Tests.Editor.asmdef"
)

try:
    runtime_refs = set(json.loads(runtime_asm).get("references", []))
    editor_refs = set(json.loads(editor_asm).get("references", []))
    tests_json = json.loads(tests_asm)
    tests_refs = set(tests_json.get("references", []))
    tests_optional = set(tests_json.get("optionalUnityReferences", []))
except Exception as exc:
    runtime_refs = editor_refs = tests_refs = tests_optional = set()
    errors.append(f"Invalid Unity asmdef JSON: {exc}")

require("Unity.InputSystem" in runtime_refs, "Runtime asmdef missing Unity.InputSystem")
require("Unity.Netcode.Runtime" in runtime_refs, "Runtime asmdef missing Unity.Netcode.Runtime")
require("DungeonsCrows.Runtime" in editor_refs, "Editor asmdef missing runtime reference")
require(
    "Unity.RenderPipelines.Universal.Runtime" in editor_refs,
    "Editor asmdef missing URP runtime reference",
)
require(
    "Unity.RenderPipelines.Core.Runtime" in editor_refs,
    "Editor asmdef missing render-pipeline core reference",
)
require("DungeonsCrows.Runtime" in tests_refs, "Tests asmdef missing runtime reference")
require("TestAssemblies" in tests_optional, "Tests asmdef is not marked as a test assembly")

protocol = text("UnityProject/Assets/Crows/Scripts/Online/OnlineProtocol.cs")
require('Version = "dc-turn/3.0"' in protocol, "Unity protocol is not dc-turn/3.0")
require(
    'public int essence;' in protocol
    and 'public int maxEssence;' in protocol,
    "Unity CombatantDto is missing Crow Essence fields",
)
require(
    '"invoke"' in protocol,
    "Unity protocol action vocabulary is missing Invoke",
)

hud = text("UnityProject/Assets/Crows/Scripts/UI/Alpha4HudController.cs")
for token in ("digit1Key", "digit2Key", "digit3Key", "digit4Key", "digit5Key"):
    require(token in hud, f"Alpha 4 HUD missing functional hotkey: {token}")
require("campaign?.LeaveHunt()" in hud, "Alpha 4 HUD missing functional Leave Hunt")
require(
    "_narrationLabel" in hud and "session.lastNarration" in hud,
    "Alpha 4 HUD is not rendering canonical Dungeon Master narration",
)
require(
    "_minimapImage" in hud and "minimap.Texture" in hud,
    "Alpha 4 HUD is not rendering a real minimap texture",
)
require(
    "_playerEssenceFill" in hud
    and "_playerEssenceText" in hud
    and "_invokeButton" in hud
    and "player.essence >= 2" in hud,
    "Alpha 4 HUD does not expose authoritative Crow Essence and Invoke",
)

presentation = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/CanonicalCombatPresentationBridge.cs"
)
delta = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/CanonicalPresentationDelta.cs"
)
enemy_presenter = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/ChapterEnemyPresenter.cs"
)
camera_feedback = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/CombatCameraFeedback.cs"
)
crow_flock = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/CrowFlockController.cs"
)
audio_director = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/CanonicalAudioDirector.cs"
)
camera = text(
    "UnityProject/Assets/Crows/Scripts/Camera/DualPerspectiveCamera.cs"
)
require(
    "envelope.resolvedActionType" in presentation,
    "Combat presentation is not using authoritative resolved action type",
)
require(
    "PresentationCue" in delta and "CanonicalPresentationDelta.From" in delta,
    "Canonical presentation delta layer is missing",
)
require(
    "expectedEnemyId" in enemy_presenter and "ApplyCanonicalSession" in enemy_presenter,
    "Chapter-aware enemy presentation binding is missing",
)
require(
    "SetPresentationOffset" in camera_feedback
    and "SetPresentationOffset" in camera,
    "Combat camera feedback is not routed through the camera presentation offset",
)
require(
    "CrowFlockMode" in crow_flock
    and "CanonicalPresentationDelta" in crow_flock
    and "Present(" in crow_flock,
    "Crow flock is not reacting to canonical presentation cues",
)
require(
    "AudioClip.Create" in audio_director
    and "CanonicalPresentationDelta" in audio_director
    and "EnsureReady" in audio_director,
    "Canonical zero-cost audio fallback is missing",
)
require(
    'case "invoke":' in audio_director
    and "invokeClip" in audio_director,
    "Canonical audio does not present Invoke",
)
require(
    'case "invoke":' in presentation
    and "invokeFx" in presentation,
    "Canonical presentation does not present Invoke",
)

campaign = text(
    "UnityProject/Assets/Crows/Scripts/Online/OnlineCampaignController.cs"
)
require(
    "LocalHuntIdentityStore.Save" in campaign
    and "LocalHuntIdentityStore.TryLoad" in campaign,
    "Unity Hunt resume persistence is incomplete",
)
require(
    "SilentPartyRefreshRoutine" in campaign,
    "Unity waiting-party synchronization fallback is missing",
)
require(
    'public void Invoke() => Submit("invoke"' in campaign,
    "Unity campaign controller does not expose Invoke",
)

render_setup = text(
    "UnityProject/Assets/Crows/Editor/Alpha4RenderPipelineSetup.cs"
)
post_setup = text(
    "UnityProject/Assets/Crows/Editor/Alpha4PostProcessingSetup.cs"
)
for token in (
    "UniversalRenderPipelineAsset.Create",
    "GraphicsSettings.defaultRenderPipeline",
    "QualitySettings.renderPipeline",
    "asset.renderScale = 1f",
    "asset.msaaSampleCount = 4",
):
    require(token in render_setup, f"Alpha 4 URP setup missing: {token}")

for token in (
    "TonemappingMode.ACES",
    "Bloom",
    "ColorAdjustments",
    "WhiteBalance",
    "Vignette",
    "renderPostProcessing = true",
):
    require(
        token in post_setup,
        f"Alpha 4 post-processing setup missing: {token}",
    )

builder = text(
    "UnityProject/Assets/Crows/Editor/GothicPrototypeSceneBuilder.cs"
)
require(
    "Alpha4RenderPipelineSetup.EnsureConfigured()" in builder,
    "Scene builder does not configure URP before creating the scene",
)
require(
    "Alpha4PostProcessingSetup.AttachToScene(camera)" in builder,
    "Scene builder does not attach Alpha 4 post-processing",
)
require(
    "Alpha4QualityDirector" in builder,
    "Scene builder does not wire the quality director",
)
require(
    "CreateMorphicLandscape" in builder
    and "chapterBridge.SetLandscape" in builder,
    "Scene builder does not wire the walkable morphic landscape",
)
require(
    "ChapterEnemyPresenter" in builder
    and "ProceduralActorMotion" in builder,
    "Scene builder does not wire chapter enemies and motion fallback",
)
require(
    "CreateMinimap" in builder
    and "hud.SetMinimap(minimap)" in builder,
    "Scene builder does not wire the real minimap",
)
require(
    "CreateCrowFlock" in builder
    and "crowFlock" in builder,
    "Scene builder does not wire the animated crow flock",
)
require(
    "CanonicalAudioDirector" in builder
    and "audioDirector" in builder,
    "Scene builder does not wire canonical audio",
)
require(
    "Crowfire Invoke" in builder
    and "invokeFx" in builder,
    "Scene builder does not wire Invoke crowfire VFX",
)

landscape = text(
    "UnityProject/Assets/Crows/Scripts/World/MorphicLandscapeController.cs"
)
procedural_motion = text(
    "UnityProject/Assets/Crows/Scripts/Presentation/ProceduralActorMotion.cs"
)
require(
    "MeshCollider" in landscape
    and "CommitStoryBoundary" in landscape
    and "SnapToChapter" in landscape,
    "Morphic landscape is not collider-backed and chapter-driven",
)
for token in (
    "PlayAttack",
    "PlayGuard",
    "PlayRite",
    "PlaySpeak",
    "PlayHit",
    "PlayDefeated",
):
    require(
        token in procedural_motion,
        f"Procedural actor motion missing: {token}",
    )

tests = text(
    "UnityProject/Assets/Crows/Tests/Editor/Alpha4PresentationTests.cs"
)
for test_name in (
    "DualPerspectiveCamera_SwitchesFirstAndThirdPerson",
    "DualPerspectiveCamera_PresentationOffsetRoundTrips",
    "MorphicEnvironment_SnapAppliesChapterPoseExactly",
    "LocalHuntIdentityStore_RoundTripsAndClears",
    "QualityProfiles_ScaleDownWithoutChangingGameRules",
    "MorphicLandscape_SnapChangesWalkableGeometry",
    "CrowFlockController_RegistersAndChangesMode",
    "CanonicalAudioDirector_GeneratesFallbackClips",
    "RuntimeMinimapCamera_CreatesRealRenderTexture",
    "CanonicalPresentationDelta_DerivesOnlyCommittedChanges",
    "ChapterEnemyPresenter_SelectsCanonicalChapterEnemy",
    "Alpha4Protocol_RequiresCrowEssenceAndInvoke",
):
    require(test_name in tests, f"Missing Alpha 4 regression test: {test_name}")

if errors:
    print("Dungeons & Crows Alpha 4 static contract FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("Dungeons & Crows Alpha 4 static contract OK")
print(" unity=6000.0.65f1")
print(" urp=17.0.4")
print(" protocol=dc-turn/3.0")
print(" resource=crow-essence")
print(" invoke=authoritative")
print(" input=real hotkeys")
print(" persistence=resume/leave")
print(" multiplayer=waiting-party refresh fallback")
