using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DungeonsCrows.Online
{
    /// <summary>
    /// Thin REST adapter for the hosted Dungeons & Crows authoritative turn service.
    /// This class transports intents and canonical state; it never resolves combat locally.
    /// </summary>
    public sealed class OnlineGameClient : MonoBehaviour
    {
        [SerializeField]
        private string baseUrl = "https://dungeons-crows-online-qmp5ax.v2.appdeploy.ai";

        [SerializeField]
        private int requestTimeoutSeconds = 15;

        public string BaseUrl => NormalizeBaseUrl(baseUrl);

        public IEnumerator VerifyProtocol(
            Action<ProtocolDescriptor> onSuccess,
            Action<string> onError)
        {
            yield return GetJson<ProtocolDescriptor>(
                "/api/protocol",
                descriptor =>
                {
                    if (descriptor == null || !ProtocolCompatibility.IsCompatible(descriptor.protocolVersion))
                    {
                        onError?.Invoke(
                            $"Online protocol mismatch. Unity expects {OnlineProtocol.Version}, " +
                            $"server reported {descriptor?.protocolVersion ?? "<none>"}.");
                        return;
                    }

                    onSuccess?.Invoke(descriptor);
                },
                onError);
        }

        public IEnumerator CreateSession(
            string playerName,
            Action<SessionEnvelope> onSuccess,
            Action<string> onError)
        {
            var request = new CreateSessionRequestDto { name = (playerName ?? string.Empty).Trim() };
            yield return PostJson(
                "/api/sessions",
                request,
                (SessionEnvelope response) => ValidateEnvelope(response, onSuccess, onError),
                onError);
        }

        public IEnumerator JoinSession(
            string sessionCode,
            string playerName,
            Action<SessionEnvelope> onSuccess,
            Action<string> onError)
        {
            var request = new JoinSessionRequestDto
            {
                code = (sessionCode ?? string.Empty).Trim().ToUpperInvariant(),
                name = (playerName ?? string.Empty).Trim()
            };

            yield return PostJson(
                "/api/sessions/join",
                request,
                (SessionEnvelope response) => ValidateEnvelope(response, onSuccess, onError),
                onError);
        }

        public IEnumerator LoadSession(
            string sessionCode,
            Action<SessionEnvelope> onSuccess,
            Action<string> onError)
        {
            string code = UnityWebRequest.EscapeURL((sessionCode ?? string.Empty).Trim().ToUpperInvariant());
            yield return GetJson<SessionEnvelope>(
                "/api/sessions/" + code,
                response => ValidateEnvelope(response, onSuccess, onError),
                onError);
        }

        public IEnumerator SubmitTurn(
            string sessionCode,
            TurnRequestDto intent,
            Action<TurnEnvelope> onSuccess,
            Action<string> onError)
        {
            string code = UnityWebRequest.EscapeURL((sessionCode ?? string.Empty).Trim().ToUpperInvariant());
            yield return PostJson(
                "/api/sessions/" + code + "/turn",
                intent,
                (TurnEnvelope response) =>
                {
                    if (response == null || !ProtocolCompatibility.IsCompatible(response.protocolVersion))
                    {
                        onError?.Invoke("Turn response used an incompatible online protocol.");
                        return;
                    }

                    onSuccess?.Invoke(response);
                },
                onError);
        }

        private void ValidateEnvelope(
            SessionEnvelope response,
            Action<SessionEnvelope> onSuccess,
            Action<string> onError)
        {
            if (response == null || !ProtocolCompatibility.IsCompatible(response.protocolVersion))
            {
                onError?.Invoke("Session response used an incompatible online protocol.");
                return;
            }

            if (response.session == null)
            {
                onError?.Invoke("Session response did not contain canonical game state.");
                return;
            }

            onSuccess?.Invoke(response);
        }

        private IEnumerator GetJson<T>(
            string path,
            Action<T> onSuccess,
            Action<string> onError)
        {
            using UnityWebRequest request = UnityWebRequest.Get(BuildUrl(path));
            request.timeout = requestTimeoutSeconds;
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private IEnumerator PostJson<TRequest, TResponse>(
            string path,
            TRequest payload,
            Action<TResponse> onSuccess,
            Action<string> onError)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(
                BuildUrl(path),
                UnityWebRequest.kHttpVerbPOST,
                new DownloadHandlerBuffer(),
                new UploadHandlerRaw(body));

            request.timeout = requestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }

        private static void HandleResponse<T>(
            UnityWebRequest request,
            Action<T> onSuccess,
            Action<string> onError)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                string body = request.downloadHandler?.text;
                string detail = string.IsNullOrWhiteSpace(body) ? request.error : body;
                onError?.Invoke($"HTTP {request.responseCode}: {detail}");
                return;
            }

            try
            {
                T parsed = JsonUtility.FromJson<T>(request.downloadHandler.text);
                onSuccess?.Invoke(parsed);
            }
            catch (Exception ex)
            {
                onError?.Invoke("Could not parse online response: " + ex.Message);
            }
        }

        private string BuildUrl(string path)
        {
            return NormalizeBaseUrl(baseUrl) + (path.StartsWith("/") ? path : "/" + path);
        }

        private static string NormalizeBaseUrl(string value)
        {
            return (value ?? string.Empty).Trim().TrimEnd('/');
        }
    }
}
