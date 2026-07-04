using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomGameController : MonoBehaviour
    {
        private const string ResourcesConfigPath = "CillyRoomPrototype/CillyRoomGameConfig";

        [SerializeField] private CillyRoomGameConfig config;
        [SerializeField] private CillyRoomArtRig artRig;
        [SerializeField] private bool loadConfigFromResources = true;

        private readonly List<CillyRoomSymbolDefinition> runtimeSymbolPool = new List<CillyRoomSymbolDefinition>();
        private readonly List<Image> cellImages = new List<Image>();
        private readonly List<Text> cellTexts = new List<Text>();

        private CillyRoomSymbolDefinition[,] board;
        private CillyRoomLevelDefinition currentLevel;
        private CillyRoomArtSet artSet;
        private System.Random random;

        private int levelIndex;
        private int levelScore;
        private int remainingTurns;
        private int columns;
        private int rows;
        private int selectionWidth;
        private int selectionHeight;
        private Vector2Int selectionOrigin;
        private bool isDraggingSelection;
        private bool inputLocked;

        private Font defaultFont;
        private Sprite solidSprite;

        private RectTransform canvasRoot;
        private RectTransform gameRoot;
        private RectTransform briefingRoot;
        private RectTransform rewardRoot;
        private RectTransform messageRoot;
        private RectTransform boardRect;
        private RectTransform boardGridRect;
        private RectTransform selectorRect;
        private GridLayoutGroup boardGrid;

        private Text briefingTitleText;
        private Text briefingBodyText;
        private Image briefingBackgroundImage;
        private Text levelTitleText;
        private Text scoreText;
        private Text targetText;
        private Text turnText;
        private Text roundScoreText;
        private Text selectedDetailText;
        private Text attackEffectText;
        private Text rewardTitleText;
        private Text messageTitleText;
        private Text messageBodyText;
        private Button attackButton;
        private Button messageButton;

        private Image hpFillImage;
        private Image topPlayerImage;
        private Image playerAnimationImage;
        private Image enemyImage;
        private Image attackEffectImage;
        private RectTransform topPlayerRect;
        private RectTransform playerAnimationRect;
        private RectTransform enemyRect;
        private RectTransform attackEffectRect;

        private static void AutoCreateController()
        {
            // Kept only for old scene compatibility. New prototypes are built from scene GameObjects
            // by CillyRoomComponentSceneBuilder and should not auto-create this monolithic controller.
            return;

#pragma warning disable CS0162
            if (FindObjectOfType<CillyRoomGameController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("CillyRoom Runtime Bootstrap");
            controllerObject.AddComponent<CillyRoomGameController>();
#pragma warning restore CS0162
        }

        public void SetConfig(CillyRoomGameConfig newConfig)
        {
            config = newConfig;
        }

        public void SetArtRig(CillyRoomArtRig newArtRig)
        {
            artRig = newArtRig;
        }

        private void Awake()
        {
            LoadConfig();
            BuildInterface();
            StartCampaign();
        }

        private void Update()
        {
            if (currentLevel == null || gameRoot == null || !gameRoot.gameObject.activeSelf || inputLocked)
            {
                return;
            }

            HandleKeyboardSelection();
            HandleMouseSelection();
            RefreshSelectorVisual();
        }

        private void LoadConfig()
        {
            if (artRig == null)
            {
                artRig = FindObjectOfType<CillyRoomArtRig>(true);
            }

            if (config == null && artRig != null && artRig.gameConfig != null)
            {
                config = artRig.gameConfig;
            }

            if (config == null && loadConfigFromResources)
            {
                config = Resources.Load<CillyRoomGameConfig>(ResourcesConfigPath);
            }

            if (config == null)
            {
                config = CreateFallbackConfig();
            }

            artSet = config.artSet != null ? config.artSet : CreateFallbackArtSet();
            if (artRig != null && artRig.artSet == null)
            {
                artRig.artSet = artSet;
            }

            random = config.useFixedSeed ? new System.Random(config.randomSeed) : new System.Random();
        }

        private void StartCampaign()
        {
            runtimeSymbolPool.Clear();
            foreach (var symbol in config.startingSymbols)
            {
                if (symbol != null)
                {
                    runtimeSymbolPool.Add(symbol);
                }
            }

            levelIndex = 0;
            ShowBriefingForLevel(levelIndex);
        }

        private void ShowBriefingForLevel(int index)
        {
            currentLevel = GetLevel(index);
            if (currentLevel == null)
            {
                ShowMessage("原型通关", "两个示例关卡已经完成。你可以重新开始，或继续扩展关卡配置。", "重新开始", StartCampaign);
                return;
            }

            HideAllScreens();
            briefingRoot.gameObject.SetActive(true);
            briefingTitleText.text = currentLevel.displayName;
            briefingBodyText.text = currentLevel.briefingText;

            SetImageSprite(briefingBackgroundImage, GetBriefingBackground(), artSet.backgroundColor);
        }

        private void BeginCurrentLevel()
        {
            currentLevel = GetLevel(levelIndex);
            if (currentLevel == null)
            {
                StartCampaign();
                return;
            }

            levelScore = 0;
            remainingTurns = currentLevel.SafeTurns;
            columns = currentLevel.SafeColumns;
            rows = currentLevel.SafeRows;
            selectionWidth = currentLevel.SafeSelectionWidth;
            selectionHeight = currentLevel.SafeSelectionHeight;
            selectionOrigin = new Vector2Int(
                Mathf.Max(0, (columns - selectionWidth) / 2),
                Mathf.Max(0, (rows - selectionHeight) / 2));

            board = new CillyRoomSymbolDefinition[columns, rows];
            FillBoard(true);
            BuildBoardCells();
            HideAllScreens();
            gameRoot.gameObject.SetActive(true);
            inputLocked = false;
            attackButton.interactable = true;
            attackEffectText.gameObject.SetActive(false);
            RefreshAllGameVisuals(0, null);
        }

        private void FillBoard(bool includeSelection)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (!includeSelection && IsSelected(x, y))
                    {
                        continue;
                    }

                    board[x, y] = GetRandomSymbol();
                }
            }
        }

        private CillyRoomSymbolDefinition GetRandomSymbol()
        {
            if (runtimeSymbolPool.Count == 0)
            {
                runtimeSymbolPool.Add(CreateRuntimeSymbol("number_1", "1", 1, new Color(0.88f, 0.88f, 0.88f, 1f)));
            }

            return runtimeSymbolPool[random.Next(0, runtimeSymbolPool.Count)];
        }

        private void Attack()
        {
            if (inputLocked || currentLevel == null)
            {
                return;
            }

            StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            inputLocked = true;
            attackButton.interactable = false;

            var selection = new RectInt(selectionOrigin.x, selectionOrigin.y, selectionWidth, selectionHeight);
            var scoreResult = CillyRoomScoreResolver.Calculate(board, selection);
            levelScore += scoreResult.total;
            remainingTurns = Mathf.Max(0, remainingTurns - 1);

            RefreshAllGameVisuals(scoreResult.total, scoreResult.lines);
            yield return PlayAttackAnimation(scoreResult.total);

            if (levelScore >= currentLevel.SafeTargetScore)
            {
                yield return PlayVictoryAnimation();
                ShowRewardScreen();
                yield break;
            }

            if (remainingTurns <= 0)
            {
                ShowMessage("任务失败", $"还差 {currentLevel.SafeTargetScore - levelScore} 分。重新部署这一关，再试一次。", "重试本关", BeginCurrentLevel);
                yield break;
            }

            FillBoard(false);
            RefreshAllGameVisuals(0, null);
            inputLocked = false;
            attackButton.interactable = true;
        }

        private IEnumerator PlayAttackAnimation(int roundScore)
        {
            float duration = 0.48f;
            float elapsed = 0f;
            Vector3 playerStart = playerAnimationRect.localScale;
            Vector3 topPlayerStart = topPlayerRect.localScale;
            Vector3 enemyStart = enemyRect.localScale;

            SetCharacterImage(topPlayerImage, GetPlayerAttackSprite(), artSet.playerAttackColor);
            SetCharacterImage(playerAnimationImage, GetPlayerAttackSprite(), artSet.playerAttackColor);
            SetImageSprite(attackEffectImage, GetAttackEffectSprite(), new Color(1f, 0.9f, 0.2f, 0.18f));
            attackEffectText.text = $"+{roundScore}";
            attackEffectImage.gameObject.SetActive(true);
            attackEffectText.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI);
                playerAnimationRect.localScale = playerStart * (1f + pulse * 0.16f);
                topPlayerRect.localScale = topPlayerStart * (1f + pulse * 0.1f);
                enemyRect.localScale = enemyStart * (1f - pulse * 0.08f);
                attackEffectRect.anchoredPosition = new Vector2(0f, 8f + pulse * 42f);
                yield return null;
            }

            playerAnimationRect.localScale = playerStart;
            topPlayerRect.localScale = topPlayerStart;
            enemyRect.localScale = enemyStart;
            attackEffectImage.gameObject.SetActive(false);
            attackEffectText.gameObject.SetActive(false);
            SetCharacterImage(topPlayerImage, GetPlayerIdleSprite(), artSet.playerColor);
            SetCharacterImage(playerAnimationImage, GetPlayerIdleSprite(), artSet.playerColor);
        }

        private IEnumerator PlayVictoryAnimation()
        {
            attackEffectText.text = "胜利";
            attackEffectText.gameObject.SetActive(true);
            SetCharacterImage(enemyImage, GetEnemyDefeatedSprite(), artSet.defeatedEnemyColor);

            float duration = 0.72f;
            float elapsed = 0f;
            Vector3 startScale = enemyRect.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                enemyRect.localScale = startScale * Mathf.Lerp(1f, 0.72f, t);
                enemyImage.color = Color.Lerp(artSet.enemyColor, artSet.defeatedEnemyColor, t);
                yield return null;
            }

            enemyRect.localScale = startScale;
        }

        private void ShowRewardScreen()
        {
            HideAllScreens();
            rewardRoot.gameObject.SetActive(true);
            rewardTitleText.text = $"{currentLevel.displayName} 胜利奖励";
            BuildRewardCards();
        }

        private void ChooseReward(CillyRoomSymbolDefinition reward)
        {
            if (reward != null)
            {
                runtimeSymbolPool.Add(reward);
            }

            GoToNextLevel();
        }

        private void GoToNextLevel()
        {
            levelIndex++;
            ShowBriefingForLevel(levelIndex);
        }

        private void BuildRewardCards()
        {
            var cardParent = rewardRoot.Find("RewardCards");
            if (cardParent == null)
            {
                return;
            }

            for (int i = cardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardParent.GetChild(i).gameObject);
            }

            var rewards = currentLevel.rewardChoices.Where(symbol => symbol != null).ToList();
            if (rewards.Count == 0)
            {
                rewards.Add(CreateRuntimeSymbol("number_7", "7", 7, new Color(1f, 0.72f, 0.18f, 1f)));
            }

            while (rewards.Count < 3)
            {
                rewards.Add(rewards[0]);
            }

            for (int i = 0; i < 3; i++)
            {
                var reward = rewards[i];
                var card = CreateButton(cardParent, $"Reward_{i + 1}", reward.SafeDisplayName, reward.SafeTint, () => ChooseReward(reward), GetRewardCardSprite());
                SetLowerLeft(card.GetComponent<RectTransform>(), 18f + i * 202f, 0f, 176f, 210f);

                var label = CreateText(card.transform, "Score", $"+{reward.SafeBaseScore} 分符号", 20, Color.white, TextAnchor.MiddleCenter);
                SetLowerLeft(label.rectTransform, 16f, 26f, 144f, 32f);
            }
        }

        private void RefreshAllGameVisuals(int lastRoundScore, List<string> scoreLines)
        {
            levelTitleText.text = currentLevel.displayName;
            scoreText.text = $"我方分数\n{levelScore} / {currentLevel.SafeTargetScore}";
            targetText.text = $"血量条（总共要达到的分数） {currentLevel.SafeTargetScore}";
            turnText.text = $"剩余回合数\n{remainingTurns}";
            roundScoreText.text = lastRoundScore > 0 ? $"本回合\n+{lastRoundScore}" : "本回合\n-";
            selectedDetailText.text = scoreLines == null || scoreLines.Count == 0
                ? "框选数字后点击攻击结算"
                : string.Join("  ", scoreLines);

            hpFillImage.fillAmount = Mathf.Clamp01(levelScore / (float)currentLevel.SafeTargetScore);
            SetCharacterImage(enemyImage, GetEnemyIdleSprite(), artSet.enemyColor);
            SetCharacterImage(topPlayerImage, GetPlayerIdleSprite(), artSet.playerColor);
            SetCharacterImage(playerAnimationImage, GetPlayerIdleSprite(), artSet.playerColor);

            RefreshBoardVisuals();
            RefreshSelectorVisual();
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            BuildCanvas();
            BuildBriefingScreen();
            BuildGameScreen();
            BuildRewardScreen();
            BuildMessageScreen();
            HideAllScreens();
        }

        private void BuildCanvas()
        {
            var existing = GameObject.Find("CillyRoomCanvas");
            if (existing != null)
            {
                Destroy(existing);
            }

            var canvasObject = new GameObject("CillyRoomCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRoot = CreateRect(canvasObject.transform, "Root");
            Stretch(canvasRoot);
        }

        private void BuildBriefingScreen()
        {
            briefingRoot = CreateRect(canvasRoot, "BriefingScreen");
            Stretch(briefingRoot);

            briefingBackgroundImage = CreateImage(briefingRoot, "Background", artSet.backgroundColor);
            Stretch(briefingBackgroundImage.rectTransform);

            var shade = CreateImage(briefingRoot, "Shade", new Color(0f, 0f, 0f, 0.38f));
            Stretch(shade.rectTransform);

            briefingTitleText = CreateText(briefingRoot, "Title", "任务简报", 48, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(briefingTitleText.rectTransform, 96f, 476f, 820f, 74f);

            briefingBodyText = CreateText(briefingRoot, "Body", string.Empty, 26, new Color(0.92f, 0.95f, 1f, 1f), TextAnchor.UpperLeft);
            SetLowerLeft(briefingBodyText.rectTransform, 100f, 250f, 860f, 190f);

            var button = CreateButton(briefingRoot, "StartButton", "进入战斗", artSet.accentColor, BeginCurrentLevel);
            SetLowerLeft(button.GetComponent<RectTransform>(), 100f, 142f, 240f, 66f);
        }

        private void BuildGameScreen()
        {
            gameRoot = CreateRect(canvasRoot, "GameScreen");
            Stretch(gameRoot);
            var gameBackground = CreateImage(gameRoot, "Background", artSet.backgroundColor);
            gameBackground.rectTransform.SetAsFirstSibling();
            Stretch(gameBackground.rectTransform);

            SetImageSprite(gameBackground, GetCombatBackground(), artSet.backgroundColor);

            var topPanel = CreatePanel(gameRoot, "TopStage", new Color(0.11f, 0.12f, 0.14f, 0.98f), GetTopStagePanelSprite());
            SetLowerLeft(topPanel, 16f, 542f, 1248f, 160f);

            levelTitleText = CreateText(topPanel, "LevelTitle", "第一关", 24, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(levelTitleText.rectTransform, 22f, 115f, 300f, 36f);

            topPlayerImage = CreateImage(topPanel, "TopPlayer", artSet.playerColor);
            topPlayerRect = topPlayerImage.rectTransform;
            SetLowerLeft(topPlayerRect, 78f, 24f, 250f, 78f);
            Stretch(CreateText(topPlayerRect, "PlayerLabel", "我方", 22, Color.white, TextAnchor.MiddleCenter).rectTransform);

            enemyImage = CreateImage(topPanel, "Enemy", artSet.enemyColor);
            enemyRect = enemyImage.rectTransform;
            SetLowerLeft(enemyRect, 915f, 24f, 250f, 78f);
            Stretch(CreateText(enemyRect, "EnemyLabel", "敌人", 22, Color.white, TextAnchor.MiddleCenter).rectTransform);

            var hpFrame = CreatePanel(topPanel, "HpFrame", new Color(0.08f, 0.08f, 0.09f, 1f));
            SetLowerLeft(hpFrame, 780f, 116f, 390f, 28f);
            hpFillImage = CreateImage(hpFrame, "HpFill", new Color(0.95f, 0.08f, 0.05f, 1f));
            hpFillImage.type = Image.Type.Filled;
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            SetLowerLeft(hpFillImage.rectTransform, 0f, 0f, 390f, 28f);
            targetText = CreateText(hpFrame, "TargetText", string.Empty, 18, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(targetText.rectTransform, 0f, 0f, 390f, 28f);

            var scorePanel = CreatePanel(topPanel, "ScorePanel", new Color(0.12f, 0.2f, 0.25f, 1f), GetScorePanelSprite());
            SetLowerLeft(scorePanel, 500f, 28f, 250f, 92f);
            scoreText = CreateText(scorePanel, "ScoreText", string.Empty, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(scoreText.rectTransform);

            attackEffectText = CreateText(topPanel, "AttackEffect", string.Empty, 40, new Color(1f, 0.9f, 0.2f, 1f), TextAnchor.MiddleCenter);
            attackEffectRect = attackEffectText.rectTransform;
            SetLowerLeft(attackEffectRect, 520f, 50f, 240f, 80f);
            attackEffectImage = CreateImage(attackEffectText.transform, "Art", new Color(1f, 0.9f, 0.2f, 0.18f));
            Stretch(attackEffectImage.rectTransform);
            attackEffectImage.transform.SetAsFirstSibling();
            attackEffectImage.gameObject.SetActive(false);
            attackEffectText.gameObject.SetActive(false);

            var buffPanel = CreatePanel(gameRoot, "BuffPanel", new Color(0.12f, 0.13f, 0.15f, 0.98f), GetBuffPanelSprite());
            SetLowerLeft(buffPanel, 20f, 286f, 210f, 234f);
            SetLowerLeft(CreateText(buffPanel, "BuffTitle", "道具", 24, Color.white, TextAnchor.MiddleLeft).rectTransform, 18f, 178f, 170f, 42f);
            AddBuffSlot(buffPanel, 20f, 116f, "增益");
            AddBuffSlot(buffPanel, 20f, 62f, "减益");
            AddBuffSlot(buffPanel, 20f, 8f, "空槽");

            var playerPanel = CreatePanel(gameRoot, "PlayerAnimationPanel", new Color(0.12f, 0.13f, 0.15f, 0.98f), GetPlayerPanelSprite());
            SetLowerLeft(playerPanel, 20f, 20f, 210f, 250f);
            SetLowerLeft(CreateText(playerPanel, "PlayerAnimTitle", "人物动画", 24, Color.white, TextAnchor.MiddleLeft).rectTransform, 18f, 196f, 170f, 42f);
            playerAnimationImage = CreateImage(playerPanel, "PlayerBody", artSet.playerColor);
            playerAnimationRect = playerAnimationImage.rectTransform;
            SetLowerLeft(playerAnimationRect, 42f, 48f, 126f, 126f);
            Stretch(CreateText(playerAnimationRect, "PlayerAnimLabel", "Idle", 22, Color.white, TextAnchor.MiddleCenter).rectTransform);

            var boardFrame = CreatePanel(gameRoot, "BoardFrame", artSet.boardColor, GetBoardPanelSprite());
            SetLowerLeft(boardFrame, 250f, 45f, 710f, 475f);
            boardRect = boardFrame;

            boardGridRect = CreateRect(boardRect, "Grid");
            Stretch(boardGridRect);
            boardGrid = boardGridRect.gameObject.AddComponent<GridLayoutGroup>();
            boardGrid.childAlignment = TextAnchor.UpperLeft;
            boardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            var selectorColor = artSet.selectorColor;
            selectorColor.a = 0.33333334f;
            var selectorImage = CreateImage(boardRect, "SelectionBox", selectorColor);
            selectorRect = selectorImage.rectTransform;
            var outline = selectorImage.gameObject.AddComponent<Outline>();
            outline.effectColor = artSet.selectorOutlineColor;
            outline.effectDistance = new Vector2(3f, -3f);
            selectorRect.SetAsLastSibling();

            var rightPanel = CreatePanel(gameRoot, "RightPanel", new Color(0.12f, 0.13f, 0.15f, 0.98f), GetRightPanelSprite());
            SetLowerLeft(rightPanel, 985f, 45f, 275f, 475f);

            roundScoreText = CreateText(rightPanel, "RoundScore", "本回合\n-", 26, Color.white, TextAnchor.MiddleCenter);
            var roundBox = CreatePanel(rightPanel, "RoundScoreBox", new Color(0.09f, 0.16f, 0.2f, 1f), GetRoundScorePanelSprite());
            SetLowerLeft(roundBox, 32f, 322f, 210f, 88f);
            roundScoreText.transform.SetParent(roundBox, false);
            Stretch(roundScoreText.rectTransform);

            var turnBox = CreatePanel(rightPanel, "TurnBox", new Color(0.16f, 0.15f, 0.13f, 1f), GetTurnPanelSprite());
            SetLowerLeft(turnBox, 56f, 198f, 162f, 96f);
            turnText = CreateText(turnBox, "TurnText", string.Empty, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(turnText.rectTransform);

            attackButton = CreateButton(rightPanel, "AttackButton", "攻击", new Color(0.78f, 0.24f, 0.18f, 1f), Attack, GetAttackButtonSprite());
            SetLowerLeft(attackButton.GetComponent<RectTransform>(), 26f, 38f, 224f, 92f);

            selectedDetailText = CreateText(gameRoot, "SelectedDetail", "框选数字后点击攻击结算", 18, new Color(0.86f, 0.9f, 0.92f, 1f), TextAnchor.MiddleLeft);
            SetLowerLeft(selectedDetailText.rectTransform, 250f, 12f, 710f, 28f);
        }

        private void BuildRewardScreen()
        {
            rewardRoot = CreateRect(canvasRoot, "RewardScreen");
            Stretch(rewardRoot);
            var rewardShade = CreateImage(rewardRoot, "Shade", new Color(0.03f, 0.04f, 0.05f, 0.9f));
            rewardShade.rectTransform.SetAsFirstSibling();
            Stretch(rewardShade.rectTransform);
            SetImageSprite(rewardShade, GetRewardBackground(), new Color(0.03f, 0.04f, 0.05f, 0.9f));

            rewardTitleText = CreateText(rewardRoot, "Title", "胜利奖励", 40, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(rewardTitleText.rectTransform, 280f, 540f, 720f, 64f);

            var cards = CreateRect(rewardRoot, "RewardCards");
            SetLowerLeft(cards, 331f, 278f, 618f, 210f);

            var skip = CreateButton(rewardRoot, "SkipReward", "跳过奖励", new Color(0.25f, 0.27f, 0.3f, 1f), GoToNextLevel);
            SetLowerLeft(skip.GetComponent<RectTransform>(), 520f, 168f, 240f, 62f);
        }

        private void BuildMessageScreen()
        {
            messageRoot = CreateRect(canvasRoot, "MessageScreen");
            Stretch(messageRoot);
            var messageShade = CreateImage(messageRoot, "Shade", new Color(0.02f, 0.02f, 0.025f, 0.88f));
            messageShade.rectTransform.SetAsFirstSibling();
            Stretch(messageShade.rectTransform);

            messageTitleText = CreateText(messageRoot, "Title", string.Empty, 42, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(messageTitleText.rectTransform, 300f, 438f, 680f, 72f);

            messageBodyText = CreateText(messageRoot, "Body", string.Empty, 24, new Color(0.9f, 0.93f, 1f, 1f), TextAnchor.MiddleCenter);
            SetLowerLeft(messageBodyText.rectTransform, 310f, 310f, 660f, 92f);

            messageButton = CreateButton(messageRoot, "Button", "继续", artSet.accentColor, () => { });
            SetLowerLeft(messageButton.GetComponent<RectTransform>(), 520f, 216f, 240f, 62f);
        }

        private void BuildBoardCells()
        {
            for (int i = boardGridRect.childCount - 1; i >= 0; i--)
            {
                Destroy(boardGridRect.GetChild(i).gameObject);
            }

            cellImages.Clear();
            cellTexts.Clear();
            boardGrid.constraintCount = columns;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var cellImage = CreateImage(boardGridRect, $"Cell_{x}_{y}", Color.white);
                    cellImages.Add(cellImage);

                    var text = CreateText(cellImage.transform, "Value", string.Empty, 26, Color.black, TextAnchor.MiddleCenter);
                    Stretch(text.rectTransform);
                    cellTexts.Add(text);

                    var border = cellImage.gameObject.AddComponent<Outline>();
                    border.effectColor = new Color(0f, 0f, 0f, 0.7f);
                    border.effectDistance = new Vector2(1f, -1f);
                }
            }

            RefreshCellSize();
        }

        private void RefreshBoardVisuals()
        {
            if (board == null)
            {
                return;
            }

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    var symbol = board[x, y];
                    if (index >= cellImages.Count || symbol == null)
                    {
                        continue;
                    }

                    Sprite symbolSprite = GetSymbolSprite(symbol);
                    cellImages[index].sprite = symbolSprite != null ? symbolSprite : SolidSprite;
                    cellImages[index].color = symbolSprite != null ? Color.white : symbol.SafeTint;
                    cellTexts[index].text = symbol.SafeDisplayName;
                    cellTexts[index].color = symbol.SafeTint.grayscale > 0.68f ? Color.black : Color.white;
                }
            }

            RefreshCellSize();
        }

        private void RefreshCellSize()
        {
            Canvas.ForceUpdateCanvases();
            float width = boardRect.rect.width > 1f ? boardRect.rect.width : boardRect.sizeDelta.x;
            float height = boardRect.rect.height > 1f ? boardRect.rect.height : boardRect.sizeDelta.y;
            boardGrid.cellSize = new Vector2(width / Mathf.Max(1, columns), height / Mathf.Max(1, rows));
            boardGrid.spacing = Vector2.zero;
            RefreshSelectorVisual();
        }

        private void RefreshSelectorVisual()
        {
            if (selectorRect == null || boardRect == null || columns <= 0 || rows <= 0)
            {
                return;
            }

            float width = boardRect.rect.width > 1f ? boardRect.rect.width : boardRect.sizeDelta.x;
            float height = boardRect.rect.height > 1f ? boardRect.rect.height : boardRect.sizeDelta.y;
            float cellWidth = width / columns;
            float cellHeight = height / rows;

            selectorRect.anchorMin = new Vector2(0f, 1f);
            selectorRect.anchorMax = new Vector2(0f, 1f);
            selectorRect.pivot = new Vector2(0f, 1f);
            selectorRect.sizeDelta = new Vector2(cellWidth * selectionWidth, cellHeight * selectionHeight);
            selectorRect.anchoredPosition = new Vector2(selectionOrigin.x * cellWidth, -selectionOrigin.y * cellHeight);
        }

        private void HandleKeyboardSelection()
        {
            int dx = 0;
            int dy = 0;

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                dx = -1;
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                dx = 1;
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                dy = -1;
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                dy = 1;
            }

            if (dx != 0 || dy != 0)
            {
                MoveSelection(dx, dy);
            }
        }

        private void HandleMouseSelection()
        {
            if (Input.GetMouseButtonDown(0) && TryGetBoardCell(Input.mousePosition, out var cell))
            {
                isDraggingSelection = true;
                CenterSelectionOnCell(cell);
            }

            if (Input.GetMouseButton(0) && isDraggingSelection && TryGetBoardCell(Input.mousePosition, out cell))
            {
                CenterSelectionOnCell(cell);
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDraggingSelection = false;
            }
        }

        private bool TryGetBoardCell(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, screenPosition, null, out var localPoint))
            {
                return false;
            }

            Rect rect = boardRect.rect;
            if (!rect.Contains(localPoint))
            {
                return false;
            }

            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float normalizedY = Mathf.InverseLerp(rect.yMax, rect.yMin, localPoint.y);
            int x = Mathf.Clamp(Mathf.FloorToInt(normalizedX * columns), 0, columns - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(normalizedY * rows), 0, rows - 1);
            cell = new Vector2Int(x, y);
            return true;
        }

        private void CenterSelectionOnCell(Vector2Int cell)
        {
            int x = Mathf.Clamp(cell.x - selectionWidth / 2, 0, columns - selectionWidth);
            int y = Mathf.Clamp(cell.y - selectionHeight / 2, 0, rows - selectionHeight);
            selectionOrigin = new Vector2Int(x, y);
        }

        private void MoveSelection(int dx, int dy)
        {
            selectionOrigin = new Vector2Int(
                Mathf.Clamp(selectionOrigin.x + dx, 0, columns - selectionWidth),
                Mathf.Clamp(selectionOrigin.y + dy, 0, rows - selectionHeight));
        }

        private bool IsSelected(int x, int y)
        {
            return x >= selectionOrigin.x
                   && y >= selectionOrigin.y
                   && x < selectionOrigin.x + selectionWidth
                   && y < selectionOrigin.y + selectionHeight;
        }

        private void ShowMessage(string title, string body, string buttonLabel, UnityEngine.Events.UnityAction action)
        {
            HideAllScreens();
            messageRoot.gameObject.SetActive(true);
            messageTitleText.text = title;
            messageBodyText.text = body;
            messageButton.GetComponentInChildren<Text>().text = buttonLabel;
            messageButton.onClick.RemoveAllListeners();
            messageButton.onClick.AddListener(action);
        }

        private void HideAllScreens()
        {
            if (briefingRoot != null)
            {
                briefingRoot.gameObject.SetActive(false);
            }

            if (gameRoot != null)
            {
                gameRoot.gameObject.SetActive(false);
            }

            if (rewardRoot != null)
            {
                rewardRoot.gameObject.SetActive(false);
            }

            if (messageRoot != null)
            {
                messageRoot.gameObject.SetActive(false);
            }
        }

        private CillyRoomLevelDefinition GetLevel(int index)
        {
            if (config.levels == null || index < 0 || index >= config.levels.Count)
            {
                return null;
            }

            return config.levels[index];
        }

        private Sprite GetBriefingBackground()
        {
            return artRig != null ? artRig.GetBriefingBackground(currentLevel, artSet) : currentLevel != null && currentLevel.briefingBackgroundOverride != null ? currentLevel.briefingBackgroundOverride : artSet.briefingBackground;
        }

        private Sprite GetCombatBackground()
        {
            return artRig != null ? artRig.GetCombatBackground(artSet) : artSet.combatBackground;
        }

        private Sprite GetRewardBackground()
        {
            return artRig != null ? artRig.GetRewardBackground(artSet) : artSet.rewardBackground;
        }

        private Sprite GetPlayerIdleSprite()
        {
            return artRig != null ? artRig.GetPlayerIdle(artSet) : artSet.playerIdleSprite;
        }

        private Sprite GetPlayerAttackSprite()
        {
            return artRig != null ? artRig.GetPlayerAttack(artSet) : artSet.playerAttackSprite;
        }

        private Sprite GetEnemyIdleSprite()
        {
            return artRig != null ? artRig.GetEnemyIdle(currentLevel, artSet) : currentLevel != null && currentLevel.enemySpriteOverride != null ? currentLevel.enemySpriteOverride : artSet.enemyIdleSprite;
        }

        private Sprite GetEnemyDefeatedSprite()
        {
            return artRig != null ? artRig.GetEnemyDefeated(artSet) : artSet.enemyDefeatedSprite;
        }

        private Sprite GetAttackEffectSprite()
        {
            return artRig != null ? artRig.GetAttackEffect(artSet) : artSet.attackEffectSprite;
        }

        private Sprite GetSymbolSprite(CillyRoomSymbolDefinition symbol)
        {
            return artRig != null ? artRig.GetSymbolSprite(symbol) : symbol != null ? symbol.artwork : null;
        }

        private Sprite GetTopStagePanelSprite() => artRig != null ? artRig.GetTopStagePanel(artSet) : artSet.topStagePanelSprite;
        private Sprite GetBuffPanelSprite() => artRig != null ? artRig.GetBuffPanel(artSet) : artSet.buffPanelSprite;
        private Sprite GetPlayerPanelSprite() => artRig != null ? artRig.GetPlayerPanel(artSet) : artSet.playerPanelSprite;
        private Sprite GetBoardPanelSprite() => artRig != null ? artRig.GetBoardPanel(artSet) : artSet.boardPanelSprite;
        private Sprite GetRightPanelSprite() => artRig != null ? artRig.GetRightPanel(artSet) : artSet.rightPanelSprite;
        private Sprite GetScorePanelSprite() => artRig != null ? artRig.GetScorePanel(artSet) : artSet.scorePanelSprite;
        private Sprite GetTurnPanelSprite() => artRig != null ? artRig.GetTurnPanel(artSet) : artSet.turnPanelSprite;
        private Sprite GetRoundScorePanelSprite() => artRig != null ? artRig.GetRoundScorePanel(artSet) : artSet.roundScorePanelSprite;
        private Sprite GetAttackButtonSprite() => artRig != null ? artRig.GetAttackButton(artSet) : artSet.attackButtonSprite;
        private Sprite GetRewardCardSprite() => artRig != null ? artRig.GetRewardCard(artSet) : artSet.rewardCardSprite;

        private void SetCharacterImage(Image image, Sprite sprite, Color fallbackColor)
        {
            SetImageSprite(image, sprite, fallbackColor);
        }

        private void SetImageSprite(Image image, Sprite sprite, Color fallbackColor)
        {
            image.sprite = sprite != null ? sprite : SolidSprite;
            image.color = sprite != null ? Color.white : fallbackColor;
        }

        private void AddBuffSlot(Transform parent, float x, float y, string label)
        {
            var slot = CreatePanel(parent, $"BuffSlot_{label}", new Color(0.2f, 0.22f, 0.25f, 1f));
            SetLowerLeft(slot, x, y, 170f, 42f);
            Stretch(CreateText(slot, "Label", label, 18, new Color(0.82f, 0.86f, 0.9f, 1f), TextAnchor.MiddleCenter).rectTransform);
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private RectTransform CreatePanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var image = CreateImage(parent, name, color);
            SetImageSprite(image, sprite, color);
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.82f, 0.9f, 0.96f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            return image.rectTransform;
        }

        private Image CreateImage(Transform parent, string name, Color color)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = SolidSprite;
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, Color color, TextAnchor alignment)
        {
            var rect = CreateRect(parent, name);
            var textComponent = rect.gameObject.AddComponent<Text>();
            textComponent.font = DefaultFont;
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.resizeTextForBestFit = true;
            textComponent.resizeTextMinSize = 10;
            textComponent.resizeTextMaxSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.color = color;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private Button CreateButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction action, Sprite sprite = null)
        {
            var image = CreateImage(parent, name, color);
            SetImageSprite(image, sprite, color);
            image.raycastTarget = true;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = Color.Lerp(color, Color.white, 0.18f),
                pressedColor = Color.Lerp(color, Color.black, 0.18f),
                selectedColor = color,
                disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            button.onClick.AddListener(action);

            var text = CreateText(image.transform, "Label", label, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private RectTransform CreateRect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetLowerLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private Font DefaultFont
        {
            get
            {
                if (defaultFont != null)
                {
                    return defaultFont;
                }

                defaultFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "SimHei", "Arial Unicode MS", "Arial" },
                    18);

                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return defaultFont;
            }
        }

        private Sprite SolidSprite
        {
            get
            {
                if (solidSprite != null)
                {
                    return solidSprite;
                }

                var texture = new Texture2D(1, 1)
                {
                    name = "CillyRoomSolidPixel",
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
                solidSprite.name = "CillyRoomSolidSprite";
                solidSprite.hideFlags = HideFlags.HideAndDontSave;
                return solidSprite;
            }
        }

        private static CillyRoomGameConfig CreateFallbackConfig()
        {
            var config = ScriptableObject.CreateInstance<CillyRoomGameConfig>();
            config.name = "Runtime Fallback CillyRoomGameConfig";
            config.artSet = CreateFallbackArtSet();

            var one = CreateRuntimeSymbol("number_1", "1", 1, new Color(0.88f, 0.88f, 0.88f, 1f));
            var two = CreateRuntimeSymbol("number_2", "2", 2, new Color(0.32f, 0.66f, 0.98f, 1f));
            var three = CreateRuntimeSymbol("number_3", "3", 3, new Color(0.44f, 0.82f, 0.36f, 1f));
            var four = CreateRuntimeSymbol("number_4", "4", 4, new Color(0.98f, 0.58f, 0.24f, 1f));
            var five = CreateRuntimeSymbol("number_5", "5", 5, new Color(0.87f, 0.44f, 0.92f, 1f));
            var six = CreateRuntimeSymbol("number_6", "6", 6, new Color(0.96f, 0.82f, 0.22f, 1f));
            var seven = CreateRuntimeSymbol("number_7", "7", 7, new Color(1f, 0.72f, 0.18f, 1f));

            config.startingSymbols.AddRange(new[] { one, two, three, four, five, six });
            config.levels.Add(CreateRuntimeLevel("level_01", "第一关：资料室遭遇", 30, 5, seven));
            config.levels.Add(CreateRuntimeLevel("level_02", "第二关：走廊追击", 48, 6, seven));
            return config;
        }

        private static CillyRoomArtSet CreateFallbackArtSet()
        {
            var art = ScriptableObject.CreateInstance<CillyRoomArtSet>();
            art.name = "Runtime Fallback CillyRoomArtSet";
            return art;
        }

        private static CillyRoomSymbolDefinition CreateRuntimeSymbol(string id, string displayName, int score, Color color)
        {
            var symbol = ScriptableObject.CreateInstance<CillyRoomSymbolDefinition>();
            symbol.name = displayName;
            symbol.symbolId = id;
            symbol.displayName = displayName;
            symbol.baseScore = score;
            symbol.tintColor = color;
            return symbol;
        }

        private static CillyRoomLevelDefinition CreateRuntimeLevel(string id, string displayName, int targetScore, int turns, CillyRoomSymbolDefinition reward)
        {
            var level = ScriptableObject.CreateInstance<CillyRoomLevelDefinition>();
            level.name = displayName;
            level.levelId = id;
            level.displayName = displayName;
            level.enemyTargetScore = targetScore;
            level.maxTurns = turns;
            level.boardColumns = 9;
            level.boardRows = 5;
            level.selectionWidth = 2;
            level.selectionHeight = 2;
            level.briefingText = $"任务简报：{displayName}。在 {turns} 个回合内，通过框选下方数字累计 {targetScore} 分。选中的格子在攻击后会保留，其余格子会刷新。";
            level.rewardChoices.Add(reward);
            level.rewardChoices.Add(reward);
            level.rewardChoices.Add(reward);
            return level;
        }
    }
}
