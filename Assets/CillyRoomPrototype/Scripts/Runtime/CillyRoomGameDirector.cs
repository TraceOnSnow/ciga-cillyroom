using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomGameDirector : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CillyRoomGameConfig config;
        [SerializeField] private CillyRoomArtRig artRig;

        [Header("Scene Components")]
        [SerializeField] private CillyRoomBriefingController briefing;
        [SerializeField] private CillyRoomUIController ui;
        [SerializeField] private CillyRoomBoardController board;
        [SerializeField] private CillyRoomSelectionController selection;
        [SerializeField] private CillyRoomActorController player;
        [SerializeField] private CillyRoomActorController enemy;
        [SerializeField] private CillyRoomRewardController rewards;

        private readonly List<CillyRoomSymbolDefinition> symbolPool = new List<CillyRoomSymbolDefinition>();
        private CillyRoomLevelDefinition currentLevel;
        private CillyRoomArtSet artSet;
        private CillyRoomTextConfig textConfig;
        private System.Random random;
        private int levelIndex;
        private int totalScore;
        private int remainingTurns;
        private bool busy;

        public void Configure(
            CillyRoomGameConfig gameConfig,
            CillyRoomArtRig rig,
            CillyRoomBriefingController briefingController,
            CillyRoomUIController uiController,
            CillyRoomBoardController boardController,
            CillyRoomSelectionController selectionController,
            CillyRoomActorController playerController,
            CillyRoomActorController enemyController,
            CillyRoomRewardController rewardController)
        {
            config = gameConfig;
            artRig = rig;
            briefing = briefingController;
            ui = uiController;
            board = boardController;
            selection = selectionController;
            player = playerController;
            enemy = enemyController;
            rewards = rewardController;
        }

        private void Start()
        {
            if (config == null && artRig != null)
            {
                config = artRig.gameConfig;
            }

            if (config == null)
            {
                config = Resources.Load<CillyRoomGameConfig>("CillyRoomPrototype/CillyRoomGameConfig");
            }

            if (config == null)
            {
                Debug.LogError("CillyRoomGameDirector needs a CillyRoomGameConfig.");
                enabled = false;
                return;
            }

            artSet = config.artSet != null ? config.artSet : ScriptableObject.CreateInstance<CillyRoomArtSet>();
            textConfig = config.textConfig != null ? config.textConfig : CillyRoomTextConfig.RuntimeDefault;
            ui.SetTextConfig(textConfig);
            briefing.SetTextConfig(textConfig);
            rewards.SetTextConfig(textConfig);
            random = config.useFixedSeed ? new System.Random(config.randomSeed) : new System.Random();
            ui.SetAttackCallback(Attack);
            StartCampaign();
        }

        private void StartCampaign()
        {
            symbolPool.Clear();
            foreach (var symbol in config.startingSymbols)
            {
                if (symbol != null)
                {
                    symbolPool.Add(symbol);
                }
            }

            levelIndex = 0;
            ShowBriefing();
        }

        private void ShowBriefing()
        {
            currentLevel = levelIndex >= 0 && levelIndex < config.levels.Count ? config.levels[levelIndex] : null;
            ui.ShowGame(false);
            ui.HideMessage();
            rewards.Hide();

            if (currentLevel == null)
            {
                ui.ShowMessage(textConfig.campaignCompleteTitle, textConfig.campaignCompleteBody, textConfig.campaignCompleteButtonText, StartCampaign);
                return;
            }

            briefing.Show(currentLevel, GetBriefingBackground(), artSet.backgroundColor, BeginLevel);
        }

        private void BeginLevel()
        {
            briefing.Hide();
            rewards.Hide();
            ui.HideMessage();
            ui.ShowGame(true);

            totalScore = 0;
            remainingTurns = currentLevel.SafeTurns;
            busy = false;

            player.SetColors(artSet.playerColor, artSet.playerAttackColor, artSet.playerColor);
            enemy.SetColors(artSet.enemyColor, artSet.enemyColor, artSet.defeatedEnemyColor);
            player.ShowIdle(GetPlayerIdle());
            enemy.ShowIdle(GetEnemyIdle());

            board.Build(currentLevel.SafeColumns, currentLevel.SafeRows, symbolPool, random, ResolveSymbolSprite);
            selection.ResetSelection(currentLevel.SafeSelectionWidth, currentLevel.SafeSelectionHeight);
            ui.SetAttackEnabled(true);
            ui.Refresh(currentLevel, totalScore, remainingTurns, 0, null);
        }

        private void Attack()
        {
            if (!busy)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            busy = true;
            ui.SetAttackEnabled(false);

            var result = CillyRoomScoreResolver.Calculate(board.Board, selection.CurrentSelection, random);
            totalScore += result.total;
            remainingTurns = Mathf.Max(0, remainingTurns - 1 + result.turnDelta);
            ui.Refresh(currentLevel, totalScore, remainingTurns, result.total, result.lines);
            LogScoreSources(result, selection.CurrentSelection);

            yield return player.PlayAttack(GetPlayerAttack());
            yield return enemy.PlayHit(GetEnemyDefeated(), 0.25f);

            if (totalScore >= currentLevel.SafeTargetScore)
            {
                enemy.ShowDefeated(GetEnemyDefeated());
                yield return player.PlayEscape();
                ShowRewards();
                yield break;
            }

            if (remainingTurns <= 0)
            {
                ui.ShowMessage(
                    textConfig.failureTitle,
                    textConfig.FormatFailureBody(currentLevel.SafeTargetScore - totalScore),
                    textConfig.failureButtonText,
                    BeginLevel);
                busy = false;
                yield break;
            }

            board.RerollOutside(selection.CurrentSelection);
            ui.Refresh(currentLevel, totalScore, remainingTurns, 0, null);
            ui.SetAttackEnabled(true);
            busy = false;
        }

        private void ShowRewards()
        {
            ui.ShowGame(false);
            rewards.Show(currentLevel, ChooseReward, SkipReward, ResolveSymbolSprite);
        }

        private void ChooseReward(CillyRoomSymbolDefinition reward)
        {
            if (reward != null)
            {
                symbolPool.Add(reward);
            }

            SkipReward();
        }

        private void SkipReward()
        {
            rewards.Hide();
            levelIndex++;
            ShowBriefing();
        }

        private Sprite ResolveSymbolSprite(CillyRoomSymbolDefinition symbol)
        {
            return artRig != null ? artRig.GetSymbolSprite(symbol) : symbol != null ? symbol.artwork : null;
        }

        private void LogScoreSources(CillyRoomScoreResult result, RectInt selectedArea)
        {
            var builder = new StringBuilder();
            builder.Append("[CillyRoom Score Detail] ");
            builder.Append(currentLevel != null ? currentLevel.displayName : "Unknown Level");
            builder.Append(" | Selection: x ");
            builder.Append(selectedArea.xMin);
            builder.Append("-");
            builder.Append(selectedArea.xMax - 1);
            builder.Append(", y ");
            builder.Append(selectedArea.yMin);
            builder.Append("-");
            builder.Append(selectedArea.yMax - 1);
            builder.Append(" | Round Score: ");
            builder.Append(result.total);
            builder.Append(" | Total Score: ");
            builder.Append(totalScore);
            builder.Append(" | Remaining Turns: ");
            builder.Append(remainingTurns);

            if (result.sources == null || result.sources.Count == 0)
            {
                builder.AppendLine();
                builder.Append("  No selected score sources.");
                Debug.Log(builder.ToString());
                return;
            }

            foreach (var source in result.sources)
            {
                builder.AppendLine();
                builder.Append("  - ");
                builder.Append(source.displayName);

                if (source.position.x >= 0 && source.position.y >= 0)
                {
                    builder.Append(" @ (");
                    builder.Append(source.position.x);
                    builder.Append(", ");
                    builder.Append(source.position.y);
                    builder.Append(")");
                }

                builder.Append(": ");
                builder.Append(source.finalScore);
                builder.Append(" = ");
                builder.Append(source.baseScore);

                if (source.multiplier != 1)
                {
                    builder.Append(" x");
                    builder.Append(source.multiplier);
                }

                if (source.originalScore != source.baseScore)
                {
                    builder.Append(" (原始 ");
                    builder.Append(source.originalScore);
                    builder.Append(")");
                }

                if (!string.IsNullOrWhiteSpace(source.detail))
                {
                    builder.Append(" | ");
                    builder.Append(source.detail);
                }
            }

            if (result.lines != null && result.lines.Count > 0)
            {
                builder.AppendLine();
                builder.Append("  Effects: ");
                builder.Append(string.Join(" / ", result.lines));
            }

            Debug.Log(builder.ToString());
        }

        private Sprite GetBriefingBackground() => artRig != null ? artRig.GetBriefingBackground(currentLevel, artSet) : currentLevel.briefingBackgroundOverride != null ? currentLevel.briefingBackgroundOverride : artSet.briefingBackground;
        private Sprite GetPlayerIdle() => artRig != null ? artRig.GetPlayerIdle(artSet) : artSet.playerIdleSprite;
        private Sprite GetPlayerAttack() => artRig != null ? artRig.GetPlayerAttack(artSet) : artSet.playerAttackSprite;
        private Sprite GetEnemyIdle() => artRig != null ? artRig.GetEnemyIdle(currentLevel, artSet) : currentLevel.enemySpriteOverride != null ? currentLevel.enemySpriteOverride : artSet.enemyIdleSprite;
        private Sprite GetEnemyDefeated() => artRig != null ? artRig.GetEnemyDefeated(artSet) : artSet.enemyDefeatedSprite;
    }
}
