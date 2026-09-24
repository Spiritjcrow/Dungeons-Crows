using DungeonsCrows.Online;
using DungeonsCrows.World;
using UnityEngine;

namespace DungeonsCrows.Presentation
{
    public sealed class CanonicalCombatPresentationBridge : MonoBehaviour
    {
        [SerializeField] private OnlineCampaignController campaign;
        [SerializeField] private ChapterVisualStateBridge chapterVisuals;
        [SerializeField] private ChapterEnemyPresenter enemyPresenter;
        [SerializeField] private CombatCameraFeedback cameraFeedback;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator enemyAnimatorFallback;

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
            ChapterEnemyPresenter chapterEnemyPresenter,
            CombatCameraFeedback combatCameraFeedback,
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
            enemyPresenter = chapterEnemyPresenter;
            cameraFeedback = combatCameraFeedback;
            attackHitFx = attackHit;
            playerDamageFx = playerDamage;
            guardFx = guard;
            healFx = heal;
            ritualFx = ritual;
            victoryFx = victory;
            defeatFx = defeat;
        }

        public void SetAnimators(
            Animator player,
            Animator enemyFallback = null)
        {
            playerAnimator = player;
            enemyAnimatorFallback = enemyFallback;
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
            if (session == null)
            {
                enemyPresenter?.ApplyCanonicalSession(null);
                return;
            }

            chapterVisuals?.ApplyCanonicalSession(session);
            enemyPresenter?.ApplyCanonicalSession(session);
        }

        private void OnTurnResolved(
            GameSessionDto before,
            TurnEnvelope envelope)
        {
            GameSessionDto after = envelope?.session;
            if (before == null || after == null || campaign == null)
                return;

            CanonicalPresentationDelta delta =
                CanonicalPresentationDelta.From(
                    before,
                    after,
                    campaign.PlayerId);

            enemyPresenter?.PresentDelta(delta, after);
            cameraFeedback?.Present(delta);

            PresentResolvedPlayerAction(
                envelope.resolvedActionType);

            if (delta.Has(PresentationCue.EnemyDamaged))
            {
                MoveToEnemy(attackHitFx);
                Play(attackHitFx);

                if (enemyPresenter == null)
                    Trigger(enemyAnimatorFallback, "Hit");
            }

            if (delta.Has(PresentationCue.EnemyDefeated) &&
                enemyPresenter == null)
            {
                Trigger(enemyAnimatorFallback, "Defeated");
            }

            if (delta.Has(PresentationCue.PlayerDamaged))
            {
                Play(playerDamageFx);
                Trigger(playerAnimator, "Hit");
            }

            if (delta.Has(PresentationCue.PlayerGuarded))
                Play(guardFx);

            if (delta.Has(PresentationCue.PlayerHealed))
                Play(healFx);

            if (delta.Has(PresentationCue.RitualAdvanced))
                Play(ritualFx);

            if (delta.Has(PresentationCue.ChapterAdvanced))
                chapterVisuals?.ApplyCanonicalSession(after);

            if (delta.Has(PresentationCue.Victory))
                Play(victoryFx);

            if (delta.Has(PresentationCue.Defeat))
                Play(defeatFx);
        }

        private void PresentResolvedPlayerAction(
            string resolvedActionType)
        {
            if (string.IsNullOrWhiteSpace(resolvedActionType))
                return;

            switch (resolvedActionType)
            {
                case "attack":
                    Trigger(playerAnimator, "AttackResolved");
                    break;
                case "defend":
                    Trigger(playerAnimator, "Guard");
                    break;
                case "interact":
                    Trigger(playerAnimator, "Rite");
                    break;
                case "speak":
                    Trigger(playerAnimator, "Speak");
                    break;
            }
        }

        private void MoveToEnemy(ParticleSystem particleSystem)
        {
            if (particleSystem == null ||
                enemyPresenter?.ActiveImpactAnchor == null)
            {
                return;
            }

            particleSystem.transform.position =
                enemyPresenter.ActiveImpactAnchor.position;
        }

        private static void Play(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
                particleSystem.Play(true);
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
