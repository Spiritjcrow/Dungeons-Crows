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
            Assert.IsTrue(ProtocolCompatibility.IsCompatible("dc-turn/1.0"));
        }

        [Test]
        public void Compatibility_RejectsUnknownProtocolVersion()
        {
            Assert.IsFalse(ProtocolCompatibility.IsCompatible("dc-turn/2.0"));
            Assert.IsFalse(ProtocolCompatibility.IsCompatible(string.Empty));
        }

        [Test]
        public void ProtocolDescriptor_ParsesRequiredFields()
        {
            const string json = "{\"protocolVersion\":\"dc-turn/1.0\",\"sessionEntityType\":\"game-session\",\"actions\":[\"attack\",\"defend\",\"interact\",\"speak\"],\"limits\":{\"playerName\":24,\"freeformText\":240,\"partySize\":4,\"chronicleEntries\":40}}";

            ProtocolDescriptor descriptor = JsonUtility.FromJson<ProtocolDescriptor>(json);

            Assert.AreEqual("dc-turn/1.0", descriptor.protocolVersion);
            Assert.AreEqual("game-session", descriptor.sessionEntityType);
            Assert.AreEqual(4, descriptor.actions.Length);
            Assert.AreEqual(240, descriptor.limits.freeformText);
        }

        [Test]
        public void SessionEnvelope_ParsesCanonicalSessionState()
        {
            const string json = "{\"protocolVersion\":\"dc-turn/1.0\",\"session\":{\"code\":\"CROW42\",\"turnNumber\":3,\"activeActorId\":\"p1\",\"party\":[{\"id\":\"p1\",\"name\":\"Rook\",\"hp\":12,\"maxHp\":12,\"defense\":10,\"isEnemy\":false}],\"enemy\":{\"id\":\"warden\",\"name\":\"Crowbound Warden\",\"hp\":18,\"maxHp\":18,\"defense\":11,\"isEnemy\":true},\"altarOpened\":false,\"seed\":17,\"lastNarration\":\"The crypt waits.\",\"log\":[]}}";

            SessionEnvelope envelope = JsonUtility.FromJson<SessionEnvelope>(json);

            Assert.AreEqual(OnlineProtocol.Version, envelope.protocolVersion);
            Assert.AreEqual("CROW42", envelope.session.code);
            Assert.AreEqual(3, envelope.session.turnNumber);
            Assert.AreEqual("Rook", envelope.session.party[0].name);
            Assert.AreEqual(18, envelope.session.enemy.hp);
        }
    }
}
