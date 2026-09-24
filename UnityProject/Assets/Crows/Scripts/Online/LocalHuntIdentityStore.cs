using System;
using UnityEngine;

namespace DungeonsCrows.Online
{
    [Serializable]
    public sealed class LocalHuntIdentity
    {
        public string code;
        public string playerId;
        public string playerName;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(code) &&
            code.Length == 6 &&
            !string.IsNullOrWhiteSpace(playerId) &&
            !string.IsNullOrWhiteSpace(playerName);
    }

    public static class LocalHuntIdentityStore
    {
        private const string StorageKey = "dungeons-crows.hunt-identity.v2";

        public static void Save(string code, string playerId, string playerName)
        {
            var identity = new LocalHuntIdentity
            {
                code = (code ?? string.Empty).Trim().ToUpperInvariant(),
                playerId = (playerId ?? string.Empty).Trim(),
                playerName = (playerName ?? string.Empty).Trim()
            };

            if (!identity.IsValid)
                return;

            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(identity));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out LocalHuntIdentity identity)
        {
            identity = null;

            if (!PlayerPrefs.HasKey(StorageKey))
                return false;

            string json = PlayerPrefs.GetString(StorageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                identity = JsonUtility.FromJson<LocalHuntIdentity>(json);
                if (identity != null && identity.IsValid)
                    return true;
            }
            catch
            {
                // Corrupt local identity should never block game startup.
            }

            Clear();
            identity = null;
            return false;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(StorageKey);
            PlayerPrefs.Save();
        }
    }
}
