import type { GameSession } from '../gameTypes';

export function Chronicle({ session }: { session: GameSession }) {
  return (
    <section className="panel chronicle">
      <div className="chronicle-layout">
        <div>
          <p className="panel-title">Dungeon Master</p>
          <div className="dm-copy">{session.lastNarration || 'The chamber waits. Somewhere beyond the stone, wings scrape against the dark.'}</div>
        </div>
        <div>
          <p className="panel-title">Shared Chronicle</p>
          <div className="log-list">
            {session.log.slice(-12).reverse().map((entry, index) => (
              <div className="log-row" key={entry.turn + '-' + index}>
                <strong>T{entry.turn} · {entry.kind}</strong><span>{entry.text}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}
