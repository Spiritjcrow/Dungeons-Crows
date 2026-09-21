# Dungeons & Crows — Online Campaign Alpha

## Live build

https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai/

AppDeploy reports the current runtime ready with no frontend, backend, or network error logs. AppDeploy did not execute its e2e suite in the latest deployment, so runtime-ready status is kept separate from gameplay acceptance testing.

## What this build is

This is a browser-playable three-chapter campaign using the same authoritative service intended for the Unity client. It is no longer an endless single-room encounter.

Implemented campaign loop:

1. The Crow Crypt — awaken the altar and defeat the Crowbound Warden.
2. The Bone Rookery — complete the cleansing rite and defeat the Bone Rook.
3. The Black Rook — break the crown and defeat the Black Rook.
4. Reach a persistent `victory` state, or lose the Hunt when every adventurer falls.

## Working mechanics

- 1–4 player Hunt sessions with six-character codes.
- Seeded d20 attack and objective rolls.
- HP, defense, guard, enemy retaliation, fallen state.
- Defend restores 2 Resolve/HP and guards through the next attack.
- Three chapter objectives with persistent flags.
- Chapter II/III rite progress capped at three legal attempts.
- Three persistent relics.
- Score.
- Full healing between completed chapters for living party members.
- Persistent chronicle and AI narration after deterministic resolution.
- Realtime browser subscription updates.
- Terminal victory and defeat states that reject further mutation.
- Legacy session migration into the v2 campaign shape.

## Verification evidence

A local execution of the deterministic engine reproduced a balance failure in an earlier candidate build. After repairing the rules, the intended Defend/Attack strategy completed the campaign for 5,000/5,000 deterministic seeds in the verification sweep. A deliberately poor strategy that repeatedly spends turns speaking under attack still reaches the defeat state.

The hosted frontend/backend build is also compiler/deployment clean. Cross-client realtime and full browser UI e2e remain part of the acceptance suite because the host did not execute those e2e tests automatically in the latest run.

## No-placeholder rule

Visible gameplay controls are backed by implemented state transitions. Features not yet runtime-verified in Unity are not exposed as playable Unity controls.

## Authority boundary

AI can narrate resolved outcomes but cannot author HP, dice results, score, relics, chapter progression, victory, defeat, or ownership.

## Next release gates

1. Run Unity 6 EditMode and bridge smoke checks.
2. Complete browser ↔ Unity same-Hunt convergence test.
3. Restore GitHub Actions runner execution tracked in issue #7.
4. Add Unity scene-facing controls only after the above pass.
5. Replace prototype visual primitives with original/licensed production assets without changing canonical mechanics.
