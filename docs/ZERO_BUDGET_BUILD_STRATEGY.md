# Zero-budget build strategy

As of 2026-09-24, Dungeons & Crows uses a zero-dollar-first execution strategy.

## Priority order

1. **GitHub public repository — source of truth and general CI**
   - Keep all canonical source on `Spiritjcrow/Dungeons-Crows`.
   - Use standard GitHub-hosted runners only.
   - Public-repository standard runners are free, so do not introduce paid runner classes.
   - Unity Editor CI stays license-gated and skips cleanly until activation is available.

2. **Existing hosted browser service — rules/network verification**
   - Continue using the deployed browser campaign for authoritative `dc-turn/2.0` rules, persistence, realtime and multiplayer verification.
   - Do not treat the browser UI as the production visual client.

3. **Unity Build Automation — occasional Editor/build fallback**
   - Use only when a real Unity Editor/build result is needed and the free monthly allowance is available.
   - Do not schedule continuous builds.
   - Prefer Linux validation first; delay Windows/Android/WebGL builds until the EditMode gate passes.

4. **GitLab manual pipeline — cold backup**
   - Keep `.gitlab-ci.yml` available.
   - Do not mirror or run it unless GitHub remains unavailable and a GitLab project is already connected.
   - The job stays manual to avoid consuming free minutes during rapid iteration.

## Services intentionally avoided while budget is zero

- Paid Replit subscription for this project.
- Premium 3D stock or generation credits.
- Paid GitHub larger runners.
- Multiple parallel CI vendors.
- Automatic platform builds on every commit.

## Development rule while Unity Editor execution is unavailable

Continue implementing and reviewing source, deterministic game rules, protocol parity, tests, scene generation, camera/movement, HUD, VFX bridges and morph-state logic. Never label Unity runtime behavior as verified until an actual Unity Editor run succeeds.

## Trigger policy

Expensive or license-bound jobs are manual or conditional. Cheap source validation may run automatically. Production player builds begin only after the Unity EditMode gate is green.
