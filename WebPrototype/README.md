# WebPrototype

Source mirror for the deployed Dungeons & Crows online vertical slice.

Live runtime: https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai/

Hosting baseline: AppDeploy `frontend+backend` + `react-vite` scaffold with generated API/realtime plumbing. The files in this directory are the Dungeons & Crows-specific application code and QA contract. AppDeploy injects `@appdeploy/client` in the frontend and `@appdeploy/sdk` in the backend.

Runtime capabilities used:
- backend router/API,
- bounded key-value database persistence,
- WebSocket entity subscriptions,
- server-side AI generation for narration.

The authoritative rules boundary is `backend/game.ts`. AI narration in `backend/narrator.ts` receives resolved mechanics and cannot commit game-state mutations.

This browser client is an early public rules/network harness. Unity 6 + URP remains the intended production renderer.