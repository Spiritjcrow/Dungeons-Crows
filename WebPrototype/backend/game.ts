export type ActionType = 'attack' | 'defend' | 'interact' | 'speak' | 'invoke';
export type CampaignStatus = 'active' | 'victory' | 'defeat';

export type Combatant = {
  id: string;
  name: string;
  hp: number;
  maxHp: number;
  essence: number;
  maxEssence: number;
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

export type TurnIntent = {
  sessionId: string;
  actorId: string;
  actorName: string;
  actionType: ActionType;
  targetId?: string;
  freeformText?: string;
};

export type TurnResolution = {
  accepted: boolean;
  deterministicResult: string;
  diceRolls: number[];
  state: GameSession;
  narrationSeed: string;
};

const encounters = {
  1: { id: 'warden', name: 'Crowbound Warden', hp: 14, defense: 11 },
  2: { id: 'bone-rook', name: 'Bone Rook', hp: 18, defense: 12 },
  3: { id: 'black-rook', name: 'Black Rook', hp: 24, defense: 13 },
} as const;

export function seededD20(seed: number, turnNumber: number, salt: number): number {
  let x = (seed ^ Math.imul(turnNumber, 1103515245) ^ salt) >>> 0;
  x ^= x << 13;
  x ^= x >>> 17;
  x ^= x << 5;
  return ((x >>> 0) % 20) + 1;
}

function enemyFor(chapter: number): Combatant {
  const e = encounters[(chapter >= 3 ? 3 : chapter <= 1 ? 1 : 2) as 1 | 2 | 3];
  return {
    id: e.id,
    name: e.name,
    hp: e.hp,
    maxHp: e.hp,
    essence: 0,
    maxEssence: 0,
    defense: e.defense,
    isEnemy: true,
  };
}

function objectiveComplete(session: GameSession): boolean {
  if (session.chapter === 1) return session.altarOpened;
  if (session.chapter === 2) return session.rookeryPurified;
  return session.crownBroken;
}

function addRelic(session: GameSession, relic: string) {
  if (!session.relics.includes(relic)) session.relics.push(relic);
}

function nextLivingActor(session: GameSession, currentActorId: string): string {
  const living = session.party.filter(member => member.hp > 0);
  if (living.length === 0) return '';
  const index = living.findIndex(member => member.id === currentActorId);
  return living[(index + 1 + living.length) % living.length].id;
}

function healBetweenChapters(session: GameSession) {
  session.party.forEach(member => {
    if (member.hp > 0) member.hp = member.maxHp;
    member.essence = member.maxEssence;
    member.defendingUntilTurn = 0;
  });
  session.ritualAttempts = 0;
}

function advanceCampaign(session: GameSession, result: string): string {
  if (session.enemy.hp > 0 || !objectiveComplete(session)) return result;

  if (session.chapter === 1) {
    session.chapter = 2;
    session.enemy = enemyFor(2);
    session.score += 150;
    healBetweenChapters(session);
    session.activeActorId = session.party.find(member => member.hp > 0)?.id || '';
    session.log.push({
      turn: session.turnNumber,
      kind: 'system',
      text: 'Chapter II: The Bone Rookery opens beneath the crypt.',
    });
    return result + ' The sealed floor splits, revealing the Bone Rookery below.';
  }

  if (session.chapter === 2) {
    session.chapter = 3;
    session.enemy = enemyFor(3);
    session.score += 250;
    healBetweenChapters(session);
    session.activeActorId = session.party.find(member => member.hp > 0)?.id || '';
    session.log.push({
      turn: session.turnNumber,
      kind: 'system',
      text: 'Chapter III: The Black Rook waits upon the broken throne.',
    });
    return result + ' The purified rookery exhales a stairway of black stone toward the throne.';
  }

  session.campaignStatus = 'victory';
  session.activeActorId = '';
  session.score += 750;
  session.log.push({
    turn: session.turnNumber,
    kind: 'system',
    text: 'Victory: The Black Rook Oath is broken.',
  });
  return result + ' The Black Rook falls and the oath binding the crypt is broken. The campaign is won.';
}

export function migrateSessionState(input: GameSession): GameSession {
  const legacy = input as GameSession & Partial<{
    chapter: number;
    campaignStatus: CampaignStatus;
    rookeryPurified: boolean;
    crownBroken: boolean;
    relics: string[];
    ritualAttempts: number;
    score: number;
  }>;

  const next = structuredClone(input) as GameSession;

  next.chapter = legacy.chapter || 1;
  next.campaignStatus = legacy.campaignStatus || 'active';
  next.altarOpened = Boolean(legacy.altarOpened);
  next.rookeryPurified = Boolean(legacy.rookeryPurified);
  next.crownBroken = Boolean(legacy.crownBroken);
  next.relics = Array.isArray(legacy.relics) ? [...legacy.relics] : [];
  next.ritualAttempts = Number.isFinite(legacy.ritualAttempts)
    ? Number(legacy.ritualAttempts)
    : 0;
  next.score = Number.isFinite(legacy.score)
    ? Number(legacy.score)
    : 0;

  next.party = (next.party || []).map(member => {
    const priorMax = member.maxHp || 20;
    const maxHp = Math.max(20, priorMax);
    const migrationHeal = priorMax < 20 ? 20 - priorMax : 0;
    const maxEssence = Number.isFinite(member.maxEssence)
      ? Math.max(4, Number(member.maxEssence))
      : 4;
    const essence = Number.isFinite(member.essence)
      ? Math.max(0, Math.min(maxEssence, Number(member.essence)))
      : maxEssence;

    return {
      ...member,
      maxHp,
      hp: Math.min(maxHp, Math.max(0, member.hp ?? maxHp) + migrationHeal),
      essence,
      maxEssence,
      defense: member.defense || 10,
    };
  });

  if (!next.enemy || !next.enemy.name) {
    next.enemy = enemyFor(next.chapter);
  } else {
    next.enemy = {
      ...next.enemy,
      essence: 0,
      maxEssence: 0,
    };
  }

  if (next.chapter === 1 && next.enemy.hp <= 0 && next.altarOpened) {
    next.chapter = 2;
    next.enemy = enemyFor(2);
    next.ritualAttempts = 0;
  }

  next.log = Array.isArray(next.log)
    ? next.log.slice(-40)
    : [];

  return next;
}

export function createSessionState(
  code: string,
  name: string,
  seed: number
): { session: GameSession; playerId: string } {
  const playerId = code + '-p1';

  const session: GameSession = {
    code,
    turnNumber: 1,
    activeActorId: playerId,
    chapter: 1,
    campaignStatus: 'active',
    altarOpened: false,
    rookeryPurified: false,
    crownBroken: false,
    relics: [],
    ritualAttempts: 0,
    score: 0,
    seed,
    party: [{
      id: playerId,
      name,
      hp: 20,
      maxHp: 20,
      essence: 4,
      maxEssence: 4,
      defense: 10,
      isEnemy: false,
    }],
    enemy: enemyFor(1),
    lastNarration:
      'The crypt door grinds shut. A black-feathered Warden raises its rusted blade beside a sealed crow altar.',
    log: [{
      turn: 0,
      kind: 'system',
      text: name + ' entered the Crow Crypt. Chapter I begins.',
    }],
  };

  return { session, playerId };
}

export function addPlayer(
  sessionInput: GameSession,
  name: string
): { session: GameSession; playerId: string } {
  const next = migrateSessionState(sessionInput);
  const playerId =
    next.code +
    '-p' +
    (next.party.length + 1) +
    '-' +
    Math.floor(Math.random() * 9999).toString(36);

  next.party.push({
    id: playerId,
    name,
    hp: 20,
    maxHp: 20,
    essence: 4,
    maxEssence: 4,
    defense: 10,
    isEnemy: false,
  });

  next.log.push({
    turn: next.turnNumber,
    kind: 'system',
    text: name + ' joined the hunt.',
  });
  next.log = next.log.slice(-40);

  return { session: next, playerId };
}

export function resolveTurn(
  sessionInput: GameSession,
  intent: TurnIntent
): TurnResolution {
  const session = migrateSessionState(sessionInput);
  const next = structuredClone(session);

  if (session.campaignStatus !== 'active') {
    return {
      accepted: false,
      deterministicResult: 'This hunt has already ended.',
      diceRolls: [],
      state: session,
      narrationSeed: '',
    };
  }

  if (intent.actorId !== session.activeActorId) {
    return {
      accepted: false,
      deterministicResult: 'It is not your turn.',
      diceRolls: [],
      state: session,
      narrationSeed: '',
    };
  }

  const actorIndex =
    next.party.findIndex(
      member => member.id === intent.actorId);

  if (actorIndex < 0) {
    return {
      accepted: false,
      deterministicResult: 'That adventurer is not part of this hunt.',
      diceRolls: [],
      state: session,
      narrationSeed: '',
    };
  }

  const actor = next.party[actorIndex];

  if (actor.hp <= 0) {
    return {
      accepted: false,
      deterministicResult: 'You cannot act while fallen.',
      diceRolls: [],
      state: session,
      narrationSeed: '',
    };
  }

  const rolls: number[] = [];
  let result = '';

  if (intent.actionType === 'attack') {
    if (next.enemy.hp <= 0) {
      return {
        accepted: false,
        deterministicResult:
          next.enemy.name +
          ' has already fallen. Complete the chapter objective.',
        diceRolls: [],
        state: session,
        narrationSeed: '',
      };
    }

    const roll = seededD20(
      next.seed,
      next.turnNumber,
      7 + actorIndex + next.chapter * 13);

    rolls.push(roll);

    const hit =
      roll + 4 + next.relics.length >=
      next.enemy.defense;

    if (hit) {
      next.enemy.hp =
        Math.max(0, next.enemy.hp - 4);
      next.score += 25;
    }

    result =
      actor.name +
      (hit
        ? ' strikes ' +
          next.enemy.name +
          ' for 4 damage'
        : ' misses ' + next.enemy.name) +
      ' (d20 ' + roll + ').';
  } else if (intent.actionType === 'invoke') {
    if (next.enemy.hp <= 0) {
      return {
        accepted: false,
        deterministicResult:
          next.enemy.name +
          ' has already fallen. Complete the chapter objective.',
        diceRolls: [],
        state: session,
        narrationSeed: '',
      };
    }

    if (actor.essence < 2) {
      return {
        accepted: false,
        deterministicResult:
          'Invoke requires 2 Crow Essence. Defend to recover Essence.',
        diceRolls: [],
        state: session,
        narrationSeed: '',
      };
    }

    actor.essence -= 2;

    const roll = seededD20(
      next.seed,
      next.turnNumber,
      313 + actorIndex + next.chapter * 37);

    rolls.push(roll);

    const hit =
      roll + 3 + next.relics.length >=
      next.enemy.defense;

    if (hit) {
      next.enemy.hp =
        Math.max(0, next.enemy.hp - 5);
      next.score += 35;
    }

    result =
      actor.name +
      ' spends 2 Crow Essence and invokes crowfire, ' +
      (hit
        ? 'searing ' +
          next.enemy.name +
          ' for 5 damage'
        : 'but the invocation misses ' +
          next.enemy.name) +
      ' (d20 ' + roll + ').';
  } else if (intent.actionType === 'defend') {
    actor.defendingUntilTurn =
      next.turnNumber + 1;
    actor.hp =
      Math.min(actor.maxHp, actor.hp + 2);

    const essenceBefore = actor.essence;
    actor.essence =
      Math.min(
        actor.maxEssence,
        actor.essence + 1);
    const essenceRestored =
      actor.essence - essenceBefore;

    next.score += 5;

    result =
      actor.name +
      ' braces behind stone and steel, restores 2 resolve' +
      (essenceRestored > 0
        ? ' and ' +
          essenceRestored +
          ' Crow Essence'
        : '') +
      ', and gains guard through the next attack.';
  } else if (intent.actionType === 'interact') {
    const expectedTarget =
      next.chapter === 1
        ? 'altar'
        : next.chapter === 2
          ? 'rookery'
          : 'crown';

    if (intent.targetId !== expectedTarget) {
      return {
        accepted: false,
        deterministicResult:
          'That is not the active chapter objective.',
        diceRolls: [],
        state: session,
        narrationSeed: '',
      };
    }

    if (objectiveComplete(next)) {
      return {
        accepted: false,
        deterministicResult:
          'The chapter objective is already complete.',
        diceRolls: [],
        state: session,
        narrationSeed: '',
      };
    }

    if (next.chapter === 1) {
      next.altarOpened = true;
      addRelic(next, 'Ember Feather');
      next.score += 75;
      result =
        actor.name +
        ' breaks the wax seal. The crow altar wakes and yields the Ember Feather.';
    } else {
      const roll = seededD20(
        next.seed,
        next.turnNumber,
        211 + actorIndex + next.chapter * 19);

      rolls.push(roll);
      next.ritualAttempts += 1;

      const bonus = next.relics.length;
      const target =
        next.chapter === 2 ? 10 : 12;
      const success =
        roll + bonus >= target ||
        next.ritualAttempts >= 3;

      if (success && next.chapter === 2) {
        next.rookeryPurified = true;
        addRelic(next, 'Bone Quill');
        next.score += 100;
        result =
          actor.name +
          ' purifies the Bone Rookery and claims the Bone Quill (d20 ' +
          roll +
          ' +' +
          bonus +
          '; rite ' +
          next.ritualAttempts +
          '/3).';
      } else if (success) {
        next.crownBroken = true;
        addRelic(next, 'Shattered Crown');
        next.enemy.defense =
          Math.max(9, next.enemy.defense - 2);
        next.score += 150;
        result =
          actor.name +
          ' breaks the Black Rook Crown, weakening the throne-bound foe (d20 ' +
          roll +
          ' +' +
          bonus +
          '; rite ' +
          next.ritualAttempts +
          '/3).';
      } else {
        result =
          actor.name +
          ' advances the rite but does not complete it (d20 ' +
          roll +
          ' +' +
          bonus +
          ' vs ' +
          target +
          '; rite ' +
          next.ritualAttempts +
          '/3).';
      }
    }
  } else {
    const spoken =
      (intent.freeformText || '').trim();

    result = spoken
      ? actor.name +
        ' says: “' +
        spoken.slice(0, 240) +
        '”'
      : actor.name +
        ' calls into the dark, but offers no words.';
  }

  if (next.enemy.hp > 0) {
    const enemyRoll = seededD20(
      next.seed,
      next.turnNumber,
      101 + actorIndex + next.chapter * 29);

    rolls.push(enemyRoll);

    const guard =
      (actor.defendingUntilTurn || 0) >=
      next.turnNumber
        ? 3 + next.relics.length
        : 0;

    const attackBonus =
      next.chapter + 1;
    const damage = 2;

    const hit =
      enemyRoll + attackBonus >=
      actor.defense + guard;

    if (hit) {
      actor.hp =
        Math.max(0, actor.hp - damage);
    }

    result +=
      ' ' +
      next.enemy.name +
      (hit
        ? ' answers for ' +
          damage +
          ' damage'
        : ' attacks, but misses') +
      ' (d20 ' +
      enemyRoll +
      ').';
  }

  next.turnNumber += 1;

  if (next.party.every(member => member.hp <= 0)) {
    next.campaignStatus = 'defeat';
    next.activeActorId = '';
    next.log.push({
      turn: next.turnNumber,
      kind: 'system',
      text: 'Defeat: every adventurer has fallen.',
    });
    result +=
      ' The last adventurer falls. The hunt is lost.';
  } else {
    next.activeActorId =
      nextLivingActor(next, actor.id);
    result =
      advanceCampaign(next, result);
  }

  next.log =
    next.log.slice(-40);

  return {
    accepted: true,
    deterministicResult: result,
    diceRolls: rolls,
    state: next,
    narrationSeed: result,
  };
}
