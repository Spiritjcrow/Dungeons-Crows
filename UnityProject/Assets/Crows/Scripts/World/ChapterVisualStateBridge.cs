using DungeonsCrows.Online;
using UnityEngine;

namespace DungeonsCrows.World
{
    /// <summary>
    /// Visual-only adapter from canonical online campaign state to the scene.
    /// It never changes authoritative game data.
    /// </summary>
    public sealed class ChapterVisualStateBridge : MonoBehaviour
    {
        [SerializeField] private MorphicEnvironmentController environment;

        private int _lastChapter;

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
