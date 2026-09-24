using DungeonsCrows.Online;
using UnityEngine;

namespace DungeonsCrows.World
{
    public sealed class ChapterVisualStateBridge : MonoBehaviour
    {
        [SerializeField] private MorphicEnvironmentController environment;
        [SerializeField] private MorphicLandscapeController landscape;

        private int _lastChapter;

        public void SetEnvironment(
            MorphicEnvironmentController controller)
        {
            environment = controller;
        }

        public void SetLandscape(
            MorphicLandscapeController controller)
        {
            landscape = controller;
        }

        public void ApplyCanonicalSession(
            GameSessionDto session,
            bool immediate = false)
        {
            if (session == null)
                return;

            int chapter = Mathf.Clamp(
                session.chapter,
                1,
                3);

            if (immediate)
            {
                environment?.SnapToChapter(chapter);
                landscape?.SnapToChapter(chapter);
                _lastChapter = chapter;
                return;
            }

            if (chapter == _lastChapter)
                return;

            environment?.CommitStoryBoundary(chapter);
            landscape?.CommitStoryBoundary(chapter);
            _lastChapter = chapter;
        }
    }
}
