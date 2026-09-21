import { useCallback, useEffect, useRef, useState } from 'react';
import { api, ws } from '@appdeploy/client';
import { objectiveFor, type ActionType, type GameSession } from './gameTypes';

const STORAGE_KEY = 'dungeons-crows-player-v2';

type SavedIdentity = {
  code: string;
  playerId: string;
  playerName: string;
};

function messageFrom(error: unknown): string {
  if (error instanceof Error) return error.message;
  return String(error || 'The crypt rejected that action.');
}

function saveIdentity(identity: SavedIdentity) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(identity));
}

function clearIdentity() {
  localStorage.removeItem(STORAGE_KEY);
}

export function useGameSession() {
  const [session, setSession] = useState<GameSession | null>(null);
  const [playerId, setPlayerId] = useState('');
  const [playerName, setPlayerName] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [connectionId, setConnectionId] = useState('');
  const [subscribed, setSubscribed] = useState(false);
  const connectionRef = useRef<ReturnType<typeof ws.connect> | null>(null);
  const activeCodeRef = useRef('');

  useEffect(() => {
    const conn = ws.connect();
    connectionRef.current = conn;
    conn.onMessage(message => {
      if (message?.type !== 'entity.update') return;
      const payload = message.payload;
      if (payload?.entity_type === 'game-session' && payload?.entity_id === activeCodeRef.current && payload?.data) {
        setSession(payload.data as GameSession);
      }
    });
    conn.ready.then(() => setConnectionId(conn.connectionId || '')).catch(() => setConnectionId(''));
    return () => conn.disconnect();
  }, []);

  useEffect(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return;
    try {
      const saved = JSON.parse(raw) as Partial<SavedIdentity>;
      if (!saved.code || !saved.playerId || !saved.playerName) {
        clearIdentity();
        return;
      }
      setBusy(true);
      api.get('/api/sessions/' + saved.code)
        .then(response => {
          if (!response.data?.session) throw new Error('Saved Hunt was not found.');
          setSession(response.data.session as GameSession);
          setPlayerId(saved.playerId || '');
          setPlayerName(saved.playerName || '');
        })
        .catch(() => clearIdentity())
        .finally(() => setBusy(false));
    } catch {
      clearIdentity();
    }
  }, []);

  useEffect(() => {
    const code = session?.code;
    if (!code || !connectionId) {
      setSubscribed(false);
      return;
    }
    activeCodeRef.current = code;
    let cancelled = false;
    setSubscribed(false);
    api.get('/api/sessions/' + code)
      .then(response => {
        if (!cancelled && response.data?.session) setSession(response.data.session as GameSession);
      })
      .catch(() => undefined);
    api.post('/api/subscriptions', { entity_type: 'game-session', entity_id: code, connection_id: connectionId })
      .then(() => { if (!cancelled) setSubscribed(true); })
      .catch(() => { if (!cancelled) setSubscribed(false); });
    return () => {
      cancelled = true;
      setSubscribed(false);
      activeCodeRef.current = '';
      api.post('/api/subscriptions/remove', { entity_type: 'game-session', entity_id: code, connection_id: connectionId }).catch(() => undefined);
    };
  }, [session?.code, connectionId]);

  const createSession = useCallback(async (name: string) => {
    setBusy(true);
    setError('');
    try {
      const cleanName = name.trim();
      const response = await api.post('/api/sessions', { name: cleanName });
      const nextSession = response.data.session as GameSession;
      const nextPlayerId = String(response.data.playerId);
      setSession(nextSession);
      setPlayerId(nextPlayerId);
      setPlayerName(cleanName);
      saveIdentity({ code: nextSession.code, playerId: nextPlayerId, playerName: cleanName });
    } catch (err) {
      setError(messageFrom(err));
    } finally {
      setBusy(false);
    }
  }, []);

  const joinSession = useCallback(async (code: string, name: string) => {
    setBusy(true);
    setError('');
    try {
      const cleanName = name.trim();
      const response = await api.post('/api/sessions/join', {
        code: code.trim().toUpperCase(),
        name: cleanName,
      });
      const nextSession = response.data.session as GameSession;
      const nextPlayerId = String(response.data.playerId);
      setSession(nextSession);
      setPlayerId(nextPlayerId);
      setPlayerName(cleanName);
      saveIdentity({ code: nextSession.code, playerId: nextPlayerId, playerName: cleanName });
    } catch (err) {
      setError(messageFrom(err));
    } finally {
      setBusy(false);
    }
  }, []);

  const submitTurn = useCallback(async (actionType: ActionType, freeformText?: string) => {
    if (!session || session.campaignStatus !== 'active') return;
    setBusy(true);
    setError('');
    try {
      const objective = objectiveFor(session);
      const response = await api.post('/api/sessions/' + session.code + '/turn', {
        actorId: playerId,
        actorName: playerName,
        actionType,
        targetId: actionType === 'interact' ? objective.id : session.enemy.id,
        freeformText: (freeformText || '').slice(0, 240),
        connectionId,
      });
      setSession(response.data.session as GameSession);
    } catch (err) {
      setError(messageFrom(err));
    } finally {
      setBusy(false);
    }
  }, [session, playerId, playerName, connectionId]);

  const leaveSession = useCallback(() => {
    clearIdentity();
    activeCodeRef.current = '';
    setSubscribed(false);
    setSession(null);
    setPlayerId('');
    setPlayerName('');
    setError('');
  }, []);

  return {
    session,
    playerId,
    error,
    busy,
    connected: subscribed,
    createSession,
    joinSession,
    submitTurn,
    leaveSession,
  };
}
