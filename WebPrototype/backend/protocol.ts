export const PROTOCOL_VERSION = 'dc-turn/3.0';

export const protocolDescriptor = {
  protocolVersion: PROTOCOL_VERSION,
  sessionEntityType: 'game-session',
  actions: ['attack', 'defend', 'interact', 'invoke', 'speak'],
  limits: {
    playerName: 24,
    freeformText: 240,
    partySize: 4,
    chronicleEntries: 40,
  },
  chapters: [1, 2, 3],
  campaignStatuses: ['active', 'victory', 'defeat'],
  combatantFields: [
    'id', 'name', 'hp', 'maxHp', 'essence', 'maxEssence',
    'defense', 'isEnemy', 'defendingUntilTurn'
  ],
  sessionFields: [
    'code', 'turnNumber', 'activeActorId', 'party', 'enemy', 'chapter',
    'campaignStatus', 'altarOpened', 'rookeryPurified', 'crownBroken',
    'relics', 'ritualAttempts', 'score', 'seed', 'lastNarration', 'log'
  ],
  turnResponseFields: [
    'session', 'narration', 'diceRolls', 'resolvedActionType'
  ],
} as const;

export function withProtocol<T extends Record<string, unknown>>(payload: T) {
  return { protocolVersion: PROTOCOL_VERSION, ...payload };
}
