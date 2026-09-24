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
                    "dc-turn/2.0"));
        }

        [Test]
        public void Compatibility_RejectsUnknownProtocolVersion()
        {
            Assert.IsFalse(
                ProtocolCompatibility.IsCompatible(
                    "dc-turn/1.0"));
            Assert.IsFalse(
                ProtocolCompatibility.IsCompatible(
                    string.Empty));
        }

        [Test]
        public void ProtocolDescriptor_ParsesCampaignContract()
        {
            const string json =
                "{\"protocolVersion\":\"dc-turn/2.0\"," +
                "\"sessionEntityType\":\"game-session\"," +
                "\"actions\":[\"attack\",\"defend\",\"interact\",\"speak\"]," +
                "\"limits\":{\"playerName\":24,\"freeformText\":240," +
                "\"partySize\":4,\"chronicleEntries\":40}," +
                "\"chapters\":[1,2,3]," +
                "\"campaignStatuses\":[\"active\",\"victory\",\"defeat\"]," +
                "\"turnResponseFields\":[\"session\",\"narration\",\"diceRolls\",\"resolvedActionType\"]}";

            ProtocolDescriptor descriptor =
                JsonUtility.FromJson<ProtocolDescriptor>(json);

            Assert.AreEqual(
                "dc-turn/2.0",
                descriptor.protocolVersion);
            Assert.AreEqual(
                "game-session",
                descriptor.sessionEntityType);
            Assert.AreEqual(4, descriptor.actions.Length);
            Assert.AreEqual(3, descriptor.chapters.Length);
            Assert.AreEqual(
                3,
                descriptor.campaignStatuses.Length);
            Assert.AreEqual(
                240,
                descriptor.limits.freeformText);
            CollectionAssert.Contains(
                descriptor.turnResponseFields,
                "resolvedActionType");
        }

        [Test]
        public void SessionEnvelope_ParsesCanonicalCampaignState()
        {
            const string json =
                "{\"protocolVersion\":\"dc-turn/2.0\"," +
                "\"session\":{" +
                "\"code\":\"CROW42\"," +
                "\"turnNumber\":9," +
                "\"activeActorId\":\"p1\"," +
                "\"party\":[{" +
                "\"id\":\"p1\"," +
                "\"name\":\"Rook\"," +
                "\"hp\":18," +
                "\"maxHp\":20," +
                "\"defense\":10," +
                "\"isEnemy\":false}]," +
                "\"enemy\":{" +
                "\"id\":\"bone-rook\"," +
                "\"name\":\"Bone Rook\"," +
                "\"hp\":10," +
                "\"maxHp\":18," +
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
                "active",
                envelope.session.campaignStatus);
            Assert.AreEqual(
                "Ember Feather",
                envelope.session.relics[0]);
            Assert.AreEqual(
                1,
                envelope.session.ritualAttempts);
            Assert.AreEqual(
                250,
                envelope.session.score);
            Assert.AreEqual(
                20,
                envelope.session.party[0].maxHp);
            Assert.AreEqual(
                "Bone Rook",
                envelope.session.enemy.name);
        }

        [Test]
        public void TurnEnvelope_ParsesResolvedActionType()
        {
            const string json =
                "{\"protocolVersion\":\"dc-turn/2.0\"," +
                "\"narration\":\"Steel cuts only air.\"," +
                "\"diceRolls\":[3,14]," +
                "\"resolvedActionType\":\"attack\"}";

            TurnEnvelope envelope =
                JsonUtility.FromJson<TurnEnvelope>(json);

            Assert.AreEqual(
                "attack",
                envelope.resolvedActionType);
            Assert.AreEqual(
                2,
                envelope.diceRolls.Length);
        }
    }
}
