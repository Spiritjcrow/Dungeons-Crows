# Dungeons & Crows: Agartha / Selma° Dreamachine Prototype

> **Status: playable 3D/WebGL2 experiment + Canvas2D fallback, not a completed game or deployed AI model.**

One self-contained, offline-first exploration demonstrator for the existing **Dungeons & Crows** game. It develops the geode-like inner-world geography without replacing the original game's identity, characters, canonical visuals or combat plans.

## Run in a browser
Open **`DungeonsAndCrows_Agartha_SelmaDreamachine_Portable.html`** in Chrome or another modern browser. This file is self-contained and does not require npm or internet. Or host this folder on any static HTTP server and open `index.html`.

## Controls
- **WASD** / on-screen arrows: move along one continuous terrain surface; arrow left/right: turn
- **Pointer drag on landscape:** look horizontally and vertically
- **Shift:** sprint
- **F / Interact:** look for hidden mineral loot near X=6, Z=-187
- **R / Survey:** get an event from Selma° based on simulated world conditions
- **L / Dream Shift:** bounded reversible visual transformation
- **Export:** create a local JSON save snapshot, **Clear Memory:** forget game-only history while retaining loot, **Restart:** reset expedition

## Implemented
- Interactive 3D landscape geometry computed from a smooth heightfield, spanning frozen fjord, sunward mountain, great fracture and second crown.
- Browser WebGL2 vertex/fragment shader pipeline for the landscape, stationary inner-sun illustration and a domain-warped liquid/fBm postprocess, including a readable center and capped distortion. There is a Canvas2D software fallback if WebGL2 is unavailable.
- Continuous player coordinates, region transitions, climate and terrain slope observations.
- Selma° deterministic ranked decision engine that reacts to environmental observations, exploration, treasure discoveries and a capped recent-game event ledger (96 entries).
- Traceable decisions, save/restore with schema checking and optional export, local-only browser storage.
- Manual visual phase triggers, reduced-motion option, mobile touch controls, and safe renderer fallback.

## Not implemented / boundaries
- This is **not a connected generative AI / LLM**. `inferenceInterface()` describes a deterministic local interface and can be extended later with a separately authorized model. Selma° cannot sense real-world surroundings; the only inputs are simulated game data.
- The project does not yet have real-time battle combat, NPC agency, networks, region streaming, validated liquid-shader GPU render on this container's browser, or realistic physics for an Earth-sized hollow inhabited shell. Interior gravity is a labeled local reference assumption, not a scientifically validated hollow-planet model. The stationary sun is a visual fixture here rather than an astrophysically modeled plasma body.
- On this build environment Chromium runs without available WebGL2, so direct browser testing used the Canvas2D fallback. GPU shader compilation on a real WebGL2 device remains to verify.
- Art/geometry are **engineering graybox**, not final visual canon. No external device sensors, hidden monitoring, cloud model calls, paid services, or permission changes.

## Developer commands

```sh
node tests/core.test.mjs
python build_portable.py
```

## Evidence
- Source syntax: `node --check` on .mjs modules
- Core tests: deterministic replay, region crossing, ranked inference, bounded memory, invalid-save refusal, persistent loot, morphing without collision mutations
- Browser smoke: Chromium used local HTML injection due navigation restrictions; tested desktop 1400x900 and mobile 390x844; simulated movement and survey buttons, no JS page errors. GPU unavailable there, renderer auto-recovered to Canvas2D.

## Git integration
Standalone module is intended for a feature branch of `Spiritjcrow/Dungeons-Crows`. Prior Alpha 8 portable/reactive realm assets are **not overwritten**. Use the module as an independent technical proving ground before integrating into the primary Alpha 8 campaign and adding a gameplay tab. No merging or release deployment is implied by creating a prototype branch.