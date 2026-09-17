# Dungeons & Crows Online Vertical Slice Design

## Goal
Ship a public browser-playable vertical slice quickly while preserving Unity 6 as the canonical 3D game client and deterministic server-side rules as canonical state authority.

## Approved direction
The game keeps custom tabletop-style D&D mechanics, turn order, stats, quests, inventory, consequences, party decisions, and AI Dungeon Master narration. Its presentation uses an original gothic dark-fantasy aesthetic inspired by the readability and atmosphere of early Diablo/Hexen-era PC games without copying proprietary art, maps, UI, audio, names, or characters.

## Delivery architecture
### Track A — Public browser slice
Use AppDeploy to host a React/Vite frontend plus backend. The slice provides a public play surface immediately: anonymous player name, party/session code, shared encounter state, turn submission, deterministic resolution, AI Dungeon Master narration, combat log, and realtime updates between connected browsers.

This browser slice is deliberately rules-first and visually stylized rather than pretending to be the finished Unity renderer. It is a live public test harness for the same turn contract the Unity client will consume.

### Track B — Unity client
Unity 6 + URP remains the full game renderer. The current branch already contains the gothic prototype builder, high-angle camera, turn coordinator, and placeholder asset tracking. Unity will later consume the same turn contract and persisted world state used by the browser slice.

### Track C — WebGL publishing
Add Unity WebGL build automation after a verified local/editor compile. Do not block the public playtest on Unity CI licensing or a complete art pipeline. When the WebGL artifact is available, host it as the primary 3D client and retain the browser shell for lobby, account/session, diagnostics, and fallback play.

## Canonical turn contract
A turn contains:
- sessionId
- turnNumber
- actorId
- actorName
- actionType: move | attack | interact | speak | defend
- targetId optional
- freeformText optional
- rollSeed

The resolver produces:
- accepted boolean
- deterministicResult
- diceRolls
- stateDelta
- narration
- nextActorId
- nextTurnNumber

The LLM may narrate and propose flavor/world text, but it must not directly alter hit points, initiative, inventory ownership, player ownership, economy balances, or committed world state. Deterministic code resolves those fields first.

## First playable encounter
The first public encounter is a small crypt chamber with four party slots and one hostile Crowbound Warden. Players may attack, defend, interact with a sealed crow altar, or speak. Attack and defend are deterministic from seeded d20-style checks. The altar interaction creates a non-combat branch so the vertical slice proves both combat and narrative interaction.

## Realtime model
Each session is a shared entity. Clients perform one initial HTTP load, then subscribe through a WebSocket connection. Every accepted turn writes the new bounded session record and broadcasts an entity.update event to subscribers. No polling is used for shared state.

## Persistence
For the first slice, store one bounded session record containing party slots, encounter state, turn number, active actor, last 40 log entries, and world flags. Keep records well below platform item limits. Session codes are short human-readable identifiers while database record IDs remain internal.

## AI Dungeon Master
Backend AI generation receives only the deterministic outcome and bounded recent context. Prompting asks for 2–4 sentences of atmospheric narration plus optional dialogue. The AI cannot return executable state mutations in v0.1. If AI generation fails, the resolver returns a deterministic fallback narration so turns remain playable.

## Browser UI
Desktop-first but mobile-functional. Layout:
- top: title, session code, connection state
- left: party and initiative
- center: stylized crypt board with player/warden positions represented by original CSS/vector forms
- right: encounter state and available actions
- bottom: Dungeon Master narration and chronological turn log

Visual language: near-black stone, warm torch highlights, desaturated iron, parchment text, crow silhouettes, beveled panels, subtle fog/noise. Avoid direct imitation of Diablo/Hexen assets or UI.

## Safety and abuse boundaries
The public prototype accepts short display names and game actions only. No arbitrary file uploads, external links, private messaging, trading, payment, or user-generated asset uploads in v0.1. Freeform action text is length-limited before AI use.

## Testing and release gates
A release is public-ready only when automated QA proves:
1. A user can create a session and see a session code.
2. A second browser can join the same session and receives realtime updates after a turn.
3. An attack advances the turn and changes only deterministic combat state.
4. The altar interaction produces a non-combat state flag and narration.
5. Invalid or out-of-turn actions are rejected visibly without mutating session state.

## Source-of-truth boundaries
- GitHub: Unity project, shared contracts, design/implementation docs, contributor issues.
- AppDeploy: temporary public browser runtime and hosted prototype source snapshot.
- Google Drive: architecture/research/assets/build logs.
- Unity: canonical production renderer.

## Explicit non-goals for this slice
No guilds, economy, open-world streaming, account system, character creator, procedural world regeneration, voice chat, final art, or production moderation stack. Those remain subsequent slices after the first complete online turn loop is verified.