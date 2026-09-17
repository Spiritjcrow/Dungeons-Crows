export const PROTOCOL_VERSION = 'dc-turn/1.0';

export const protocolDescriptor = {
  protocolVersion: PROTOCOL_VERSION,
  sessionEntityType: 'game-session',
  actions: ['attack', 'defend', 'interact', 'speak'],
  limits: {
    playerName: 24,
    freeformText: 240,
    partySize: 4,
    chronicleEntries: 40,
  },
  sessionFields: [
    'code',
    'turnNumber',
    'activeActorId',
    'party',
    'enemy',
    'altarOpened',
    'seed',
    'lastNarration',
    'log',
  ],
  turnResponseFields: ['session', 'narration', 'diceRolls'],
} as const;

export function withProtocol<T extends Record<string, unknown>>(payload: T) {
  return { protocolVersion: PROTOCOL_VERSION, ...payload };
}
