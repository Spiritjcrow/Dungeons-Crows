import { router, json, error } from '@appdeploy/sdk';
import { resolveTurn, type ActionType } from './game';
import { narrate } from './narrator';
import { protocolDescriptor, withProtocol } from './protocol';
import {
  createSession,
  joinSession,
  loadSession,
  saveSession,
} from './sessions';
import {
  notifySubscribers,
  realtimeSubscriptionRoutes,
} from './realtime-subscribers';

function bodyObject(body: unknown): Record<string, unknown> {
  return body && typeof body === 'object'
    ? (body as Record<string, unknown>)
    : {};
}

export const handler = router({
  'GET /api/_healthcheck': [async () => json({ message: 'Success' })],
  'GET /api/protocol': [async () => json(protocolDescriptor)],
  'POST /api/sessions': [
    async ({ body }) => {
      try {
        const data = bodyObject(body);
        const created = await createSession(String(data.name || ''));
        return json(withProtocol(created), 201);
      } catch (err) {
        return error(
          err instanceof Error ? err.message : 'Could not create session.',
          400
        );
      }
    },
  ],
  'POST /api/sessions/join': [
    async ({ body }) => {
      try {
        const data = bodyObject(body);
        const joined = await joinSession(
          String(data.code || ''),
          String(data.name || '')
        );
        await notifySubscribers(
          'game-session',
          joined.session.code,
          joined.session
        );
        return json(withProtocol(joined));
      } catch (err) {
        return error(
          err instanceof Error ? err.message : 'Could not join session.',
          400
        );
      }
    },
  ],
  'GET /api/sessions/:code': [
    async ({ params }) => {
      const stored = await loadSession(params.code || '');
      if (!stored) return error('No hunt exists under that session code.', 404);
      return json(withProtocol({ session: stored.session }));
    },
  ],
  'POST /api/sessions/:code/turn': [
    async ({ params, body }) => {
      try {
        const stored = await loadSession(params.code || '');
        if (!stored)
          return error('No hunt exists under that session code.', 404);
        const data = bodyObject(body);
        const action = String(data.actionType || '') as ActionType;
        if (!['attack', 'defend', 'interact', 'speak'].includes(action))
          return error('Unknown action.', 400);
        const resolution = resolveTurn(stored.session, {
          sessionId: stored.session.code,
          actorId: String(data.actorId || ''),
          actorName: String(data.actorName || '').slice(0, 24),
          actionType: action,
          targetId: data.targetId ? String(data.targetId) : undefined,
          freeformText: data.freeformText
            ? String(data.freeformText).slice(0, 240)
            : undefined,
        });
        if (!resolution.accepted)
          return error(resolution.deterministicResult, 409);
        const narration = await narrate(resolution, stored.session.log);
        resolution.state.lastNarration = narration;
        resolution.state.log.push({
          turn: stored.session.turnNumber,
          kind: 'action',
          text: resolution.deterministicResult,
        });
        resolution.state.log.push({
          turn: stored.session.turnNumber,
          kind: 'dm',
          text: narration,
        });
        resolution.state.log = resolution.state.log.slice(-40);
        await saveSession(stored.id, resolution.state);
        const connectionId = data.connectionId
          ? String(data.connectionId)
          : undefined;
        await notifySubscribers(
          'game-session',
          resolution.state.code,
          resolution.state,
          connectionId
        );
        return json(withProtocol({
          session: resolution.state,
          narration,
          diceRolls: resolution.diceRolls,
        }));
      } catch (err) {
        return error(
          err instanceof Error
            ? err.message
            : 'The turn could not be resolved.',
          500
        );
      }
    },
  ],
  ...realtimeSubscriptionRoutes,
});
