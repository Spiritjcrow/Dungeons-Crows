export type ActionType = 'attack' | 'defend' | 'interact' | 'speak';
export type Combatant = { id: string; name: string; hp: number; maxHp: number; defense: number; isEnemy: boolean; defendingUntilTurn?: number };
export type LogEntry = { turn: number; kind: 'system' | 'action' | 'dm'; text: string };
export type GameSession = { code: string; turnNumber: number; activeActorId: string; party: Combatant[]; enemy: Combatant; altarOpened: boolean; seed: number; lastNarration: string; log: LogEntry[] };
export type TurnIntent = { sessionId: string; actorId: string; actorName: string; actionType: ActionType; targetId?: string; freeformText?: string };
export type TurnResolution = { accepted: boolean; deterministicResult: string; diceRolls: number[]; state: GameSession; narrationSeed: string };

export function seededD20(seed: number, turnNumber: number, salt: number): number {
    let x = (seed ^ Math.imul(turnNumber, 1103515245) ^ salt) >>> 0;
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) % 20 + 1;
}

export function createSessionState(code: string, name: string, seed: number): { session: GameSession; playerId: string } {
    const playerId = code + '-p1';
    const session: GameSession = {
        code, turnNumber: 1, activeActorId: playerId, altarOpened: false, seed,
        party: [{ id: playerId, name, hp: 12, maxHp: 12, defense: 10, isEnemy: false }],
        enemy: { id: 'warden', name: 'Crowbound Warden', hp: 18, maxHp: 18, defense: 11, isEnemy: true },
        lastNarration: 'The crypt door grinds shut. A black-feathered Warden raises its rusted blade beside a sealed crow altar.',
        log: [{ turn: 0, kind: 'system', text: name + ' entered the Crow Crypt.' }],
    };
    return { session, playerId };
}

export function addPlayer(session: GameSession, name: string): { session: GameSession; playerId: string } {
    const next = structuredClone(session);
    const playerId = session.code + '-p' + (session.party.length + 1) + '-' + Math.floor(Math.random() * 9999).toString(36);
    next.party.push({ id: playerId, name, hp: 12, maxHp: 12, defense: 10, isEnemy: false });
    next.log.push({ turn: next.turnNumber, kind: 'system', text: name + ' joined the hunt.' });
    next.log = next.log.slice(-40);
    return { session: next, playerId };
}

export function resolveTurn(session: GameSession, intent: TurnIntent): TurnResolution {
    const next = structuredClone(session);
    if (intent.actorId !== session.activeActorId) return { accepted: false, deterministicResult: 'It is not your turn.', diceRolls: [], state: session, narrationSeed: '' };
    const actorIndex = next.party.findIndex(member => member.id === intent.actorId);
    if (actorIndex < 0) return { accepted: false, deterministicResult: 'That adventurer is not part of this hunt.', diceRolls: [], state: session, narrationSeed: '' };
    const actor = next.party[actorIndex];
    if (actor.hp <= 0) return { accepted: false, deterministicResult: 'You cannot act while fallen.', diceRolls: [], state: session, narrationSeed: '' };

    const rolls: number[] = [];
    let result = '';
    if (intent.actionType === 'attack') {
        if (next.enemy.hp <= 0) return { accepted: false, deterministicResult: 'The Warden has already fallen.', diceRolls: [], state: session, narrationSeed: '' };
        const roll = seededD20(next.seed, next.turnNumber, 7 + actorIndex); rolls.push(roll);
        const hit = roll + 3 >= next.enemy.defense;
        if (hit) next.enemy.hp = Math.max(0, next.enemy.hp - 3);
        result = actor.name + (hit ? ' strikes the Warden for 3 damage' : ' misses the Warden') + ' (d20 ' + roll + ').';
    } else if (intent.actionType === 'defend') {
        actor.defendingUntilTurn = next.turnNumber + 1;
        result = actor.name + ' braces behind stone and steel, gaining +2 defense against the Warden reply.';
    } else if (intent.actionType === 'interact') {
        if (intent.targetId !== 'altar') return { accepted: false, deterministicResult: 'There is nothing there to awaken.', diceRolls: [], state: session, narrationSeed: '' };
        if (next.altarOpened) return { accepted: false, deterministicResult: 'The crow altar is already awake.', diceRolls: [], state: session, narrationSeed: '' };
        next.altarOpened = true;
        result = actor.name + ' breaks the wax seal. The crow altar wakes with ember-light.';
    } else {
        const spoken = (intent.freeformText || '').trim();
        result = spoken ? actor.name + ' says: “' + spoken.slice(0, 240) + '”' : actor.name + ' calls into the crypt, but offers no words.';
    }

    if (next.enemy.hp > 0) {
        const enemyRoll = seededD20(next.seed, next.turnNumber, 101 + actorIndex); rolls.push(enemyRoll);
        const bonus = (actor.defendingUntilTurn || 0) >= next.turnNumber ? 2 : 0;
        const hit = enemyRoll + 2 >= actor.defense + bonus;
        if (hit) actor.hp = Math.max(0, actor.hp - 2);
        result += ' The Warden ' + (hit ? 'answers for 2 damage' : 'answers, but its blade misses') + ' (d20 ' + enemyRoll + ').';
    }

    next.turnNumber += 1;
    const living = next.party.filter(member => member.hp > 0);
    if (living.length > 0) {
        const currentLivingIndex = living.findIndex(member => member.id === actor.id);
        next.activeActorId = living[(currentLivingIndex + 1 + living.length) % living.length].id;
    }
    return { accepted: true, deterministicResult: result, diceRolls: rolls, state: next, narrationSeed: result };
}