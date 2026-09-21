export const PROTOCOL_VERSION = 'dc-turn/2.0';

export const protocolDescriptor = {
  protocolVersion: PROTOCOL_VERSION,
  sessionEntityType: 'game-session',
  actions: ['attack', 'defend', 'interact', 'speak'],
  limits: { playerName: 24, freeformText: 240, partySize: 4, chronicleEntries: 40 },
  chapters: [1, 2, 3],
  campaignStatuses: ['active', 'victory', 'defeat'],
  sessionFields: [
    'code', 'turnNumber', 'activeActorId', 'party', 'enemy', 'chapter',
    'campaignStatus', 'altarOpened', 'rookeryPurified', 'crownBroken',
    'relics', 'ritualAttempts', 'score', 'seed', 'lastNarration', 'log'
  ],
  turnResponseFields: ['session', 'narration', 'diceRolls'],
} as const;

export function withProtocol<T extends Record<string, unknown>>(payload: T) {
  return { protocolVersion: PROTOCOL_VERSION, ...payload };
}
