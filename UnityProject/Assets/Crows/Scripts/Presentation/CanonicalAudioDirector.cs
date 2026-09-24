using DungeonsCrows.Online;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CanonicalAudioDirector : MonoBehaviour
    {
        [Header("Optional production clips")]
        [SerializeField] private AudioClip attackClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip guardClip;
        [SerializeField] private AudioClip ritualClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField] private AudioClip speakClip;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.55f;

        private AudioSource _source;

        public bool Ready =>
            _source != null &&
            attackClip != null &&
            hitClip != null &&
            guardClip != null &&
            ritualClip != null &&
            victoryClip != null &&
            defeatClip != null;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;

            EnsureFallbackClips();
        }

        public void Present(
            CanonicalPresentationDelta delta,
            string resolvedActionType)
        {
            EnsureFallbackClips();

            switch (resolvedActionType)
            {
                case "attack":
                    Play(attackClip, 0.72f);
                    break;
                case "defend":
                    Play(guardClip, 0.58f);
                    break;
                case "interact":
                    Play(ritualClip, 0.48f);
                    break;
                case "speak":
                    Play(speakClip, 0.36f);
                    break;
            }

            if (delta.Has(PresentationCue.EnemyDamaged) ||
                delta.Has(PresentationCue.PlayerDamaged))
            {
                Play(hitClip, 0.78f);
            }

            if (delta.Has(PresentationCue.ObjectiveCompleted) ||
                delta.Has(PresentationCue.ChapterAdvanced))
            {
                Play(ritualClip, 0.72f);
            }

            if (delta.Has(PresentationCue.Victory))
                Play(victoryClip, 0.9f);

            if (delta.Has(PresentationCue.Defeat))
                Play(defeatClip, 0.82f);
        }

        private void Play(
            AudioClip clip,
            float scale)
        {
            if (_source == null ||
                clip == null)
            {
                return;
            }

            _source.PlayOneShot(
                clip,
                Mathf.Clamp01(volume * scale));
        }

        private void EnsureFallbackClips()
        {
            if (_source == null)
                _source = GetComponent<AudioSource>();

            attackClip ??=
                CreateClip(
                    "DC_AttackFallback",
                    175f,
                    92f,
                    0.16f,
                    0.22f,
                    0.16f);

            hitClip ??=
                CreateClip(
                    "DC_HitFallback",
                    105f,
                    58f,
                    0.12f,
                    0.28f,
                    0.34f);

            guardClip ??=
                CreateClip(
                    "DC_GuardFallback",
                    260f,
                    180f,
                    0.2f,
                    0.18f,
                    0.08f);

            ritualClip ??=
                CreateClip(
                    "DC_RitualFallback",
                    310f,
                    520f,
                    0.5f,
                    0.14f,
                    0.05f);

            victoryClip ??=
                CreateClip(
                    "DC_VictoryFallback",
                    330f,
                    660f,
                    0.72f,
                    0.16f,
                    0.02f);

            defeatClip ??=
                CreateClip(
                    "DC_DefeatFallback",
                    150f,
                    42f,
                    0.65f,
                    0.2f,
                    0.12f);

            speakClip ??=
                CreateClip(
                    "DC_SpeakFallback",
                    220f,
                    245f,
                    0.08f,
                    0.08f,
                    0.02f);
        }

        private static AudioClip CreateClip(
            string name,
            float startFrequency,
            float endFrequency,
            float duration,
            float amplitude,
            float noiseAmount)
        {
            const int sampleRate = 44100;

            int sampleCount =
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        duration * sampleRate));

            float[] data =
                new float[sampleCount];

            uint noiseState = 0x9E3779B9u;

            for (int i = 0;
                 i < sampleCount;
                 i++)
            {
                float t =
                    (float)i /
                    Mathf.Max(
                        1,
                        sampleCount - 1);

                float frequency =
                    Mathf.Lerp(
                        startFrequency,
                        endFrequency,
                        t);

                float phase =
                    2f *
                    Mathf.PI *
                    frequency *
                    (i / (float)sampleRate);

                float envelope =
                    Mathf.Sin(
                        Mathf.PI * t);

                noiseState ^=
                    noiseState << 13;
                noiseState ^=
                    noiseState >> 17;
                noiseState ^=
                    noiseState << 5;

                float noise =
                    ((noiseState & 0xFFFFu) /
                     32767.5f) -
                    1f;

                data[i] =
                    (Mathf.Sin(phase) *
                     (1f - noiseAmount) +
                     noise * noiseAmount) *
                    amplitude *
                    envelope;
            }

            AudioClip clip =
                AudioClip.Create(
                    name,
                    sampleCount,
                    1,
                    sampleRate,
                    false);

            clip.SetData(data, 0);
            return clip;
        }
    }
}
