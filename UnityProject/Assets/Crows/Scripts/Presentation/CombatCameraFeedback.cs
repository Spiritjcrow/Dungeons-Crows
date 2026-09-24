using System.Collections;
using DungeonsCrows.CameraSystem;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    /// <summary>
    /// Lightweight camera impulse driven only by resolved canonical cues.
    /// It writes to the camera rig's presentation offset rather than fighting
    /// the first/third-person follow transform.
    /// </summary>
    public sealed class CombatCameraFeedback : MonoBehaviour
    {
        [SerializeField] private DualPerspectiveCamera cameraRig;
        [SerializeField] private float hitDuration = 0.11f;
        [SerializeField] private float hitAmplitude = 0.07f;
        [SerializeField] private float damageDuration = 0.16f;
        [SerializeField] private float damageAmplitude = 0.11f;
        [SerializeField] private float ritualDuration = 0.3f;
        [SerializeField] private float ritualAmplitude = 0.045f;

        private Coroutine _impulse;

        public void Configure(DualPerspectiveCamera rig)
        {
            cameraRig = rig;
        }

        public void Present(CanonicalPresentationDelta delta)
        {
            float duration = 0f;
            float amplitude = 0f;

            if (delta.Has(PresentationCue.PlayerDamaged))
            {
                duration = damageDuration;
                amplitude = damageAmplitude;
            }
            else if (delta.Has(PresentationCue.EnemyDamaged))
            {
                duration = hitDuration;
                amplitude = hitAmplitude;
            }
            else if (delta.Has(PresentationCue.RitualAdvanced) ||
                     delta.Has(PresentationCue.ChapterAdvanced))
            {
                duration = ritualDuration;
                amplitude = ritualAmplitude;
            }

            if (cameraRig == null ||
                duration <= 0f ||
                amplitude <= 0f)
            {
                return;
            }

            if (_impulse != null)
                StopCoroutine(_impulse);

            _impulse = StartCoroutine(
                ImpulseRoutine(duration, amplitude));
        }

        private IEnumerator ImpulseRoutine(
            float duration,
            float amplitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade =
                    1f - Mathf.Clamp01(elapsed / duration);

                Vector2 circle =
                    Random.insideUnitCircle *
                    amplitude *
                    fade;

                cameraRig.SetPresentationOffset(
                    new Vector3(circle.x, circle.y, 0f));

                yield return null;
            }

            cameraRig.ClearPresentationOffset();
            _impulse = null;
        }

        private void OnDisable()
        {
            if (_impulse != null)
                StopCoroutine(_impulse);

            if (cameraRig != null)
                cameraRig.ClearPresentationOffset();

            _impulse = null;
        }
    }
}
