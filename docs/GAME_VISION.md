# Dungeons & Crows — Game Vision v0.2

## Identity
A persistent multiplayer dark-fantasy action RPG with tabletop-derived authoritative mechanics, an AI Dungeon Master, voice interaction, corvid heroes, and a world that physically changes with the story.

The visual and interaction target is the established **Alpha 4 Dungeons & Crows visual canon**, not a flat board-game dashboard.

## Primary gameplay presentation
- Fully navigable **3D HD world**.
- Player-selectable **first-person** and **third-person over-the-shoulder** gameplay.
- Instant perspective switching without changing canonical character state.
- Free exploration movement outside deterministic action resolution.
- Combat, rites, loot, quests, progression, and persistent consequences remain validated by authoritative systems.
- Cinematic camera/VFX reactions are presentation only and cannot create game-state effects by themselves.

## Alpha 4 visual canon
- Armored anthropomorphic/corvid warriors with readable silhouettes, feathers, steel, leather, cloth, relics, and weapons.
- Moonlit gothic keeps, ruined cities, crypts, cathedrals, bridges, mountain valleys, forests, battlefronts, and underground ruins.
- Strong black/steel/stone materials contrasted by fire, moonlight, red health energy, blue crow-essence/magic, and supernatural violet accents.
- Dense atmospheric fog, volumetric-looking light shafts, embers, sparks, smoke, weather, wet stone, snow, ash, and magical corruption.
- Functional combat HUD language inspired by classic dark action RPG readability: health/essence orbs, ability hotbar, enemy health banner, minimap, objective tracker, party identifiers.
- UI must never present a nonworking gameplay function.

## Morphic world
The environment is not a static level backdrop.

Committed story changes may alter:
- terrain elevation and traversal routes;
- bridges, gates, towers, crypt walls, stairways, platforms, caves, ruins, and ritual architecture;
- weather, fog, time-of-day feel, environmental lighting, corruption, vegetation, fluids, debris, and persistent battle damage;
- entrances and exits revealed by chapter state;
- footprints, traces, destructible artifacts, and other InkMesh-style persistent evidence.

Morphs occur only at **story-safe committed boundaries**. A wall never moves merely because the AI described it, and terrain never changes underneath an unresolved legal action.

## Perspective and movement
Unity camera target:
- First person for immersion, ranged inspection, dialogue, traps, confined spaces, and player preference.
- Third person for traversal, melee readability, character fashion/gear, party play, and large encounters.
- Collision-aware shoulder camera.
- Keyboard/mouse and gamepad baseline.
- Future touch control profile for Android-class devices.

Current prototype controls:
- WASD / left stick: move.
- Shift / left-stick press: sprint.
- Space / gamepad south button: jump.
- Mouse / right stick: look.
- V: toggle first-person / third-person.

## Game loop
1. Player explores or resumes a persistent region in first or third person.
2. AI Dungeon Master reads canonical world, party, quest, and environmental state.
3. Player uses voice or gameplay controls to declare actions.
4. STT converts voice to structured intent when voice is used.
5. Authoritative rules validate actions, checks, ownership, combat, and progression.
6. AI Dungeon Master narrates resolved results and proposes schema-valid story/world deltas.
7. Validator accepts or rejects proposed deltas.
8. Server commits canonical state.
9. Unity animates combat, VFX, NPC reactions, camera response, and story-safe environment morphs.
10. Persistent world state is saved for the next player/session.

## Architecture rule
The LLM may narrate and propose. It does not directly author HP, dice results, inventory, initiative, player ownership, economy, score, relics, chapter state, victory/defeat, or persistent geometry state.

## Rendering target
Unity 6 + URP 17 generation, using scalable quality tiers:
- **Cinematic/PC:** high-resolution textures, dense shadows, post-processing, atmospheric effects, high particle counts, long view distance.
- **Balanced/Chromebook:** reduced shadow cascades, particle density and terrain detail while keeping visual identity.
- **Mobile/Android:** lower render scale, LOD density, effect complexity and view distance while preserving the same authored world and mechanics.

Addressables own replaceable world, character, creature, VFX, audio, and environment bundles so visual production can advance without rewriting the rules layer.

## Production art rule
Primitive geometry remains development-only and is always tagged as placeholder. No release build should claim the Alpha 4 visual target while required production environment, character, animation, material, VFX, or UI assets remain placeholders.
