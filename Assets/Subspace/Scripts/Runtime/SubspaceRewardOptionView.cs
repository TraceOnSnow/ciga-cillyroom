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

       public void SetReward(SubspaceSymbolDefinition reward, Sprite sprite, UnityAction onClick)
       {
           if (button != null)
           {
               button.onClick.RemoveAllListeners();
               button.onClick.AddListener(onClick);
           }

           if (icon != null)
           {
               icon.sprite = sprite;
               icon.color = sprite != null ? Color.white : reward != null ? reward.SafeTint : Color.clear;
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

           if (icon != null)
           {
               icon.sprite = null;
               icon.color = upgrade != null ? new Color(0.98f, 0.84f, 0.22f, 1f) : Color.clear;
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

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
