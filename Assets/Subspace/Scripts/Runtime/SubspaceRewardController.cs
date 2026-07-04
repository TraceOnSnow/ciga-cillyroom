using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceRewardController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private Button skipButton;
        [SerializeField] private SubspaceRewardOptionView optionPrefab;
        [SerializeField] private SubspaceTextConfig textConfig;

        private readonly List<SubspaceRewardOptionView> options = new List<SubspaceRewardOptionView>();

        public void Configure(GameObject rootObject, Text title, Transform cards, Button skip, SubspaceRewardOptionView prefab)
        {
            root = rootObject;
            titleText = title;
            cardRoot = cards;
            skipButton = skip;
            optionPrefab = prefab;
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
            if (optionPrefab != null)
            {
                optionPrefab.SetTextConfig(TextConfig);
            }
        }

        public void Show(SubspaceLevelDefinition level, Action<SubspaceSymbolDefinition> onChoose, Action onSkip, Func<SubspaceSymbolDefinition, Sprite> spriteResolver)
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

            var rewards = level != null ? level.rewardChoices.Where(symbol => symbol != null).ToList() : new List<SubspaceSymbolDefinition>();
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

       public void ShowWithUpgrades(
           string titleText,
           IReadOnlyList<SubspaceSymbolDefinition> symbolRewards,
           IReadOnlyList<SubspaceUpgradeDefinition> upgradeRewards,
           Action<SubspaceSymbolDefinition> onChooseSymbol,
           Action<SubspaceUpgradeDefinition> onChooseUpgrade,
           Action onSkip,
           Func<SubspaceSymbolDefinition, Sprite> spriteResolver)
       {
           if (root != null)
           {
               root.SetActive(true);
           }

           if (this.titleText != null)
           {
               this.titleText.text = titleText;
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

           var items = new List<object>();
           if (symbolRewards != null)
           {
               foreach (var s in symbolRewards)
               {
                   if (s != null) items.Add(s);
               }
           }

           if (upgradeRewards != null)
           {
               foreach (var u in upgradeRewards)
               {
                   if (u != null) items.Add(u);
               }
           }

           EnsureOptions(3);
           for (int i = 0; i < options.Count; i++)
           {
               options[i].SetTextConfig(TextConfig);
               if (i < items.Count)
               {
                   options[i].gameObject.SetActive(true);
                   if (items[i] is SubspaceSymbolDefinition symbol)
                   {
                       options[i].SetReward(symbol, spriteResolver != null ? spriteResolver(symbol) : null, () => onChooseSymbol?.Invoke(symbol));
                   }
                   else if (items[i] is SubspaceUpgradeDefinition upgrade)
                   {
                       options[i].SetUpgrade(upgrade, () => onChooseUpgrade?.Invoke(upgrade));
                   }
               }
               else
               {
                   options[i].gameObject.SetActive(false);
               }
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

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
