import { Bird, Shield } from 'lucide-react';
import { objectiveFor, type GameSession } from '../gameTypes';

export function PartyPanel({
  session,
  playerId,
}: {
  session: GameSession;
  playerId: string;
}) {
  const objective = objectiveFor(session);

  return (
    <aside className="panel party-panel">
      <p className="panel-title">Party & Initiative</p>

      {session.party.map((member, index) => {
        const hpPct = Math.max(0, (member.hp / member.maxHp) * 100);
        const active = session.activeActorId === member.id;

        return (
          <div
            className={
              'party-member' +
              (active ? ' active' : '') +
              (member.hp <= 0 ? ' fallen' : '')
            }
            key={member.id}
          >
            <div className="token-icon">
              {member.id === playerId
                ? <Bird size={18} />
                : <Shield size={17} />}
            </div>

            <div>
              <strong>
                {member.name}
                {member.id === playerId ? ' · YOU' : ''}
              </strong>

              <div className="tiny">
                HP {member.hp}/{member.maxHp}
                {' · ESS '}
                {member.essence}/{member.maxEssence}
                {member.hp <= 0 ? ' · FALLEN' : ''}
              </div>

              <div className="hpbar">
                <span style={{ width: hpPct + '%' }} />
              </div>
            </div>

            <div className="initiative">
              #{index + 1}{active ? ' ◀' : ''}
            </div>
          </div>
        );
      })}

      <div className="campaign-stats">
        <span>Turn {session.turnNumber}</span>
        <span>Score {session.score}</span>
        <span>Relics {session.relics.length}/3</span>
        {session.chapter > 1 && !objective.complete &&
          <span>Rite {session.ritualAttempts}/3</span>}
      </div>

      {session.relics.length > 0 &&
        <p className="tiny relic-list">
          {session.relics.join(' · ')}
        </p>}
    </aside>
  );
}
