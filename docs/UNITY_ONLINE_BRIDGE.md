# Unity Online Bridge

## Purpose

The Unity client may participate in the same hosted hunt as the browser client without owning or duplicating authoritative combat logic.

## Current components

- `OnlineProtocol.cs` — Unity-side DTOs and protocol constants for `dc-turn/1.0`.
- `OnlineGameClient.cs` — UnityWebRequest transport for protocol verification, create/join/load, and turn submission.
- `OnlineProtocolTests.cs` — EditMode tests for version compatibility and canonical JSON parsing.
- `WebPrototype/backend/protocol.ts` — hosted protocol descriptor and response tagging.
- `scripts/check_protocol_contract.py` — cross-language drift check.

## Runtime sequence

1. Unity calls `GET /api/protocol`.
2. Client requires exact compatibility with `dc-turn/1.0`.
3. Player creates or joins a hunt.
4. Unity stores the returned `playerId` locally for that session.
5. Unity submits action intent to `/api/sessions/:code/turn`.
6. Hosted deterministic rules resolve dice, HP, turn ownership and world flags.
7. AI Dungeon Master narration is generated after the deterministic result.
8. Unity receives canonical session state and animates/renders it.

## Important boundary

Unity animation, camera, audio and presentation may be predictive, but persisted online game state comes from the hosted service response. Local visual effects must never be treated as authoritative state mutations.

## Editor verification still required

This branch has not yet been opened and compiled in an actual Unity 6 Editor during this automation session. Before promoting PR #6 out of draft:

- open `UnityProject` in Unity 6;
- let Package Manager resolve dependencies;
- run EditMode tests including `OnlineProtocolTests`;
- build the Gothic prototype scene;
- create a hosted hunt from Unity;
- join the same hunt from a browser;
- submit one turn from each client and verify canonical state converges;
- only then add scene-facing UI/orchestration around `OnlineGameClient`.
