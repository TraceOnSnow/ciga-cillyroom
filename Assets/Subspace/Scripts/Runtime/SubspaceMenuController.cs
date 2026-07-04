using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button startButton;
        [SerializeField] private SubspaceTextConfig textConfig;

        public void Configure(GameObject rootObject, Image backgroundImage, Text title, Text subtitle, Button button)
        {
            root = rootObject;
            background = backgroundImage;
            titleText = title;
            subtitleText = subtitle;
            startButton = button;
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
            UpdateStaticText();
        }

        public void Show(Sprite backgroundSprite, Color fallbackColor, UnityAction onStart)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (background != null)
            {
                background.sprite = backgroundSprite;
                background.color = backgroundSprite != null ? Color.white : fallbackColor;
            }

            if (titleText != null)
            {
                titleText.text = TextConfig.menuTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = TextConfig.menuSubtitle;
            }

            if (startButton != null)
            {
                var buttonText = startButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = TextConfig.menuStartButtonText;
                }

                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(onStart);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void UpdateStaticText()
        {
            if (titleText != null)
            {
                titleText.text = TextConfig.menuTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = TextConfig.menuSubtitle;
            }

            if (startButton != null)
            {
                var buttonText = startButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = TextConfig.menuStartButtonText;
                }
            }
        }

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}