using UnityEngine;
using Spine.Unity;

namespace Subspace
{
    public sealed class SubspaceSpineActorView : MonoBehaviour
    {
        [SerializeField] private SkeletonGraphic skeletonGraphic;
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private string idleAnimation = "stand";
        [SerializeField] private string attackAnimation = "attack";
        [SerializeField] private string hitAnimation = "stand";
        [SerializeField] private string defeatedAnimation = "stand";

        public bool IsConfigured => gameObject.activeSelf && (skeletonGraphic != null || skeletonAnimation != null);
        public Color GraphicColor
        {
            get => skeletonGraphic != null ? skeletonGraphic.color : Color.white;
            set
            {
                if (skeletonGraphic != null)
                {
                    skeletonGraphic.color = value;
                }
            }
        }

        public void Configure(SkeletonGraphic graphic, SkeletonAnimation animation)
        {
            skeletonGraphic = graphic;
            skeletonAnimation = animation;
        }

        private void Awake()
        {
            if (skeletonGraphic == null)
            {
                skeletonGraphic = GetComponent<SkeletonGraphic>();
            }

            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }
        }

        public void ApplyLevel(SubspaceLevelDefinition level)
        {
            if (level == null || level.enemySpineSkeleton == null)
            {
                SetVisible(false);
                return;
            }

            idleAnimation = string.IsNullOrWhiteSpace(level.enemySpineIdleAnimation) ? "stand" : level.enemySpineIdleAnimation;
            attackAnimation = string.IsNullOrWhiteSpace(level.enemySpineAttackAnimation) ? "attack" : level.enemySpineAttackAnimation;
            hitAnimation = string.IsNullOrWhiteSpace(level.enemySpineHitAnimation) ? idleAnimation : level.enemySpineHitAnimation;
            defeatedAnimation = string.IsNullOrWhiteSpace(level.enemySpineDefeatedAnimation) ? idleAnimation : level.enemySpineDefeatedAnimation;

            if (skeletonGraphic != null)
            {
                skeletonGraphic.skeletonDataAsset = level.enemySpineSkeleton;
                if (skeletonAnimation != null)
                {
                    skeletonGraphic.Animation = skeletonAnimation;
                }

                if (level.enemySpineMaterial != null)
                {
                    skeletonGraphic.material = level.enemySpineMaterial;
                }

                skeletonGraphic.Initialize(true);
            }

            if (skeletonAnimation != null)
            {
                skeletonAnimation.SkeletonDataAsset = level.enemySpineSkeleton;
                skeletonAnimation.AnimationName = idleAnimation;
                skeletonAnimation.loop = true;
                skeletonAnimation.Initialize(true);
            }

            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = level.enemySpineAnchoredPosition;
                rect.sizeDelta = level.enemySpineSize;
            }

            transform.localScale = level.enemySpineScale == Vector3.zero ? Vector3.one : level.enemySpineScale;
            SetVisible(true);
            PlayIdle();
        }

        public void Clear()
        {
            SetVisible(false);
        }

        public bool PlayIdle() => Play(idleAnimation, true);
        public bool PlayAttack() => Play(attackAnimation, false);
        public bool PlayHit() => Play(hitAnimation, false);
        public bool PlayDefeated() => Play(defeatedAnimation, false);

        private bool Play(string animationName, bool loop)
        {
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return false;
            }

            var animationState = GetAnimationState();
            if (animationState != null)
            {
                animationState.SetAnimation(0, animationName, loop);
                return true;
            }

            return false;
        }

        private Spine.AnimationState GetAnimationState()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                return skeletonAnimation.AnimationState;
            }

            if (skeletonGraphic != null && skeletonGraphic.Animation is IAnimationStateComponent animationStateComponent)
            {
                return animationStateComponent.AnimationState;
            }

            return null;
        }

        private void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
