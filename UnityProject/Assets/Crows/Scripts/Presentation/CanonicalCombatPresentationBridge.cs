using System;
using DungeonsCrows.Online;
using DungeonsCrows.World;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    public sealed class CanonicalCombatPresentationBridge : MonoBehaviour
    {
        [SerializeField] private OnlineCampaignController campaign;
        [SerializeField] private ChapterVisualStateBridge chapterVisuals;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator enemyAnimator;

        [Header("Resolved combat VFX")]
        [SerializeField] private ParticleSystem attackHitFx;
        [SerializeField] private ParticleSystem playerDamageFx;
        [SerializeField] private ParticleSystem guardFx;
        [SerializeField] private ParticleSystem healFx;
        [SerializeField] private ParticleSystem ritualFx;
        [SerializeField] private ParticleSystem victoryFx;
        [SerializeField] private ParticleSystem defeatFx;

        public void Configure(
            OnlineCampaignController campaignController,
            ChapterVisualStateBridge chapterBridge,
            ParticleSystem attackHit,
            ParticleSystem playerDamage,
            ParticleSystem guard,
            ParticleSystem heal,
            ParticleSystem ritual,
            ParticleSystem victory,
            ParticleSystem defeat)
        {
            campaign = campaignController;
            chapterVisuals = chapterBridge;
            attackHitFx = attackHit;
            playerDamageFx = playerDamage;
            guardFx = guard;
            healFx = heal;
            ritualFx = ritual;
            victoryFx = victory;
            defeatFx = defeat;
        }

        private void OnEnable()
        {
            if (campaign == null) return;
            campaign.SessionChanged += OnSessionChanged;
            campaign.TurnResolved += OnTurnResolved;
        }

        private void OnDisable()
        {
            if (campaign == null) return;
            campaign.SessionChanged -= OnSessionChanged;
            campaign.TurnResolved -= OnTurnResolved;
        }

        private void OnSessionChanged(GameSessionDto session)
        {
            if (session == null) return;
            if (chapterVisuals != null)
                chapterVisuals.ApplyCanonicalSession(session);
        }

        private void OnTurnResolved(GameSessionDto before, TurnEnvelope envelope)
        {
            GameSessionDto after = envelope?.session;
            if (before == null || after == null) return;

            CombatantDto beforePlayer = FindPlayer(before, campaign.PlayerId);
            CombatantDto afterPlayer = FindPlayer(after, campaign.PlayerId);

            if (before.enemy != null && after.enemy != null &&
                after.enemy.hp < before.enemy.hp)
            {
                Play(attackHitFx);
                Trigger(enemyAnimator, "Hit");
                Trigger(playerAnimator, "AttackResolved");
            }

            if (beforePlayer != null && afterPlayer != null)
            {
                if (afterPlayer.hp < beforePlayer.hp)
                {
                    Play(playerDamageFx);
                    Trigger(playerAnimator, "Hit");
                }

                if (afterPlayer.hp > beforePlayer.hp)
                {
                    Play(healFx);
                    Play(guardFx);
                    Trigger(playerAnimator, "Guard");
                }
            }

            bool ritualAdvanced =
                (!before.altarOpened && after.altarOpened) ||
                (!before.rookeryPurified && after.rookeryPurified) ||
                (!before.crownBroken && after.crownBroken) ||
                after.ritualAttempts > before.ritualAttempts;

            if (ritualAdvanced)
                Play(ritualFx);

            if (before.chapter != after.chapter && chapterVisuals != null)
                chapterVisuals.ApplyCanonicalSession(after);

            if (!string.Equals(before.campaignStatus, after.campaignStatus, StringComparison.Ordinal))
            {
                if (string.Equals(after.campaignStatus, "victory", StringComparison.Ordinal))
                    Play(victoryFx);
                else if (string.Equals(after.campaignStatus, "defeat", StringComparison.Ordinal))
                    Play(defeatFx);
            }
        }

        private static CombatantDto FindPlayer(GameSessionDto session, string playerId)
        {
            if (session?.party == null || string.IsNullOrEmpty(playerId)) return null;
            foreach (CombatantDto combatant in session.party)
            {
                if (combatant != null &&
                    string.Equals(combatant.id, playerId, StringComparison.Ordinal))
                    return combatant;
            }

            return null;
        }

        private static void Play(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
                particleSystem.Play(true);
        }

        private static void Trigger(Animator animator, string trigger)
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetTrigger(trigger);
        }
    }
}
