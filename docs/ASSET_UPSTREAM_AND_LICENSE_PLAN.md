# Asset, Upstream, and License Plan

## Rules/content foundation
Use only content that is either original Dungeons & Crows material or explicitly licensed for reuse. If SRD-based fifth-edition rules text is used, keep it isolated under the applicable Creative Commons attribution requirements and do not assume that non-SRD D&D settings, characters, art, logos, or book text are covered.

Recommended rules baseline: SRD 5.2.1-compatible mechanics plus original Dungeons & Crows classes, creatures, factions, locations, crow systems, quests, UI, lore, and visual identity.

## Candidate reusable art sources
These are candidates, not silently imported dependencies. Every selected pack must be recorded with source, version/date, license, files used, and any attribution obligations.

- Poly Haven: CC0 textures, HDRIs, and 3D models. Strong candidate for stone, wood, metals, skies, and environmental PBR material sources.
- Kenney: asset-page game assets are generally CC0. Useful for prototype UI, icons, and generic development assets where the specific pack license is confirmed.
- Quaternius: suitable low-poly/optimized 3D packs, subject to the license shipped with the chosen pack. Good candidate for mobile-friendly prototype characters/environment pieces.

## Candidate open-source engineering upstreams
- OndrejNepozitek/Edgar-Unity: MIT-licensed Unity procedural dungeon generation project. Evaluate algorithms/API boundaries before integrating. Preserve the MIT notice for any copied or derived code.
- MoonlightByte/NeverEndingQuest: useful architecture research for AI DM concerns such as persistent memory, call-site separation, web player state, and voice; its current Fair Source licensing means it should be treated as a design reference unless the intended use is clearly within that license. Do not copy protected implementation into Dungeons & Crows by default.

## Original-production requirement
The shipped game's recognizable identity must be original. Classic games may inform mood, camera readability, pacing, lighting philosophy, and genre language, but Dungeons & Crows must not copy their maps, textures, sprites, HUD artwork, characters, names, sounds, music, or proprietary visual assets.

## Asset registry fields
Each non-original asset should eventually have:
- asset_id
- source/project
- upstream author or organization
- source version/date
- license identifier
- original filename/path
- Dungeons & Crows destination
- modifications
- attribution text if required
- redistribution constraints
- production/prototype status

## Contributor lanes
1. Unity/rendering: scenes, shaders, lighting, animation, Addressables, performance profiles.
2. Gameplay/rules: deterministic actions, initiative, dice/stat checks, combat, inventory, progression.
3. AI Dungeon Master: intent parsing, world-delta schema, NPC cognition, memory, narration, safety/validation.
4. World generation: region graph, dungeon layout, biome grammar, morph rules, persistence.
5. Multiplayer/backend: authoritative turns, identity, parties, guilds/flocks, persistence, matchmaking/instances.
6. Voice/cinematics: STT, TTS, interruption handling, Timeline events, lip/facial animation where applicable.
7. Art/audio: original crow-centric visual language, UI, creature/environment art, SFX, music.
8. QA/tools: deterministic replay, save migration, asset-license audit, performance and mobile validation.
