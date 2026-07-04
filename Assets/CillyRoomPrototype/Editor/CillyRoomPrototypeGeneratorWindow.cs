using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CillyRoomPrototype.Editor
{
    public sealed class CillyRoomPrototypeGeneratorWindow : EditorWindow
    {
        private const string RootFolder = "Assets/CillyRoomPrototype";
        private const string GeneratedFolder = RootFolder + "/Generated";
        private const string ResourcesFolder = GeneratedFolder + "/Resources";
        private const string ResourcesConfigFolder = ResourcesFolder + "/CillyRoomPrototype";
        private const string SymbolsFolder = GeneratedFolder + "/Symbols";
        private const string LevelsFolder = GeneratedFolder + "/Levels";
        private const string AnimatorsFolder = GeneratedFolder + "/Animators";

        [MenuItem("CillyRoom/原型生成器")]
        public static void Open()
        {
            GetWindow<CillyRoomPrototypeGeneratorWindow>("CillyRoom 原型");
        }

        [MenuItem("CillyRoom/一键生成两个关卡")]
        public static void GenerateFromMenu()
        {
            GeneratePrototype();
        }

        public static void GenerateForBatchMode()
        {
            GeneratePrototype();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("组件化框选战斗原型", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("生成数据资产，并在当前场景里创建 GameObject + 对应脚本组件。主角、敌人、棋盘、框选、UI、奖励各自有独立 Controller。", MessageType.Info);

            if (GUILayout.Button("一键生成两个关卡", GUILayout.Height(42f)))
            {
                GeneratePrototype();
            }
        }

        private static void GeneratePrototype()
        {
            EnsureFolders();
            EnsureEventSystem();

            var artSet = CreateArtSet();
            var textConfig = CreateTextConfig();
            var symbols = CreateSymbols();
            var levels = CreateLevels(symbols[5], symbols[6], symbols[7]);
            var config = CreateConfig(artSet, textConfig, symbols, levels);
            var playerAnimatorController = CreateActorAnimatorController("PlayerActor.controller");
            var enemyAnimatorController = CreateActorAnimatorController("EnemyActor.controller");

            var root = RecreateRoot("CillyRoom Component Prototype");
            var artRig = CreateArtRig(root.transform, config, artSet, symbols);
            var scene = BuildScene(root.transform, config, artRig, artSet, textConfig, playerAnimatorController, enemyAnimatorController);

            scene.director.Configure(
                config,
                artRig,
                scene.briefing,
                scene.ui,
                scene.board,
                scene.selection,
                scene.player,
                scene.enemy,
                scene.rewards);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("CillyRoom", "已生成组件化 GameObject 版本。请在 Hierarchy 中查看 CillyRoom Component Prototype。", "好");
            }
        }

        private static CillyRoomArtSet CreateArtSet()
        {
            return CreateAssetIfMissing<CillyRoomArtSet>(
                ResourcesConfigFolder + "/CillyRoomArtSet.asset",
                asset =>
                {
                    asset.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
                    asset.panelColor = new Color(0.16f, 0.17f, 0.19f, 0.96f);
                    asset.boardColor = new Color(0.09f, 0.1f, 0.12f, 1f);
                    asset.playerColor = new Color(0.23f, 0.58f, 0.92f, 1f);
                    asset.playerAttackColor = new Color(0.99f, 0.73f, 0.2f, 1f);
                    asset.enemyColor = new Color(0.86f, 0.24f, 0.22f, 1f);
                    asset.defeatedEnemyColor = new Color(0.32f, 0.32f, 0.36f, 1f);
                    asset.selectorColor = new Color(1f, 0.56f, 0.18f, 0.33333334f);
                    asset.selectorOutlineColor = new Color(1f, 0.43f, 0.12f, 1f);
                    asset.accentColor = new Color(0.09f, 0.72f, 0.94f, 1f);
                });
        }

        private static CillyRoomTextConfig CreateTextConfig()
        {
            return CreateAssetIfMissing<CillyRoomTextConfig>(
                ResourcesConfigFolder + "/CillyRoomTextConfig.asset",
                asset => { });
        }

        private static List<CillyRoomSymbolDefinition> CreateSymbols()
        {
            return new List<CillyRoomSymbolDefinition>
            {
                CreateElementSymbol(1, "beacon", "信标", CillyRoomSymbolKind.Beacon, 3, new Color(0.88f, 0.88f, 0.88f, 1f)),
                CreateElementSymbol(2, "life_signal", "生命信号", CillyRoomSymbolKind.LifeSignal, 0, new Color(0.32f, 0.66f, 0.98f, 1f)),
                CreateElementSymbol(3, "anchor", "锚点", CillyRoomSymbolKind.Anchor, 5, new Color(0.44f, 0.82f, 0.36f, 1f)),
                CreateElementSymbol(4, "energy_core", "能量核心", CillyRoomSymbolKind.EnergyCore, 1, new Color(0.98f, 0.58f, 0.24f, 1f)),
                CreateElementSymbol(5, "turbulence", "乱流", CillyRoomSymbolKind.Turbulence, -2, new Color(0.87f, 0.44f, 0.92f, 1f)),
                CreateElementSymbol(6, "subspace_rift", "亚空间裂缝", CillyRoomSymbolKind.SubspaceRift, -5, new Color(0.96f, 0.22f, 0.5f, 1f)),
                CreateElementSymbol(7, "cosmic_dust", "宇宙尘埃", CillyRoomSymbolKind.CosmicDust, 0, new Color(0.56f, 0.62f, 0.68f, 1f)),
                CreateElementSymbol(8, "reality_singularity", "现实奇点", CillyRoomSymbolKind.RealitySingularity, 0, new Color(1f, 0.72f, 0.18f, 1f))
            };
        }

        private static List<CillyRoomLevelDefinition> CreateLevels(CillyRoomSymbolDefinition rewardA, CillyRoomSymbolDefinition rewardB, CillyRoomSymbolDefinition rewardC)
        {
            return new List<CillyRoomLevelDefinition>
            {
                CreateLevel(
                    "Level_01.asset",
                    "level_01",
                    "第一关：资料室遭遇",
                    "任务简报：资料室里出现了第一名敌人。你有 5 个回合，用下方框选区收集数字分数，累计达到 30 分即可击倒敌人。",
                    30,
                    5,
                    rewardA,
                    rewardB,
                    rewardC),
                CreateLevel(
                    "Level_02.asset",
                    "level_02",
                    "第二关：走廊追击",
                    "任务简报：敌人的血量条变厚了，但上一关带走的奖励数字会加入池子。继续框选数字，在 6 个回合内累计达到 48 分。",
                    48,
                    6,
                    rewardA,
                    rewardB,
                    rewardC)
            };
        }

        private static RuntimeAnimatorController CreateActorAnimatorController(string fileName)
        {
            var path = $"{AnimatorsFolder}/{fileName}";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
            {
                EnsureActorAnimatorStates(controller);
                return controller;
            }

            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            EnsureActorAnimatorStates(controller);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureActorAnimatorStates(AnimatorController controller)
        {
            EnsureTriggerParameter(controller, "Attack");
            EnsureTriggerParameter(controller, "Hit");
            EnsureTriggerParameter(controller, "Escape");

            var stateMachine = controller.layers[0].stateMachine;
            var idle = EnsureAnimatorState(stateMachine, "Idle", new Vector3(250f, 60f, 0f));
            EnsureAnimatorState(stateMachine, "Attack", new Vector3(520f, 0f, 0f));
            EnsureAnimatorState(stateMachine, "Hit", new Vector3(520f, 100f, 0f));
            EnsureAnimatorState(stateMachine, "Escape", new Vector3(520f, 200f, 0f));
            EnsureAnimatorState(stateMachine, "Defeated", new Vector3(250f, 190f, 0f));
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState EnsureAnimatorState(AnimatorStateMachine stateMachine, string stateName, Vector3 position)
        {
            foreach (var childState in stateMachine.states)
            {
                if (childState.state != null && childState.state.name == stateName)
                {
                    return childState.state;
                }
            }

            return stateMachine.AddState(stateName, position);
        }

        private static void EnsureTriggerParameter(AnimatorController controller, string parameterName)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    return;
                }
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
        }

        private static CillyRoomGameConfig CreateConfig(CillyRoomArtSet artSet, CillyRoomTextConfig textConfig, IReadOnlyList<CillyRoomSymbolDefinition> symbols, IReadOnlyList<CillyRoomLevelDefinition> levels)
        {
            return CreateOrUpdateAsset<CillyRoomGameConfig>(
                ResourcesConfigFolder + "/CillyRoomGameConfig.asset",
                asset =>
                {
                    if (asset.artSet == null)
                    {
                        asset.artSet = artSet;
                    }

                    if (asset.textConfig == null)
                    {
                        asset.textConfig = textConfig;
                    }

                    if (asset.randomSeed == 0)
                    {
                        asset.randomSeed = 2026;
                    }

                    if (!ContainsSameSymbols(asset.startingSymbols, symbols, 5))
                    {
                        asset.startingSymbols = new List<CillyRoomSymbolDefinition>
                        {
                            symbols[0], symbols[1], symbols[2], symbols[3], symbols[4]
                        };
                    }

                    if (asset.levels == null || asset.levels.Count == 0)
                    {
                        asset.levels = new List<CillyRoomLevelDefinition> { levels[0], levels[1] };
                    }
                });
        }

        private static ComponentScene BuildScene(
            Transform root,
            CillyRoomGameConfig config,
            CillyRoomArtRig artRig,
            CillyRoomArtSet artSet,
            CillyRoomTextConfig textConfig,
            RuntimeAnimatorController playerAnimatorController,
            RuntimeAnimatorController enemyAnimatorController)
        {
            var canvas = CreateCanvas(root);

            var directorObject = CreateChild(root, "CillyRoom Game Director");
            var director = directorObject.AddComponent<CillyRoomGameDirector>();

            var briefing = BuildBriefing(canvas.transform, artSet, textConfig);
            var game = BuildGame(canvas.transform, artSet, textConfig);
            var reward = BuildRewards(canvas.transform, artSet, textConfig);
            var message = BuildMessage(canvas.transform, artSet, textConfig);

            game.ui.Configure(
                game.root,
                game.levelText,
                game.scoreText,
                game.targetText,
                game.turnText,
                game.roundScoreText,
                game.detailText,
                game.hpFill,
                game.attackButton,
                message.root,
                message.titleText,
                message.bodyText,
                message.button);
            game.ui.SetTextConfig(textConfig);

            var board = game.boardObject.AddComponent<CillyRoomBoardController>();
            board.Configure(game.boardRect, game.grid, game.cellPrefab);

            var selector = game.selectorObject.AddComponent<CillyRoomSelectionController>();
            selector.Configure(game.selectorRect, board);

            var player = game.playerObject.AddComponent<CillyRoomActorController>();
            var playerAnimator = game.playerObject.AddComponent<Animator>();
            playerAnimator.runtimeAnimatorController = playerAnimatorController;
            player.Configure(game.playerImage, game.playerObject.AddComponent<SpriteRenderer>(), game.playerObject.AddComponent<Rigidbody2D>(), playerAnimator);

            var enemy = game.enemyObject.AddComponent<CillyRoomActorController>();
            var enemyAnimator = game.enemyObject.AddComponent<Animator>();
            enemyAnimator.runtimeAnimatorController = enemyAnimatorController;
            enemy.Configure(game.enemyImage, game.enemyObject.AddComponent<SpriteRenderer>(), game.enemyObject.AddComponent<Rigidbody2D>(), enemyAnimator);

            reward.controller.Configure(reward.root, reward.titleText, reward.cardsRoot, reward.skipButton, reward.optionPrefab);
            reward.controller.SetTextConfig(textConfig);
            briefing.controller.SetTextConfig(textConfig);

            game.root.SetActive(false);
            reward.root.SetActive(false);
            message.root.SetActive(false);

            return new ComponentScene
            {
                director = director,
                briefing = briefing.controller,
                ui = game.ui,
                board = board,
                selection = selector,
                player = player,
                enemy = enemy,
                rewards = reward.controller
            };
        }

        private static BriefingParts BuildBriefing(Transform canvas, CillyRoomArtSet artSet, CillyRoomTextConfig textConfig)
        {
            var root = CreatePanel(canvas, "Briefing Screen", artSet.backgroundColor, true);
            Stretch(root.rectTransform);

            var title = CreateText(root.transform, "Briefing Title", textConfig.briefingFallbackTitle, 48, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(title.rectTransform, 96f, 476f, 820f, 74f);

            var body = CreateText(root.transform, "Briefing Body", string.Empty, 26, new Color(0.92f, 0.95f, 1f, 1f), TextAnchor.UpperLeft);
            SetLowerLeft(body.rectTransform, 100f, 250f, 860f, 190f);

            var button = CreateButton(root.transform, "Continue Button", textConfig.briefingContinueButtonText, artSet.accentColor);
            SetLowerLeft(button.GetComponent<RectTransform>(), 100f, 142f, 240f, 66f);

            var controller = root.gameObject.AddComponent<CillyRoomBriefingController>();
            controller.Configure(root.gameObject, root, title, body, button);
            return new BriefingParts { controller = controller };
        }

        private static GameParts BuildGame(Transform canvas, CillyRoomArtSet artSet, CillyRoomTextConfig textConfig)
        {
            var root = CreatePanel(canvas, "Game Screen", artSet.backgroundColor, true);
            Stretch(root.rectTransform);

            var topPanel = CreatePanel(root.transform, "Top Stage Panel", new Color(0.11f, 0.12f, 0.14f, 0.98f), true);
            SetLowerLeft(topPanel.rectTransform, 16f, 542f, 1248f, 160f);

            var levelText = CreateText(topPanel.transform, "Level Text", textConfig.initialLevelText, 24, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(levelText.rectTransform, 22f, 115f, 300f, 36f);

            var hpFrame = CreatePanel(topPanel.transform, "Enemy HP Frame", new Color(0.08f, 0.08f, 0.09f, 1f), true);
            SetLowerLeft(hpFrame.rectTransform, 780f, 116f, 390f, 28f);
            var hpFill = CreatePanel(hpFrame.transform, "Enemy HP Fill", new Color(0.95f, 0.08f, 0.05f, 1f), false);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            SetLowerLeft(hpFill.rectTransform, 0f, 0f, 390f, 28f);
            var targetText = CreateText(hpFrame.transform, "Target Text", string.Empty, 18, Color.white, TextAnchor.MiddleCenter);
            Stretch(targetText.rectTransform);

            var playerObject = CreateCharacter(topPanel.transform, "Top Player Actor", 78f, 24f, 250f, 78f, Color.white, textConfig.playerLabel, out var playerImage);
            var enemyObject = CreateCharacter(topPanel.transform, "Enemy Actor", 915f, 24f, 250f, 78f, Color.white, textConfig.enemyLabel, out var enemyImage);

            var buffPanel = CreatePanel(root.transform, "Buff Item Panel", new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            SetLowerLeft(buffPanel.rectTransform, 20f, 286f, 210f, 234f);
            var buffTitle = CreateText(buffPanel.transform, "Buff Title", textConfig.buffPanelTitle, 24, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(buffTitle.rectTransform, 18f, 178f, 170f, 42f);

            var playerPanel = CreatePanel(root.transform, "Player Animation Panel", new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            SetLowerLeft(playerPanel.rectTransform, 20f, 20f, 210f, 250f);
            var playerPanelTitle = CreateText(playerPanel.transform, "Player Animation Title", textConfig.playerAnimationPanelTitle, 24, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(playerPanelTitle.rectTransform, 18f, 196f, 170f, 42f);

            var boardPanel = CreatePanel(root.transform, "Board Panel", artSet.boardColor, true);
            SetLowerLeft(boardPanel.rectTransform, 250f, 45f, 710f, 475f);
            var gridObject = CreateChild(boardPanel.transform, "Board Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            var gridRect = gridObject.GetComponent<RectTransform>();
            Stretch(gridRect);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            var cellPrefab = CreateSymbolCellPrefab(boardPanel.transform, artSet);
            cellPrefab.gameObject.SetActive(false);

            var selectorObject = CreatePanel(boardPanel.transform, "Selection Box", WithAlpha(artSet.selectorColor, 0.33333334f), false);
            selectorObject.raycastTarget = false;
            var selectorOutline = selectorObject.gameObject.AddComponent<Outline>();
            selectorOutline.effectColor = artSet.selectorOutlineColor;
            selectorOutline.effectDistance = new Vector2(3f, -3f);
            selectorOutline.useGraphicAlpha = false;
            selectorObject.transform.SetAsLastSibling();

            var rightPanel = CreatePanel(root.transform, "Right Control Panel", new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            SetLowerLeft(rightPanel.rectTransform, 985f, 45f, 275f, 475f);

            var scorePanel = CreatePanel(rightPanel.transform, "Score Panel", new Color(0.12f, 0.2f, 0.25f, 1f), true);
            SetLowerLeft(scorePanel.rectTransform, 32f, 350f, 210f, 88f);
            var scoreText = CreateText(scorePanel.transform, "Score Text", string.Empty, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(scoreText.rectTransform);

            var roundBox = CreatePanel(rightPanel.transform, "Round Score Box", new Color(0.09f, 0.16f, 0.2f, 1f), true);
            SetLowerLeft(roundBox.rectTransform, 32f, 242f, 210f, 88f);
            var roundScoreText = CreateText(roundBox.transform, "Round Score Text", textConfig.roundScoreEmptyText, 26, Color.white, TextAnchor.MiddleCenter);
            Stretch(roundScoreText.rectTransform);

            var turnBox = CreatePanel(rightPanel.transform, "Turn Box", new Color(0.16f, 0.15f, 0.13f, 1f), true);
            SetLowerLeft(turnBox.rectTransform, 56f, 126f, 162f, 96f);
            var turnText = CreateText(turnBox.transform, "Turn Text", string.Empty, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(turnText.rectTransform);

            var attackButton = CreateButton(rightPanel.transform, "Attack Button", textConfig.attackButtonText, new Color(0.78f, 0.24f, 0.18f, 1f));
            SetLowerLeft(attackButton.GetComponent<RectTransform>(), 26f, 18f, 224f, 88f);

            var detailText = CreateText(root.transform, "Selected Detail Text", textConfig.selectionHintText, 18, new Color(0.86f, 0.9f, 0.92f, 1f), TextAnchor.MiddleLeft);
            SetLowerLeft(detailText.rectTransform, 250f, 12f, 710f, 28f);

            var ui = root.gameObject.AddComponent<CillyRoomUIController>();

            return new GameParts
            {
                root = root.gameObject,
                ui = ui,
                levelText = levelText,
                scoreText = scoreText,
                targetText = targetText,
                turnText = turnText,
                roundScoreText = roundScoreText,
                detailText = detailText,
                hpFill = hpFill,
                attackButton = attackButton,
                boardObject = boardPanel.gameObject,
                boardRect = boardPanel.rectTransform,
                grid = grid,
                cellPrefab = cellPrefab,
                selectorObject = selectorObject.gameObject,
                selectorRect = selectorObject.rectTransform,
                playerObject = playerObject,
                playerImage = playerImage,
                enemyObject = enemyObject,
                enemyImage = enemyImage
            };
        }

        private static RewardParts BuildRewards(Transform canvas, CillyRoomArtSet artSet, CillyRoomTextConfig textConfig)
        {
            var root = CreatePanel(canvas, "Reward Screen", new Color(0.03f, 0.04f, 0.05f, 0.92f), true);
            Stretch(root.rectTransform);

            var title = CreateText(root.transform, "Reward Title", textConfig.rewardFallbackTitle, 40, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(title.rectTransform, 280f, 540f, 720f, 64f);

            var cardsObject = CreateChild(root.transform, "Reward Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var cards = cardsObject.GetComponent<RectTransform>();
            SetLowerLeft(cards, 331f, 278f, 618f, 210f);
            var layout = cardsObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 26f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var prefab = CreateRewardOptionPrefab(cards, artSet, textConfig);
            prefab.gameObject.SetActive(false);

            var skip = CreateButton(root.transform, "Skip Reward Button", textConfig.rewardSkipButtonText, new Color(0.25f, 0.27f, 0.3f, 1f));
            SetLowerLeft(skip.GetComponent<RectTransform>(), 520f, 168f, 240f, 62f);

            var controller = root.gameObject.AddComponent<CillyRoomRewardController>();
            return new RewardParts
            {
                root = root.gameObject,
                controller = controller,
                titleText = title,
                cardsRoot = cards,
                skipButton = skip,
                optionPrefab = prefab
            };
        }

        private static MessageParts BuildMessage(Transform canvas, CillyRoomArtSet artSet, CillyRoomTextConfig textConfig)
        {
            var root = CreatePanel(canvas, "Message Screen", new Color(0.02f, 0.02f, 0.025f, 0.88f), true);
            Stretch(root.rectTransform);

            var title = CreateText(root.transform, "Message Title", string.Empty, 42, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(title.rectTransform, 300f, 438f, 680f, 72f);

            var body = CreateText(root.transform, "Message Body", string.Empty, 24, new Color(0.9f, 0.93f, 1f, 1f), TextAnchor.MiddleCenter);
            SetLowerLeft(body.rectTransform, 310f, 310f, 660f, 92f);

            var button = CreateButton(root.transform, "Message Continue Button", textConfig.messageContinueButtonText, artSet.accentColor);
            SetLowerLeft(button.GetComponent<RectTransform>(), 520f, 216f, 240f, 62f);

            return new MessageParts { root = root.gameObject, titleText = title, bodyText = body, button = button };
        }

        private static GameObject CreateCharacter(Transform parent, string name, float x, float y, float width, float height, Color color, string label, out Image image)
        {
            var actor = CreatePanel(parent, name, color, true);
            SetLowerLeft(actor.rectTransform, x, y, width, height);
            image = actor;
            var text = CreateText(actor.transform, "Label", label, 22, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return actor.gameObject;
        }

        private static CillyRoomSymbolCellView CreateSymbolCellPrefab(Transform parent, CillyRoomArtSet artSet)
        {
            var image = CreatePanel(parent, "Symbol Cell Prefab", Color.white, true);
            var label = CreateText(image.transform, "Value Text", string.Empty, 26, Color.black, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            var view = image.gameObject.AddComponent<CillyRoomSymbolCellView>();
            view.Configure(image, label);
            return view;
        }

        private static CillyRoomRewardOptionView CreateRewardOptionPrefab(Transform parent, CillyRoomArtSet artSet, CillyRoomTextConfig textConfig)
        {
            var button = CreateButton(parent, "Reward Option Prefab", "奖励", new Color(1f, 0.72f, 0.18f, 1f));
            SetLowerLeft(button.GetComponent<RectTransform>(), 0f, 0f, 176f, 210f);
            var icon = button.GetComponent<Image>();
            var name = button.GetComponentInChildren<Text>();
            var score = CreateText(button.transform, "Score Text", textConfig.FormatRewardScore("奖励"), 20, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(score.rectTransform, 16f, 26f, 144f, 32f);
            var view = button.gameObject.AddComponent<CillyRoomRewardOptionView>();
            view.Configure(button, icon, name, score);
            view.SetTextConfig(textConfig);
            return view;
        }

        private static CillyRoomArtRig CreateArtRig(Transform parent, CillyRoomGameConfig config, CillyRoomArtSet artSet, IReadOnlyList<CillyRoomSymbolDefinition> symbols)
        {
            var rigObject = CreateChild(parent, "CillyRoom Art Rig");
            var rig = rigObject.AddComponent<CillyRoomArtRig>();
            rig.gameConfig = config;
            rig.artSet = artSet;

            EnsureArtCategory(rig.transform, "01 Backgrounds");
            EnsureArtCategory(rig.transform, "02 Player");
            EnsureArtCategory(rig.transform, "03 Enemy");
            EnsureArtCategory(rig.transform, "04 Effects");
            EnsureArtCategory(rig.transform, "05 UI Frames");
            var symbolRoot = EnsureArtCategory(rig.transform, "06 Board Symbols");

            rig.symbolSprites = new List<SymbolSpriteBinding>();
            foreach (var symbol in symbols)
            {
                EnsureArtCategory(symbolRoot, $"{symbol.displayName} Sprite Slot");
                rig.symbolSprites.Add(new SymbolSpriteBinding { symbolId = symbol.symbolId, symbol = symbol, sprite = symbol.artwork });
            }

            return rig;
        }

        private static GameObject RecreateRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            var root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Create CillyRoom Component Prototype");
            return root;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var canvasObject = CreateChild(parent, "CillyRoom Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            var image = CreatePanel(parent, name, color, true);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(image.transform, "Label", label, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Image CreatePanel(Transform parent, string name, Color color, bool addOutline)
        {
            var gameObject = CreateChild(parent, name, typeof(RectTransform), typeof(Image));
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            if (addOutline)
            {
                var outline = gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.82f, 0.9f, 0.96f, 0.42f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Color color, TextAnchor alignment)
        {
            var gameObject = CreateChild(parent, name, typeof(RectTransform), typeof(Text));
            var text = gameObject.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, size);
            text.text = value;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
        {
            var gameObject = new GameObject(name, components);
            gameObject.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(gameObject, "Create CillyRoom Scene Object");
            return gameObject;
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

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static CillyRoomSymbolDefinition CreateElementSymbol(int value, string symbolId, string displayName, CillyRoomSymbolKind kind, int baseScore, Color color)
        {
            return CreateOrUpdateAsset<CillyRoomSymbolDefinition>(
                $"{SymbolsFolder}/Number_{value}.asset",
                asset =>
                {
                    asset.symbolId = symbolId;
                    asset.displayName = displayName;
                    asset.kind = kind;
                    asset.baseScore = baseScore;
                    asset.tintColor = color;
                    asset.effect = CillyRoomSymbolEffect.None;
                    asset.effectMultiplier = 2;
                    asset.effectIncludesDiagonals = true;
                });
        }

        private static CillyRoomLevelDefinition CreateLevel(
            string fileName,
            string id,
            string displayName,
            string briefing,
            int targetScore,
            int turns,
            CillyRoomSymbolDefinition rewardA,
            CillyRoomSymbolDefinition rewardB,
            CillyRoomSymbolDefinition rewardC)
        {
            var path = $"{LevelsFolder}/{fileName}";
            var asset = AssetDatabase.LoadAssetAtPath<CillyRoomLevelDefinition>(path);
            var isNew = asset == null;
            if (isNew)
            {
                asset = CreateInstance<CillyRoomLevelDefinition>();
                AssetDatabase.CreateAsset(asset, path);
                asset.displayName = displayName;
                asset.briefingText = briefing;
            }

            if (string.IsNullOrWhiteSpace(asset.levelId))
            {
                asset.levelId = id;
            }

            asset.enemyTargetScore = asset.enemyTargetScore <= 0 ? targetScore : asset.enemyTargetScore;
            asset.maxTurns = asset.maxTurns <= 0 ? turns : asset.maxTurns;
            asset.boardColumns = asset.boardColumns <= 0 ? 9 : asset.boardColumns;
            asset.boardRows = asset.boardRows <= 0 ? 5 : asset.boardRows;
            asset.selectionWidth = asset.selectionWidth < 3 ? 3 : asset.selectionWidth;
            asset.selectionHeight = asset.selectionHeight < 3 ? 3 : asset.selectionHeight;

            asset.rewardChoices = new List<CillyRoomSymbolDefinition> { rewardA, rewardB, rewardC };

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static bool ContainsSameSymbols(IReadOnlyList<CillyRoomSymbolDefinition> currentSymbols, IReadOnlyList<CillyRoomSymbolDefinition> expectedSymbols, int expectedCount)
        {
            if (currentSymbols == null || currentSymbols.Count != expectedCount)
            {
                return false;
            }

            for (int i = 0; i < expectedCount; i++)
            {
                if (currentSymbols[i] != expectedSymbols[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static T CreateOrUpdateAsset<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            configure(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T CreateAssetIfMissing<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Transform EnsureArtCategory(Transform parent, string name)
        {
            var child = CreateChild(parent, name);
            return child.transform;
        }

        private static void EnsureFolders()
        {
            EnsureFolder(RootFolder, "Generated");
            EnsureFolder(GeneratedFolder, "Resources");
            EnsureFolder(ResourcesFolder, "CillyRoomPrototype");
            EnsureFolder(GeneratedFolder, "Symbols");
            EnsureFolder(GeneratedFolder, "Levels");
            EnsureFolder(GeneratedFolder, "Animators");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private sealed class ComponentScene
        {
            public CillyRoomGameDirector director;
            public CillyRoomBriefingController briefing;
            public CillyRoomUIController ui;
            public CillyRoomBoardController board;
            public CillyRoomSelectionController selection;
            public CillyRoomActorController player;
            public CillyRoomActorController enemy;
            public CillyRoomRewardController rewards;
        }

        private sealed class BriefingParts
        {
            public CillyRoomBriefingController controller;
        }

        private sealed class GameParts
        {
            public GameObject root;
            public CillyRoomUIController ui;
            public Text levelText;
            public Text scoreText;
            public Text targetText;
            public Text turnText;
            public Text roundScoreText;
            public Text detailText;
            public Image hpFill;
            public Button attackButton;
            public GameObject boardObject;
            public RectTransform boardRect;
            public GridLayoutGroup grid;
            public CillyRoomSymbolCellView cellPrefab;
            public GameObject selectorObject;
            public RectTransform selectorRect;
            public GameObject playerObject;
            public Image playerImage;
            public GameObject enemyObject;
            public Image enemyImage;
        }

        private sealed class RewardParts
        {
            public GameObject root;
            public CillyRoomRewardController controller;
            public Text titleText;
            public Transform cardsRoot;
            public Button skipButton;
            public CillyRoomRewardOptionView optionPrefab;
        }

        private sealed class MessageParts
        {
            public GameObject root;
            public Text titleText;
            public Text bodyText;
            public Button button;
        }
    }
}
