using System.Collections;
using DungeonsCrows.CameraSystem;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    /// <summary>
    /// Lightweight camera impulse driven only by resolved canonical cues.
    /// It is presentation-only and cannot affect combat state.
    /// </summary>
    public sealed class CombatCameraFeedback : MonoBehaviour
    {
        [SerializeField] private DualPerspectiveCamera cameraRig;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float hitDuration = 0.11f;
        [SerializeField] private float hitAmplitude = 0.07f;
        [SerializeField] private float damageDuration = 0.16f;
        [SerializeField] private float damageAmplitude = 0.11f;
        [SerializeField] private float ritualDuration = 0.3f;
        [SerializeField] private float ritualAmplitude = 0.045f;

        private Coroutine _impulse;
        private Vector3 _offset;

        public void Configure(
            DualPerspectiveCamera rig,
            Transform targetTransform)
        {
            cameraRig = rig;
            cameraTransform = targetTransform;
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

            if (duration <= 0f || amplitude <= 0f)
                return;

            if (_impulse != null)
                StopCoroutine(_impulse);

            _impulse = StartCoroutine(
                ImpulseRoutine(duration, amplitude));
        }

        private IEnumerator ImpulseRoutine(
            float duration,
            float amplitude)
        {
            if (cameraTransform == null)
            {
                _impulse = null;
                yield break;
            }

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

                Vector3 nextOffset =
                    new Vector3(circle.x, circle.y, 0f);

                cameraTransform.localPosition +=
                    nextOffset - _offset;

                _offset = nextOffset;
                yield return null;
            }

            cameraTransform.localPosition -= _offset;
            _offset = Vector3.zero;
            _impulse = null;
        }

        private void OnDisable()
        {
            if (_impulse != null)
                StopCoroutine(_impulse);

            if (cameraTransform != null)
                cameraTransform.localPosition -= _offset;

            _offset = Vector3.zero;
            _impulse = null;
        }
    }
}
