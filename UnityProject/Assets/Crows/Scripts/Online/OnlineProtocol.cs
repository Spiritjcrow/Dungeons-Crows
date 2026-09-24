using System;

namespace DungeonsCrows.Online
{
    public static class OnlineProtocol
    {
        public const string Version = "dc-turn/3.0";
        public const string SessionEntityType = "game-session";
        public const int MaxPlayerNameLength = 24;
        public const int MaxFreeformTextLength = 240;
        public const int MaxPartySize = 4;
        public const int MaxChronicleEntries = 40;

        public static readonly string[] Actions =
        {
            "attack",
            "defend",
            "interact",
            "invoke",
            "speak"
        };
    }

    public static class ProtocolCompatibility
    {
        public static bool IsCompatible(string serverVersion)
        {
            return string.Equals(
                serverVersion,
                OnlineProtocol.Version,
                StringComparison.Ordinal);
        }
    }

    [Serializable]
    public sealed class ProtocolLimits
    {
        public int playerName;
        public int freeformText;
        public int partySize;
        public int chronicleEntries;
    }

    [Serializable]
    public sealed class ProtocolDescriptor
    {
        public string protocolVersion;
        public string sessionEntityType;
        public string[] actions;
        public ProtocolLimits limits;
        public int[] chapters;
        public string[] campaignStatuses;
        public string[] combatantFields;
        public string[] sessionFields;
        public string[] turnResponseFields;
    }

    [Serializable]
    public sealed class CombatantDto
    {
        public string id;
        public string name;
        public int hp;
        public int maxHp;
        public int essence;
        public int maxEssence;
        public int defense;
        public bool isEnemy;
        public int defendingUntilTurn;
    }

    [Serializable]
    public sealed class LogEntryDto
    {
        public int turn;
        public string kind;
        public string text;
    }

    [Serializable]
    public sealed class GameSessionDto
    {
        public string code;
        public int turnNumber;
        public string activeActorId;
        public CombatantDto[] party;
        public CombatantDto enemy;
        public int chapter;
        public string campaignStatus;
        public bool altarOpened;
        public bool rookeryPurified;
        public bool crownBroken;
        public string[] relics;
        public int ritualAttempts;
        public int score;
        public int seed;
        public string lastNarration;
        public LogEntryDto[] log;
    }

    [Serializable]
    public sealed class SessionEnvelope
    {
        public string protocolVersion;
        public GameSessionDto session;
        public string playerId;
    }

    [Serializable]
    public sealed class TurnRequestDto
    {
        public string actorId;
        public string actorName;
        public string actionType;
        public string targetId;
        public string freeformText;
        public string connectionId;
    }

    [Serializable]
    public sealed class TurnEnvelope
    {
        public string protocolVersion;
        public GameSessionDto session;
        public string narration;
        public int[] diceRolls;
        public string resolvedActionType;
    }

    [Serializable]
    public sealed class CreateSessionRequestDto
    {
        public string name;
    }

    [Serializable]
    public sealed class JoinSessionRequestDto
    {
        public string code;
        public string name;
    }
}
