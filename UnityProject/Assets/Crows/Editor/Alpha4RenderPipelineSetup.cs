#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DungeonsCrows.EditorTools
{
    public static class Alpha4RenderPipelineSetup
    {
        private const string RenderingFolder = "Assets/Crows/Rendering";
        private const string PipelineAssetPath =
            RenderingFolder + "/Alpha4UniversalRenderPipeline.asset";
        private const string BuiltinRendererPath =
            "Assets/UniversalRenderer.asset";

        [MenuItem("Dungeons & Crows/Configure Alpha 4 URP")]
        public static void ConfigureFromMenu()
        {
            UniversalRenderPipelineAsset asset = EnsureConfigured();
            Selection.activeObject = asset;
            Debug.Log(
                "Dungeons & Crows Alpha 4 URP configured: " +
                PipelineAssetPath);
        }

        public static UniversalRenderPipelineAsset EnsureConfigured()
        {
            EnsureFolder("Assets/Crows");
            EnsureFolder(RenderingFolder);

            UniversalRenderPipelineAsset asset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                    PipelineAssetPath);

            ScriptableRendererData renderer =
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                    BuiltinRendererPath);

            if (asset == null)
            {
                if (renderer != null)
                {
                    asset = UniversalRenderPipelineAsset.Create(renderer);
                    asset.name = "Alpha4UniversalRenderPipeline";
                    AssetDatabase.CreateAsset(asset, PipelineAssetPath);
                }
                else
                {
                    asset = UniversalRenderPipelineAsset.Create();
                    asset.name = "Alpha4UniversalRenderPipeline";
                    AssetDatabase.CreateAsset(asset, PipelineAssetPath);

                    renderer = asset.LoadBuiltinRendererData(
                        RendererType.UniversalRenderer);
                }
            }

            asset.renderScale = 1f;
            asset.msaaSampleCount = 4;
            asset.supportsHDR = true;
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;
            asset.shadowDistance = 140f;
            asset.shadowCascadeCount = 4;
            asset.maxAdditionalLightsCount = 8;
            asset.useSRPBatcher = true;

            GraphicsSettings.defaultRenderPipeline = asset;
            QualitySettings.renderPipeline = asset;

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            int slash = folder.LastIndexOf('/');
            if (slash <= 0)
                return;

            string parent = folder.Substring(0, slash);
            string name = folder.Substring(slash + 1);

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
