using System;

namespace DungeonsCrows.Online
{
    public static class OnlineProtocol
    {
        public const string Version = "dc-turn/1.0";
    }

    public static class ProtocolCompatibility
    {
        public static bool IsCompatible(string serverVersion)
        {
            return string.Equals(serverVersion, OnlineProtocol.Version, StringComparison.Ordinal);
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
        public bool altarOpened;
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
