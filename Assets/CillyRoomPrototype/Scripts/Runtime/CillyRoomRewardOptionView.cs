using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomRewardOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Text nameText;
        [SerializeField] private Text scoreText;
        [SerializeField] private CillyRoomTextConfig textConfig;

        public void Configure(Button buttonComponent, Image iconImage, Text nameLabel, Text scoreLabel)
        {
            button = buttonComponent;
            icon = iconImage;
            nameText = nameLabel;
            scoreText = scoreLabel;
        }

        public void SetTextConfig(CillyRoomTextConfig config)
        {
            textConfig = config != null ? config : CillyRoomTextConfig.RuntimeDefault;
        }

        public void SetReward(CillyRoomSymbolDefinition reward, Sprite sprite, UnityAction onClick)
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

        private CillyRoomTextConfig TextConfig => textConfig != null ? textConfig : CillyRoomTextConfig.RuntimeDefault;
    }
}
