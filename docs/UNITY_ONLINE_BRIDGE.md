# Unity Online Bridge

## Purpose

The Unity client may participate in the same hosted campaign as the browser client without duplicating or overriding authoritative combat and campaign logic.

## Current components

- `OnlineProtocol.cs` — Unity DTOs and constants for `dc-turn/2.0`.
- `OnlineGameClient.cs` — UnityWebRequest transport for protocol verification, create/join/load, and turn submission.
- `OnlineProtocolTests.cs` — EditMode tests for version compatibility and v2 campaign JSON parsing.
- `WebPrototype/backend/protocol.ts` — hosted protocol descriptor and response tagging.
- `scripts/check_protocol_contract.py` — cross-language drift check.

## Runtime sequence

1. Unity calls `GET /api/protocol`.
2. Client requires exact compatibility with `dc-turn/2.0`.
3. Player creates or joins an active Hunt.
4. Unity stores the returned `playerId` locally.
5. Unity renders canonical chapter, enemy, objective flags, relics, rite progress, score, HP, and turn ownership.
6. Unity submits an intent to `/api/sessions/:code/turn`.
7. Hosted deterministic rules resolve combat and campaign state.
8. AI Dungeon Master narration is generated only after the deterministic result.
9. Unity receives canonical session state and animates/renders it.

## Important boundary

Unity camera, animation, VFX, audio, and presentation may be predictive, but persisted online state always comes from the hosted service response. Local effects never count as canonical mechanics.

## What is verified vs pending

The browser campaign is deployed. The Unity source DTOs now represent the v2 campaign contract, but this branch still has not been compiled in an actual Unity 6 Editor during this automation session.

Before enabling scene-facing Unity online controls:

- open `UnityProject` in Unity 6;
- resolve packages;
- run EditMode tests including `OnlineProtocolTests`;
- run **Dungeons & Crows → Validate Online Bridge Configuration**;
- generate the Gothic prototype scene;
- create a hosted Hunt from Unity;
- join the same Hunt from the browser;
- complete at least one action and one chapter-state update from each client;
- verify both clients converge on identical canonical state.

Until those checks pass, the REST adapter is an integration layer, not a claimed finished Unity multiplayer client.
