using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomRewardController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private Button skipButton;
        [SerializeField] private CillyRoomRewardOptionView optionPrefab;
        [SerializeField] private CillyRoomTextConfig textConfig;

        private readonly List<CillyRoomRewardOptionView> options = new List<CillyRoomRewardOptionView>();

        public void Configure(GameObject rootObject, Text title, Transform cards, Button skip, CillyRoomRewardOptionView prefab)
        {
            root = rootObject;
            titleText = title;
            cardRoot = cards;
            skipButton = skip;
            optionPrefab = prefab;
        }

        public void SetTextConfig(CillyRoomTextConfig config)
        {
            textConfig = config != null ? config : CillyRoomTextConfig.RuntimeDefault;
            if (optionPrefab != null)
            {
                optionPrefab.SetTextConfig(TextConfig);
            }
        }

        public void Show(CillyRoomLevelDefinition level, Action<CillyRoomSymbolDefinition> onChoose, Action onSkip, Func<CillyRoomSymbolDefinition, Sprite> spriteResolver)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = level != null ? TextConfig.FormatRewardTitle(level.displayName) : TextConfig.rewardFallbackTitle;
            }

            if (skipButton != null)
            {
                var buttonText = skipButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = TextConfig.rewardSkipButtonText;
                }

                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(() => onSkip?.Invoke());
            }

            var rewards = level != null ? level.rewardChoices.Where(symbol => symbol != null).ToList() : new List<CillyRoomSymbolDefinition>();
            while (rewards.Count < 3 && rewards.Count > 0)
            {
                rewards.Add(rewards[0]);
            }

            EnsureOptions(3);
            for (int i = 0; i < options.Count; i++)
            {
                var reward = i < rewards.Count ? rewards[i] : null;
                options[i].gameObject.SetActive(reward != null);
                options[i].SetTextConfig(TextConfig);
                options[i].SetReward(reward, reward != null && spriteResolver != null ? spriteResolver(reward) : null, () => onChoose?.Invoke(reward));
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void EnsureOptions(int count)
        {
            while (options.Count < count)
            {
                var option = Instantiate(optionPrefab, cardRoot);
                option.gameObject.SetActive(true);
                option.SetTextConfig(TextConfig);
                options.Add(option);
            }
        }

        private CillyRoomTextConfig TextConfig => textConfig != null ? textConfig : CillyRoomTextConfig.RuntimeDefault;
    }
}
