#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DungeonsCrows.EditorTools
{
    public static class Alpha4PostProcessingSetup
    {
        private const string RenderingFolder =
            "Assets/Crows/Rendering";
        private const string ProfilePath =
            RenderingFolder +
            "/Alpha4PostProcessProfile.asset";

        public static Volume AttachToScene(Camera camera)
        {
            EnsureFolder("Assets/Crows");
            EnsureFolder(RenderingFolder);

            VolumeProfile profile =
                GetOrCreateProfile();

            GameObject root =
                new GameObject("Alpha 4 Global Volume");

            Volume volume =
                root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            if (camera != null)
            {
                UniversalAdditionalCameraData data =
                    camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.requiresColorOption =
                    CameraOverrideOption.On;
                data.requiresDepthOption =
                    CameraOverrideOption.On;
            }

            return volume;
        }

        private static VolumeProfile GetOrCreateProfile()
        {
            VolumeProfile profile =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                    ProfilePath);

            if (profile == null)
            {
                profile =
                    ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name =
                    "Alpha4PostProcessProfile";
                AssetDatabase.CreateAsset(
                    profile,
                    ProfilePath);
            }

            Bloom bloom =
                GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.intensity.Override(0.42f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.62f);

            Tonemapping tonemapping =
                GetOrAdd<Tonemapping>(profile);
            tonemapping.active = true;
            tonemapping.mode.Override(
                TonemappingMode.ACES);

            ColorAdjustments color =
                GetOrAdd<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.Override(-0.08f);
            color.contrast.Override(11f);
            color.saturation.Override(-6f);

            WhiteBalance whiteBalance =
                GetOrAdd<WhiteBalance>(profile);
            whiteBalance.active = true;
            whiteBalance.temperature.Override(-7f);
            whiteBalance.tint.Override(2f);

            Vignette vignette =
                GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.52f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static T GetOrAdd<T>(
            VolumeProfile profile)
            where T : VolumeComponent
        {
            if (profile.TryGet<T>(out T component))
                return component;

            return profile.Add<T>(true);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            int slash = folder.LastIndexOf('/');
            if (slash <= 0)
                return;

            string parent =
                folder.Substring(0, slash);
            string name =
                folder.Substring(slash + 1);

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(
                parent,
                name);
        }
    }
}
#endif
