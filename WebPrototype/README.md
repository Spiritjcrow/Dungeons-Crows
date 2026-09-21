# WebPrototype

Source mirror for the deployed Dungeons & Crows online campaign alpha.

Live runtime: https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai/

Protocol: `dc-turn/2.0`

Hosting baseline: AppDeploy `frontend+backend` + `react-vite`. AppDeploy injects `@appdeploy/client` in the frontend and `@appdeploy/sdk` in the backend.

Implemented runtime capabilities:
- backend HTTP API,
- bounded database persistence,
- WebSocket entity subscriptions,
- server-side AI narration,
- deterministic three-chapter campaign engine,
- victory and defeat locks,
- v1 persisted-session migration.

The authoritative rules boundary is `backend/game.ts`. AI narration in `backend/narrator.ts` receives resolved mechanics and cannot commit canonical game-state mutations.

The browser build is currently the verified playable client. Unity 6 + URP remains the intended production 3D renderer and now carries matching v2 DTOs, but Unity scene integration is not represented as complete until editor/runtime verification passes.
