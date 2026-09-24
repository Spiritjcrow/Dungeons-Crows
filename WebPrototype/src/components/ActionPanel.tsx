import { MessageCircle, Shield, Sparkles, Swords, Zap } from 'lucide-react';
import { useState } from 'react';
import { objectiveFor, type ActionType, type GameSession } from '../gameTypes';

export function ActionPanel({
  session, playerId, busy, error, onAction,
}: {
  session: GameSession;
  playerId: string;
  busy: boolean;
  error: string;
  onAction: (action: ActionType, text?: string) => Promise<void>;
}) {
  const [text, setText] = useState('');
  const yours = session.activeActorId === playerId;
  const active = session.campaignStatus === 'active';
  const player = session.party.find(member => member.id === playerId);
  const objective = objectiveFor(session);
  const riteSuffix =
    session.chapter > 1 && !objective.complete
      ? ' ' + session.ritualAttempts + '/3'
      : '';

  const act = async (action: ActionType) => {
    await onAction(action, text);
    if (action === 'speak') setText('');
  };

  const canInvoke =
    Boolean(player) &&
    (player?.essence ?? 0) >= 2 &&
    session.enemy.hp > 0;

  return (
    <aside className="panel action-panel">
      <p className="panel-title">Choose Your Turn</p>

      <div className={'turn-banner' + (yours && active ? ' yours' : '')}>
        {!active
          ? (session.campaignStatus === 'victory'
              ? 'The campaign is won.'
              : 'The party has fallen.')
          : yours
            ? 'The crow turns toward you.'
            : 'Another oath acts first.'}
      </div>

      <div className="action-grid">
        <button
          className="action-button"
          disabled={busy || !yours || !active || session.enemy.hp <= 0}
          onClick={() => act('attack')}
        >
          <Swords size={19} /> Attack
        </button>

        <button
          className="action-button"
          disabled={busy || !yours || !active}
          onClick={() => act('defend')}
        >
          <Shield size={19} /> Defend
        </button>

        <button
          className="action-button"
          disabled={busy || !yours || !active || objective.complete}
          onClick={() => act('interact')}
        >
          <Sparkles size={19} /> {objective.label}{riteSuffix}
        </button>

        <button
          className="action-button"
          disabled={busy || !yours || !active || !canInvoke}
          onClick={() => act('invoke')}
        >
          <Zap size={19} /> Invoke −2
        </button>

        <button
          className="action-button"
          disabled={busy || !yours || !active}
          onClick={() => act('speak')}
        >
          <MessageCircle size={19} /> Speak
        </button>
      </div>

      <label>
        Spoken action / role-play
        <input
          aria-label="Spoken action"
          maxLength={240}
          disabled={!active}
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="I ask what oath binds this place…"
        />
      </label>

      {error && <p className="error-banner" role="alert">{error}</p>}

      <p className="tiny">
        Defend restores 2 Resolve, 1 Crow Essence and guard. Invoke costs 2 Essence
        for a deterministic 5-damage crowfire strike on a hit. Chapter rites finish
        no later than the third legal attempt.
      </p>
    </aside>
  );
}
