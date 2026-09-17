import { Bird, Shield } from 'lucide-react';
import type { GameSession } from '../gameTypes';

export function PartyPanel({ session, playerId }: { session: GameSession; playerId: string }) {
    return <aside className="panel party-panel"><p className="panel-title">Party & Initiative</p>{session.party.map((member, index) => {
        const pct = Math.max(0, member.hp / member.maxHp * 100);
        const active = session.activeActorId === member.id;
        return <div className={'party-member' + (active ? ' active' : '')} key={member.id}><div className="token-icon">{member.id === playerId ? <Bird size={18} /> : <Shield size={17} />}</div><div><strong>{member.name}{member.id === playerId ? ' · YOU' : ''}</strong><div className="tiny">HP {member.hp}/{member.maxHp}</div><div className="hpbar"><span style={{ width: pct + '%' }} /></div></div><div className="initiative">#{index + 1}{active ? ' ◀' : ''}</div></div>;
    })}<p className="tiny" style={{ marginTop: 16 }}>Turn {session.turnNumber}. The crow marks the active oath.</p></aside>;
}