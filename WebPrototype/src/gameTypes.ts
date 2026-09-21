export type ActionType = 'attack' | 'defend' | 'interact' | 'speak';
export type CampaignStatus = 'active' | 'victory' | 'defeat';

export type Combatant = {
  id: string;
  name: string;
  hp: number;
  maxHp: number;
  defense: number;
  isEnemy: boolean;
  defendingUntilTurn?: number;
};

export type LogEntry = {
  turn: number;
  kind: 'system' | 'action' | 'dm';
  text: string;
};

export type GameSession = {
  code: string;
  turnNumber: number;
  activeActorId: string;
  party: Combatant[];
  enemy: Combatant;
  chapter: number;
  campaignStatus: CampaignStatus;
  altarOpened: boolean;
  rookeryPurified: boolean;
  crownBroken: boolean;
  relics: string[];
  ritualAttempts: number;
  score: number;
  seed: number;
  lastNarration: string;
  log: LogEntry[];
};

export function chapterTitle(chapter: number): string {
  if (chapter === 2) return 'The Bone Rookery';
  if (chapter === 3) return 'The Black Rook';
  return 'The Crow Crypt';
}

export function objectiveFor(session: GameSession) {
  if (session.chapter === 2) return { id: 'rookery', label: 'Cleanse Rookery', complete: session.rookeryPurified };
  if (session.chapter === 3) return { id: 'crown', label: 'Break Crown', complete: session.crownBroken };
  return { id: 'altar', label: 'Awaken Altar', complete: session.altarOpened };
}
