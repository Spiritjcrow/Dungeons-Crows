# Dungeons & Crows

A turn-based persistent dark-fantasy RPG/MMORPG built around tabletop-style story mechanics, an intelligent AI Dungeon Master, voice interaction, animated characters, and a world that can evolve with the story.

## Playable online campaign alpha

Live build:

https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai/

The browser client now contains a finishable three-chapter campaign rather than a one-room prototype:

1. **The Crow Crypt** — awaken the altar and defeat the Crowbound Warden.
2. **The Bone Rookery** — complete the cleansing rite and defeat the Bone Rook.
3. **The Black Rook** — break the crown and defeat the Black Rook.

Implemented canonical mechanics include 1–4 player Hunt sessions, seeded d20 rolls, HP, defense, guard, enemy retaliation, fallen state, three chapter objectives, three relics, score, persistent chronicle, bounded AI narration, realtime browser updates, refresh-resume identity, working Leave Hunt/Return to Gate, legacy-session migration, victory, defeat, and terminal-state mutation locks.

### Verification rule

Visible gameplay controls must correspond to implemented state transitions. Unverified Unity scene controls are not exposed as finished gameplay.

The current deterministic acceptance sweep passed:
- 5,000/5,000 seeded campaigns using the documented Defend/Attack strategy;
- 200/200 intentional-defeat seeds;
- out-of-turn immutability;
- victory/defeat mutation locks;
- legacy session migration.

The latest hosted deployment reports ready with no frontend, backend, or network error logs. The host did not execute its browser e2e suite automatically, so cross-client UI e2e remains a separate acceptance gate.

## Unity ↔ web protocol

Current protocol: `dc-turn/2.0`

- Protocol guide: `docs/ONLINE_PROTOCOL.md`
- Unity integration guide: `docs/UNITY_ONLINE_BRIDGE.md`
- OpenAPI 3.1 contract: `docs/openapi/dungeons-crows-online-v2.yaml`
- Historical v1 contract: `docs/openapi/dungeons-crows-online-v1.yaml`
- Drift checker: `scripts/check_protocol_contract.py`
- Unity REST adapter: `UnityProject/Assets/Crows/Scripts/Online/OnlineGameClient.cs`

Unity DTOs now mirror chapter, campaign status, objectives, relics, rite progress, score, HP, and turn ownership. Unity still requires actual Unity 6 Editor compilation and browser↔Unity convergence testing before its scene-facing online controls can be called finished.

## Visual identity

The game targets an original gothic old-PC atmosphere: high-angle 3D dungeon exploration, torch-lit stone, crypts, ruins, occult architecture, readable silhouettes, restrained retro detail, and dramatic magical effects. The mood is informed by classic isometric and dark-fantasy PC games while using original Dungeons & Crows art, UI, characters, lore, maps, audio, and gameplay assets.

## Core authority rule

The AI Dungeon Master narrates, role-plays NPCs, and can propose story direction. It does **not** author authoritative combat math, HP, dice results, score, relics, victory, defeat, character ownership, economy, or persisted game state. Deterministic services validate and commit those changes.

## Unity prototype

The `UnityProject/` folder targets Unity 6.

Current foundation:
- Unity Netcode for GameObjects authoritative turn coordinator.
- Unity AI Inference package for future local inference workloads.
- Addressables for replaceable/streamable environments, creatures, audio, and world chunks.
- Input System for keyboard, gamepad, touch, and accessibility-friendly mappings.
- Timeline for story beats and cinematics.
- RetroGothicCamera for high-angle exploration.
- One-click gothic prototype scene generator.
- Placeholder asset marker so temporary primitives cannot silently become production assets.
- `dc-turn/2.0` online DTOs plus UnityWebRequest create/join/load/turn transport.
- EditMode protocol compatibility and canonical JSON parsing tests.

### Unity verification gate

1. Open `UnityProject` in Unity 6.
2. Resolve packages.
3. Choose **Dungeons & Crows → Build Gothic Prototype Scene**.
4. Choose **Dungeons & Crows → Validate Online Bridge Configuration**.
5. Run EditMode tests including `OnlineProtocolTests`.
6. Create a Hunt from Unity and join it from the browser.
7. Submit actions from each client and verify identical chapter, HP, relic, objective, score, and turn state.
8. Only then enable scene-facing online gameplay controls.

## CI note

GitHub Actions has previously failed before workflow steps received a runner (`runner_id: 0`, `steps: []`) for both the protocol gate and the pre-existing repo-health workflow. Infrastructure issue #7 tracks that condition. The protocol contract is also checked independently during development.

## Planned production expansion

- Procedural/morphing region graph and dungeon generator.
- STT/TTS voice turns with interruption handling.
- Animated crow flock/familiar system.
- AI NPC intent plus deterministic gameplay behavior.
- Persistent characters, parties, guilds/flocks, shared towns, instanced adventures, and world events.
- Cross-platform quality profiles for PC, Chromebook, and Android-class hardware.

See `docs/GAME_VISION.md` for the broader design contract.


## Alpha 4 production client

The production presentation is a full 3D Unity 6 dark-fantasy action RPG, not the flat browser systems harness.

Current Unity branch now includes:
- first-person / third-person camera switching;
- collision-aware over-the-shoulder camera;
- WASD/gamepad free exploration, sprint and jump;
- canonical-state-driven morphic environment controller;
- chapter visual bridge;
- Alpha 4 visual canon and scalable rendering direction.

The browser build remains useful for authoritative rules/network testing, but it is not the final visual client.

See `docs/VISUAL_CANON_ALPHA4.md`.
