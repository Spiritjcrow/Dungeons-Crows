using System.Collections.Generic;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    public enum CrowFlockMode
    {
        Orbit,
        Ritual,
        Victory,
        Defeat
    }

    /// <summary>
    /// Presentation-only crow flock. Its motion reacts to canonical cues but
    /// never changes combat, quests, ownership, or persistent state.
    /// </summary>
    public sealed class CrowFlockController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private List<Transform> crows =
            new List<Transform>();
        [SerializeField] private CrowFlockMode mode =
            CrowFlockMode.Orbit;
        [SerializeField] private float orbitRadius = 3.8f;
        [SerializeField] private float orbitHeight = 2.6f;
        [SerializeField] private float orbitSpeed = 0.75f;
        [SerializeField] private float verticalBob = 0.35f;
        [SerializeField] private float reactionSeconds = 2.2f;

        private float _reactionUntil;

        public CrowFlockMode Mode => mode;
        public int CrowCount => crows.Count;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        public void RegisterCrow(Transform crow)
        {
            if (crow != null && !crows.Contains(crow))
                crows.Add(crow);
        }

        public void SetMode(CrowFlockMode nextMode)
        {
            mode = nextMode;

            if (nextMode == CrowFlockMode.Orbit)
                _reactionUntil = 0f;
            else if (nextMode == CrowFlockMode.Ritual)
                _reactionUntil =
                    Time.unscaledTime + reactionSeconds;
        }

        public void Present(
            CanonicalPresentationDelta delta)
        {
            if (delta.Has(PresentationCue.Defeat))
            {
                SetMode(CrowFlockMode.Defeat);
                return;
            }

            if (delta.Has(PresentationCue.Victory))
            {
                SetMode(CrowFlockMode.Victory);
                return;
            }

            if (delta.Has(PresentationCue.RitualAdvanced) ||
                delta.Has(PresentationCue.ChapterAdvanced))
            {
                SetMode(CrowFlockMode.Ritual);
            }
        }

        private void Update()
        {
            if (target == null || crows.Count == 0)
                return;

            if (mode == CrowFlockMode.Ritual &&
                _reactionUntil > 0f &&
                Time.unscaledTime >= _reactionUntil)
            {
                mode = CrowFlockMode.Orbit;
                _reactionUntil = 0f;
            }

            float time =
                Time.unscaledTime * orbitSpeed;

            for (int i = 0;
                 i < crows.Count;
                 i++)
            {
                Transform crow = crows[i];
                if (crow == null)
                    continue;

                float phase =
                    time +
                    i * (Mathf.PI * 2f / crows.Count);

                Vector3 desired =
                    PositionFor(
                        i,
                        phase);

                crow.position =
                    Vector3.Lerp(
                        crow.position,
                        desired,
                        1f -
                        Mathf.Exp(
                            -8f * Time.unscaledDeltaTime));

                Vector3 tangent =
                    TangentFor(
                        i,
                        phase);

                if (tangent.sqrMagnitude > 0.001f)
                {
                    Quaternion rotation =
                        Quaternion.LookRotation(
                            tangent.normalized,
                            Vector3.up);

                    crow.rotation =
                        Quaternion.Slerp(
                            crow.rotation,
                            rotation,
                            1f -
                            Mathf.Exp(
                                -10f *
                                Time.unscaledDeltaTime));
                }
            }
        }

        private Vector3 PositionFor(
            int index,
            float phase)
        {
            Vector3 center =
                target.position +
                Vector3.up * orbitHeight;

            float radius = orbitRadius;
            float heightOffset =
                Mathf.Sin(
                    phase * 1.9f +
                    index * 0.7f) *
                verticalBob;

            switch (mode)
            {
                case CrowFlockMode.Ritual:
                    radius *= 0.68f;
                    heightOffset +=
                        0.8f +
                        Mathf.Sin(
                            phase * 2.8f) *
                        0.35f;
                    break;

                case CrowFlockMode.Victory:
                    radius *=
                        1.1f +
                        0.2f *
                        Mathf.Sin(
                            phase * 0.7f);
                    heightOffset +=
                        1.5f +
                        index * 0.18f;
                    break;

                case CrowFlockMode.Defeat:
                    radius *=
                        1.8f +
                        index * 0.08f;
                    heightOffset +=
                        1.1f +
                        index * 0.25f;
                    break;
            }

            return center +
                new Vector3(
                    Mathf.Cos(phase) * radius,
                    heightOffset,
                    Mathf.Sin(phase) * radius);
        }

        private Vector3 TangentFor(
            int index,
            float phase)
        {
            float direction =
                mode == CrowFlockMode.Defeat
                    ? 1.35f
                    : 1f;

            return new Vector3(
                -Mathf.Sin(phase) * direction,
                Mathf.Cos(
                    phase * 1.7f +
                    index) * 0.12f,
                Mathf.Cos(phase) * direction);
        }
    }
}
