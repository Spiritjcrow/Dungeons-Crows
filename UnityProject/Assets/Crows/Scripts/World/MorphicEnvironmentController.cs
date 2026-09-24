using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonsCrows.World
{
    [Serializable]
    public sealed class MorphPose
    {
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale = Vector3.one;

        public MorphPose() { }

        public MorphPose(Vector3 position, Vector3 eulerAngles, Vector3 scale)
        {
            localPosition = position;
            localEulerAngles = eulerAngles;
            localScale = scale;
        }
    }

    [Serializable]
    public sealed class MorphChannel
    {
        public Transform target;
        public MorphPose crowCrypt = new MorphPose();
        public MorphPose boneRookery = new MorphPose();
        public MorphPose blackRook = new MorphPose();

        public MorphPose PoseForChapter(int chapter)
        {
            if (chapter >= 3) return blackRook;
            if (chapter == 2) return boneRookery;
            return crowCrypt;
        }
    }

    /// <summary>
    /// Visual-only environment morphing. The authoritative campaign decides
    /// when a story boundary is committed; this component renders that state.
    /// </summary>
    public sealed class MorphicEnvironmentController : MonoBehaviour
    {
        [Header("Geometry")]
        [SerializeField] private List<MorphChannel> channels = new List<MorphChannel>();
        [SerializeField, Min(0.05f)] private float morphDuration = 2.8f;
        [SerializeField] private AnimationCurve morphCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Atmosphere")]
        [SerializeField] private Light keyLight;
        [SerializeField] private Color crowCryptFog = new Color(0.035f, 0.04f, 0.055f);
        [SerializeField] private Color boneRookeryFog = new Color(0.07f, 0.055f, 0.045f);
        [SerializeField] private Color blackRookFog = new Color(0.025f, 0.018f, 0.035f);
        [SerializeField] private Color crowCryptLight = new Color(0.42f, 0.5f, 0.72f);
        [SerializeField] private Color boneRookeryLight = new Color(0.9f, 0.42f, 0.2f);
        [SerializeField] private Color blackRookLight = new Color(0.5f, 0.22f, 0.7f);

        [Header("Optional VFX")]
        [SerializeField] private ParticleSystem morphBurst;
        [SerializeField] private AudioSource morphAudio;

        private Coroutine _activeMorph;
        private int _chapter = 1;

        public int CurrentChapter => _chapter;
        public bool IsMorphing => _activeMorph != null;

        public void SetKeyLight(Light light) => keyLight = light;

        public void SetMorphVfx(ParticleSystem burst, AudioSource audio = null)
        {
            morphBurst = burst;
            morphAudio = audio;
        }

        public void RegisterChannel(
            Transform target,
            MorphPose crowCrypt,
            MorphPose boneRookery,
            MorphPose blackRook)
        {
            channels.Add(new MorphChannel
            {
                target = target,
                crowCrypt = crowCrypt,
                boneRookery = boneRookery,
                blackRook = blackRook
            });
        }

        public void CommitStoryBoundary(int committedChapter)
        {
            int nextChapter = Mathf.Clamp(committedChapter, 1, 3);
            if (nextChapter == _chapter && _activeMorph == null) return;

            if (_activeMorph != null)
                StopCoroutine(_activeMorph);

            _activeMorph = StartCoroutine(MorphTo(nextChapter));
        }

        public void SnapToChapter(int chapter)
        {
            _chapter = Mathf.Clamp(chapter, 1, 3);
            ApplyPoseImmediate(_chapter);
            ApplyAtmosphere(_chapter, 1f);
        }

        private IEnumerator MorphTo(int nextChapter)
        {
            var starts = new List<MorphPose>(channels.Count);
            foreach (MorphChannel channel in channels)
            {
                if (channel.target == null)
                {
                    starts.Add(null);
                    continue;
                }

                starts.Add(new MorphPose(
                    channel.target.localPosition,
                    channel.target.localEulerAngles,
                    channel.target.localScale));
            }

            Color startFog = RenderSettings.fogColor;
            Color startLight = keyLight != null ? keyLight.color : Color.white;
            Color targetFog = FogFor(nextChapter);
            Color targetLight = LightFor(nextChapter);

            if (morphBurst != null) morphBurst.Play(true);
            if (morphAudio != null) morphAudio.Play();

            float elapsed = 0f;
            while (elapsed < morphDuration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / morphDuration);
                float t = morphCurve.Evaluate(normalized);

                for (int i = 0; i < channels.Count; i++)
                {
                    MorphChannel channel = channels[i];
                    MorphPose start = starts[i];
                    if (channel.target == null || start == null) continue;

                    MorphPose end = channel.PoseForChapter(nextChapter);
                    channel.target.localPosition = Vector3.LerpUnclamped(
                        start.localPosition,
                        end.localPosition,
                        t);
                    channel.target.localRotation = Quaternion.SlerpUnclamped(
                        Quaternion.Euler(start.localEulerAngles),
                        Quaternion.Euler(end.localEulerAngles),
                        t);
                    channel.target.localScale = Vector3.LerpUnclamped(
                        start.localScale,
                        end.localScale,
                        t);
                }

                RenderSettings.fogColor = Color.Lerp(startFog, targetFog, t);
                if (keyLight != null)
                    keyLight.color = Color.Lerp(startLight, targetLight, t);

                yield return null;
            }

            _chapter = nextChapter;
            ApplyPoseImmediate(_chapter);
            ApplyAtmosphere(_chapter, 1f);
            _activeMorph = null;
        }

        private void ApplyPoseImmediate(int chapter)
        {
            foreach (MorphChannel channel in channels)
            {
                if (channel.target == null) continue;
                MorphPose pose = channel.PoseForChapter(chapter);
                channel.target.localPosition = pose.localPosition;
                channel.target.localRotation = Quaternion.Euler(pose.localEulerAngles);
                channel.target.localScale = pose.localScale;
            }
        }

        private void ApplyAtmosphere(int chapter, float weight)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.Lerp(
                RenderSettings.fogColor,
                FogFor(chapter),
                weight);

            if (keyLight != null)
                keyLight.color = Color.Lerp(
                    keyLight.color,
                    LightFor(chapter),
                    weight);
        }

        private Color FogFor(int chapter)
        {
            if (chapter >= 3) return blackRookFog;
            if (chapter == 2) return boneRookeryFog;
            return crowCryptFog;
        }

        private Color LightFor(int chapter)
        {
            if (chapter >= 3) return blackRookLight;
            if (chapter == 2) return boneRookeryLight;
            return crowCryptLight;
        }
    }
}
