import { useState } from 'react';
import { Bird, DoorOpen, Feather, LogOut, Swords } from 'lucide-react';
import { ActionPanel } from './components/ActionPanel';
import { Chronicle } from './components/Chronicle';
import { CryptBoard } from './components/CryptBoard';
import { PartyPanel } from './components/PartyPanel';
import { chapterTitle } from './gameTypes';
import { useGameSession } from './useGameSession';

function App() {
  const game = useGameSession();
  const [name, setName] = useState('');
  const [joinCode, setJoinCode] = useState('');
  const [mode, setMode] = useState<'create' | 'join'>('create');

  if (!game.session) {
    const submit = async () => {
      if (mode === 'create') await game.createSession(name);
      else await game.joinSession(joinCode, name);
    };
    return (
      <main className="landing-shell">
        <section className="gate-card">
          <div className="sigil"><Bird size={42} /></div>
          <p className="eyebrow">PLAYABLE ONLINE CAMPAIGN ALPHA</p>
          <h1>Dungeons <span>&</span> Crows</h1>
          <p className="lede">
            Enter a three-chapter gothic hunt. Deterministic rules own combat, objectives,
            relics, victory and defeat while the Dungeon Master narrates only what actually happened.
          </p>
          <div className="mode-tabs">
            <button className={mode === 'create' ? 'active' : ''} onClick={() => setMode('create')}><Feather size={16} /> Create Hunt</button>
            <button className={mode === 'join' ? 'active' : ''} onClick={() => setMode('join')}><DoorOpen size={16} /> Join Hunt</button>
          </div>
          <label>Adventurer name
            <input aria-label="Adventurer name" maxLength={24} value={name} onChange={e => setName(e.target.value)} placeholder="Rook" />
          </label>
          {mode === 'join' && (
            <label>Session code
              <input aria-label="Session code" maxLength={6} value={joinCode} onChange={e => setJoinCode(e.target.value.toUpperCase())} placeholder="CROW42" />
            </label>
          )}
          <button className="primary" disabled={game.busy || name.trim().length < 2 || (mode === 'join' && joinCode.trim().length !== 6)} onClick={submit}>
            <Swords size={18} /> {game.busy ? 'Opening the gate…' : mode === 'create' ? 'Begin Campaign' : 'Join the Party'}
          </button>
          {game.error && <p className="error-banner" role="alert">{game.error}</p>}
          <p className="fineprint">Your current Hunt identity resumes automatically after a refresh on this browser. No cosmetic controls: every visible action is backed by real state.</p>
        </section>
      </main>
    );
  }

  const title = chapterTitle(game.session.chapter);
  return (
    <main className="game-shell">
      <header className="game-header">
        <div>
          <p className="eyebrow">CHAPTER {game.session.chapter} · {title.toUpperCase()}</p>
          <h1>Dungeons <span>&</span> Crows</h1>
        </div>
        <div className="header-actions">
          <div className="session-chip">
            <span className={game.connected ? 'pulse online' : 'pulse'} /> Session <strong>{game.session.code}</strong>
          </div>
          <button className="exit-button" onClick={game.leaveSession}><LogOut size={15} /> Leave Hunt</button>
        </div>
      </header>
      {game.session.campaignStatus !== 'active' && (
        <section className={'campaign-result ' + game.session.campaignStatus}>
          <div>
            <strong>{game.session.campaignStatus === 'victory' ? 'THE BLACK ROOK OATH IS BROKEN' : 'THE OATH IS BROKEN UPON YOU'}</strong>
            <span>Final score: {game.session.score} · Relics: {game.session.relics.length}</span>
          </div>
          <button className="exit-button" onClick={game.leaveSession}>Return to Gate</button>
        </section>
      )}
      <section className="game-grid">
        <PartyPanel session={game.session} playerId={game.playerId} />
        <CryptBoard session={game.session} playerId={game.playerId} />
        <ActionPanel session={game.session} playerId={game.playerId} busy={game.busy} error={game.error} onAction={game.submitTurn} />
        <Chronicle session={game.session} />
      </section>
    </main>
  );
}

export default App;
