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

        public void ApplySkeleton(
            SkeletonDataAsset skeleton,
            Material material,
            string idle,
            string attack,
            string hit,
            string defeated,
            Vector2 anchoredPosition,
            Vector2 size,
            Vector3 scale,
            bool playIdle = true)
        {
            if (skeleton == null)
            {
                SetVisible(false);
                return;
            }

            idleAnimation = string.IsNullOrWhiteSpace(idle) ? "stand" : idle;
            attackAnimation = string.IsNullOrWhiteSpace(attack) ? "attack" : attack;
            hitAnimation = string.IsNullOrWhiteSpace(hit) ? idleAnimation : hit;
            defeatedAnimation = string.IsNullOrWhiteSpace(defeated) ? idleAnimation : defeated;

            if (NeedsSkeletonDataUpdate(skeleton, material))
            {
                ApplySkeletonData(skeleton, material);
            }

            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            transform.localScale = scale == Vector3.zero ? Vector3.one : scale;
            SetVisible(true);
            if (playIdle)
            {
                PlayIdle();
            }
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

            ApplySkeletonData(level.enemySpineSkeleton, level.enemySpineMaterial);

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

        public float GetAttackDuration(float fallback) => GetAnimationDuration(attackAnimation, fallback);
        public float GetDefeatedDuration(float fallback) => GetAnimationDuration(defeatedAnimation, fallback);

        private void ApplySkeletonData(SkeletonDataAsset skeleton, Material material)
        {
            if (skeletonGraphic != null)
            {
                skeletonGraphic.skeletonDataAsset = skeleton;
                skeletonGraphic.allowMultipleCanvasRenderers = true;
                if (skeletonAnimation != null)
                {
                    skeletonGraphic.Animation = skeletonAnimation;
                }

                skeletonGraphic.material = material;

                skeletonGraphic.Initialize(true);
            }

            if (skeletonAnimation != null)
            {
                skeletonAnimation.SkeletonDataAsset = skeleton;
                skeletonAnimation.AnimationName = idleAnimation;
                skeletonAnimation.loop = true;
                skeletonAnimation.Initialize(true);
            }
        }

        private bool NeedsSkeletonDataUpdate(SkeletonDataAsset skeleton, Material material)
        {
            if (skeletonGraphic != null)
            {
                if (skeletonGraphic.skeletonDataAsset != skeleton)
                {
                    return true;
                }

                if (skeletonGraphic.material != material)
                {
                    return true;
                }
            }

            if (skeletonAnimation != null && skeletonAnimation.SkeletonDataAsset != skeleton)
            {
                return true;
            }

            return GetAnimationState() == null;
        }

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

        private float GetAnimationDuration(string animationName, float fallback)
        {
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return fallback;
            }

            var skeletonData = skeletonGraphic != null && skeletonGraphic.SkeletonData != null
                ? skeletonGraphic.SkeletonData
                : skeletonAnimation != null && skeletonAnimation.SkeletonDataAsset != null
                    ? skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false)
                    : null;
            var animation = skeletonData != null ? skeletonData.FindAnimation(animationName) : null;
            return animation != null ? Mathf.Max(0.01f, animation.Duration) : fallback;
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
