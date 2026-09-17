export type ActionType = 'attack' | 'defend' | 'interact' | 'speak';

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
    altarOpened: boolean;
    seed: number;
    lastNarration: string;
    log: LogEntry[];
};