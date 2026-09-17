# Dungeons & Crows — Online Playtest

## Live vertical slice
https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai/

Status: public browser prototype deployed and AppDeploy runtime QA reported ready with no frontend or backend error logs on 2026-09-16.

## What this build is
This is the early rules/network vertical slice for Dungeons & Crows. It proves public session creation/joining, deterministic turn resolution, persistent shared encounter state, realtime browser updates, a non-combat crow-altar interaction, and bounded AI Dungeon Master narration.

It deliberately uses a stylized browser-rendered crypt instead of pretending to be the finished game renderer. Unity 6 + URP remains the production 3D client and will converge on the same authoritative turn contract.

## Current encounter
The Crow Crypt contains:
- up to four adventurers,
- one Crowbound Warden,
- a sealed crow altar,
- attack, defend, interact, and speak actions,
- seeded d20-style resolution,
- Warden retaliation,
- shared turn chronicle,
- realtime session updates.

## Authority boundary
Mechanical state resolves deterministically before narration. AI can narrate accepted results but does not directly control hit points, turn ownership, dice results, inventory, economy, or committed world state.

## Visual direction
The public slice uses original gothic-crow styling: dark stone, torch warmth, iron framing, fog/noise, parchment text, and high-contrast tactical readability. It is inspired by the atmosphere of classic dark-fantasy PC games without copying proprietary Diablo/Hexen art, UI, maps, audio, characters, or names.

## Next release gates
1. Mirror hosted prototype source into this repository.
2. Verify the Unity project in an actual Unity 6 editor runtime.
3. Complete one Unity end-to-end turn using the shared contract.
4. Add Unity WebGL build automation only after the editor baseline is verified.
5. Replace development primitives with licensed/original production assets.