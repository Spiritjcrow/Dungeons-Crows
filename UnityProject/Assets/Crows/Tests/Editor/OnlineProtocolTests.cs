using DungeonsCrows.Online;
using NUnit.Framework;
using UnityEngine;

namespace DungeonsCrows.Tests.Editor
{
    public sealed class OnlineProtocolTests
    {
        [Test]
        public void Compatibility_AcceptsExactProtocolVersion()
        {
            Assert.IsTrue(
                ProtocolCompatibility.IsCompatible(
                    "dc-turn/3.0"));
        }

        [Test]
        public void Compatibility_RejectsUnknownProtocolVersion()
        {
            Assert.IsFalse(
                ProtocolCompatibility.IsCompatible(
                    "dc-turn/2.0"));
            Assert.IsFalse(
                ProtocolCompatibility.IsCompatible(
                    string.Empty));
        }

        [Test]
        public void ProtocolDescriptor_ParsesCampaignAndEssenceContract()
        {
            const string json =
                "{\"protocolVersion\":\"dc-turn/3.0\"," +
                "\"sessionEntityType\":\"game-session\"," +
                "\"actions\":[\"attack\",\"defend\",\"interact\",\"invoke\",\"speak\"]," +
                "\"limits\":{\"playerName\":24,\"freeformText\":240," +
                "\"partySize\":4,\"chronicleEntries\":40}," +
                "\"chapters\":[1,2,3]," +
                "\"campaignStatuses\":[\"active\",\"victory\",\"defeat\"]," +
                "\"combatantFields\":[\"id\",\"name\",\"hp\",\"maxHp\",\"essence\",\"maxEssence\",\"defense\",\"isEnemy\",\"defendingUntilTurn\"]," +
                "\"turnResponseFields\":[\"session\",\"narration\",\"diceRolls\",\"resolvedActionType\"]}";

            ProtocolDescriptor descriptor =
                JsonUtility.FromJson<ProtocolDescriptor>(json);

            Assert.AreEqual(
                OnlineProtocol.Version,
                descriptor.protocolVersion);
            Assert.AreEqual(
                "game-session",
                descriptor.sessionEntityType);
            CollectionAssert.AreEqual(
                new[]
                {
                    "attack",
                    "defend",
                    "interact",
                    "invoke",
                    "speak"
                },
                descriptor.actions);
            CollectionAssert.Contains(
                descriptor.combatantFields,
                "essence");
            CollectionAssert.Contains(
                descriptor.combatantFields,
                "maxEssence");
            CollectionAssert.Contains(
                descriptor.turnResponseFields,
                "resolvedActionType");
            Assert.AreEqual(
                240,
                descriptor.limits.freeformText);
        }

        [Test]
        public void SessionEnvelope_ParsesCanonicalCrowEssenceState()
        {
            const string json =
                "{\"protocolVersion\":\"dc-turn/3.0\"," +
                "\"session\":{" +
                "\"code\":\"CROW42\"," +
                "\"turnNumber\":9," +
                "\"activeActorId\":\"p1\"," +
                "\"party\":[{" +
                "\"id\":\"p1\"," +
                "\"name\":\"Rook\"," +
                "\"hp\":18," +
                "\"maxHp\":20," +
                "\"essence\":2," +
                "\"maxEssence\":4," +
                "\"defense\":10," +
                "\"isEnemy\":false}]," +
                "\"enemy\":{" +
                "\"id\":\"bone-rook\"," +
                "\"name\":\"Bone Rook\"," +
                "\"hp\":10," +
                "\"maxHp\":18," +
                "\"essence\":0," +
                "\"maxEssence\":0," +
                "\"defense\":12," +
                "\"isEnemy\":true}," +
                "\"chapter\":2," +
                "\"campaignStatus\":\"active\"," +
                "\"altarOpened\":true," +
                "\"rookeryPurified\":false," +
                "\"crownBroken\":false," +
                "\"relics\":[\"Ember Feather\"]," +
                "\"ritualAttempts\":1," +
                "\"score\":250," +
                "\"seed\":17," +
                "\"lastNarration\":\"The rookery waits.\"," +
                "\"log\":[]}}";

            SessionEnvelope envelope =
                JsonUtility.FromJson<SessionEnvelope>(json);

            Assert.AreEqual(
                OnlineProtocol.Version,
                envelope.protocolVersion);
            Assert.AreEqual(
                "CROW42",
                envelope.session.code);
            Assert.AreEqual(
                2,
                envelope.session.chapter);
            Assert.AreEqual(
                2,
                envelope.session.party[0].essence);
            Assert.AreEqual(
                4,
                envelope.session.party[0].maxEssence);
            Assert.AreEqual(
                0,
                envelope.session.enemy.essence);
            Assert.AreEqual(
                "Bone Rook",
                envelope.session.enemy.name);
        }

        [Test]
        public void TurnEnvelope_ParsesResolvedInvokeAction()
        {
            const string json =
                "{\"protocolVersion\":\"dc-turn/3.0\"," +
                "\"narration\":\"Blue crowfire tears through the crypt.\"," +
                "\"diceRolls\":[14,7]," +
                "\"resolvedActionType\":\"invoke\"}";

            TurnEnvelope envelope =
                JsonUtility.FromJson<TurnEnvelope>(json);

            Assert.AreEqual(
                "invoke",
                envelope.resolvedActionType);
            Assert.AreEqual(
                2,
                envelope.diceRolls.Length);
        }
    }
}
