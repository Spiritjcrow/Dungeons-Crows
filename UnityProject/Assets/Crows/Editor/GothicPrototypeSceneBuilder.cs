#if UNITY_EDITOR
using DungeonsCrows.CameraSystem;
using DungeonsCrows.Online;
using DungeonsCrows.Player;
using DungeonsCrows.Presentation;
using DungeonsCrows.Rendering;
using DungeonsCrows.UI;
using DungeonsCrows.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DungeonsCrows.EditorTools
{
    public static class GothicPrototypeSceneBuilder
    {
        private const string RootFolder = "Assets/Crows";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string MaterialFolder = RootFolder + "/Materials/Prototype";
        private const string UiFolder = RootFolder + "/UI";

        [MenuItem("Dungeons & Crows/Build Gothic Prototype Scene")]
        public static void Build()
        {
            EnsureFolders();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            ConfigureAtmosphere();

            Material stone = GetOrCreateMaterial(
                "PrototypeStone",
                new Color(0.16f, 0.17f, 0.18f),
                0.15f,
                0.45f);
            Material altarMat = GetOrCreateMaterial(
                "PrototypeAltar",
                new Color(0.20f, 0.12f, 0.12f),
                0.05f,
                0.35f);
            Material crowMat = GetOrCreateMaterial(
                "PrototypeCrow",
                new Color(0.012f, 0.014f, 0.018f),
                0f,
                0.3f);

            CreateWorldShell(stone);

            GameObject altar = CreateBox(
                "[PLACEHOLDER] Central Altar",
                new Vector3(0f, 0.55f, 3.5f),
                new Vector3(3.2f, 1.1f, 1.8f),
                altarMat,
                PlaceholderCategory.Prop,
                "Replace with original crow-ritual altar.");

            GameObject dais = CreateBox(
                "[PLACEHOLDER] Raised Dais",
                new Vector3(0f, 0.2f, 3.5f),
                new Vector3(6f, 0.4f, 4f),
                stone,
                PlaceholderCategory.Environment,
                "Replace with morph-capable ritual platform.");

            GameObject leftSpire = CreateBox(
                "[PLACEHOLDER] Left Morph Spire",
                new Vector3(-5.5f, 2.6f, 5f),
                new Vector3(1.4f, 5.2f, 1.4f),
                stone,
                PlaceholderCategory.Environment,
                "Replace with morph-capable gothic spire.");

            GameObject rightSpire = CreateBox(
                "[PLACEHOLDER] Right Morph Spire",
                new Vector3(5.5f, 2.6f, 5f),
                new Vector3(1.4f, 5.2f, 1.4f),
                stone,
                PlaceholderCategory.Environment,
                "Replace with morph-capable gothic spire.");

            CreateTorch(new Vector3(-9f, 2.5f, -9f));
            CreateTorch(new Vector3(9f, 2.5f, -9f));
            CreateTorch(new Vector3(-9f, 2.5f, 9f));
            CreateTorch(new Vector3(9f, 2.5f, 9f));

            GameObject player = CreatePlayer();
            GameObject head = CreateFirstPersonAnchor(player);
            GameObject enemy = CreateEnemyPlaceholder(altarMat);
            CreateCrowFlock(crowMat);

            Camera camera = CreateCamera(player, head, out DualPerspectiveCamera cameraRig);
            ExplorationMotor motor = player.AddComponent<ExplorationMotor>();
            motor.SetViewReference(camera.transform);
            motor.SetPerspectiveCamera(cameraRig);

            GameObject qualityRoot = new GameObject("Alpha 4 Quality Director");
            Alpha4QualityDirector qualityDirector =
                qualityRoot.AddComponent<Alpha4QualityDirector>();
            qualityDirector.Configure(
                camera,
                VisualQualityTier.Balanced);

            Light moonLight = CreateMoonLight();

            MorphicEnvironmentController morph =
                CreateMorphEnvironment(
                    moonLight,
                    altar,
                    dais,
                    leftSpire,
                    rightSpire);

            GameObject onlineRoot = new GameObject("Online Campaign Runtime");
            OnlineGameClient onlineClient = onlineRoot.AddComponent<OnlineGameClient>();
            OnlineCampaignController campaign =
                onlineRoot.AddComponent<OnlineCampaignController>();
            campaign.SetClient(onlineClient);

            ChapterVisualStateBridge chapterBridge =
                onlineRoot.AddComponent<ChapterVisualStateBridge>();
            chapterBridge.SetEnvironment(morph);

            ParticleSystem attackFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Resolved Attack Hit",
                enemy.transform.position + Vector3.up * 1.2f,
                new Color(1f, 0.42f, 0.12f),
                42,
                6f,
                0.18f);

            ParticleSystem playerDamageFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Player Damage",
                player.transform.position + Vector3.up * 1.1f,
                new Color(0.8f, 0.05f, 0.05f),
                32,
                4.5f,
                0.16f);

            ParticleSystem guardFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Guard Feathers",
                player.transform.position + Vector3.up * 1.1f,
                new Color(0.28f, 0.42f, 0.58f),
                48,
                3.5f,
                0.12f);

            ParticleSystem healFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Resolve Restore",
                player.transform.position + Vector3.up * 0.6f,
                new Color(0.3f, 0.72f, 0.48f),
                28,
                2.2f,
                0.13f);

            ParticleSystem ritualFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Ritual Surge",
                altar.transform.position + Vector3.up * 1.1f,
                new Color(0.58f, 0.22f, 0.8f),
                80,
                5.5f,
                0.2f);

            ParticleSystem victoryFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Victory Crowstorm",
                new Vector3(0f, 4f, 2f),
                new Color(0.92f, 0.72f, 0.28f),
                140,
                8f,
                0.2f);

            ParticleSystem defeatFx = CreatePrototypeBurst(
                "[PLACEHOLDER VFX] Defeat Ash",
                player.transform.position + Vector3.up * 1f,
                new Color(0.22f, 0.08f, 0.08f),
                72,
                2.8f,
                0.22f);

            CanonicalCombatPresentationBridge presentation =
                onlineRoot.AddComponent<CanonicalCombatPresentationBridge>();
            presentation.Configure(
                campaign,
                chapterBridge,
                attackFx,
                playerDamageFx,
                guardFx,
                healFx,
                ritualFx,
                victoryFx,
                defeatFx);

            CreateHud(campaign);

            string scenePath = SceneFolder + "/GothicPrototype.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = player;

            Debug.Log(
                "Built Dungeons & Crows Alpha 4 integration prototype: " +
                scenePath +
                " | WASD/gamepad move · Shift sprint · Space jump · V first/third person · " +
                "online Create/Join + canonical Attack/Defend/Rite/Speak HUD + state-driven VFX/morphs.");
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;
            RenderSettings.fogColor = new Color(0.035f, 0.04f, 0.05f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.07f, 0.075f, 0.09f);
            RenderSettings.ambientEquatorColor = new Color(0.045f, 0.04f, 0.05f);
            RenderSettings.ambientGroundColor = new Color(0.018f, 0.018f, 0.022f);
        }

        private static void CreateWorldShell(Material stone)
        {
            CreateBox(
                "[PLACEHOLDER] Floor",
                new Vector3(0f, -0.25f, 0f),
                new Vector3(28f, 0.5f, 28f),
                stone,
                PlaceholderCategory.Environment,
                "Replace with HD modular gothic terrain.");

            for (int i = -7; i <= 7; i++)
            {
                CreateBox(
                    "[PLACEHOLDER] NorthWall",
                    new Vector3(i * 2f, 2f, 14f),
                    new Vector3(2f, 4.5f, 0.8f),
                    stone,
                    PlaceholderCategory.Environment,
                    "Replace with gothic keep/cathedral wall.");

                CreateBox(
                    "[PLACEHOLDER] SouthWall",
                    new Vector3(i * 2f, 2f, -14f),
                    new Vector3(2f, 4.5f, 0.8f),
                    stone,
                    PlaceholderCategory.Environment,
                    "Replace with gothic keep/cathedral wall.");

                CreateBox(
                    "[PLACEHOLDER] EastWall",
                    new Vector3(14f, 2f, i * 2f),
                    new Vector3(0.8f, 4.5f, 2f),
                    stone,
                    PlaceholderCategory.Environment,
                    "Replace with gothic keep/cathedral wall.");

                CreateBox(
                    "[PLACEHOLDER] WestWall",
                    new Vector3(-14f, 2f, i * 2f),
                    new Vector3(0.8f, 4.5f, 2f),
                    stone,
                    PlaceholderCategory.Environment,
                    "Replace with gothic keep/cathedral wall.");
            }
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("[PLACEHOLDER] PlayerPawn");
            player.transform.position = new Vector3(0f, 0f, -6f);

            CharacterController controller =
                player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.42f;
            controller.center = new Vector3(0f, 1f, 0f);

            Mark(
                player,
                PlaceholderCategory.Character,
                "Replace with HD rigged corvid warrior character.");

            GameObject visual =
                GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "[PLACEHOLDER] Corvid Warrior Visual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);

            CapsuleCollider primitiveCollider =
                visual.GetComponent<CapsuleCollider>();
            if (primitiveCollider != null)
                Object.DestroyImmediate(primitiveCollider);

            Mark(
                visual,
                PlaceholderCategory.Character,
                "Replace with animated HD corvid warrior mesh and rig.");

            return player;
        }

        private static GameObject CreateFirstPersonAnchor(GameObject player)
        {
            GameObject head = new GameObject("First Person Anchor");
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.72f, 0.08f);
            return head;
        }

        private static GameObject CreateEnemyPlaceholder(Material material)
        {
            GameObject enemy =
                GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "[PLACEHOLDER] Canonical Enemy Visual";
            enemy.transform.position = new Vector3(0f, 1f, 7.5f);
            enemy.transform.localScale = new Vector3(1.4f, 1.6f, 1.4f);
            enemy.GetComponent<Renderer>().sharedMaterial = material;

            Mark(
                enemy,
                PlaceholderCategory.Creature,
                "Replace with chapter-aware animated enemy prefabs.");
            return enemy;
        }

        private static void CreateCrowFlock(Material crowMat)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i / 8f * Mathf.PI * 2f;
                GameObject crow =
                    GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crow.name = "[PLACEHOLDER] CrowScout";
                crow.transform.localScale =
                    new Vector3(0.45f, 0.22f, 0.7f);
                crow.transform.position = new Vector3(
                    Mathf.Cos(angle) * 4.5f,
                    2.5f + (i % 2) * 0.4f,
                    3.5f + Mathf.Sin(angle) * 4.5f);
                crow.GetComponent<Renderer>().sharedMaterial = crowMat;
                Mark(
                    crow,
                    PlaceholderCategory.Crow,
                    "Replace with rigged HD crow scout/familiar.");
            }
        }

        private static Camera CreateCamera(
            GameObject player,
            GameObject head,
            out DualPerspectiveCamera cameraRig)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 68f;

            cameraRig =
                cameraObject.AddComponent<DualPerspectiveCamera>();
            cameraRig.SetTarget(player.transform, head.transform);

            cameraObject.transform.position =
                player.transform.position + new Vector3(0.8f, 2f, -5f);

            return camera;
        }

        private static Light CreateMoonLight()
        {
            GameObject moon = new GameObject("Cold Directional Light");
            Light moonLight = moon.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.intensity = 0.5f;
            moonLight.color = new Color(0.35f, 0.42f, 0.55f);
            moonLight.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            return moonLight;
        }

        private static MorphicEnvironmentController CreateMorphEnvironment(
            Light moonLight,
            GameObject altar,
            GameObject dais,
            GameObject leftSpire,
            GameObject rightSpire)
        {
            GameObject morphRoot = new GameObject("Morphic Environment");
            MorphicEnvironmentController morph =
                morphRoot.AddComponent<MorphicEnvironmentController>();
            morph.SetKeyLight(moonLight);

            morph.RegisterChannel(
                altar.transform,
                Pose(
                    new Vector3(0f, 0.55f, 3.5f),
                    Vector3.zero,
                    new Vector3(3.2f, 1.1f, 1.8f)),
                Pose(
                    new Vector3(0f, 1.2f, 3.5f),
                    new Vector3(0f, 12f, 0f),
                    new Vector3(4.4f, 2.2f, 2.6f)),
                Pose(
                    new Vector3(0f, 1.8f, 3.5f),
                    new Vector3(0f, -18f, 0f),
                    new Vector3(5.4f, 3.2f, 3.4f)));

            morph.RegisterChannel(
                dais.transform,
                Pose(
                    new Vector3(0f, 0.2f, 3.5f),
                    Vector3.zero,
                    new Vector3(6f, 0.4f, 4f)),
                Pose(
                    new Vector3(0f, 0.4f, 3.5f),
                    new Vector3(0f, 8f, 0f),
                    new Vector3(8f, 0.8f, 6f)),
                Pose(
                    new Vector3(0f, 0.8f, 3.5f),
                    new Vector3(0f, -10f, 0f),
                    new Vector3(10f, 1.5f, 8f)));

            morph.RegisterChannel(
                leftSpire.transform,
                Pose(
                    new Vector3(-5.5f, 2.6f, 5f),
                    Vector3.zero,
                    new Vector3(1.4f, 5.2f, 1.4f)),
                Pose(
                    new Vector3(-7f, 4.2f, 5.5f),
                    new Vector3(0f, 16f, -6f),
                    new Vector3(1.8f, 8.4f, 1.8f)),
                Pose(
                    new Vector3(-8.2f, 6f, 6.5f),
                    new Vector3(0f, 28f, -10f),
                    new Vector3(2.2f, 12f, 2.2f)));

            morph.RegisterChannel(
                rightSpire.transform,
                Pose(
                    new Vector3(5.5f, 2.6f, 5f),
                    Vector3.zero,
                    new Vector3(1.4f, 5.2f, 1.4f)),
                Pose(
                    new Vector3(7f, 4.2f, 5.5f),
                    new Vector3(0f, -16f, 6f),
                    new Vector3(1.8f, 8.4f, 1.8f)),
                Pose(
                    new Vector3(8.2f, 6f, 6.5f),
                    new Vector3(0f, -28f, 10f),
                    new Vector3(2.2f, 12f, 2.2f)));

            morph.SnapToChapter(1);
            return morph;
        }

        private static void CreateHud(
            OnlineCampaignController campaign)
        {
            GameObject hudObject = new GameObject("Alpha 4 HUD");
            UIDocument document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = GetOrCreatePanelSettings();

            Alpha4HudController hud =
                hudObject.AddComponent<Alpha4HudController>();
            hud.SetCampaign(campaign);
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            string path = UiFolder + "/Alpha4PanelSettings.asset";
            PanelSettings settings =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(path);

            if (settings == null)
            {
                settings =
                    ScriptableObject.CreateInstance<PanelSettings>();
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                AssetDatabase.CreateAsset(settings, path);
            }

            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static ParticleSystem CreatePrototypeBurst(
            string name,
            Vector3 position,
            Color color,
            short count,
            float speed,
            float size)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;

            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startColor = color;
            main.maxParticles = Mathf.Max(64, count * 2);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, count)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;

            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            Mark(
                go,
                PlaceholderCategory.Vfx,
                "Replace with production Alpha 4 VFX while preserving canonical trigger semantics.");

            return particles;
        }

        private static MorphPose Pose(
            Vector3 position,
            Vector3 rotation,
            Vector3 scale)
        {
            return new MorphPose(position, rotation, scale);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(SceneFolder);
            EnsureFolder(RootFolder + "/Materials");
            EnsureFolder(MaterialFolder);
            EnsureFolder(UiFolder);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            string name = folder.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject CreateBox(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            PlaceholderCategory category,
            string intent)
        {
            GameObject go =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Mark(go, category, intent);
            return go;
        }

        private static void CreateTorch(Vector3 position)
        {
            GameObject root =
                new GameObject("[PLACEHOLDER] Torch");
            root.transform.position = position;

            Mark(
                root,
                PlaceholderCategory.Prop,
                "Replace with animated wall torch + flame VFX.");

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 5f;
            light.color = new Color(1f, 0.42f, 0.14f);
            light.shadows = LightShadows.Soft;
        }

        private static Material GetOrCreateMaterial(
            string assetName,
            Color color,
            float metallic,
            float smoothness)
        {
            string path = $"{MaterialFolder}/{assetName}.mat";
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader =
                    Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard");

                material = new Material(shader)
                {
                    name = assetName
                };

                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Mark(
            GameObject go,
            PlaceholderCategory category,
            string intent)
        {
            PlaceholderAssetMarker marker =
                go.AddComponent<PlaceholderAssetMarker>();
            marker.category = category;
            marker.replacementIntent = intent;
        }
    }
}
#endif
