import { Bird, Eye } from 'lucide-react';
import type { GameSession } from '../gameTypes';

const positions = [{ left: '30%', top: '72%' }, { left: '44%', top: '78%' }, { left: '58%', top: '78%' }, { left: '72%', top: '72%' }];

export function CryptBoard({ session, playerId }: { session: GameSession; playerId: string }) {
    return <section className="panel board-panel" aria-label="Crypt encounter board"><div className="crypt-floor" /><div className="warden-hp">CROWBOUND WARDEN · HP {session.enemy.hp}/{session.enemy.maxHp}</div><div className="enemy-token"><Eye size={25} /><span>WARDEN</span></div><div className={'altar' + (session.altarOpened ? ' open' : '')}><Bird size={26} /><span className="tiny">{session.altarOpened ? 'AWAKE' : 'SEALED'}</span></div>{session.party.map((member, index) => <div key={member.id} className={'player-token' + (member.id === playerId ? ' me' : '')} style={positions[index] || positions[0]}><Bird size={16} /><span>{member.name.slice(0, 8)}</span></div>)}</section>;
}