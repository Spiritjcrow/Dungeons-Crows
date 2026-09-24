using DungeonsCrows.CameraSystem;
using DungeonsCrows.Online;
using DungeonsCrows.Rendering;
using DungeonsCrows.World;
using NUnit.Framework;
using UnityEngine;

namespace DungeonsCrows.Tests.Editor
{
    public sealed class Alpha4PresentationTests
    {
        [Test]
        public void DualPerspectiveCamera_SwitchesFirstAndThirdPerson()
        {
            GameObject cameraObject = new GameObject("Test Camera");
            cameraObject.AddComponent<Camera>();
            DualPerspectiveCamera rig =
                cameraObject.AddComponent<DualPerspectiveCamera>();

            rig.SetPerspective(PerspectiveMode.FirstPerson);
            Assert.AreEqual(PerspectiveMode.FirstPerson, rig.Mode);

            rig.TogglePerspective();
            Assert.AreEqual(PerspectiveMode.ThirdPerson, rig.Mode);

            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void MorphicEnvironment_SnapAppliesChapterPoseExactly()
        {
            GameObject root = new GameObject("Morph Root");
            GameObject target = new GameObject("Morph Target");

            MorphicEnvironmentController morph =
                root.AddComponent<MorphicEnvironmentController>();

            morph.RegisterChannel(
                target.transform,
                new MorphPose(Vector3.zero, Vector3.zero, Vector3.one),
                new MorphPose(
                    new Vector3(2f, 3f, 4f),
                    new Vector3(0f, 30f, 0f),
                    new Vector3(2f, 2f, 2f)),
                new MorphPose(
                    new Vector3(5f, 6f, 7f),
                    new Vector3(0f, 60f, 0f),
                    new Vector3(3f, 3f, 3f)));

            morph.SnapToChapter(2);

            Assert.AreEqual(2, morph.CurrentChapter);
            Assert.AreEqual(new Vector3(2f, 3f, 4f), target.transform.localPosition);
            Assert.AreEqual(new Vector3(2f, 2f, 2f), target.transform.localScale);
            Assert.That(
                Quaternion.Angle(
                    Quaternion.Euler(0f, 30f, 0f),
                    target.transform.localRotation),
                Is.LessThan(0.01f));

            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ChapterVisualBridge_ConsumesCanonicalChapterState()
        {
            GameObject root = new GameObject("Visual Bridge Root");
            GameObject target = new GameObject("Morph Target");

            MorphicEnvironmentController morph =
                root.AddComponent<MorphicEnvironmentController>();

            morph.RegisterChannel(
                target.transform,
                new MorphPose(Vector3.zero, Vector3.zero, Vector3.one),
                new MorphPose(
                    new Vector3(0f, 2f, 0f),
                    Vector3.zero,
                    Vector3.one),
                new MorphPose(
                    new Vector3(0f, 4f, 0f),
                    Vector3.zero,
                    Vector3.one));

            ChapterVisualStateBridge bridge =
                root.AddComponent<ChapterVisualStateBridge>();
            bridge.SetEnvironment(morph);

            var session = new GameSessionDto
            {
                chapter = 3,
                campaignStatus = "active"
            };

            bridge.ApplyCanonicalSession(session, true);

            Assert.AreEqual(3, morph.CurrentChapter);
            Assert.AreEqual(
                new Vector3(0f, 4f, 0f),
                target.transform.localPosition);

            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
        }


        [Test]
        public void QualityProfiles_ScaleDownWithoutChangingGameRules()
        {
            Alpha4QualityProfile cinematic =
                Alpha4QualityDirector.ProfileFor(
                    VisualQualityTier.Cinematic);

            Alpha4QualityProfile balanced =
                Alpha4QualityDirector.ProfileFor(
                    VisualQualityTier.Balanced);

            Alpha4QualityProfile mobile =
                Alpha4QualityDirector.ProfileFor(
                    VisualQualityTier.Mobile);

            Assert.Greater(
                cinematic.ShadowDistance,
                balanced.ShadowDistance);
            Assert.Greater(
                balanced.ShadowDistance,
                mobile.ShadowDistance);
            Assert.GreaterOrEqual(
                cinematic.AntiAliasing,
                balanced.AntiAliasing);
            Assert.GreaterOrEqual(
                balanced.AntiAliasing,
                mobile.AntiAliasing);
            Assert.IsTrue(cinematic.Hdr);
            Assert.IsFalse(mobile.Hdr);
        }

        [Test]
        public void LocalHuntIdentityStore_RoundTripsAndClears()
        {
            LocalHuntIdentityStore.Clear();

            try
            {
                LocalHuntIdentityStore.Save(
                    "CROW42",
                    "CROW42-p1",
                    "Rook");

                Assert.IsTrue(
                    LocalHuntIdentityStore.TryLoad(
                        out LocalHuntIdentity identity));

                Assert.AreEqual("CROW42", identity.code);
                Assert.AreEqual("CROW42-p1", identity.playerId);
                Assert.AreEqual("Rook", identity.playerName);

                LocalHuntIdentityStore.Clear();

                Assert.IsFalse(
                    LocalHuntIdentityStore.TryLoad(out _));
            }
            finally
            {
                LocalHuntIdentityStore.Clear();
            }
        }

        [Test]
        public void Alpha4Protocol_StillRequiresCanonicalCampaignFields()
        {
            Assert.AreEqual("dc-turn/2.0", OnlineProtocol.Version);
            CollectionAssert.AreEqual(
                new[] { "attack", "defend", "interact", "speak" },
                OnlineProtocol.Actions);
        }
    }
}
