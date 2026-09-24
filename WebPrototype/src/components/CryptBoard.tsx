import { Bird, Eye, Skull, Crown } from 'lucide-react';
import { chapterTitle, objectiveFor, type GameSession } from '../gameTypes';

const positions = [
  { left: '30%', top: '72%' }, { left: '44%', top: '78%' },
  { left: '58%', top: '78%' }, { left: '72%', top: '72%' },
];

function ObjectiveIcon({ chapter }: { chapter: number }) {
  if (chapter === 2) return <Skull size={26} />;
  if (chapter === 3) return <Crown size={26} />;
  return <Bird size={26} />;
}

export function CryptBoard({ session, playerId }: { session: GameSession; playerId: string }) {
  const objective = objectiveFor(session);
  return (
    <section className="panel board-panel" aria-label="Campaign encounter board">
      <div className="crypt-floor" />
      <div className="chapter-name">{chapterTitle(session.chapter)}</div>
      <div className="warden-hp">{session.enemy.name.toUpperCase()} · HP {session.enemy.hp}/{session.enemy.maxHp}</div>
      <div className="enemy-token"><Eye size={25} /><span>{session.enemy.name.split(' ').slice(-1)[0].toUpperCase()}</span></div>
      <div className={'altar' + (objective.complete ? ' open' : '')}>
        <ObjectiveIcon chapter={session.chapter} />
        <span className="tiny">{objective.complete ? 'COMPLETE' : objective.label.toUpperCase()}</span>
      </div>
      {session.party.map((member, index) => (
        <div key={member.id} className={'player-token' + (member.id === playerId ? ' me' : '') + (member.hp <= 0 ? ' fallen' : '')} style={positions[index] || positions[0]}>
          <Bird size={16} /><span>{member.name.slice(0, 8)}</span>
        </div>
      ))}
    </section>
  );
}
