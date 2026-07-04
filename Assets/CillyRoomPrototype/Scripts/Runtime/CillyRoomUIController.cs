using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomUIController : MonoBehaviour
    {
        [SerializeField] private GameObject gameRoot;
        [SerializeField] private Text levelText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text targetText;
        [SerializeField] private Text turnText;
        [SerializeField] private Text roundScoreText;
        [SerializeField] private Text detailText;
        [SerializeField] private Image hpFill;
        [SerializeField] private Button attackButton;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private Text messageTitleText;
        [SerializeField] private Text messageBodyText;
        [SerializeField] private Button messageButton;
        [SerializeField] private CillyRoomTextConfig textConfig;

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

        public void SetTextConfig(CillyRoomTextConfig config)
        {
            textConfig = config != null ? config : CillyRoomTextConfig.RuntimeDefault;
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

        public void Refresh(CillyRoomLevelDefinition level, int totalScore, int remainingTurns, int lastRoundScore, IReadOnlyList<string> scoreLines)
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

        private CillyRoomTextConfig TextConfig => textConfig != null ? textConfig : CillyRoomTextConfig.RuntimeDefault;
    }
}
