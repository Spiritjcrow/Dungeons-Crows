using System;
using DungeonsCrows.Online;

namespace DungeonsCrows.Presentation
{
    [Flags]
    public enum PresentationCue
    {
        None = 0,
        EnemyDamaged = 1 << 0,
        EnemyDefeated = 1 << 1,
        PlayerDamaged = 1 << 2,
        PlayerHealed = 1 << 3,
        PlayerGuarded = 1 << 4,
        RitualAdvanced = 1 << 5,
        ObjectiveCompleted = 1 << 6,
        ChapterAdvanced = 1 << 7,
        Victory = 1 << 8,
        Defeat = 1 << 9
    }

    public readonly struct CanonicalPresentationDelta
    {
        public readonly PresentationCue Cues;
        public readonly int EnemyDamage;
        public readonly int PlayerDamage;
        public readonly int PlayerHealing;
        public readonly int PreviousChapter;
        public readonly int CurrentChapter;

        public CanonicalPresentationDelta(
            PresentationCue cues,
            int enemyDamage,
            int playerDamage,
            int playerHealing,
            int previousChapter,
            int currentChapter)
        {
            Cues = cues;
            EnemyDamage = enemyDamage;
            PlayerDamage = playerDamage;
            PlayerHealing = playerHealing;
            PreviousChapter = previousChapter;
            CurrentChapter = currentChapter;
        }

        public bool Has(PresentationCue cue) =>
            (Cues & cue) == cue;

        public static CanonicalPresentationDelta From(
            GameSessionDto before,
            GameSessionDto after,
            string playerId)
        {
            if (before == null || after == null)
                return new CanonicalPresentationDelta(
                    PresentationCue.None,
                    0,
                    0,
                    0,
                    before?.chapter ?? 0,
                    after?.chapter ?? 0);

            PresentationCue cues = PresentationCue.None;

            int enemyDamage = 0;
            if (before.enemy != null && after.enemy != null)
            {
                enemyDamage = Math.Max(
                    0,
                    before.enemy.hp - after.enemy.hp);

                if (enemyDamage > 0)
                    cues |= PresentationCue.EnemyDamaged;

                if (before.enemy.hp > 0 && after.enemy.hp <= 0)
                    cues |= PresentationCue.EnemyDefeated;
            }

            CombatantDto beforePlayer =
                FindPlayer(before, playerId);
            CombatantDto afterPlayer =
                FindPlayer(after, playerId);

            int playerDamage = 0;
            int playerHealing = 0;

            if (beforePlayer != null && afterPlayer != null)
            {
                playerDamage = Math.Max(
                    0,
                    beforePlayer.hp - afterPlayer.hp);

                playerHealing = Math.Max(
                    0,
                    afterPlayer.hp - beforePlayer.hp);

                if (playerDamage > 0)
                    cues |= PresentationCue.PlayerDamaged;

                if (playerHealing > 0)
                    cues |= PresentationCue.PlayerHealed;

                bool guardStarted =
                    afterPlayer.defendingUntilTurn >
                    beforePlayer.defendingUntilTurn;

                if (guardStarted)
                    cues |= PresentationCue.PlayerGuarded;
            }

            bool ritualAdvanced =
                after.ritualAttempts > before.ritualAttempts;

            bool objectiveCompleted =
                (!before.altarOpened && after.altarOpened) ||
                (!before.rookeryPurified &&
                 after.rookeryPurified) ||
                (!before.crownBroken && after.crownBroken);

            if (ritualAdvanced || objectiveCompleted)
                cues |= PresentationCue.RitualAdvanced;

            if (objectiveCompleted)
                cues |= PresentationCue.ObjectiveCompleted;

            if (after.chapter > before.chapter)
                cues |= PresentationCue.ChapterAdvanced;

            if (!string.Equals(
                    before.campaignStatus,
                    after.campaignStatus,
                    StringComparison.Ordinal))
            {
                if (string.Equals(
                        after.campaignStatus,
                        "victory",
                        StringComparison.Ordinal))
                {
                    cues |= PresentationCue.Victory;
                }
                else if (string.Equals(
                             after.campaignStatus,
                             "defeat",
                             StringComparison.Ordinal))
                {
                    cues |= PresentationCue.Defeat;
                }
            }

            return new CanonicalPresentationDelta(
                cues,
                enemyDamage,
                playerDamage,
                playerHealing,
                before.chapter,
                after.chapter);
        }

        private static CombatantDto FindPlayer(
            GameSessionDto session,
            string playerId)
        {
            if (session?.party == null ||
                string.IsNullOrEmpty(playerId))
            {
                return null;
            }

            foreach (CombatantDto combatant in session.party)
            {
                if (combatant != null &&
                    string.Equals(
                        combatant.id,
                        playerId,
                        StringComparison.Ordinal))
                {
                    return combatant;
                }
            }

            return null;
        }
    }
}
