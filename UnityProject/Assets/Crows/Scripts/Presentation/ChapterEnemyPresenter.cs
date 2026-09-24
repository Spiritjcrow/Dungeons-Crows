using System;
using System.Collections.Generic;
using DungeonsCrows.Online;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    [Serializable]
    public sealed class ChapterEnemyVisual
    {
        public int chapter = 1;
        public GameObject root;
        public Animator animator;
        public ProceduralActorMotion proceduralMotion;
        public Transform impactAnchor;
        public string expectedEnemyId;
    }

    /// <summary>
    /// Chooses the visible enemy representation from canonical chapter state.
    /// This component never changes HP or combat state.
    /// </summary>
    public sealed class ChapterEnemyPresenter : MonoBehaviour
    {
        [SerializeField] private List<ChapterEnemyVisual> visuals =
            new List<ChapterEnemyVisual>();

        private ChapterEnemyVisual _active;

        public ChapterEnemyVisual ActiveVisual => _active;
        public Transform ActiveImpactAnchor =>
            _active?.impactAnchor != null
                ? _active.impactAnchor
                : _active?.root != null
                    ? _active.root.transform
                    : null;

        public void Register(
            int chapter,
            string expectedEnemyId,
            GameObject root,
            Animator animator = null,
            ProceduralActorMotion proceduralMotion = null,
            Transform impactAnchor = null)
        {
            visuals.Add(new ChapterEnemyVisual
            {
                chapter = chapter,
                expectedEnemyId = expectedEnemyId,
                root = root,
                animator = animator,
                proceduralMotion = proceduralMotion,
                impactAnchor = impactAnchor
            });
        }

        public void ApplyCanonicalSession(
            GameSessionDto session,
            bool immediate = false)
        {
            if (session == null)
            {
                SetActive(null);
                return;
            }

            ChapterEnemyVisual next = FindFor(session);

            if (!ReferenceEquals(next, _active))
            {
                SetActive(next);
                _active?.proceduralMotion?.ResetPose();

                if (!immediate)
                    Trigger(_active?.animator, "Spawn");
            }

            if (_active?.root != null)
            {
                bool shouldShow = session.enemy != null;
                _active.root.SetActive(shouldShow);
            }
        }

        public void PresentDelta(
            CanonicalPresentationDelta delta,
            GameSessionDto after)
        {
            ApplyCanonicalSession(after);

            if (_active == null)
                return;

            if (delta.Has(PresentationCue.EnemyDamaged))
            {
                if (_active.animator != null)
                    Trigger(_active.animator, "Hit");
                else
                    _active.proceduralMotion?.PlayHit();
            }

            if (delta.Has(PresentationCue.EnemyDefeated))
            {
                if (_active.animator != null)
                    Trigger(_active.animator, "Defeated");
                else
                    _active.proceduralMotion?.PlayDefeated();
            }
        }

        private ChapterEnemyVisual FindFor(GameSessionDto session)
        {
            ChapterEnemyVisual chapterFallback = null;

            foreach (ChapterEnemyVisual visual in visuals)
            {
                if (visual == null ||
                    visual.chapter != session.chapter)
                {
                    continue;
                }

                if (chapterFallback == null)
                    chapterFallback = visual;

                if (session.enemy != null &&
                    !string.IsNullOrEmpty(
                        visual.expectedEnemyId) &&
                    string.Equals(
                        visual.expectedEnemyId,
                        session.enemy.id,
                        StringComparison.Ordinal))
                {
                    return visual;
                }
            }

            return chapterFallback;
        }

        private void SetActive(ChapterEnemyVisual next)
        {
            foreach (ChapterEnemyVisual visual in visuals)
            {
                if (visual?.root != null)
                    visual.root.SetActive(
                        ReferenceEquals(visual, next));
            }

            _active = next;
        }

        private static void Trigger(
            Animator animator,
            string trigger)
        {
            if (animator != null &&
                animator.isActiveAndEnabled)
            {
                animator.SetTrigger(trigger);
            }
        }
    }
}
