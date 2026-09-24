using System;
using UnityEngine;

namespace DungeonsCrows.Rendering
{
    public enum VisualQualityTier
    {
        Cinematic,
        Balanced,
        Mobile
    }

    [Serializable]
    public readonly struct Alpha4QualityProfile
    {
        public readonly int TargetFrameRate;
        public readonly int VSyncCount;
        public readonly int AntiAliasing;
        public readonly float ShadowDistance;
        public readonly int ShadowCascades;
        public readonly float LodBias;
        public readonly int MaximumLodLevel;
        public readonly bool Hdr;
        public readonly bool Msaa;
        public readonly bool DynamicResolution;
        public readonly bool RealtimeReflectionProbes;
        public readonly AnisotropicFiltering AnisotropicFiltering;

        public Alpha4QualityProfile(
            int targetFrameRate,
            int vSyncCount,
            int antiAliasing,
            float shadowDistance,
            int shadowCascades,
            float lodBias,
            int maximumLodLevel,
            bool hdr,
            bool msaa,
            bool dynamicResolution,
            bool realtimeReflectionProbes,
            AnisotropicFiltering anisotropicFiltering)
        {
            TargetFrameRate = targetFrameRate;
            VSyncCount = vSyncCount;
            AntiAliasing = antiAliasing;
            ShadowDistance = shadowDistance;
            ShadowCascades = shadowCascades;
            LodBias = lodBias;
            MaximumLodLevel = maximumLodLevel;
            Hdr = hdr;
            Msaa = msaa;
            DynamicResolution = dynamicResolution;
            RealtimeReflectionProbes = realtimeReflectionProbes;
            AnisotropicFiltering = anisotropicFiltering;
        }
    }

    /// <summary>
    /// Applies engine-level quality budgets without changing authoritative game state.
    /// URP-specific asset tuning remains owned by the project's URP asset.
    /// </summary>
    public sealed class Alpha4QualityDirector : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private VisualQualityTier tier = VisualQualityTier.Balanced;
        [SerializeField] private bool applyOnAwake = true;

        public VisualQualityTier Tier => tier;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            if (applyOnAwake)
                Apply();
        }

        public void Configure(Camera camera, VisualQualityTier initialTier)
        {
            targetCamera = camera;
            tier = initialTier;
            Apply();
        }

        public void SetTier(VisualQualityTier nextTier)
        {
            tier = nextTier;
            Apply();
        }

        public void Apply()
        {
            Alpha4QualityProfile profile = ProfileFor(tier);

            Application.targetFrameRate = profile.TargetFrameRate;
            QualitySettings.vSyncCount = profile.VSyncCount;
            QualitySettings.antiAliasing = profile.AntiAliasing;
            QualitySettings.shadowDistance = profile.ShadowDistance;
            QualitySettings.shadowCascades = profile.ShadowCascades;
            QualitySettings.lodBias = profile.LodBias;
            QualitySettings.maximumLODLevel = profile.MaximumLodLevel;
            QualitySettings.realtimeReflectionProbes = profile.RealtimeReflectionProbes;
            QualitySettings.anisotropicFiltering = profile.AnisotropicFiltering;

            if (targetCamera != null)
            {
                targetCamera.allowHDR = profile.Hdr;
                targetCamera.allowMSAA = profile.Msaa;
                targetCamera.allowDynamicResolution = profile.DynamicResolution;
            }
        }

        public static Alpha4QualityProfile ProfileFor(VisualQualityTier tier)
        {
            switch (tier)
            {
                case VisualQualityTier.Cinematic:
                    return new Alpha4QualityProfile(
                        60,
                        1,
                        4,
                        140f,
                        4,
                        2f,
                        0,
                        true,
                        true,
                        false,
                        true,
                        AnisotropicFiltering.ForceEnable);

                case VisualQualityTier.Mobile:
                    return new Alpha4QualityProfile(
                        45,
                        0,
                        0,
                        45f,
                        0,
                        0.85f,
                        1,
                        false,
                        false,
                        true,
                        false,
                        AnisotropicFiltering.Enable);

                default:
                    return new Alpha4QualityProfile(
                        60,
                        1,
                        2,
                        90f,
                        2,
                        1.3f,
                        0,
                        true,
                        true,
                        true,
                        true,
                        AnisotropicFiltering.Enable);
            }
        }
    }
}
