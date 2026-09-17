# Dungeons & Crows — Game Vision v0.1

## Identity
A turn-based multiplayer dark-fantasy role-playing game with a persistent shared world, an AI Dungeon Master, and dynamically changing maps. It keeps tabletop-style story mechanics, party decisions, dice/stat checks, initiative, quests, inventory, character progression, consequences, and role-play at the center.

## Visual direction
The physical and visual feel is inspired by the atmosphere and readability of early gothic PC games such as Diablo-era isometric dungeon crawlers and Hexen-era dark fantasy, without copying their characters, textures, maps, UI, sounds, names, or proprietary assets.

Target presentation:
- 3D gothic stone dungeons and ruins.
- Fixed or semi-fixed high-angle camera for exploration and turn-based tactical encounters.
- Optional close first-person/over-shoulder cinematic view for dialogue, traps, and scripted story beats.
- Heavy shadow, torch/fire pools, stained glass, crypts, catacombs, ruined keeps, wet stone, fog, supernatural glow.
- Chunky readable silhouettes and slightly retro material detail rather than photorealistic clutter.
- Crows are a core world language: scouts, omens, familiars, factions, enemies, messengers, environmental guides, and living map signals.
- Morphing maps visibly change only at narratively valid boundaries so the world feels alive without moving floors underneath a player's legal turn.

## Game loop
1. Party enters or resumes a persistent region.
2. AI Dungeon Master reads canonical world/party/quest state.
3. Players speak or select actions.
4. STT converts voice to structured player intent.
5. Authoritative rules server validates legal actions, checks stats/dice, and resolves deterministic game mechanics.
6. LLM Dungeon Master narrates the result and proposes world/NPC/quest deltas.
7. Validator accepts only schema-valid deltas.
8. Turn server commits the new canonical state.
9. Unity animates the result, morphs the allowed scene elements, and plays TTS/dialogue.
10. State is persisted for the next turn/session.

## Architecture rule
The LLM may propose story outcomes but does not directly alter authoritative stats, inventory, initiative, combat math, player ownership, economy, or world persistence. Those are committed by deterministic server systems after validation.

## MMORPG scope
Persistent accounts, characters, parties, guilds/flocks, shared towns, instanced adventures, world events, trading/economy, asynchronous consequences, seasonal story arcs, and AI-run NPC factions are planned as services around the same authoritative turn engine.

## Rendering target
Unity 6 + URP. First target is desktop/Chromebook-capable streaming or local PC build, with a performance profile that can scale down toward Android. Addressables own world/creature/environment bundles so art can be replaced and streamed without rewriting game logic.

## Art replacement policy
Primitive/placeholder geometry is development-only. Every placeholder carries a replacement tag/category. Production builds should fail asset validation when required production categories still point to placeholder assets.
