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
        [SerializeField] private RectTransform upgradeItemRoot;
        [SerializeField] private Image hpFill;
        [SerializeField] private Button attackButton;
        [SerializeField] private SubspaceAudioController audioController;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private Text messageTitleText;
        [SerializeField] private Text messageBodyText;
        [SerializeField] private Button messageButton;
        [SerializeField] private SubspaceTextConfig textConfig;
        [SerializeField] private SubspaceDamageTooltipTarget damageTooltipTarget;

        private readonly List<SubspaceUpgradeItemView> upgradeItems = new List<SubspaceUpgradeItemView>();
        private readonly List<SubspaceScoreSource> lastDamageSources = new List<SubspaceScoreSource>();
        private RectTransform enemyHpRuntimeRoot;
        private Image enemyHpTrackImage;
        private Text enemyHpValueText;
        private Coroutine hpFillRoutine;

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
                scoreText.text = BuildEnemyAbilityText(level);
            }

            if (targetText != null)
            {
                RefreshEnemyHpBar(level, totalScore);
            }

            if (turnText != null)
            {
                turnText.text = TextConfig.FormatTurns(remainingTurns);
            }

            if (roundScoreText != null)
            {
                roundScoreText.text = lastRoundScore != 0 ? $"\u4e0a\u6b21\u4f24\u5bb3\n{lastRoundScore}" : "\u4e0a\u6b21\u4f24\u5bb3\n-";
                EnsureDamageTooltipTarget();
            }

            if (detailText != null)
            {
                detailText.text = scoreLines == null || scoreLines.Count == 0
                    ? TextConfig.selectionHintText
                    : string.Join("  ", scoreLines);
            }

            if (hpFill != null && level != null)
            {
                SetHpFill(1f - Mathf.Clamp01(totalScore / (float)level.SafeTargetScore));
            }

            RefreshUpgrades(activeUpgrades);
        }

        public void SetDamageBreakdown(IReadOnlyList<SubspaceScoreSource> sources)
        {
            lastDamageSources.Clear();
            if (sources != null)
            {
                foreach (var source in sources)
                {
                    lastDamageSources.Add(source);
                }
            }

            lastDamageSources.Sort((a, b) => b.finalScore.CompareTo(a.finalScore));
            EnsureDamageTooltipTarget();
            if (damageTooltipTarget != null)
            {
                damageTooltipTarget.SetTooltip("\u4e0a\u6b21\u4f24\u5bb3\u8d21\u732e", BuildDamageTooltip());
            }

            if (roundScoreText != null && lastDamageSources.Count == 0)
            {
                roundScoreText.text = "\u4e0a\u6b21\u4f24\u5bb3\n-";
            }
        }

        public void RefreshUpgrades(IReadOnlyList<SubspaceUpgradeDefinition> activeUpgrades)
        {
            EnsureUpgradeText();
            EnsureUpgradeTextLayout();
            if (upgradeText == null)
            {
                return;
            }

            if (activeUpgrades == null || activeUpgrades.Count == 0)
            {
                upgradeText.text = "\u6682\u65e0";
                RefreshUpgradeItems(null);
                return;
            }

            upgradeText.text = string.Empty;
            RefreshUpgradeItems(activeUpgrades);
        }

        public Image EnsurePlayerPortraitImage()
        {
            if (gameRoot == null)
            {
                return null;
            }

            var panel = FindDeepChild(gameRoot.transform, "Player Animation Panel");
            if (panel == null)
            {
                return null;
            }

            var existing = FindDeepChild(panel, "Player Portrait");
            if (existing != null && existing.TryGetComponent<Image>(out var existingImage))
            {
                existingImage.preserveAspect = true;
                existingImage.raycastTarget = false;
                if (existingImage.sprite == null)
                {
                    existingImage.color = Color.clear;
                }

                return existingImage;
            }

            var portraitObject = new GameObject("Player Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            portraitObject.transform.SetParent(panel, false);
            var portrait = portraitObject.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.color = Color.clear;
            SetLowerLeft(portrait.rectTransform, 26f, 24f, 158f, 164f);
            return portrait;
        }

        public void HidePlayerPortraitImage()
        {
            if (gameRoot == null)
            {
                return;
            }

            var panel = FindDeepChild(gameRoot.transform, "Player Animation Panel");
            if (panel == null)
            {
                return;
            }

            var existing = FindDeepChild(panel, "Player Portrait");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
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

        private void EnsureUpgradeText()
        {
            if (upgradeText != null || gameRoot == null)
            {
                return;
            }

            var panelObject = new GameObject("Upgrade Item Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(gameRoot.transform, false);
            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0.12f, 0.13f, 0.15f, 0.98f);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = Vector2.zero;
            panelRect.anchoredPosition = new Vector2(20f, 286f);
            panelRect.sizeDelta = new Vector2(210f, 234f);

            var textObject = new GameObject("SelectedUpgradesText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);

            upgradeText = textObject.GetComponent<Text>();
            upgradeText.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);
            upgradeText.fontSize = 15;
            upgradeText.alignment = TextAnchor.UpperLeft;
            upgradeText.color = new Color(0.92f, 0.95f, 1f, 1f);
            upgradeText.raycastTarget = false;
            upgradeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            upgradeText.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = upgradeText.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18f, 18f);
            rect.offsetMax = new Vector2(-18f, -18f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void EnsureDamageTooltipTarget()
        {
            if (roundScoreText == null)
            {
                return;
            }

            roundScoreText.raycastTarget = true;
            if (damageTooltipTarget == null || damageTooltipTarget.transform != roundScoreText.transform)
            {
                damageTooltipTarget = roundScoreText.GetComponent<SubspaceDamageTooltipTarget>();
            }

            if (damageTooltipTarget == null)
            {
                damageTooltipTarget = roundScoreText.gameObject.AddComponent<SubspaceDamageTooltipTarget>();
            }

            damageTooltipTarget.SetTooltip("\u4e0a\u6b21\u4f24\u5bb3\u8d21\u732e", BuildDamageTooltip());
        }

        private void SetHpFill(float targetFill)
        {
            targetFill = Mathf.Clamp01(targetFill);
            EnsureEnemyHpBarLayout();
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            if (!gameObject.activeInHierarchy)
            {
                ApplyHpFillVisual(targetFill);
                return;
            }

            if (hpFillRoutine != null)
            {
                StopCoroutine(hpFillRoutine);
            }

            hpFillRoutine = StartCoroutine(AnimateHpFill(targetFill));
        }

        private void RefreshEnemyHpBar(SubspaceLevelDefinition level, int totalScore)
        {
            EnsureEnemyHpBarLayout();
            if (targetText != null)
            {
                targetText.text = "\u654c\u4eba HP";
            }

            if (enemyHpValueText != null)
            {
                enemyHpValueText.text = level != null
                    ? $"{Mathf.Max(0, level.SafeTargetScore - totalScore)}/{level.SafeTargetScore}"
                    : string.Empty;
            }
        }

        private void EnsureEnemyHpBarLayout()
        {
            if (gameRoot == null || targetText == null || hpFill == null)
            {
                return;
            }

            if (enemyHpRuntimeRoot == null)
            {
                enemyHpRuntimeRoot = CreateEnemyHpRuntimeRoot();
            }

            targetText.transform.SetParent(enemyHpRuntimeRoot, false);
            ConfigureHpLabel(targetText);

            if (enemyHpTrackImage == null)
            {
                enemyHpTrackImage = CreateHpTrack(enemyHpRuntimeRoot);
            }

            hpFill.transform.SetParent(enemyHpTrackImage.transform, false);
            ConfigureHpFill(hpFill);

            if (enemyHpValueText == null)
            {
                enemyHpValueText = CreateHpValueText(enemyHpRuntimeRoot, targetText);
            }
        }

        private RectTransform CreateEnemyHpRuntimeRoot()
        {
            var rootObject = new GameObject("Enemy HP Runtime Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(gameRoot.transform, false);
            rootObject.transform.SetAsLastSibling();

            var rect = rootObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(671.8f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-31.09998f, rect.offsetMax.y);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -29.1f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 40f);

            var image = rootObject.GetComponent<Image>();
            image.color = new Color(0.018f, 0.03f, 0.048f, 0.78f);
            image.raycastTarget = false;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.34f, 0.42f, 0.62f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = true;

            return rect;
        }

        private Image CreateHpTrack(RectTransform parent)
        {
            var trackObject = new GameObject("Enemy HP Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            trackObject.transform.SetParent(parent, false);

            var rect = trackObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(32f, -1f);
            rect.sizeDelta = new Vector2(-250f, 16f);

            var image = trackObject.GetComponent<Image>();
            image.color = new Color(0.11f, 0.035f, 0.035f, 0.94f);
            image.raycastTarget = false;

            var outline = trackObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.76f, 0.18f, 0.14f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            return image;
        }

        private void ConfigureHpLabel(Text label)
        {
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.88f, 0.93f, 0.98f, 1f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(14f, 0f);
            rect.sizeDelta = new Vector2(112f, 0f);
        }

        private void ConfigureHpFill(Image fill)
        {
            fill.color = new Color(0.95f, 0.08f, 0.05f, 1f);
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            var rect = fill.rectTransform;
            float currentFill = rect.anchorMax.x > 0f && rect.anchorMax.x <= 1f ? rect.anchorMax.x : Mathf.Clamp01(fill.fillAmount);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(currentFill, 1f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
            rect.pivot = new Vector2(0f, 0.5f);
        }

        private Text CreateHpValueText(RectTransform parent, Text template)
        {
            var valueObject = new GameObject("Enemy HP Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            valueObject.transform.SetParent(parent, false);

            var value = valueObject.GetComponent<Text>();
            value.font = template != null ? template.font : Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 20);
            value.fontSize = 22;
            value.fontStyle = FontStyle.Bold;
            value.alignment = TextAnchor.MiddleRight;
            value.color = new Color(0.9f, 0.94f, 1f, 1f);
            value.raycastTarget = false;
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = value.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-18f, 0f);
            rect.sizeDelta = new Vector2(118f, 0f);
            return value;
        }

        private System.Collections.IEnumerator AnimateHpFill(float targetFill)
        {
            float start = GetCurrentHpFillVisual();
            const float duration = 0.28f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                ApplyHpFillVisual(Mathf.Lerp(start, targetFill, t));
                yield return null;
            }

            ApplyHpFillVisual(targetFill);
            hpFillRoutine = null;
        }

        private void ApplyHpFillVisual(float fill)
        {
            fill = Mathf.Clamp01(fill);
            if (hpFill == null)
            {
                return;
            }

            hpFill.fillAmount = fill;
            var rect = hpFill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(fill, 1f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
        }

        private float GetCurrentHpFillVisual()
        {
            if (hpFill == null)
            {
                return 0f;
            }

            return Mathf.Clamp01(hpFill.rectTransform.anchorMax.x);
        }

        private string BuildDamageTooltip()
        {
            if (lastDamageSources.Count == 0)
            {
                return "\u5c1a\u672a\u9020\u6210\u4f24\u5bb3\u3002";
            }

            var lines = new List<string>();
            for (int i = 0; i < lastDamageSources.Count; i++)
            {
                var source = lastDamageSources[i];
                string name = string.IsNullOrWhiteSpace(source.displayName) ? "\u672a\u77e5\u6765\u6e90" : source.displayName;
                string score = source.finalScore >= 0 ? $"+{source.finalScore}" : source.finalScore.ToString();
                string position = source.position.x >= 0 && source.position.y >= 0 ? $" ({source.position.x + 1},{source.position.y + 1})" : string.Empty;
                string detail = string.IsNullOrWhiteSpace(source.detail) ? string.Empty : $"\n   {source.detail}";
                lines.Add($"{i + 1}. {name}{position}: {score}{detail}");
            }

            return string.Join("\n", lines);
        }

        private static string BuildEnemyAbilityText(SubspaceLevelDefinition level)
        {
            if (level == null)
            {
                return "\u654c\u4eba\u80fd\u529b\n-";
            }

            string ability;
            switch (level.monsterPressureType)
            {
                case SubspaceMonsterPressureType.ErodeStrongestTile:
                    ability = $"\u541e\u566c\u6700\u5f3a\u5730\u5757 x{Mathf.Max(1, level.monsterPressureAmount)}";
                    break;
                case SubspaceMonsterPressureType.JamScanner:
                    ability = $"\u5e72\u6270\u626b\u63cf x{Mathf.Max(1, level.monsterPressureAmount)}";
                    break;
                case SubspaceMonsterPressureType.CollapseAnchors:
                    ability = $"\u524a\u5f31\u951a\u7f51 x{Mathf.Max(1, level.monsterPressureAmount)}";
                    break;
                case SubspaceMonsterPressureType.SpreadPollution:
                    ability = $"\u6269\u6563\u6c61\u67d3 x{Mathf.Max(1, level.monsterPressureAmount)}";
                    break;
                default:
                    ability = "\u65e0";
                    break;
            }

            return $"\u654c\u4eba\u80fd\u529b\n{level.monsterDisplayName}\n{ability}";
        }

        private void EnsureUpgradeTextLayout()
        {
            if (gameRoot == null || upgradeText == null)
            {
                return;
            }

            var panel = FindDeepChild(gameRoot.transform, "Buff Item Panel");
            if (panel == null)
            {
                return;
            }

            if (upgradeText.transform.parent != panel)
            {
                upgradeText.transform.SetParent(panel, false);
            }

            upgradeText.fontSize = 15;
            upgradeText.alignment = TextAnchor.UpperLeft;
            upgradeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            upgradeText.verticalOverflow = VerticalWrapMode.Truncate;
            SetLowerLeft(upgradeText.rectTransform, 18f, 20f, 174f, 24f);
            EnsureUpgradeItemRoot(panel);
        }

        private void EnsureUpgradeItemRoot(Transform panel)
        {
            if (upgradeItemRoot != null)
            {
                return;
            }

            var existing = FindDeepChild(panel, "Upgrade Item Grid");
            if (existing != null)
            {
                upgradeItemRoot = existing as RectTransform;
            }

            if (upgradeItemRoot == null)
            {
                var gridObject = new GameObject("Upgrade Item Grid", typeof(RectTransform), typeof(GridLayoutGroup));
                gridObject.transform.SetParent(panel, false);
                upgradeItemRoot = gridObject.GetComponent<RectTransform>();
                SetLowerLeft(upgradeItemRoot, 18f, 36f, 174f, 136f);
            }

            var grid = upgradeItemRoot.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(38f, 38f);
            grid.spacing = new Vector2(7f, 7f);
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
        }

        private void RefreshUpgradeItems(IReadOnlyList<SubspaceUpgradeDefinition> activeUpgrades)
        {
            if (upgradeItemRoot == null && gameRoot != null)
            {
                var panel = FindDeepChild(gameRoot.transform, "Buff Item Panel");
                if (panel != null)
                {
                    EnsureUpgradeItemRoot(panel);
                }
            }

            if (upgradeItemRoot == null)
            {
                return;
            }

            int count = activeUpgrades != null ? activeUpgrades.Count : 0;
            while (upgradeItems.Count < count)
            {
                upgradeItems.Add(CreateUpgradeItem());
            }

            for (int i = 0; i < upgradeItems.Count; i++)
            {
                bool show = i < count && activeUpgrades[i] != null;
                upgradeItems[i].gameObject.SetActive(show);
                if (show)
                {
                    upgradeItems[i].SetUpgrade(activeUpgrades[i]);
                }
            }
        }

        private SubspaceUpgradeItemView CreateUpgradeItem()
        {
            var itemObject = new GameObject("Upgrade Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(SubspaceUpgradeItemView));
            itemObject.transform.SetParent(upgradeItemRoot, false);

            var image = itemObject.GetComponent<Image>();
            image.color = new Color(0.98f, 0.84f, 0.22f, 1f);
            image.raycastTarget = true;

            var outline = itemObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);

            var labelObject = new GameObject("Item Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(itemObject.transform, false);
            var label = labelObject.GetComponent<Text>();
            label.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            Stretch(label.rectTransform);

            var view = itemObject.GetComponent<SubspaceUpgradeItemView>();
            view.Configure(image, label);
            return view;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                var match = FindDeepChild(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void SetLowerLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private SubspaceTextConfig TextConfig => textConfig != null ? textConfig : SubspaceTextConfig.RuntimeDefault;
    }
}
