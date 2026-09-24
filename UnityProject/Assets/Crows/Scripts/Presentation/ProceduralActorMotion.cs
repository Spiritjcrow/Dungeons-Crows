using System.Collections;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    /// <summary>
    /// Development-safe presentation fallback for actors without a production
    /// Animator. It moves only the visual transform, never the authoritative
    /// character controller or combat state.
    /// </summary>
    public sealed class ProceduralActorMotion : MonoBehaviour
    {
        [SerializeField] private float actionDuration = 0.28f;
        [SerializeField] private float hitDuration = 0.18f;
        [SerializeField] private float attackDistance = 0.45f;
        [SerializeField] private float hitDistance = 0.22f;
        [SerializeField] private float guardScaleY = 0.9f;
        [SerializeField] private float riteLift = 0.22f;

        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private Quaternion _baseLocalRotation;
        private Coroutine _motion;
        private bool _defeated;

        private void Awake()
        {
            CaptureBasePose();
        }

        public void CaptureBasePose()
        {
            _baseLocalPosition = transform.localPosition;
            _baseLocalScale = transform.localScale;
            _baseLocalRotation = transform.localRotation;
        }

        public void PlayAttack()
        {
            if (_defeated) return;
            Run(ActionRoutine(
                _baseLocalPosition + Vector3.forward * attackDistance,
                _baseLocalScale,
                _baseLocalRotation,
                actionDuration));
        }

        public void PlayGuard()
        {
            if (_defeated) return;
            Run(ActionRoutine(
                _baseLocalPosition,
                new Vector3(
                    _baseLocalScale.x * 1.08f,
                    _baseLocalScale.y * guardScaleY,
                    _baseLocalScale.z * 1.08f),
                _baseLocalRotation *
                Quaternion.Euler(-7f, 0f, 0f),
                actionDuration));
        }

        public void PlayRite()
        {
            if (_defeated) return;
            Run(ActionRoutine(
                _baseLocalPosition + Vector3.up * riteLift,
                _baseLocalScale * 1.03f,
                _baseLocalRotation *
                Quaternion.Euler(0f, 18f, 0f),
                actionDuration * 1.4f));
        }

        public void PlaySpeak()
        {
            if (_defeated) return;
            Run(ActionRoutine(
                _baseLocalPosition + Vector3.up * 0.06f,
                _baseLocalScale,
                _baseLocalRotation *
                Quaternion.Euler(0f, -8f, 0f),
                actionDuration * 0.75f));
        }

        public void PlayHit()
        {
            if (_defeated) return;
            Run(ActionRoutine(
                _baseLocalPosition - Vector3.forward * hitDistance,
                _baseLocalScale,
                _baseLocalRotation *
                Quaternion.Euler(5f, 0f, 5f),
                hitDuration));
        }

        public void PlayVictory()
        {
            if (_defeated) return;
            Run(ActionRoutine(
                _baseLocalPosition + Vector3.up * 0.14f,
                _baseLocalScale * 1.04f,
                _baseLocalRotation *
                Quaternion.Euler(0f, 24f, 0f),
                actionDuration * 1.6f));
        }

        public void PlayDefeated()
        {
            if (_defeated) return;
            _defeated = true;

            if (_motion != null)
                StopCoroutine(_motion);

            _motion = StartCoroutine(
                DefeatRoutine());
        }

        public void ResetPose()
        {
            if (_motion != null)
                StopCoroutine(_motion);

            _motion = null;
            _defeated = false;
            transform.localPosition = _baseLocalPosition;
            transform.localScale = _baseLocalScale;
            transform.localRotation = _baseLocalRotation;
        }

        private void Run(IEnumerator routine)
        {
            if (_motion != null)
                StopCoroutine(_motion);

            _motion = StartCoroutine(routine);
        }

        private IEnumerator ActionRoutine(
            Vector3 peakPosition,
            Vector3 peakScale,
            Quaternion peakRotation,
            float duration)
        {
            float half = Mathf.Max(0.04f, duration * 0.5f);

            yield return BlendPose(
                _baseLocalPosition,
                peakPosition,
                _baseLocalScale,
                peakScale,
                _baseLocalRotation,
                peakRotation,
                half);

            yield return BlendPose(
                peakPosition,
                _baseLocalPosition,
                peakScale,
                _baseLocalScale,
                peakRotation,
                _baseLocalRotation,
                half);

            RestoreBasePose();
            _motion = null;
        }

        private IEnumerator DefeatRoutine()
        {
            Vector3 targetPosition =
                _baseLocalPosition + Vector3.down * 0.45f;
            Quaternion targetRotation =
                _baseLocalRotation *
                Quaternion.Euler(0f, 0f, 78f);

            yield return BlendPose(
                transform.localPosition,
                targetPosition,
                transform.localScale,
                _baseLocalScale,
                transform.localRotation,
                targetRotation,
                0.45f);

            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;
            _motion = null;
        }

        private IEnumerator BlendPose(
            Vector3 fromPosition,
            Vector3 toPosition,
            Vector3 fromScale,
            Vector3 toScale,
            Quaternion fromRotation,
            Quaternion toRotation,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));

                transform.localPosition =
                    Vector3.LerpUnclamped(
                        fromPosition,
                        toPosition,
                        t);
                transform.localScale =
                    Vector3.LerpUnclamped(
                        fromScale,
                        toScale,
                        t);
                transform.localRotation =
                    Quaternion.SlerpUnclamped(
                        fromRotation,
                        toRotation,
                        t);

                yield return null;
            }
        }

        private void RestoreBasePose()
        {
            transform.localPosition = _baseLocalPosition;
            transform.localScale = _baseLocalScale;
            transform.localRotation = _baseLocalRotation;
        }

        private void OnDisable()
        {
            if (_motion != null)
                StopCoroutine(_motion);

            _motion = null;

            if (!_defeated)
                RestoreBasePose();
        }
    }
}
