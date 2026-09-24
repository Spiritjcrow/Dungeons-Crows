#if UNITY_EDITOR
using DungeonsCrows.Online;
using UnityEditor;
using UnityEngine;

namespace DungeonsCrows.EditorTools
{
    public static class OnlineBridgeSmokeCheck
    {
        [MenuItem("Dungeons & Crows/Validate Online Bridge Configuration")]
        public static void Validate()
        {
            bool ok = true;

            if (!ProtocolCompatibility.IsCompatible(OnlineProtocol.Version))
            {
                Debug.LogError("Online protocol self-check failed.");
                ok = false;
            }

            if (OnlineProtocol.Actions == null || OnlineProtocol.Actions.Length != 4)
            {
                Debug.LogError("Expected four dc-turn/2.0 online actions.");
                ok = false;
            }

            if (OnlineProtocol.MaxPlayerNameLength != 24 ||
                OnlineProtocol.MaxFreeformTextLength != 240 ||
                OnlineProtocol.MaxPartySize != 4 ||
                OnlineProtocol.MaxChronicleEntries != 40)
            {
                Debug.LogError("Unity online limits do not match dc-turn/2.0.");
                ok = false;
            }

            if (ok)
            {
                Debug.Log(
                    "Dungeons & Crows online bridge configuration OK: " +
                    OnlineProtocol.Version +
                    ". Next run EditMode tests and a live create/join/turn campaign smoke test.");
            }
        }
    }
}
#endif
