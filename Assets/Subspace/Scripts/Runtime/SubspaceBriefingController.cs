using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceBriefingController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button continueButton;
        [SerializeField] private SubspaceTextConfig textConfig;

        public void Configure(GameObject rootObject, Image backgroundImage, Text title, Text body, Button button)
        {
            root = rootObject;
            background = backgroundImage;
            titleText = title;
            bodyText = body;
            continueButton = button;
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
            UpdateStaticText();
        }

        public void Show(SubspaceLevelDefinition level, Sprite backgroundSprite, Color fallbackColor, UnityAction onContinue)
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
                titleText.text = level != null ? level.displayName : TextConfig.briefingFallbackTitle;
            }

            if (bodyText != null)
            {
                bodyText.text = level != null ? level.briefingText : string.Empty;
            }

            if (continueButton != null)
            {
                var buttonText = continueButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = TextConfig.briefingContinueButtonText;
                }

                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(onContinue);
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
            if (continueButton == null)
            {
                return;
            }

            var buttonText = continueButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = TextConfig.briefingContinueButtonText;
            }
        }

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
