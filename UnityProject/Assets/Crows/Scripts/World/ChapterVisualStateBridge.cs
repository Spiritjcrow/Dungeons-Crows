using DungeonsCrows.Online;
using UnityEngine;

namespace DungeonsCrows.World
{
    public sealed class ChapterVisualStateBridge : MonoBehaviour
    {
        [SerializeField] private MorphicEnvironmentController environment;

        private int _lastChapter;

        public void SetEnvironment(MorphicEnvironmentController controller)
        {
            environment = controller;
        }

        public void ApplyCanonicalSession(GameSessionDto session, bool immediate = false)
        {
            if (session == null || environment == null) return;

            int chapter = Mathf.Clamp(session.chapter, 1, 3);
            if (immediate)
            {
                environment.SnapToChapter(chapter);
                _lastChapter = chapter;
                return;
            }

            if (chapter == _lastChapter) return;

            environment.CommitStoryBoundary(chapter);
            _lastChapter = chapter;
        }
    }
}
