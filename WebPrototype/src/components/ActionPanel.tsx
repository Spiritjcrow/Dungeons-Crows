import { MessageCircle, Shield, Sparkles, Swords } from 'lucide-react';
import { useState } from 'react';
import type { ActionType, GameSession } from '../gameTypes';

export function ActionPanel({ session, playerId, busy, error, onAction }: { session: GameSession; playerId: string; busy: boolean; error: string; onAction: (action: ActionType, text?: string) => Promise<void> }) {
    const [text, setText] = useState('');
    const yours = session.activeActorId === playerId;
    const act = async (action: ActionType) => { await onAction(action, text); if (action === 'speak') setText(''); };
    return <aside className="panel action-panel"><p className="panel-title">Choose Your Turn</p><div className={'turn-banner' + (yours ? ' yours' : '')}>{yours ? 'The crow turns toward you.' : 'Another oath acts first.'}</div><div className="action-grid"><button className="action-button" disabled={busy || !yours || session.enemy.hp <= 0} onClick={() => act('attack')}><Swords size={19} /> Attack</button><button className="action-button" disabled={busy || !yours} onClick={() => act('defend')}><Shield size={19} /> Defend</button><button className="action-button" disabled={busy || !yours || session.altarOpened} onClick={() => act('interact')}><Sparkles size={19} /> Altar</button><button className="action-button" disabled={busy || !yours} onClick={() => act('speak')}><MessageCircle size={19} /> Speak</button></div><label>Spoken action / role-play<input aria-label="Spoken action" maxLength={240} value={text} onChange={e => setText(e.target.value)} placeholder="I ask the Warden who sealed this place…" /></label>{error && <p className="error-banner" role="alert">{error}</p>}<p className="tiny">Rules resolve first. The Dungeon Master narrates the accepted result afterward.</p></aside>;
}