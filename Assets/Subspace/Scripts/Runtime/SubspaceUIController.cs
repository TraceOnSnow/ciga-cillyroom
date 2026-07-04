using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceUIController : MonoBehaviour
    {
        [SerializeField] private GameObject gameRoot;
        [SerializeField] private Text levelText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text targetText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text roundScoreText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text upgradeText;
        [SerializeField] private Image hpFill;
        [SerializeField] private Button attackButton;
        [SerializeField] private SubspaceAudioController audioController;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private Text messageTitleText;
        [SerializeField] private Text messageBodyText;
        [SerializeField] private Button messageButton;
        [SerializeField] private SubspaceTextConfig textConfig;

        public void Configure(
            GameObject root,
            Text level,
            Text score,
            Text target,
            Text turns,
            Text roundScore,
            Text detail,
            Image hp,
            Button attack,
            GameObject message,
            Text messageTitle,
            Text messageBody,
            Button messageContinue)
        {
            gameRoot = root;
            levelText = level;
            scoreText = score;
            targetText = target;
            turnText = turns;
            roundScoreText = roundScore;
            detailText = detail;
            hpFill = hp;
            attackButton = attack;
            messageRoot = message;
            messageTitleText = messageTitle;
            messageBodyText = messageBody;
            messageButton = messageContinue;
        }

        public void SetTextConfig(SubspaceTextConfig config)
        {
            textConfig = config != null ? config : SubspaceTextConfig.RuntimeDefault;
        }

        public void SetUpgradeText(Text text)
        {
            upgradeText = text;
        }

        public void SetAudioController(SubspaceAudioController controller)
        {
            audioController = controller;
            if (audioController != null)
            {
                audioController.RegisterButton(attackButton, true, false);
            }
        }

        public void ShowGame(bool show)
        {
            if (gameRoot != null)
            {
                gameRoot.SetActive(show);
            }
        }

        public void SetAttackCallback(UnityAction callback)
        {
            if (attackButton == null)
            {
                return;
            }

            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(callback);
        }

        public void SetAttackEnabled(bool enabled)
        {
            if (attackButton != null)
            {
                attackButton.interactable = enabled;
            }
        }

        public void Refresh(
            SubspaceLevelDefinition level,
            int totalScore,
            int remainingTurns,
            int lastRoundScore,
            IReadOnlyList<string> scoreLines,
            IReadOnlyList<SubspaceUpgradeDefinition> activeUpgrades = null)
        {
            if (levelText != null)
            {
                levelText.text = level != null ? level.displayName : string.Empty;
            }

            if (scoreText != null)
            {
                scoreText.text = level != null ? TextConfig.FormatScore(totalScore, level.SafeTargetScore) : string.Empty;
            }

            if (targetText != null)
            {
                targetText.text = level != null ? TextConfig.FormatTarget(level.SafeTargetScore) : string.Empty;
            }

            if (turnText != null)
            {
                turnText.text = TextConfig.FormatTurns(remainingTurns);
            }

            if (roundScoreText != null)
            {
                roundScoreText.text = lastRoundScore > 0 ? TextConfig.FormatRoundScore(lastRoundScore) : TextConfig.roundScoreEmptyText;
            }

            if (detailText != null)
            {
                detailText.text = scoreLines == null || scoreLines.Count == 0
                    ? TextConfig.selectionHintText
                    : string.Join("  ", scoreLines);
            }

            if (hpFill != null && level != null)
            {
                hpFill.fillAmount = Mathf.Clamp01(totalScore / (float)level.SafeTargetScore);
            }

            RefreshUpgrades(activeUpgrades);
        }

        public void RefreshUpgrades(IReadOnlyList<SubspaceUpgradeDefinition> activeUpgrades)
        {
            EnsureUpgradeText();
            if (upgradeText == null)
            {
                return;
            }

            if (activeUpgrades == null || activeUpgrades.Count == 0)
            {
                upgradeText.text = "\u5df2\u9009\u5347\u7ea7\n-";
                return;
            }

            var names = new List<string>();
            foreach (var upgrade in activeUpgrades)
            {
                if (upgrade != null)
                {
                    names.Add(upgrade.displayName);
                }
            }

            upgradeText.text = names.Count == 0 ? "\u5df2\u9009\u5347\u7ea7\n-" : $"\u5df2\u9009\u5347\u7ea7\n{string.Join("\n", names)}";
        }

        public void ShowMessage(string title, string body, string buttonLabel, UnityAction onClick)
        {
            ShowGame(false);
            if (messageRoot != null)
            {
                messageRoot.SetActive(true);
            }

            if (messageTitleText != null)
            {
                messageTitleText.text = title;
            }

            if (messageBodyText != null)
            {
                messageBodyText.text = body;
            }

            if (messageButton != null)
            {
                messageButton.GetComponentInChildren<Text>().text = buttonLabel;
                messageButton.onClick.RemoveAllListeners();
                messageButton.onClick.AddListener(onClick);
            }
        }

        public void HideMessage()
        {
            if (messageRoot != null)
            {
                messageRoot.SetActive(false);
            }
        }

        private void EnsureUpgradeText()
        {
            if (upgradeText != null || gameRoot == null)
            {
                return;
            }

            var textObject = new GameObject("SelectedUpgradesText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(gameRoot.transform, false);

            upgradeText = textObject.GetComponent<Text>();
            upgradeText.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);
            upgradeText.fontSize = 16;
            upgradeText.alignment = TextAnchor.UpperLeft;
            upgradeText.color = new Color(0.92f, 0.95f, 1f, 1f);
            upgradeText.raycastTarget = false;
            upgradeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            upgradeText.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = upgradeText.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-26f, 145f);
            rect.sizeDelta = new Vector2(224f, 120f);
        }

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
