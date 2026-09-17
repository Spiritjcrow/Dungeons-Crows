using System;
using System.Collections;
using UnityEngine;

namespace DungeonsCrows.Online
{
    /// <summary>
    /// Scene-facing state controller for online hunts. UI and animation systems bind to this
    /// component instead of talking directly to HTTP or resolving rules themselves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineSessionController : MonoBehaviour
    {
        [SerializeField]
        private OnlineGameClient client;

        public GameSessionDto CurrentSession { get; private set; }
        public string PlayerId { get; private set; } = string.Empty;
        public string PlayerName { get; private set; } = string.Empty;
        public bool ProtocolReady { get; private set; }
        public bool Busy { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public event Action<GameSessionDto> SessionChanged;
        public event Action<string> ErrorRaised;
        public event Action<bool> BusyChanged;
        public event Action ProtocolVerified;

        private void Awake()
        {
            if (client == null)
                client = GetComponent<OnlineGameClient>();
        }

        public void VerifyProtocol()
        {
            if (client == null)
            {
                RaiseError("OnlineGameClient is missing from the scene.");
                return;
            }

            StartCoroutine(RunBusy(client.VerifyProtocol(
                descriptor =>
                {
                    ProtocolReady = true;
                    LastError = string.Empty;
                    ProtocolVerified?.Invoke();
                },
                RaiseError)));
        }

        public void CreateHunt(string playerName)
        {
            if (!CanStartRequest()) return;
            PlayerName = NormalizePlayerName(playerName);
            StartCoroutine(RunBusy(client.CreateSession(
                PlayerName,
                response => ApplySessionEnvelope(response, true),
                RaiseError)));
        }

        public void JoinHunt(string sessionCode, string playerName)
        {
            if (!CanStartRequest()) return;
            PlayerName = NormalizePlayerName(playerName);
            StartCoroutine(RunBusy(client.JoinSession(
                sessionCode,
                PlayerName,
                response => ApplySessionEnvelope(response, true),
                RaiseError)));
        }

        public void RefreshSession()
        {
            if (!CanStartRequest() || CurrentSession == null) return;
            StartCoroutine(RunBusy(client.LoadSession(
                CurrentSession.code,
                response => ApplySessionEnvelope(response, false),
                RaiseError)));
        }

        public void SubmitTurn(string actionType, string targetId = null, string freeformText = null)
        {
            if (!CanStartRequest() || CurrentSession == null) return;
            if (string.IsNullOrWhiteSpace(PlayerId))
            {
                RaiseError("This client has no player identity for the current hunt.");
                return;
            }

            var intent = new TurnRequestDto
            {
                actorId = PlayerId,
                actorName = PlayerName,
                actionType = actionType ?? string.Empty,
                targetId = targetId ?? string.Empty,
                freeformText = Truncate(freeformText, OnlineProtocol.MaxFreeformTextLength),
                connectionId = string.Empty
            };

            StartCoroutine(RunBusy(client.SubmitTurn(
                CurrentSession.code,
                intent,
                response =>
                {
                    if (response.session == null)
                    {
                        RaiseError("Turn response did not include canonical session state.");
                        return;
                    }

                    CurrentSession = response.session;
                    LastError = string.Empty;
                    SessionChanged?.Invoke(CurrentSession);
                },
                RaiseError)));
        }

        public bool IsLocalPlayersTurn()
        {
            return CurrentSession != null &&
                   !string.IsNullOrWhiteSpace(PlayerId) &&
                   string.Equals(CurrentSession.activeActorId, PlayerId, StringComparison.Ordinal);
        }

        private bool CanStartRequest()
        {
            if (Busy) return false;
            if (!ProtocolReady)
            {
                RaiseError("Verify the online protocol before starting or changing a hunt.");
                return false;
            }
            return true;
        }

        private void ApplySessionEnvelope(SessionEnvelope response, bool updatePlayerIdentity)
        {
            if (response?.session == null)
            {
                RaiseError("Online response did not include canonical session state.");
                return;
            }

            if (updatePlayerIdentity && !string.IsNullOrWhiteSpace(response.playerId))
                PlayerId = response.playerId;

            CurrentSession = response.session;
            LastError = string.Empty;
            SessionChanged?.Invoke(CurrentSession);
        }

        private IEnumerator RunBusy(IEnumerator operation)
        {
            SetBusy(true);
            yield return operation;
            SetBusy(false);
        }

        private void SetBusy(bool value)
        {
            Busy = value;
            BusyChanged?.Invoke(value);
        }

        private void RaiseError(string message)
        {
            LastError = string.IsNullOrWhiteSpace(message) ? "Unknown online error." : message;
            ErrorRaised?.Invoke(LastError);
        }

        private static string NormalizePlayerName(string value)
        {
            return Truncate((value ?? string.Empty).Trim(), OnlineProtocol.MaxPlayerNameLength);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
