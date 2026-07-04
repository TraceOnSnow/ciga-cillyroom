using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceGameDirector : MonoBehaviour
    {
        private const int ForcedFailurePenalty = 9999;

        [Header("Data")]
        [SerializeField] private SubspaceGameConfig config;
        [SerializeField] private SubspaceArtRig artRig;

       [Header("Scene Components")]
       [SerializeField] private SubspaceBriefingController briefing;
       [SerializeField] private SubspaceMenuController menu;
       [SerializeField] private SubspaceUIController ui;
        [SerializeField] private SubspaceBoardController board;
        [SerializeField] private SubspaceSelectionController selection;
        [SerializeField] private SubspaceActorController player;
        [SerializeField] private SubspaceActorController enemy;
        [SerializeField] private SubspaceRewardController rewards;

        [Header("Beam")]
        [FormerlySerializedAs("projectileParent")]
        [SerializeField] private RectTransform beamParent;
        [Tooltip("Drag your beam prefab GameObject here. If it does not have SubspaceProjectileView, the director will add one to the spawned instance.")]
        [SerializeField] private GameObject beamPrefabObject;
        [FormerlySerializedAs("projectilePrefab")]
        [Tooltip("Legacy component slot. You can ignore this if Beam Prefab Object is assigned.")]
        [SerializeField] private SubspaceProjectileView beamPrefab;
        [FormerlySerializedAs("projectileColor")]
        [SerializeField] private Color beamColor = new Color(1f, 0.92f, 0.28f, 0.88f);
        [FormerlySerializedAs("projectileSize")]
        [SerializeField] private Vector2 beamSize = new Vector2(120f, 18f);
        [FormerlySerializedAs("projectileAnimationDuration")]
        [SerializeField] private float beamAnimationDuration = 0.28f;

        [Header("Beam Placement")]
        [Tooltip("Optional. If empty, the beam starts from the player object.")]
        [SerializeField] private Transform beamStartPoint;
        [Tooltip("Optional. If empty, the beam ends at the enemy object.")]
        [SerializeField] private Transform beamEndPoint;
        [SerializeField] private Vector3 beamStartOffset;
        [SerializeField] private Vector3 beamEndOffset;
        [Tooltip("Beam thickness in UI units. This overrides Beam Size Y when greater than 0.")]
        [SerializeField] private float beamThickness = 18f;
        [SerializeField] private float beamStartExtension;
        [SerializeField] private float beamEndExtension;

        [Header("Failure Debug")]
        [SerializeField] private bool forceFailureOnNextAttack;
        [SerializeField] private bool keepForceFailureEnabled;

        [Header("Failure Counterattack")]
        [SerializeField] private float enemyCounterAttackDelay = 0.1f;
        [SerializeField] private float enemyCounterAttackDuration = 0.55f;
        [SerializeField] private float playerHitDuration = 0.25f;

       private readonly List<SubspaceSymbolDefinition> symbolPool = new List<SubspaceSymbolDefinition>();
       private readonly List<SubspaceUpgradeDefinition> activeUpgrades = new List<SubspaceUpgradeDefinition>();
       private SubspaceLevelDefinition currentLevel;
        private SubspaceArtSet artSet;
        private SubspaceTextConfig textConfig;
        private System.Random random;
        private int levelIndex;
        private int totalScore;
        private int remainingTurns;
        private bool busy;

        public void Configure(
            SubspaceGameConfig gameConfig,
            SubspaceArtRig rig,
           SubspaceBriefingController briefingController,
           SubspaceMenuController menuController,
           SubspaceUIController uiController,
            SubspaceBoardController boardController,
            SubspaceSelectionController selectionController,
            SubspaceActorController playerController,
            SubspaceActorController enemyController,
            SubspaceRewardController rewardController)
        {
            config = gameConfig;
            artRig = rig;
           briefing = briefingController;
           menu = menuController;
           ui = uiController;
            board = boardController;
            selection = selectionController;
            player = playerController;
            enemy = enemyController;
            rewards = rewardController;
        }

        public void ForceFailureForTest()
        {
            forceFailureOnNextAttack = true;
        }

        public void ConfigureBeamPlacement(Transform startPoint, Transform endPoint)
        {
            beamStartPoint = startPoint;
            beamEndPoint = endPoint;
        }

        private void Start()
        {
            if (config == null && artRig != null)
            {
                config = artRig.gameConfig;
            }

            if (config == null)
            {
                config = Resources.Load<SubspaceGameConfig>("Subspace/SubspaceGameConfig");
            }

            if (config == null)
            {
                Debug.LogError("SubspaceGameDirector needs a SubspaceGameConfig.");
                enabled = false;
                return;
            }

            artSet = config.artSet != null ? config.artSet : ScriptableObject.CreateInstance<SubspaceArtSet>();
            textConfig = config.textConfig != null ? config.textConfig : SubspaceTextConfig.RuntimeDefault;
           ui.SetTextConfig(textConfig);
           briefing.SetTextConfig(textConfig);
           if (menu != null)
           {
               menu.SetTextConfig(textConfig);
           }
           rewards.SetTextConfig(textConfig);
           random = config.useFixedSeed ? new System.Random(config.randomSeed) : new System.Random();
           ui.SetAttackCallback(Attack);
           ShowMenu();
        }

       private void ShowMenu()
       {
           ui.ShowGame(false);
           ui.HideMessage();
           rewards.Hide();
           briefing.Hide();

           if (menu != null)
           {
               menu.Show(GetMenuBackground(), artSet.backgroundColor, StartCampaign);
           }
           else
           {
               StartCampaign();
           }
       }

       private void StartCampaign()
        {
           symbolPool.Clear();
           activeUpgrades.Clear();
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
           if (menu != null)
           {
               menu.Hide();
           }

           if (currentLevel == null)
            {
               ui.ShowMessage(textConfig.campaignCompleteTitle, textConfig.campaignCompleteBody, textConfig.campaignCompleteButtonText, ShowMenu);
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
            enemy.SetAnimatorStateNames("Stand", "Attack", "Hit", "Defeated");
            enemy.ApplySpineLevel(currentLevel);
            player.ShowIdle(GetPlayerIdle());
            enemy.ShowIdle(GetEnemyIdle());

           if (levelIndex == 0 || board.Tiles == null)
           {
               board.Build(currentLevel.SafeColumns, currentLevel.SafeRows, symbolPool, random, ResolveSymbolSprite);
           }
           else
           {
               board.RerollAllSymbols();
           }
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

            var upgradeIds = new List<string>();
            foreach (var upgrade in activeUpgrades)
            {
                upgradeIds.Add(upgrade.SafeId);
            }

            var context = new SubspaceScoreContext(board.Tiles, upgradeIds);
            var result = SubspaceScoreResolver.Calculate(board.Tiles, selection.CurrentShape, context, random);
            board.RefreshAll();
            totalScore += result.total;
            bool forcedFailure = forceFailureOnNextAttack;
            if (forceFailureOnNextAttack)
            {
                totalScore -= ForcedFailurePenalty;
                Debug.Log($"[Subspace Debug] Force failure test applied: score -{ForcedFailurePenalty}.");
                forceFailureOnNextAttack = keepForceFailureEnabled;
            }

            remainingTurns = Mathf.Max(0, remainingTurns - 1 + result.turnDelta);
            if (forcedFailure)
            {
                remainingTurns = 0;
            }
            ui.Refresh(currentLevel, totalScore, remainingTurns, result.total, result.lines);
            LogScoreSources(result, selection.CurrentSelection);

            yield return PlayPlayerAttackWithBeamAndEnemyHit();

            if (totalScore >= currentLevel.SafeTargetScore)
            {
                enemy.ShowDefeated(GetEnemyDefeated());
                yield return player.PlayEscape();
                ShowRewards();
                yield break;
            }

            if (remainingTurns <= 0)
            {
                yield return PlayFailureCounterAttack();
                ShowFailureMessage();
                yield break;
            }

            board.RerollOutside(selection.CurrentSelection);
            ui.Refresh(currentLevel, totalScore, remainingTurns, 0, null);
            ui.SetAttackEnabled(true);
            busy = false;
        }

        private IEnumerator PlayPlayerAttackWithBeamAndEnemyHit()
        {
            Coroutine beamRoutine = StartCoroutine(PlayPlayerBeam());
            Coroutine enemyHitRoutine = enemy != null ? StartCoroutine(enemy.PlayHit(GetEnemyDefeated(), 0.25f)) : null;
            yield return player.PlayAttack(GetPlayerAttack());

            if (beamRoutine != null)
            {
                yield return beamRoutine;
            }

            if (enemyHitRoutine != null)
            {
                yield return enemyHitRoutine;
            }
        }

        private IEnumerator PlayPlayerBeam()
        {
            if (player == null || enemy == null)
            {
                yield break;
            }

            var beam = CreateBeam();
            if (beam == null)
            {
                yield break;
            }

            beam.transform.SetAsLastSibling();
            CalculateBeamPositions(out var startPosition, out var endPosition);
            yield return beam.PlayBeam(startPosition, endPosition, beamAnimationDuration, GetBeamThickness());
            Destroy(beam.gameObject);
        }

        private void CalculateBeamPositions(out Vector3 startPosition, out Vector3 endPosition)
        {
            var startTransform = beamStartPoint != null ? beamStartPoint : player.transform;
            var endTransform = beamEndPoint != null ? beamEndPoint : enemy.transform;

            startPosition = startTransform.position + beamStartOffset;
            endPosition = endTransform.position + beamEndOffset;

            Vector3 delta = endPosition - startPosition;
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 direction = delta.normalized;
            startPosition -= direction * beamStartExtension;
            endPosition += direction * beamEndExtension;
        }

        private SubspaceProjectileView CreateBeam()
        {
            Transform defaultParent = ui != null ? ui.transform : transform;
            if (beamPrefabObject != null)
            {
                if (ShouldCreateUiBeamProxy(beamPrefabObject, defaultParent))
                {
                    Transform prefabParent = beamParent != null ? beamParent : transform;
                    var hiddenInstance = Instantiate(beamPrefabObject, prefabParent, true);
                    hiddenInstance.name = beamPrefabObject.name;
                    hiddenInstance.SetActive(false);
                    Debug.Log($"[Subspace Beam] Spawned Beam Prefab Object for reference: {hiddenInstance.name}; using a UI proxy so it is visible above the overlay canvas.");
                    var proxy = CreateUiBeamProxy(beamPrefabObject, defaultParent);
                    proxy.AttachLifetimeObject(hiddenInstance);
                    return proxy;
                }

                Transform parent = beamParent != null ? beamParent : transform;
                var instance = Instantiate(beamPrefabObject, parent, true);
                instance.name = beamPrefabObject.name;
                Debug.Log($"[Subspace Beam] Spawned Beam Prefab Object: {instance.name}.");
                return EnsureBeamView(instance);
            }

            if (beamPrefab != null)
            {
                Transform parent = beamParent != null ? beamParent : transform;
                var instance = Instantiate(beamPrefab, parent, true);
                Debug.Log($"[Subspace Beam] Spawned legacy Beam Prefab: {instance.name}.");
                return instance;
            }

            var beamObject = new GameObject("Player Beam Effect", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SubspaceProjectileView));
            beamObject.transform.SetParent(defaultParent, false);

            var rect = beamObject.GetComponent<RectTransform>();
            rect.sizeDelta = beamSize;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = beamObject.GetComponent<Image>();
            image.color = beamColor;
            image.raycastTarget = false;

            var outline = beamObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.5f, 0.08f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var view = beamObject.GetComponent<SubspaceProjectileView>();
            view.Configure(rect, image);
            view.SetThickness(GetBeamThickness());
            return view;
        }

        private bool ShouldCreateUiBeamProxy(GameObject prefab, Transform defaultParent)
        {
            return beamParent == null
                && defaultParent != null
                && prefab.GetComponent<RectTransform>() == null
                && prefab.GetComponentInChildren<SpriteRenderer>(true) != null;
        }

        private SubspaceProjectileView CreateUiBeamProxy(GameObject prefab, Transform parent)
        {
            var sourceRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            var beamObject = new GameObject($"{prefab.name} UI Beam", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(SubspaceProjectileView));
            beamObject.transform.SetParent(parent, false);

            var rect = beamObject.GetComponent<RectTransform>();
            rect.sizeDelta = beamSize;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = beamObject.GetComponent<Image>();
            image.sprite = sourceRenderer.sprite;
            image.color = sourceRenderer.color;
            image.raycastTarget = false;
            image.preserveAspect = false;

            var view = beamObject.GetComponent<SubspaceProjectileView>();
            view.Configure(rect, image);
            view.SetThickness(GetBeamThickness());
            Debug.Log($"[Subspace Beam] Spawned UI beam proxy from SpriteRenderer prefab: {prefab.name}.");
            return view;
        }

        private SubspaceProjectileView EnsureBeamView(GameObject beamObject)
        {
            var view = beamObject.GetComponent<SubspaceProjectileView>();
            if (view == null)
            {
                view = beamObject.AddComponent<SubspaceProjectileView>();
            }

            var rect = beamObject.GetComponent<RectTransform>();
            var image = beamObject.GetComponent<Image>();
            if (image == null)
            {
                image = beamObject.GetComponentInChildren<Image>(true);
            }

            view.Configure(rect, image);
            view.SetThickness(GetBeamThickness());
            return view;
        }

        private float GetBeamThickness()
        {
            return beamThickness > 0f ? beamThickness : Mathf.Max(1f, beamSize.y);
        }

        private void OnDrawGizmosSelected()
        {
            if (player == null || enemy == null)
            {
                return;
            }

            CalculateBeamPositions(out var startPosition, out var endPosition);
            Gizmos.color = new Color(1f, 0.9f, 0.1f, 0.9f);
            Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawSphere(startPosition, 8f);
            Gizmos.color = new Color(1f, 0.15f, 0.08f, 0.9f);
            Gizmos.DrawSphere(endPosition, 8f);
        }

        private IEnumerator PlayFailureCounterAttack()
        {
            yield return new WaitForSeconds(enemyCounterAttackDelay);

            if (enemy != null)
            {
                yield return enemy.PlayAttack(GetEnemyIdle(), enemyCounterAttackDuration);
            }

            if (player != null)
            {
                yield return player.PlayHit(GetPlayerIdle(), playerHitDuration);
                player.ShowIdle(GetPlayerIdle());
            }
        }

        private void ShowFailureMessage()
        {
            ui.ShowMessage(
                textConfig.failureTitle,
                textConfig.FormatFailureBody(currentLevel.SafeTargetScore - totalScore),
                textConfig.failureButtonText,
                BeginLevel);
            busy = false;
        }

       private void ShowRewards()
       {
           ui.ShowGame(false);
           var symbolRewards = currentLevel != null ? currentLevel.rewardChoices : null;
           var upgradeRewards = GetAvailableUpgradeChoices();
           var title = currentLevel != null ? textConfig.FormatRewardTitle(currentLevel.displayName) : textConfig.rewardFallbackTitle;
           rewards.ShowWithUpgrades(title, symbolRewards, upgradeRewards, ChooseReward, ChooseUpgrade, SkipReward, ResolveSymbolSprite);
       }

       private void ChooseReward(SubspaceSymbolDefinition reward)
       {
           if (reward != null)
           {
               symbolPool.Add(reward);
           }

           SkipReward();
       }

       private void ChooseUpgrade(SubspaceUpgradeDefinition upgrade)
       {
           if (upgrade != null)
           {
               activeUpgrades.Add(upgrade);
               ApplyUpgradeEffect(upgrade);
           }

           SkipReward();
       }

       private System.Collections.Generic.List<SubspaceUpgradeDefinition> GetAvailableUpgradeChoices()
       {
           var result = new System.Collections.Generic.List<SubspaceUpgradeDefinition>();
           if (config == null || config.upgradePool == null)
           {
               return result;
           }

           foreach (var upgrade in config.upgradePool)
           {
               if (upgrade == null)
               {
                   continue;
               }

               bool alreadyOwned = false;
               foreach (var owned in activeUpgrades)
               {
                   if (owned != null && owned.SafeId == upgrade.SafeId)
                   {
                       alreadyOwned = true;
                       break;
                   }
               }

               if (!alreadyOwned)
               {
                   result.Add(upgrade);
               }
           }

           if (result.Count > 2)
           {
               var shuffled = new System.Collections.Generic.List<SubspaceUpgradeDefinition>(result);
               for (int i = shuffled.Count - 1; i > 0; i--)
               {
                   int j = random.Next(0, i + 1);
                   (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
               }

               result = shuffled.GetRange(0, 2);
           }

           return result;
       }

       private void ApplyUpgradeEffect(SubspaceUpgradeDefinition upgrade)
       {
           if (upgrade == null)
           {
               return;
           }

           switch (upgrade.type)
           {
               case SubspaceUpgradeType.ScannerShape:
                   if (upgrade.shapeOffsets != null && upgrade.shapeOffsets.Count > 0)
                   {
                       selection.ApplyCustomShape(new System.Collections.Generic.List<Vector2Int>(upgrade.shapeOffsets));
                   }
                   break;
               default:
                   Debug.Log($"[Subspace] Upgrade applied: {upgrade.displayName} ({upgrade.type})");
                   break;
           }
       }

        private void SkipReward()
        {
            rewards.Hide();
            levelIndex++;
            ShowBriefing();
        }

        private Sprite ResolveSymbolSprite(SubspaceSymbolDefinition symbol)
        {
            return artRig != null ? artRig.GetSymbolSprite(symbol) : symbol != null ? symbol.artwork : null;
        }

        private void LogScoreSources(SubspaceScoreResult result, RectInt selectedArea)
        {
            var builder = new StringBuilder();
            builder.Append("[Subspace Score Detail] ");
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
       private Sprite GetMenuBackground() => artRig != null ? artRig.backgrounds.briefingBackground : artSet.briefingBackground;
        private Sprite GetPlayerIdle() => artRig != null ? artRig.GetPlayerIdle(artSet) : artSet.playerIdleSprite;
        private Sprite GetPlayerAttack() => artRig != null ? artRig.GetPlayerAttack(artSet) : artSet.playerAttackSprite;
        private Sprite GetEnemyIdle() => artRig != null ? artRig.GetEnemyIdle(currentLevel, artSet) : currentLevel.enemySpriteOverride != null ? currentLevel.enemySpriteOverride : artSet.enemyIdleSprite;
        private Sprite GetEnemyDefeated() => artRig != null ? artRig.GetEnemyDefeated(artSet) : artSet.enemyDefeatedSprite;
    }
}
