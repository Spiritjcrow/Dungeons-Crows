using System;
using System.Collections;
using UnityEngine;

namespace DungeonsCrows.Online
{
    public sealed class OnlineCampaignController : MonoBehaviour
    {
        [SerializeField] private OnlineGameClient client;
        [SerializeField] private bool verifyProtocolOnStart = true;

        private bool _busy;
        private bool _protocolReady;
        private string _playerId = string.Empty;
        private string _playerName = string.Empty;
        private GameSessionDto _session;

        public bool Busy => _busy;
        public bool ProtocolReady => _protocolReady;
        public string PlayerId => _playerId;
        public string PlayerName => _playerName;
        public GameSessionDto Session => _session;

        public event Action<bool> BusyChanged;
        public event Action<bool> ProtocolReadyChanged;
        public event Action<GameSessionDto> SessionChanged;
        public event Action<GameSessionDto, TurnEnvelope> TurnResolved;
        public event Action<string> ErrorRaised;

        private void Awake()
        {
            if (client == null)
                client = GetComponent<OnlineGameClient>();
        }

        private void Start()
        {
            if (verifyProtocolOnStart)
                VerifyProtocol();
        }

        public void VerifyProtocol()
        {
            if (_busy || client == null) return;
            StartCoroutine(VerifyRoutine());
        }

        public void CreateHunt(string playerName)
        {
            if (!CanStartSession(playerName)) return;
            StartCoroutine(CreateRoutine(playerName.Trim()));
        }

        public void JoinHunt(string code, string playerName)
        {
            if (!CanStartSession(playerName)) return;
            string normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length != 6)
            {
                RaiseError("Hunt code must be six characters.");
                return;
            }

            StartCoroutine(JoinRoutine(normalized, playerName.Trim()));
        }

        public void RefreshSession()
        {
            if (_busy || client == null || _session == null) return;
            StartCoroutine(RefreshRoutine(_session.code));
        }

        public void Attack() => Submit("attack", string.Empty);
        public void Defend() => Submit("defend", string.Empty);
        public void Interact() => Submit("interact", string.Empty);
        public void Speak(string text) => Submit("speak", text);

        private void Submit(string actionType, string freeformText)
        {
            if (_busy || client == null || _session == null) return;
            if (!string.Equals(_session.campaignStatus, "active", StringComparison.Ordinal))
            {
                RaiseError("This Hunt has already ended.");
                return;
            }

            if (!string.Equals(_session.activeActorId, _playerId, StringComparison.Ordinal))
            {
                RaiseError("It is not your turn.");
                return;
            }

            var intent = new TurnRequestDto
            {
                actorId = _playerId,
                actorName = _playerName,
                actionType = actionType,
                targetId = TargetFor(actionType, _session),
                freeformText = (freeformText ?? string.Empty).Trim(),
                connectionId = string.Empty
            };

            if (intent.freeformText.Length > OnlineProtocol.MaxFreeformTextLength)
                intent.freeformText = intent.freeformText.Substring(0, OnlineProtocol.MaxFreeformTextLength);

            StartCoroutine(SubmitRoutine(_session.code, intent));
        }

        private IEnumerator VerifyRoutine()
        {
            SetBusy(true);
            bool done = false;

            yield return client.VerifyProtocol(
                _ =>
                {
                    _protocolReady = true;
                    ProtocolReadyChanged?.Invoke(true);
                    done = true;
                },
                message =>
                {
                    _protocolReady = false;
                    ProtocolReadyChanged?.Invoke(false);
                    RaiseError(message);
                    done = true;
                });

            SetBusy(false);
            if (!done) RaiseError("Protocol verification did not complete.");
        }

        private IEnumerator CreateRoutine(string playerName)
        {
            if (!_protocolReady)
            {
                RaiseError("Online protocol is not ready.");
                yield break;
            }

            SetBusy(true);
            yield return client.CreateSession(
                playerName,
                envelope =>
                {
                    _playerName = playerName;
                    _playerId = envelope.playerId ?? string.Empty;
                    SetSession(envelope.session);
                },
                RaiseError);
            SetBusy(false);
        }

        private IEnumerator JoinRoutine(string code, string playerName)
        {
            if (!_protocolReady)
            {
                RaiseError("Online protocol is not ready.");
                yield break;
            }

            SetBusy(true);
            yield return client.JoinSession(
                code,
                playerName,
                envelope =>
                {
                    _playerName = playerName;
                    _playerId = envelope.playerId ?? string.Empty;
                    SetSession(envelope.session);
                },
                RaiseError);
            SetBusy(false);
        }

        private IEnumerator RefreshRoutine(string code)
        {
            SetBusy(true);
            yield return client.LoadSession(
                code,
                envelope => SetSession(envelope.session),
                RaiseError);
            SetBusy(false);
        }

        private IEnumerator SubmitRoutine(string code, TurnRequestDto intent)
        {
            SetBusy(true);
            GameSessionDto before = _session;

            yield return client.SubmitTurn(
                code,
                intent,
                envelope =>
                {
                    _session = envelope.session;
                    TurnResolved?.Invoke(before, envelope);
                    SessionChanged?.Invoke(_session);
                },
                RaiseError);

            SetBusy(false);
        }

        private bool CanStartSession(string playerName)
        {
            if (_busy || client == null) return false;
            if (!_protocolReady)
            {
                RaiseError("Online protocol is not ready.");
                return false;
            }

            string clean = (playerName ?? string.Empty).Trim();
            if (clean.Length < 2 || clean.Length > OnlineProtocol.MaxPlayerNameLength)
            {
                RaiseError("Player name must be 2-24 characters.");
                return false;
            }

            return true;
        }

        private static string TargetFor(string actionType, GameSessionDto session)
        {
            if (actionType == "interact")
            {
                if (session.chapter >= 3) return "crown";
                if (session.chapter == 2) return "rookery";
                return "altar";
            }

            return session.enemy != null ? session.enemy.id : string.Empty;
        }

        private void SetSession(GameSessionDto session)
        {
            _session = session;
            SessionChanged?.Invoke(_session);
        }

        private void SetBusy(bool busy)
        {
            if (_busy == busy) return;
            _busy = busy;
            BusyChanged?.Invoke(_busy);
        }

        private void RaiseError(string message)
        {
            ErrorRaised?.Invoke(string.IsNullOrWhiteSpace(message)
                ? "Unknown online error."
                : message);
        }
    }
}
