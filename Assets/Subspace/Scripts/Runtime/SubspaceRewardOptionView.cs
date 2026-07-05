using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceRewardOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text scoreText;
        [SerializeField] private SubspaceTextConfig textConfig;

        public void Configure(Button buttonComponent, Image iconImage, Text nameLabel, Text scoreLabel)
        {
            button = buttonComponent;
            icon = iconImage;
            nameText = nameLabel;
            scoreText = scoreLabel;
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
        }

        private void Awake()
        {
            EnsureIconImage();
        }

       public void SetReward(SubspaceSymbolDefinition reward, Sprite sprite, UnityAction onClick)
       {
           if (button != null)
           {
               button.onClick.RemoveAllListeners();
               button.onClick.AddListener(onClick);
           }

           EnsureIconImage();
           if (icon != null)
           {
               icon.sprite = sprite;
               icon.color = sprite != null ? Color.white : Color.clear;
               icon.preserveAspect = true;
               icon.raycastTarget = false;
           }

           if (nameText != null)
           {
               nameText.text = reward != null ? reward.SafeDisplayName : string.Empty;
           }

           if (scoreText != null)
           {
               scoreText.text = reward != null ? TextConfig.FormatRewardScore(reward.SafeDisplayName) : string.Empty;
           }
       }

       public void SetUpgrade(SubspaceUpgradeDefinition upgrade, UnityAction onClick)
       {
           if (button != null)
           {
               button.onClick.RemoveAllListeners();
               button.onClick.AddListener(onClick);
           }

           EnsureIconImage();
           if (icon != null)
           {
               icon.sprite = null;
               icon.color = Color.clear;
               icon.preserveAspect = true;
               icon.raycastTarget = false;
           }

           if (nameText != null)
           {
               nameText.text = upgrade != null ? upgrade.displayName : string.Empty;
           }

           if (scoreText != null)
           {
               scoreText.text = upgrade != null ? upgrade.description : string.Empty;
           }
       }

        private void EnsureIconImage()
        {
            if (icon != null && icon.gameObject != gameObject)
            {
                return;
            }

            var iconTransform = transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent<Image>(out var existingIcon))
            {
                icon = existingIcon;
                return;
            }

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            icon = iconObject.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.color = Color.clear;

            var rect = icon.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 22f);
            rect.sizeDelta = new Vector2(84f, 84f);
            iconObject.transform.SetAsFirstSibling();
        }

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
