import { db } from '@appdeploy/sdk';
import { addPlayer, createSessionState, type GameSession } from './game';

const alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
function tableName(code: string) { return 'game_session_' + code; }
function normalizeCode(code: string) { return code.trim().toUpperCase().replace(/[^A-Z2-9]/g, '').slice(0, 6); }
function validName(name: string) { const clean = name.trim().slice(0, 24); if (clean.length < 2) throw new Error('Name must be 2–24 characters.'); return clean; }
function makeCode() { let code = ''; for (let i = 0; i < 6; i++) code += alphabet[Math.floor(Math.random() * alphabet.length)]; return code; }

export async function loadSession(codeInput: string): Promise<{ id: string; session: GameSession } | null> {
    const code = normalizeCode(codeInput);
    if (code.length !== 6) return null;
    const { items } = await db.list<GameSession>(tableName(code), { limit: 1 });
    if (items.length === 0) return null;
    const { id, ...session } = items[0];
    return { id, session: session as GameSession };
}

export async function saveSession(id: string, session: GameSession): Promise<void> {
    const [ok] = await db.update(tableName(session.code), [{ id, record: session as unknown as Record<string, unknown> }]);
    if (!ok) throw new Error('The crypt could not preserve this turn.');
}

export async function createSession(nameInput: string): Promise<{ session: GameSession; playerId: string }> {
    const name = validName(nameInput);
    for (let attempt = 0; attempt < 10; attempt++) {
        const code = makeCode();
        const existing = await db.list(tableName(code), { limit: 1 });
        if (existing.items.length > 0) continue;
        const created = createSessionState(code, name, Math.floor(Math.random() * 0xffffffff));
        const [id] = await db.add(tableName(code), [created.session as unknown as Record<string, unknown>]);
        if (!id) throw new Error('The crypt failed to open a new hunt.');
        return created;
    }
    throw new Error('Could not mint a unique session code. Try again.');
}

export async function joinSession(codeInput: string, nameInput: string): Promise<{ session: GameSession; playerId: string }> {
    const stored = await loadSession(codeInput);
    if (!stored) throw new Error('No hunt exists under that session code.');
    const name = validName(nameInput);
    if (stored.session.party.length >= 4) throw new Error('This party already has four adventurers.');
    if (stored.session.party.some(member => member.name.toLowerCase() === name.toLowerCase())) throw new Error('That adventurer name is already in this party.');
    const joined = addPlayer(stored.session, name);
    await saveSession(stored.id, joined.session);
    return joined;
}