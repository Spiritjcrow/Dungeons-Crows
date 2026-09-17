import { useCallback, useEffect, useRef, useState } from 'react';
import { api, ws } from '@appdeploy/client';
import type { ActionType, GameSession } from './gameTypes';

function messageFrom(error: unknown): string {
    if (error instanceof Error) return error.message;
    return String(error || 'The crypt rejected that action.');
}

export function useGameSession() {
    const [session, setSession] = useState<GameSession | null>(null);
    const [playerId, setPlayerId] = useState('');
    const [playerName, setPlayerName] = useState('');
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);
    const [connectionId, setConnectionId] = useState('');
    const activeCodeRef = useRef('');

    useEffect(() => {
        const conn = ws.connect();
        conn.onMessage(message => {
            if (message?.type !== 'entity.update') return;
            const payload = message.payload;
            if (payload?.entity_type === 'game-session' && payload?.entity_id === activeCodeRef.current && payload?.data) setSession(payload.data as GameSession);
        });
        conn.ready.then(() => setConnectionId(conn.connectionId || '')).catch(() => setConnectionId(''));
        return () => conn.disconnect();
    }, []);

    useEffect(() => {
        const code = session?.code;
        if (!code || !connectionId) return;
        activeCodeRef.current = code;
        let cancelled = false;
        api.get('/api/sessions/' + code).then(response => {
            if (!cancelled && response.data?.session) setSession(response.data.session as GameSession);
        }).catch(() => undefined);
        api.post('/api/subscriptions', { entity_type: 'game-session', entity_id: code, connection_id: connectionId }).catch(() => undefined);
        return () => {
            cancelled = true;
            activeCodeRef.current = '';
            api.post('/api/subscriptions/remove', { entity_type: 'game-session', entity_id: code, connection_id: connectionId }).catch(() => undefined);
        };
    }, [session?.code, connectionId]);

    const createSession = useCallback(async (name: string) => {
        setBusy(true); setError('');
        try {
            const response = await api.post('/api/sessions', { name: name.trim() });
            setSession(response.data.session as GameSession);
            setPlayerId(String(response.data.playerId));
            setPlayerName(name.trim());
        } catch (err) { setError(messageFrom(err)); }
        finally { setBusy(false); }
    }, []);

    const joinSession = useCallback(async (code: string, name: string) => {
        setBusy(true); setError('');
        try {
            const response = await api.post('/api/sessions/join', { code: code.trim().toUpperCase(), name: name.trim() });
            setSession(response.data.session as GameSession);
            setPlayerId(String(response.data.playerId));
            setPlayerName(name.trim());
        } catch (err) { setError(messageFrom(err)); }
        finally { setBusy(false); }
    }, []);

    const submitTurn = useCallback(async (actionType: ActionType, freeformText?: string) => {
        if (!session) return;
        setBusy(true); setError('');
        try {
            const response = await api.post('/api/sessions/' + session.code + '/turn', {
                actorId: playerId,
                actorName: playerName,
                actionType,
                targetId: actionType === 'interact' ? 'altar' : 'warden',
                freeformText: (freeformText || '').slice(0, 240),
                connectionId,
            });
            setSession(response.data.session as GameSession);
        } catch (err) { setError(messageFrom(err)); }
        finally { setBusy(false); }
    }, [session, playerId, playerName, connectionId]);

    return { session, playerId, error, busy, connected: Boolean(connectionId), createSession, joinSession, submitTurn };
}