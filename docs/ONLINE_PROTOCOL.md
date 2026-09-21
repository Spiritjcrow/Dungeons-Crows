# Dungeons & Crows Online Turn Protocol

## Current version

`dc-turn/2.0`

The protocol is the compatibility boundary between the hosted authoritative game service and Unity clients. A client must call `GET /api/protocol` before creating, joining, loading, or submitting online turns. Unity rejects a server whose `protocolVersion` is not exactly compatible with the client version.

## Live service

`https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai`

## Endpoints

- `GET /api/protocol` — compatibility descriptor.
- `POST /api/sessions` — create a three-chapter hunt.
- `POST /api/sessions/join` — join an active hunt.
- `GET /api/sessions/:code` — load canonical campaign state.
- `POST /api/sessions/:code/turn` — submit one deterministic turn intent.

All session and turn responses carry `protocolVersion` at the top level.

## Actions

The v2 action vocabulary remains deliberately small and fully implemented:

- `attack` — seeded d20 attack against the current chapter enemy.
- `defend` — restores 2 Resolve/HP and grants guard through the next attack.
- `interact` — resolves the current chapter objective.
- `speak` — records a role-play action while still consuming a legal turn.

Chapter objective targets are `altar`, `rookery`, and `crown`. Chapter II and III rites succeed by their third legal attempt at the latest, preventing an unwinnable objective lock.

## Campaign state

The campaign contains three canonical chapters:

1. The Crow Crypt — Crowbound Warden + Awaken Altar.
2. The Bone Rookery — Bone Rook + Cleanse Rookery.
3. The Black Rook — Black Rook + Break Crown.

Terminal campaign states are `victory` and `defeat`. After either terminal state the server rejects further state mutation.

## Limits

- Player name: 24 characters.
- Freeform speech/action text: 240 characters.
- Party size: 4 players.
- Retained chronicle: 40 entries.

## Canonical session fields

- `code`
- `turnNumber`
- `activeActorId`
- `party`
- `enemy`
- `chapter`
- `campaignStatus`
- `altarOpened`
- `rookeryPurified`
- `crownBroken`
- `relics`
- `ritualAttempts`
- `score`
- `seed`
- `lastNarration`
- `log`

The backend migrates earlier persisted v1 sessions into v2 state instead of orphaning valid Hunt codes.

## Authority rule

The hosted deterministic resolver owns HP, dice rolls, objectives, relics, score, chapter transitions, victory, defeat, and turn ownership. AI narration occurs only after deterministic resolution and cannot commit canonical state.

## Realtime

Browser clients perform an initial HTTP state load, subscribe to `game-session` by Hunt code through the realtime service, apply `entity.update` messages, and unsubscribe when leaving the session. The green session indicator represents a successful entity subscription rather than merely an open socket.

## Compatibility changes

Breaking changes to required field meaning, actions, campaign semantics, or ownership require a new protocol version. The v1 OpenAPI contract remains archived; the live contract is `docs/openapi/dungeons-crows-online-v2.yaml`.
