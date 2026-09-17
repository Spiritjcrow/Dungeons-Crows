# Dungeons & Crows Online Turn Protocol

## Current version

`dc-turn/1.0`

The protocol is the compatibility boundary between the hosted authoritative game service and Unity clients. A client must call `GET /api/protocol` before creating, joining, loading, or submitting online turns. A Unity client must reject a server whose `protocolVersion` is not exactly compatible with the client version.

## Live service

`https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai`

## Endpoints

- `GET /api/protocol` — compatibility descriptor.
- `POST /api/sessions` — create a hunt. JSON body: `{ "name": "Rook" }`.
- `POST /api/sessions/join` — join a hunt. JSON body: `{ "code": "ABC123", "name": "Rook" }`.
- `GET /api/sessions/:code` — load canonical session state.
- `POST /api/sessions/:code/turn` — submit one intent to the deterministic resolver.

All session and turn responses carry `protocolVersion` at the top level.

## Actions

The v1 action vocabulary is deliberately small:

- `attack`
- `defend`
- `interact`
- `speak`

`interact` currently targets the crow altar. `speak` may carry `freeformText` but does not bypass deterministic game rules.

## Limits

- Player name: 24 characters.
- Freeform speech/action text: 240 characters.
- Party size: 4 players.
- Retained chronicle: 40 entries.

The same constants are represented in `WebPrototype/backend/protocol.ts` and `UnityProject/Assets/Crows/Scripts/Online/OnlineProtocol.cs`. `scripts/check_protocol_contract.py` fails when these copies drift.

## Canonical session fields

- `code`
- `turnNumber`
- `activeActorId`
- `party`
- `enemy`
- `altarOpened`
- `seed`
- `lastNarration`
- `log`

Combatant state includes `id`, `name`, `hp`, `maxHp`, `defense`, `isEnemy`, and optional/zero `defendingUntilTurn`.

## Authority rule

The server owns canonical state. Unity sends intent and renders the accepted result. The LLM Dungeon Master supplies narration after deterministic resolution and cannot directly author HP, dice rolls, initiative/turn ownership, inventory, or persisted world state.

## Realtime

The browser client currently subscribes to entity type `game-session` by session code. Unity v1 uses HTTP create/join/load/turn calls first; realtime transport can be layered onto the same canonical session DTO without changing combat semantics.

## Compatibility changes

Any breaking change to field meaning, required fields, action vocabulary, turn resolution semantics, or canonical ownership requires a protocol version change. Additive fields may remain in `dc-turn/1.0` only when older clients can safely ignore them.
