using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    [AddComponentMenu("CillyRoom/Beam Effect View")]
    public sealed class CillyRoomProjectileView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [FormerlySerializedAs("image")]
        [SerializeField] private Image beamImage;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private bool useAnimatorWhenAvailable = true;
        [SerializeField] private string playTriggerName = "Play";
        [SerializeField] private string playStateName = "Beam";
        [SerializeField] private float maxThickness = 18f;
        [SerializeField] private float impactPulseScale = 1.18f;
        private Vector3 initialScale = Vector3.one;
        private GameObject linkedLifetimeObject;

        public void Configure(RectTransform rect, Image image)
        {
            rectTransform = rect;
            beamImage = image;
        }

        public void SetThickness(float thickness)
        {
            if (thickness > 0f)
            {
                maxThickness = thickness;
            }
        }

        public void AttachLifetimeObject(GameObject lifetimeObject)
        {
            linkedLifetimeObject = lifetimeObject;
        }

        private void OnDestroy()
        {
            if (linkedLifetimeObject != null)
            {
                Destroy(linkedLifetimeObject);
            }
        }

        private void Awake()
        {
            initialScale = transform.localScale;

            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (beamImage == null)
            {
                beamImage = GetComponent<Image>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        public IEnumerator PlayBeam(Vector3 startWorldPosition, Vector3 endWorldPosition, float duration, float thickness = -1f)
        {
            SetThickness(thickness);

            Vector3 delta = endWorldPosition - startWorldPosition;
            float length = delta.magnitude;
            if (length <= 0.01f)
            {
                yield break;
            }

            PlaceBeam(startWorldPosition, endWorldPosition, delta, length);

            if (TryPlayAnimatorBeam())
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
                yield break;
            }

            if (beamImage == null)
            {
                yield break;
            }

            Color baseColor = beamImage.color;
            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float pulse = Mathf.Sin(t * Mathf.PI);

                rectTransform.localScale = new Vector3(1f, Mathf.Lerp(0.2f, impactPulseScale, pulse), 1f);
                beamImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, baseColor.a, pulse));
                yield return null;
            }

            beamImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        }

        private void PlaceBeam(Vector3 startWorldPosition, Vector3 endWorldPosition, Vector3 delta, float length)
        {
            float thickness = maxThickness;
            if (rectTransform == null && IsLikelyScreenSpacePosition(startWorldPosition, endWorldPosition))
            {
                startWorldPosition = ScreenToWorldPoint(startWorldPosition);
                endWorldPosition = ScreenToWorldPoint(endWorldPosition);
                thickness = ScreenDistanceToWorldDistance(maxThickness);
                delta = endWorldPosition - startWorldPosition;
                length = delta.magnitude;
            }

            transform.position = (startWorldPosition + endWorldPosition) * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(length, thickness);
                return;
            }

            if (spriteRenderer != null)
            {
                var spriteSize = spriteRenderer.size;
                if (spriteRenderer.drawMode != SpriteDrawMode.Simple && spriteSize.x > 0.01f && spriteSize.y > 0.01f)
                {
                    spriteRenderer.size = new Vector2(length, thickness);
                    return;
                }

                var bounds = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.size : Vector3.one;
                float width = Mathf.Max(0.01f, bounds.x);
                float height = Mathf.Max(0.01f, bounds.y);
                transform.localScale = new Vector3(length / width, thickness / height, initialScale.z);
                return;
            }

            transform.localScale = new Vector3(length, thickness, initialScale.z);
        }

        private static bool IsLikelyScreenSpacePosition(Vector3 startPosition, Vector3 endPosition)
        {
            return Mathf.Abs(startPosition.x) > 50f
                || Mathf.Abs(startPosition.y) > 50f
                || Mathf.Abs(endPosition.x) > 50f
                || Mathf.Abs(endPosition.y) > 50f;
        }

        private static Vector3 ScreenToWorldPoint(Vector3 screenPosition)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return screenPosition;
            }

            float distance = Mathf.Abs(camera.transform.position.z);
            var worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
            worldPosition.z = 0f;
            return worldPosition;
        }

        private static float ScreenDistanceToWorldDistance(float screenDistance)
        {
            var camera = Camera.main;
            if (camera == null || screenDistance <= 0f)
            {
                return screenDistance;
            }

            float distance = Mathf.Abs(camera.transform.position.z);
            var start = camera.ScreenToWorldPoint(new Vector3(0f, 0f, distance));
            var end = camera.ScreenToWorldPoint(new Vector3(0f, screenDistance, distance));
            return Mathf.Max(0.01f, Vector3.Distance(start, end));
        }

        private bool TryPlayAnimatorBeam()
        {
            if (!HasAnimatorController())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(playStateName) && HasAnimatorState(playStateName))
            {
                animator.Play(playStateName, 0, 0f);
                animator.Update(0f);
                return true;
            }

            if (!string.IsNullOrWhiteSpace(playTriggerName) && HasAnimatorParameter(playTriggerName, AnimatorControllerParameterType.Trigger))
            {
                animator.ResetTrigger(playTriggerName);
                animator.SetTrigger(playTriggerName);
                return true;
            }

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
