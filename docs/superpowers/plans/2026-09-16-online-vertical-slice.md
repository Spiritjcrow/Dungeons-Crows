# Online Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish a public browser-playable Dungeons & Crows turn loop with persistent shared sessions, realtime updates, deterministic combat/interactions, and bounded AI Dungeon Master narration, while preserving Unity 6 as the production renderer.

**Architecture:** AppDeploy hosts the temporary public React/Vite + backend runtime. A small deterministic TypeScript resolver owns combat and world flags; AppDeploy database persists bounded session records; WebSockets broadcast accepted turns; AI generation narrates already-resolved outcomes only. GitHub stores the durable contract/spec and mirrors the browser prototype source after the hosted slice passes QA.

**Tech Stack:** React 19, TypeScript, Vite, AppDeploy API/database/realtime/AI SDK, Unity 6 + URP, GitHub.

**Spec:** `docs/superpowers/specs/2026-09-16-online-vertical-slice-design.md`

## Global Constraints
- Unity 6 + URP remains the canonical production renderer.
- No API secrets in browser code.
- AI narration cannot mutate authoritative HP, initiative, inventory, ownership, economy, or committed state.
- Shared session updates use WebSockets, not polling.
- Public v0.1 accepts only short display names and game actions; no uploads, DMs, trading, payments, or external links.
- Visuals are original gothic-crow designs; do not copy Diablo/Hexen proprietary assets or UI.

---

### Task 1: Deterministic shared turn contract

**Files:**
- Create: hosted `backend/game.ts`
- Create: hosted `src/gameTypes.ts`
- Test: hosted `tests/tests.json`
- Mirror after QA: `WebPrototype/backend/game.ts`, `WebPrototype/src/gameTypes.ts`

**Interfaces:**
- Consumes: `TurnIntent { sessionId, actorId, actorName, actionType, targetId?, freeformText? }`
- Produces: `resolveTurn(session, intent): TurnResolution`

- [ ] **Step 1: Define QA contract before production code**

Use one sanity workflow plus cross-client, interaction, invalid-turn, and mobile tests. At minimum, `covers` must include session creation, session joining, deterministic turn resolution, realtime propagation, non-combat interaction, out-of-turn rejection, and mobile action submission.

- [ ] **Step 2: Implement focused shared types**

```ts
export type ActionType = 'attack' | 'defend' | 'interact' | 'speak';
export type Combatant = { id: string; name: string; hp: number; maxHp: number; defense: number; isEnemy: boolean };
export type GameSession = { code: string; turnNumber: number; activeActorId: string; party: Combatant[]; enemy: Combatant; altarOpened: boolean; seed: number; log: LogEntry[] };
export type TurnIntent = { sessionId: string; actorId: string; actorName: string; actionType: ActionType; targetId?: string; freeformText?: string };
export type TurnResolution = { accepted: boolean; deterministicResult: string; diceRolls: number[]; state: GameSession; narrationSeed: string };
```

- [ ] **Step 3: Implement deterministic seeded d20 helper and resolver**

```ts
export function seededD20(seed: number, turnNumber: number, salt: number): number {
  let x = (seed ^ (turnNumber * 1103515245) ^ salt) >>> 0;
  x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
  return (x >>> 0) % 20 + 1;
}
```

Rules: reject actors who are not active; attack rolls against defense and applies fixed 3 damage on hit; defend grants a one-turn defense bonus encoded in state; interact with `altar` sets `altarOpened=true` once; speak changes no mechanical state. Advance active actor and turn number only on accepted actions.

- [ ] **Step 4: Verify deterministic behavior**

Run hosted QA after deployment and manually repeat the same seeded first attack in two fresh sessions with the same seed. Expected: same roll and mechanical outcome.

- [ ] **Step 5: Commit/mirror only after QA is green**

Commit message: `feat: add deterministic online turn contract`.

### Task 2: Persistent session API + realtime propagation

**Files:**
- Create: hosted `backend/sessions.ts`
- Modify: hosted `backend/index.ts`
- Use existing generated `backend/realtime-subscribers.ts`
- Create: hosted `src/useGameSession.ts`
- Mirror after QA under `WebPrototype/`

**Interfaces:**
- Produces: `POST /api/sessions`, `POST /api/sessions/join`, `GET /api/sessions/:code`, `POST /api/sessions/:code/turn`
- Realtime entity: `{ entity_type: 'game-session', entity_id: sessionCode }`

- [ ] **Step 1: Add API-focused QA coverage before endpoint code**

The cross-client test must create a session in Actor A, join it as Actor B, perform a legal turn in Actor A, and observe the changed turn/log in Actor B without refresh.

- [ ] **Step 2: Implement bounded session persistence**

Use one database table `game_sessions`. Create sessions with a 6-character code and retain the database record ID internally. Keep the log trimmed to 40 entries on every accepted turn.

- [ ] **Step 3: Implement mutation broadcast**

After a successful turn write, call:

```ts
await notifySubscribers('game-session', session.code, savedSession, requestConnectionId);
```

- [ ] **Step 4: Implement initial-load + subscribe hook**

Frontend hook sequence: HTTP GET -> `ws.connect()` -> wait for `ready` -> POST subscription with `connection_id` -> handle `entity.update` for `game-session` -> unsubscribe on session exit -> disconnect only on app unmount.

- [ ] **Step 5: Verify two-browser synchronization**

Expected: second client updates after accepted turn without reload and without HTTP polling.

### Task 3: AI Dungeon Master narration boundary

**Files:**
- Create: hosted `backend/narrator.ts`
- Modify: hosted turn route in `backend/index.ts`
- Mirror after QA under `WebPrototype/backend/narrator.ts`

**Interfaces:**
- Consumes: deterministic `TurnResolution`
- Produces: `narrate(resolution, recentLog): Promise<string>`

- [ ] **Step 1: Add QA expectation for visible narration and failure fallback**

Use an API fault on the turn route only if platform fault injection can isolate narration; otherwise verify ordinary narration in automated QA and deterministic fallback with a direct backend guard.

- [ ] **Step 2: Implement bounded AI generation**

```ts
const result = await ai.generate({
  system: 'You are the Dungeon Master for Dungeons & Crows. Narrate only the already-resolved result. Never alter stats or claim uncommitted mechanical effects.',
  prompt: context,
  maxTokens: 220,
  temperature: 0.7,
  thinkingMode: 'FAST'
});
```

Limit recent context to the last 8 log entries and freeform action text to 240 characters.

- [ ] **Step 3: Add deterministic fallback narration**

On AI failure return a concise string generated solely from `deterministicResult`; do not fail or roll back the accepted turn.

- [ ] **Step 4: Verify state authority**

Expected: mechanical fields are identical whether narration succeeds or falls back.

### Task 4: Public gothic browser UI and deployment

**Files:**
- Modify hosted template: `src/App.tsx`, `src/index.css`, `index.html`
- Create hosted: `src/components/CryptBoard.tsx`, `src/components/PartyPanel.tsx`, `src/components/ActionPanel.tsx`, `src/components/Chronicle.tsx`
- Test: hosted `tests/tests.json`
- Mirror after QA under `WebPrototype/src/`

**Interfaces:**
- Consumes: `useGameSession()` and turn API
- Produces: complete browser-playable session flow

- [ ] **Step 1: Build the smallest complete UI**

Create/join screen; party + initiative panel; stylized crypt board; action buttons; target/action text; Dungeon Master narration; chronological log; connection badge.

- [ ] **Step 2: Apply original gothic-crow styling**

Near-black stone surfaces, warm torch gradients, iron borders, parchment text, subtle fog/noise, CSS crow sigils and token silhouettes. No external proprietary game assets.

- [ ] **Step 3: Make mobile actions usable**

At 375px width, board remains visible above a sticky action tray; no horizontal overflow.

- [ ] **Step 4: Run AppDeploy preflight**

Confirm: exactly one sanity test; 3–5 coverage-complete tests; every new backend route is exercised through a user-visible workflow; realtime has initial load/subscription/update/unsubscribe; no secrets; no direct fetch/axios; no absolute SPA routes.

- [ ] **Step 5: Deploy and poll to terminal status**

Do not publish the URL until deployment status is `ready`. If QA fails, inspect detailed QA output, fix all reported errors in one pass, and redeploy up to three times.

### Task 5: GitHub/Unity integration checkpoint

**Files:**
- Create: `docs/ONLINE_PLAYTEST.md`
- Create/mirror: `WebPrototype/**`
- Modify: `README.md`
- Create later after Unity editor verification: `.github/workflows/unity-webgl.yml`

**Interfaces:**
- Browser and Unity both converge on the canonical turn contract documented in Task 1.

- [ ] **Step 1: Mirror the passing prototype source into GitHub**

Only mirror code that passed hosted QA; keep AppDeploy-specific imports isolated under `WebPrototype/Adapters/AppDeploy` where practical.

- [ ] **Step 2: Document public playtest URL and status**

`docs/ONLINE_PLAYTEST.md` must identify the browser slice as an early public rules/network test harness and Unity as the intended production renderer.

- [ ] **Step 3: Update the draft PR**

Add the online slice, QA result, and public URL to PR #6; keep PR draft until Unity editor compilation is verified.

- [ ] **Step 4: Defer Unity WebGL CI until editor compilation is verified**

Do not add a misleading CI workflow that cannot build without a valid Unity project/editor/license baseline. Once verified, add GameCI or equivalent WebGL automation as a separate reviewed task.

## Self-review
- Spec coverage: public access, deterministic state, realtime, AI boundary, gothic UI, persistence, and Unity boundary are each mapped to tasks.
- Placeholder scan: no TBD/TODO implementation gaps are present.
- Type consistency: `GameSession`, `TurnIntent`, `TurnResolution`, and `game-session` entity naming are consistent across tasks.
