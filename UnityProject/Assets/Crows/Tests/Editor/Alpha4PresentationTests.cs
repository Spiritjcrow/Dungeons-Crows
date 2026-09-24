using DungeonsCrows.CameraSystem;
using DungeonsCrows.Online;
using DungeonsCrows.Presentation;
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
        public void MorphicLandscape_SnapChangesWalkableGeometry()
        {
            GameObject root =
                new GameObject("Morphic Landscape Test");

            try
            {
                MorphicLandscapeController landscape =
                    root.AddComponent<MorphicLandscapeController>();

                landscape.Configure(
                    null,
                    12f,
                    12f,
                    11);

                landscape.SnapToChapter(1);

                MeshFilter filter =
                    root.GetComponent<MeshFilter>();
                Assert.IsNotNull(filter.sharedMesh);
                Assert.AreEqual(
                    121,
                    landscape.VertexCount);

                float chapterOneHeight =
                    filter.sharedMesh.vertices[0].y;

                landscape.SnapToChapter(3);

                float chapterThreeHeight =
                    filter.sharedMesh.vertices[0].y;

                Assert.AreEqual(
                    3,
                    landscape.CurrentChapter);
                Assert.AreNotEqual(
                    chapterOneHeight,
                    chapterThreeHeight);
                Assert.IsNotNull(
                    root.GetComponent<MeshCollider>().sharedMesh);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RuntimeMinimapCamera_CreatesRealRenderTexture()
        {
            GameObject player =
                new GameObject("Minimap Player");
            GameObject cameraObject =
                new GameObject("Minimap Camera");
            cameraObject.AddComponent<Camera>();

            try
            {
                RuntimeMinimapCamera minimap =
                    cameraObject.AddComponent<RuntimeMinimapCamera>();

                minimap.Configure(
                    player.transform,
                    20f,
                    10f,
                    256);

                RenderTexture texture =
                    minimap.Texture;

                Assert.IsNotNull(texture);
                Assert.AreEqual(256, texture.width);
                Assert.AreEqual(256, texture.height);
                Assert.AreSame(
                    texture,
                    cameraObject
                        .GetComponent<Camera>()
                        .targetTexture);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(player);
            }
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
        public void CanonicalPresentationDelta_DerivesOnlyCommittedChanges()
        {
            var before = new GameSessionDto
            {
                chapter = 2,
                campaignStatus = "active",
                ritualAttempts = 0,
                party = new[]
                {
                    new CombatantDto
                    {
                        id = "p1",
                        hp = 12,
                        maxHp = 20,
                        defendingUntilTurn = 0
                    }
                },
                enemy = new CombatantDto
                {
                    id = "bone-rook",
                    hp = 18,
                    maxHp = 18
                }
            };

            var after = new GameSessionDto
            {
                chapter = 2,
                campaignStatus = "active",
                ritualAttempts = 1,
                party = new[]
                {
                    new CombatantDto
                    {
                        id = "p1",
                        hp = 14,
                        maxHp = 20,
                        defendingUntilTurn = 7
                    }
                },
                enemy = new CombatantDto
                {
                    id = "bone-rook",
                    hp = 14,
                    maxHp = 18
                }
            };

            CanonicalPresentationDelta delta =
                CanonicalPresentationDelta.From(
                    before,
                    after,
                    "p1");

            Assert.IsTrue(
                delta.Has(PresentationCue.EnemyDamaged));
            Assert.IsTrue(
                delta.Has(PresentationCue.PlayerHealed));
            Assert.IsTrue(
                delta.Has(PresentationCue.PlayerGuarded));
            Assert.IsTrue(
                delta.Has(PresentationCue.RitualAdvanced));
            Assert.AreEqual(4, delta.EnemyDamage);
            Assert.AreEqual(2, delta.PlayerHealing);
        }

        [Test]
        public void ChapterEnemyPresenter_SelectsCanonicalChapterEnemy()
        {
            GameObject presenterObject =
                new GameObject("Enemy Presenter");
            GameObject warden =
                new GameObject("Warden");
            GameObject boneRook =
                new GameObject("Bone Rook");
            GameObject blackRook =
                new GameObject("Black Rook");

            try
            {
                ChapterEnemyPresenter presenter =
                    presenterObject.AddComponent<ChapterEnemyPresenter>();

                presenter.Register(
                    1,
                    "warden",
                    warden);
                presenter.Register(
                    2,
                    "bone-rook",
                    boneRook);
                presenter.Register(
                    3,
                    "black-rook",
                    blackRook);

                presenter.ApplyCanonicalSession(
                    new GameSessionDto
                    {
                        chapter = 2,
                        campaignStatus = "active",
                        enemy = new CombatantDto
                        {
                            id = "bone-rook",
                            hp = 18,
                            maxHp = 18
                        }
                    },
                    true);

                Assert.IsFalse(warden.activeSelf);
                Assert.IsTrue(boneRook.activeSelf);
                Assert.IsFalse(blackRook.activeSelf);
                Assert.AreSame(
                    boneRook,
                    presenter.ActiveVisual.root);
            }
            finally
            {
                Object.DestroyImmediate(warden);
                Object.DestroyImmediate(boneRook);
                Object.DestroyImmediate(blackRook);
                Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void DualPerspectiveCamera_PresentationOffsetRoundTrips()
        {
            GameObject cameraObject =
                new GameObject("Presentation Camera");
            cameraObject.AddComponent<Camera>();
            DualPerspectiveCamera rig =
                cameraObject.AddComponent<DualPerspectiveCamera>();

            Vector3 offset =
                new Vector3(0.05f, -0.03f, 0f);

            rig.SetPresentationOffset(offset);
            Assert.AreEqual(
                offset,
                rig.PresentationOffset);

            rig.ClearPresentationOffset();
            Assert.AreEqual(
                Vector3.zero,
                rig.PresentationOffset);

            Object.DestroyImmediate(cameraObject);
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
