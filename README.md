# Dungeons & Crows

A turn-based persistent dark-fantasy RPG/MMORPG built around tabletop-style story mechanics, an intelligent AI Dungeon Master, voice interaction, animated characters, and a world map that can evolve with the story.

## Current development branch
`selma/unity-ai-foundation-v0.1`

## Visual identity
The game targets an original gothic old-PC atmosphere: high-angle 3D dungeon exploration, torch-lit stone, crypts, ruins, occult architecture, readable silhouettes, restrained retro detail, and dramatic magical effects. The mood is informed by the *feel* of classic Diablo-era isometric crawlers and Hexen-era dark fantasy while using original Dungeons & Crows art, UI, characters, lore, maps, audio, and gameplay assets.

## Core rule
The AI Dungeon Master narrates, role-plays NPCs, plans encounters, and proposes world changes. It does **not** directly author authoritative combat math, inventory, character ownership, economy, or persisted game state. Deterministic game services validate and commit those changes.

## Unity prototype
The `UnityProject/` folder targets Unity 6.

Current foundation:
- Unity Netcode for GameObjects authoritative turn coordinator.
- Unity AI Inference package available for local neural inference workloads.
- Addressables for replaceable/streamable environments, creatures, audio, and world chunks.
- Input System for keyboard, gamepad, touch, and accessibility-friendly input mapping.
- Timeline for story beats and cinematics.
- RetroGothicCamera for high-angle exploration.
- One-click gothic prototype scene generator.
- Placeholder asset marker so temporary primitives cannot silently become production assets.

### Build the first scene
1. Open `UnityProject` in Unity 6.
2. Allow Package Manager to resolve dependencies.
3. Choose **Dungeons & Crows → Build Gothic Prototype Scene**.
4. Open `Assets/Crows/Scenes/GothicPrototype.unity` if Unity does not open it automatically.
5. Press Play after adding a NetworkManager prefab/configuration when testing multiplayer turn state.

## Major systems planned
- AI Dungeon Master orchestration and memory.
- Deterministic D&D-inspired custom rules engine.
- Procedural/morphing region graph and dungeon generator.
- STT/TTS voice turns with interruption handling.
- Animated crow flock/familiar system.
- AI NPC intent + deterministic gameplay behavior.
- Persistent characters, parties, guilds/flocks, shared towns, instanced adventures, and world events.
- Cross-platform quality profiles for PC, Chromebook, and Android-class hardware.

See `docs/GAME_VISION.md` for the design contract.
