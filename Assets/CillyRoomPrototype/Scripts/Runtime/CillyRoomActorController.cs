using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomActorController : MonoBehaviour
    {
        [SerializeField] private Image uiImage;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Animator animator;

        [Header("Animator Replacement")]
        [SerializeField] private bool useAnimatorWhenAvailable = true;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string attackStateName = "Attack";
        [SerializeField] private string hitTriggerName = "Hit";
        [SerializeField] private string hitStateName = "Hit";
        [SerializeField] private string escapeTriggerName = "Escape";
        [SerializeField] private string escapeStateName = "Escape";
        [SerializeField] private string defeatedStateName = "Defeated";
        [SerializeField] private float attackAnimationDuration = 0.45f;
        [SerializeField] private float hitAnimationDuration = 0.25f;
        [SerializeField] private float escapeAnimationDuration = 0.65f;

        [Header("Sprite Fallback")]
        [SerializeField] private bool applyFallbackColor;
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color actionColor = Color.white;
        [SerializeField] private Color defeatedColor = Color.gray;

        private Vector3 baseScale;
        private Coroutine animationRoutine;
        private Sprite idleSprite;

        public void Configure(Image image, SpriteRenderer renderer, Rigidbody2D rigidbody2D, Animator actorAnimator = null)
        {
            uiImage = image;
            spriteRenderer = renderer;
            body = rigidbody2D;
            animator = actorAnimator;

            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.simulated = false;
            }
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        public void SetColors(Color idle, Color action, Color defeated)
        {
            idleColor = idle;
            actionColor = action;
            defeatedColor = defeated;
        }

        public void ShowIdle(Sprite sprite)
        {
            StopAnimation();
            idleSprite = sprite;
            ApplySprite(sprite, idleColor);
            transform.localScale = baseScale;
            PlayAnimatorState(idleStateName);
        }

        public void ShowDefeated(Sprite sprite)
        {
            StopAnimation();
            ApplySprite(sprite, defeatedColor);
            transform.localScale = baseScale;
            PlayAnimatorState(defeatedStateName);
        }

        public IEnumerator PlayAttack(Sprite sprite, float duration = 0.45f)
        {
            StopAnimation();
            if (TryPlayAnimatorAction(attackTriggerName, attackStateName))
            {
                yield return new WaitForSeconds(attackAnimationDuration > 0f ? attackAnimationDuration : duration);
                PlayAnimatorState(idleStateName);
                yield break;
            }

            animationRoutine = StartCoroutine(Pulse(sprite, actionColor, 1.16f, duration));
            yield return animationRoutine;
            animationRoutine = null;
            ApplySprite(idleSprite, idleColor);
            transform.localScale = baseScale;
        }

        public IEnumerator PlayHit(Sprite sprite, float duration = 0.45f)
        {
            StopAnimation();
            if (TryPlayAnimatorAction(hitTriggerName, hitStateName))
            {
                yield return new WaitForSeconds(hitAnimationDuration > 0f ? hitAnimationDuration : duration);
                yield break;
            }

            animationRoutine = StartCoroutine(Pulse(sprite, defeatedColor, 0.88f, duration));
            yield return animationRoutine;
            animationRoutine = null;
        }

        public IEnumerator PlayEscape(float duration = 0.65f)
        {
            StopAnimation();
            if (TryPlayAnimatorAction(escapeTriggerName, escapeStateName))
            {
                yield return new WaitForSeconds(escapeAnimationDuration > 0f ? escapeAnimationDuration : duration);
                yield break;
            }

            yield return new WaitForSeconds(duration);
        }

        private IEnumerator Pulse(Sprite sprite, Color color, float peakScale, float duration)
        {
            ApplySprite(sprite, color);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(t * Mathf.PI);
                transform.localScale = baseScale * Mathf.Lerp(1f, peakScale, wave);
                yield return null;
            }

            transform.localScale = baseScale;
        }

        private void StopAnimation()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        private void ApplySprite(Sprite sprite, Color fallbackColor)
        {
            if (uiImage != null)
            {
                uiImage.sprite = sprite;
                if (applyFallbackColor)
                {
                    uiImage.color = sprite != null ? Color.white : fallbackColor;
                }
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                if (applyFallbackColor)
                {
                    spriteRenderer.color = sprite != null ? Color.white : fallbackColor;
                }
            }
        }

        private bool TryPlayAnimatorAction(string triggerName, string stateName)
        {
            if (!HasAnimatorController())
            {
                return false;
            }

            if (PlayAnimatorState(stateName))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(triggerName) && HasAnimatorParameter(triggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger(triggerName);
                animator.SetTrigger(triggerName);
                return true;
            }

            return false;
        }

        private bool PlayAnimatorState(string stateName)
        {
            if (!HasAnimatorController() || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            if (!HasAnimatorState(stateName))
            {
                return false;
            }

            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
            return true;
        }

        private bool HasAnimatorController()
        {
            return useAnimatorWhenAvailable
                && animator != null
                && animator.isActiveAndEnabled
                && animator.runtimeAnimatorController != null;
        }

        private bool HasAnimatorState(string stateName)
        {
            int stateHash = Animator.StringToHash(stateName);
            int fullPathHash = Animator.StringToHash($"Base Layer.{stateName}");
            return animator.HasState(0, stateHash) || animator.HasState(0, fullPathHash);
        }

        private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == parameterType && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
