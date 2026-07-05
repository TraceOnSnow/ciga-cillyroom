using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Spine.Unity;

namespace Subspace.Editor
{
    public sealed class SubspaceGeneratorWindow : EditorWindow
    {
        private const string RootFolder = "Assets/Subspace";
        private const string GeneratedFolder = RootFolder + "/Generated";
        private const string ResourcesFolder = GeneratedFolder + "/Resources";
        private const string ResourcesConfigFolder = ResourcesFolder + "/Subspace";
        private const string SymbolsFolder = GeneratedFolder + "/Symbols";
        private const string LevelsFolder = GeneratedFolder + "/Levels";
        private const string AnimatorsFolder = GeneratedFolder + "/Animators";
        private const string MusicFolder = "Assets/Art/Audio/Music";
        private const string SfxFolder = "Assets/Art/Audio/SFX";
        private const string SymbolIconFolder = "Assets/Art/Land/图标";
        private const string DisappearPrefabPath = "Assets/Art/Land/Disappear.prefab";
        private const string MonsterOneSkeletonPath = "Assets/Art/Monster/Monster_1/MONSTER1_SkeletonData.asset";
        private const string MonsterOneMaterialPath = "Assets/Art/Monster/Monster_1/MONSTER1_Material.mat";
        private const string MonsterThreeSkeletonPath = "Assets/Art/Monster/Monster_3/skeleton_SkeletonData.asset";
        private const string MonsterThreeMaterialPath = "Assets/Art/Monster/Monster_3/skeleton_Material.mat";
        private const string MonsterThreeAttackEffectPath = "Assets/Art/Monster/Monster_3/Mon3ATK.prefab";

        [MenuItem("Subspace/原型生成器")]
        public static void Open()
        {
            GetWindow<SubspaceGeneratorWindow>("Subspace 原型");
        }

        [MenuItem("Subspace/一键生成两个关卡")]
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
           var levels = CreateLevels(symbols);
           var upgrades = CreateUpgrades();
           var config = CreateConfig(artSet, textConfig, symbols, levels, upgrades);
            var playerAnimatorController = CreateActorAnimatorController("PlayerActor.controller");
            var enemyAnimatorController = CreateActorAnimatorController("EnemyActor.controller");

            var root = RecreateRoot("Subspace Component Prototype");
            var artRig = CreateArtRig(root.transform, config, artSet, symbols);
            var scene = BuildScene(root.transform, config, artRig, artSet, textConfig, playerAnimatorController, enemyAnimatorController);

           scene.director.Configure(
               config,
               artRig,
               scene.briefing,
               scene.menu,
               scene.pauseMenu,
               scene.ui,
               scene.audio,
               scene.board,
               scene.selection,
               scene.player,
               scene.enemy,
               scene.rewards);
            scene.director.ConfigureBeamPlacement(scene.beamStartPoint, scene.beamEndPoint);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Subspace", "已生成组件化 GameObject 版本。请在 Hierarchy 中查看 Subspace Component Prototype。", "好");
            }
        }

        private static SubspaceArtSet CreateArtSet()
        {
            return CreateAssetIfMissing<SubspaceArtSet>(
                ResourcesConfigFolder + "/SubspaceArtSet.asset",
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

        private static SubspaceTextConfig CreateTextConfig()
        {
            return CreateAssetIfMissing<SubspaceTextConfig>(
                ResourcesConfigFolder + "/SubspaceTextConfig.asset",
                asset => { });
        }

        private static List<SubspaceSymbolDefinition> CreateSymbols()
        {
            return new List<SubspaceSymbolDefinition>
            {
                CreateElementSymbol(1, "signal_node", "信号节点", SubspaceSymbolKind.SignalNode, SubspaceElementCategory.Resource, SubspaceElementRarity.Common, 10, new Color(0.38f, 0.78f, 0.96f, 1f)),
                CreateElementSymbol(2, "data", "数据", SubspaceSymbolKind.Data, SubspaceElementCategory.Resource, SubspaceElementRarity.Common, 12, new Color(0.42f, 0.68f, 1f, 1f)),
                CreateElementSymbol(3, "gravity_flow", "引力流", SubspaceSymbolKind.GravityFlow, SubspaceElementCategory.Resource, SubspaceElementRarity.Common, 14, new Color(0.5f, 0.72f, 0.92f, 1f)),
                CreateElementSymbol(4, "chaos_signal", "混乱信号", SubspaceSymbolKind.ChaosSignal, SubspaceElementCategory.Resource, SubspaceElementRarity.Common, 10, new Color(0.86f, 0.46f, 0.9f, 1f)),
                CreateElementSymbol(5, "void_signal", "虚无信号", SubspaceSymbolKind.VoidSignal, SubspaceElementCategory.Resource, SubspaceElementRarity.Common, 20, new Color(0.55f, 0.55f, 0.66f, 1f)),
                CreateElementSymbol(6, "torn_space", "撕裂空间", SubspaceSymbolKind.TornSpace, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 30, new Color(0.9f, 0.36f, 0.44f, 1f)),
                CreateElementSymbol(7, "overclock", "超频", SubspaceSymbolKind.Overclock, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 30, new Color(0.96f, 0.58f, 0.24f, 1f)),
                CreateElementSymbol(8, "prism", "棱镜", SubspaceSymbolKind.Prism, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 6, new Color(0.72f, 0.84f, 1f, 1f)),
                CreateElementSymbol(9, "energy_shard", "能量碎片", SubspaceSymbolKind.EnergyShard, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 10, new Color(1f, 0.78f, 0.28f, 1f)),
                CreateElementSymbol(10, "multidimensional_analysis", "多维分析", SubspaceSymbolKind.MultidimensionalAnalysis, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 6, new Color(0.48f, 0.9f, 0.74f, 1f)),
                CreateElementSymbol(11, "resonance_signal", "共振信号", SubspaceSymbolKind.ResonanceSignal, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 8, new Color(0.68f, 0.62f, 1f, 1f)),
                CreateElementSymbol(12, "blocking_signal", "阻塞信号", SubspaceSymbolKind.BlockingSignal, SubspaceElementCategory.Resource, SubspaceElementRarity.Uncommon, 30, new Color(0.4f, 0.45f, 0.5f, 1f)),
                CreateElementSymbol(13, "energy_transition", "能量跃迁", SubspaceSymbolKind.EnergyTransition, SubspaceElementCategory.Resource, SubspaceElementRarity.Epic, 5, new Color(1f, 0.92f, 0.35f, 1f)),
                CreateElementSymbol(14, "reality_anchor", "现实锚点", SubspaceSymbolKind.RealityAnchor, SubspaceElementCategory.Anchor, SubspaceElementRarity.Common, 0, new Color(0.44f, 0.86f, 0.48f, 1f)),
                CreateElementSymbol(15, "magnetic_field", "磁场", SubspaceSymbolKind.MagneticField, SubspaceElementCategory.Anchor, SubspaceElementRarity.Common, 2, new Color(0.34f, 0.72f, 0.76f, 1f)),
                CreateElementSymbol(16, "signal_conversion", "信号转换", SubspaceSymbolKind.SignalConversion, SubspaceElementCategory.Anchor, SubspaceElementRarity.Common, 3, new Color(0.42f, 0.76f, 0.95f, 1f)),
                CreateElementSymbol(17, "signal_enhancer", "信号加强器", SubspaceSymbolKind.SignalEnhancer, SubspaceElementCategory.Anchor, SubspaceElementRarity.Common, 3, new Color(0.3f, 0.82f, 0.62f, 1f)),
                CreateElementSymbol(18, "signal_boost_point", "信号加强点", SubspaceSymbolKind.SignalBoostPoint, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 2, new Color(0.32f, 0.88f, 0.72f, 1f)),
                CreateElementSymbol(19, "growth_node", "成长节点", SubspaceSymbolKind.GrowthNode, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 2, new Color(0.54f, 0.9f, 0.38f, 1f)),
                CreateElementSymbol(20, "reality_link", "现实链接", SubspaceSymbolKind.RealityLink, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 1, new Color(0.52f, 0.78f, 1f, 1f)),
                CreateElementSymbol(21, "signal_sacrifice", "信号献祭", SubspaceSymbolKind.SignalSacrifice, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 1, new Color(0.9f, 0.52f, 0.48f, 1f)),
                CreateElementSymbol(22, "data_flow", "数据流", SubspaceSymbolKind.DataFlow, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 2, new Color(0.46f, 0.62f, 1f, 1f)),
                CreateElementSymbol(23, "stable_field", "稳定场", SubspaceSymbolKind.StableField, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 2, new Color(0.66f, 0.92f, 0.86f, 1f)),
                CreateElementSymbol(24, "hot_core", "炎热核心", SubspaceSymbolKind.HotCore, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 3, new Color(1f, 0.46f, 0.26f, 1f)),
                CreateElementSymbol(25, "chaos_field", "混乱场", SubspaceSymbolKind.ChaosField, SubspaceElementCategory.Anchor, SubspaceElementRarity.Uncommon, 2, new Color(0.8f, 0.42f, 0.86f, 1f)),
                CreateElementSymbol(26, "double_excitation", "双重激发", SubspaceSymbolKind.DoubleExcitation, SubspaceElementCategory.Anchor, SubspaceElementRarity.Epic, 1, new Color(1f, 0.84f, 0.42f, 1f)),
                CreateElementSymbol(27, "space_turbulence_field", "空间乱流", SubspaceSymbolKind.SpaceTurbulenceField, SubspaceElementCategory.Anchor, SubspaceElementRarity.Epic, 0, new Color(0.88f, 0.36f, 0.92f, 1f)),
                CreateElementSymbol(28, "energy_element", "能量元素", SubspaceSymbolKind.EnergyElement, SubspaceElementCategory.Anchor, SubspaceElementRarity.Epic, 2, new Color(1f, 0.74f, 0.22f, 1f)),
                CreateElementSymbol(29, "cosmic_prism", "宇宙棱镜", SubspaceSymbolKind.CosmicPrism, SubspaceElementCategory.Anchor, SubspaceElementRarity.Epic, 2, new Color(0.72f, 0.78f, 1f, 1f)),
                CreateElementSymbol(30, "chaos_stance", "混乱立场", SubspaceSymbolKind.ChaosStance, SubspaceElementCategory.Anchor, SubspaceElementRarity.Epic, 0, new Color(0.72f, 0.26f, 0.82f, 1f))
            };
        }

        private static List<SubspaceLevelDefinition> CreateLevels(IReadOnlyList<SubspaceSymbolDefinition> symbols)
        {
            return new List<SubspaceLevelDefinition>
            {
                CreateLevel(
                    "Level_01.asset",
                    "level_01",
                    "第一关：幼体侵蚀",
                    "任务简报：亚空间幼体开始污染局部地图。在有限回合内建立第一个现实锚点。",
                    300,
                    8,
                    "subspace_larva",
                    "亚空间幼体",
                    SubspaceMonsterPressureType.SpreadPollution,
                    1,
                    symbols[5],
                    symbols[6],
                    symbols[7]),
                CreateLevel(
                    "Level_02.asset",
                    "level_02",
                    "第二关：噬能团块",
                    "任务简报：噬能团块会优先侵蚀高收益地块。利用资源联动撑过压力。",
                    600,
                    9,
                    "energy_devourer",
                    "噬能团块",
                    SubspaceMonsterPressureType.ErodeStrongestTile,
                    2,
                    symbols[0],
                    symbols[6],
                    symbols[7]),
                CreateLevel(
                    "Level_03.asset",
                    "level_03",
                    "第三关：干扰者",
                    "任务简报：干扰者会扰乱扫描路径，使地图成长变得不稳定。",
                    900,
                    10,
                    "scanner_jammer",
                    "扫描干扰者",
                    SubspaceMonsterPressureType.JamScanner,
                    2,
                    symbols[1],
                    symbols[3],
                    symbols[7]),
                CreateLevel(
                    "Level_04.asset",
                    "level_04",
                    "第四关：锚点吞噬者",
                    "任务简报：锚点吞噬者会削弱已经培养出的现实网络。",
                    1200,
                    11,
                    "anchor_devourer",
                    "锚点吞噬者",
                    SubspaceMonsterPressureType.CollapseAnchors,
                    1,
                    symbols[2],
                    symbols[3],
                    symbols[7]),
                CreateLevel(
                    "Level_05.asset",
                    "level_05",
                    "第五关：亚空间核心",
                    "任务简报：亚空间核心持续释放高强度污染。锁定最终现实坐标。",
                    2000,
                    12,
                    "subspace_core",
                    "亚空间核心",
                    SubspaceMonsterPressureType.SpreadPollution,
                    3,
                    symbols[2],
                    symbols[5],
                    symbols[7])
            };
       }

       private static List<SubspaceUpgradeDefinition> CreateUpgrades()
       {
           var folder = GeneratedFolder + "/Upgrades";
           EnsureFolder(GeneratedFolder, "Upgrades");

           var scanner2x3 = CreateUpgradeAsset($"{folder}/Scanner_2x3.asset", "scanner_2x3",
               "\u626b\u63cf\u5668 2\u00d73",
               "\u626b\u63cf\u8303\u56f4\u6269\u5927\u4e3a 2\u00d73\u3002",
               SubspaceUpgradeType.ScannerShape,
               new List<Vector2Int>
               {
                   new Vector2Int(0, 0), new Vector2Int(1, 0),
                   new Vector2Int(0, 1), new Vector2Int(1, 1),
                   new Vector2Int(0, 2)
               });

           var scanner3x3 = CreateUpgradeAsset($"{folder}/Scanner_3x3.asset", "scanner_3x3",
               "\u626b\u63cf\u5668 3\u00d73",
               "\u626b\u63cf\u8303\u56f4\u6269\u5927\u4e3a 3\u00d73\u3002",
               SubspaceUpgradeType.ScannerShape,
               new List<Vector2Int>
               {
                   new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                   new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                   new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
               });

           var crossShape = CreateUpgradeAsset($"{folder}/Scanner_Cross.asset", "scanner_cross",
               "\u5341\u5b57\u626b\u63cf\u5668",
               "\u626b\u63cf\u8303\u56f4\u53d8\u4e3a\u5341\u5b57\u5f62\u3002",
               SubspaceUpgradeType.ScannerShape,
               new List<Vector2Int>
               {
                   new Vector2Int(1, 0),
                   new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                   new Vector2Int(1, 2)
               });

           var resourceBoost = CreateUpgradeAsset($"{folder}/Resource_Boost.asset", "resource_boost_20",
               "\u8d44\u6e90\u52a0\u6210",
               "\u6240\u6709\u8d44\u6e90\u5143\u7d20\u5373\u65f6\u5f97\u5206 +20%\u3002",
               SubspaceUpgradeType.ResourceScoreBoost,
               floatParam: 0.2f);

           var anchorBoost = CreateUpgradeAsset($"{folder}/Anchor_Boost.asset", "anchor_effect_50",
               "\u951a\u5b9a\u589e\u5f3a",
               "\u951a\u5b9a\u5143\u7d20\u5730\u5757\u6548\u679c +50%\u3002",
               SubspaceUpgradeType.AnchorEffectBoost,
               floatParam: 0.5f);

           var firstScanDouble = CreateUpgradeAsset($"{folder}/First_Scan_Double.asset", "first_scan_double",
               "\u9996\u6b21\u626b\u63cf\u7ffb\u500d",
               "\u6bcf\u5173\u9996\u6b21\u7ed3\u7b97\u5f97\u5206 x2\u3002",
               SubspaceUpgradeType.FirstScanDouble);

           var pollutionReduction = CreateUpgradeAsset($"{folder}/Pollution_Reduction.asset", "pollution_reduction_50",
               "\u6c61\u67d3\u51c0\u5316",
               "Debuff \u9020\u6210\u7684\u8d1f\u5206\u964d\u4f4e 50%\u3002",
               SubspaceUpgradeType.PollutionReduction,
               floatParam: 0.5f);

           var extraScan = CreateUpgradeAsset($"{folder}/Extra_Scan.asset", "extra_scan",
               "\u989d\u5916\u626b\u63cf",
               "\u6bcf\u56de\u5408\u53ef\u989d\u5916\u626b\u63cf\u4e00\u6b21\u3002",
               SubspaceUpgradeType.ExtraScan,
               intParam: 2);

            var overload = CreateUpgradeAsset($"{folder}/Overload.asset", "overload",
                "\u8fc7\u8f7d",
                "\u8d44\u6e90\u5143\u7d20\u5373\u65f6\u5f97\u5206 +10%\u3002",
                SubspaceUpgradeType.ResourceScoreBoost,
                floatParam: 0.1f);

            var damageControl = CreateUpgradeAsset($"{folder}/Damage_Control.asset", "damage_control",
                "\u5373\u65f6\u6b62\u635f",
                "Debuff \u6548\u679c\u51cf\u534a\u3002",
                SubspaceUpgradeType.PollutionReduction,
                floatParam: 0.5f);

            var doubleScan = CreateUpgradeAsset($"{folder}/Double_Scan.asset", "double_scan",
                "\u53cc\u91cd\u626b\u63cf",
                "\u6bcf\u56de\u5408\u53ef\u626b\u63cf\u4e24\u6b21\u3002",
                SubspaceUpgradeType.ExtraScan,
                intParam: 2);

            return new List<SubspaceUpgradeDefinition>
            {
                scanner2x3,
                scanner3x3,
                crossShape,
                resourceBoost,
                anchorBoost,
                firstScanDouble,
                pollutionReduction,
                extraScan,
                overload,
                damageControl,
                doubleScan
            };
       }

       private static SubspaceUpgradeDefinition CreateUpgradeAsset(
           string path,
           string id,
           string displayName,
           string description,
           SubspaceUpgradeType type,
           List<Vector2Int> shapeOffsets = null,
           float floatParam = 0f,
           int intParam = 0)
       {
           return CreateOrUpdateAsset<SubspaceUpgradeDefinition>(path, asset =>
           {
               asset.upgradeId = id;
               asset.displayName = displayName;
               asset.description = description;
               asset.type = type;
               asset.shapeOffsets = shapeOffsets ?? new List<Vector2Int>();
               asset.floatParam = floatParam;
               asset.intParam = intParam;
           });
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
            EnsureAnimatorState(stateMachine, "Stand", new Vector3(250f, -40f, 0f));
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

       private static SubspaceGameConfig CreateConfig(SubspaceArtSet artSet, SubspaceTextConfig textConfig, IReadOnlyList<SubspaceSymbolDefinition> symbols, IReadOnlyList<SubspaceLevelDefinition> levels, IReadOnlyList<SubspaceUpgradeDefinition> upgrades)
        {
            return CreateOrUpdateAsset<SubspaceGameConfig>(
                ResourcesConfigFolder + "/SubspaceGameConfig.asset",
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

                    if (asset.startingSymbols == null || asset.startingSymbols.Count != symbols.Count)
                    {
                        asset.startingSymbols = new List<SubspaceSymbolDefinition>(symbols);
                    }

                   if (asset.levels == null || asset.levels.Count != levels.Count)
                   {
                       asset.levels = new List<SubspaceLevelDefinition>(levels);
                   }

                   if (asset.upgradePool == null)
                   {
                       asset.upgradePool = new List<SubspaceUpgradeDefinition>();
                   }

                   foreach (var upgrade in upgrades)
                   {
                       if (upgrade != null && !asset.upgradePool.Contains(upgrade))
                       {
                           asset.upgradePool.Add(upgrade);
                       }
                   }
               });
        }

        private static ComponentScene BuildScene(
            Transform root,
            SubspaceGameConfig config,
            SubspaceArtRig artRig,
            SubspaceArtSet artSet,
            SubspaceTextConfig textConfig,
            RuntimeAnimatorController playerAnimatorController,
            RuntimeAnimatorController enemyAnimatorController)
        {
            var canvas = CreateCanvas(root);

            var directorObject = CreateChild(root, "Subspace Game Director");
            var director = directorObject.AddComponent<SubspaceGameDirector>();

           var briefing = BuildBriefing(canvas.transform, artSet, textConfig);
           var menu = BuildMenu(canvas.transform, artSet, textConfig);
           var game = BuildGame(canvas.transform, artSet, textConfig);
            var reward = BuildRewards(canvas.transform, artSet, textConfig);
            var message = BuildMessage(canvas.transform, artSet, textConfig);
            var pause = BuildPauseMenu(canvas.transform, artSet, textConfig);
            var audio = BuildAudio(root);

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
            game.ui.SetUpgradeText(game.upgradeText);
            game.ui.SetTextConfig(textConfig);
            game.ui.SetAudioController(audio);

            var board = game.boardObject.AddComponent<SubspaceBoardController>();
            board.Configure(game.boardRect, game.grid, game.cellPrefab);
            board.SetDisappearPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DisappearPrefabPath));

            var selector = game.selectorObject.AddComponent<SubspaceSelectionController>();
            selector.Configure(game.selectorRect, board);

            var player = game.playerObject.AddComponent<SubspaceActorController>();
            var playerAnimator = game.playerObject.AddComponent<Animator>();
            playerAnimator.runtimeAnimatorController = playerAnimatorController;
            player.Configure(game.playerImage, game.playerObject.AddComponent<SpriteRenderer>(), game.playerObject.AddComponent<Rigidbody2D>(), playerAnimator);
            player.AddMirrorImage(game.playerPortraitImage);

            var enemy = game.enemyObject.AddComponent<SubspaceActorController>();
            var enemyAnimator = game.enemyObject.AddComponent<Animator>();
            var enemySpine = CreateEnemySpineView(game.enemyObject.transform);
            enemyAnimator.runtimeAnimatorController = enemyAnimatorController;
            enemy.Configure(game.enemyImage, game.enemyObject.AddComponent<SpriteRenderer>(), game.enemyObject.AddComponent<Rigidbody2D>(), enemyAnimator, enemySpine);
            enemy.SetAnimatorStateNames("Stand", "Attack", "Hit", "Defeated");

            reward.controller.Configure(reward.root, reward.titleText, reward.cardsRoot, reward.skipButton, reward.optionPrefab);
           reward.controller.SetTextConfig(textConfig);
           briefing.controller.SetTextConfig(textConfig);
           menu.controller.SetTextConfig(textConfig);
           pause.controller.SetTextConfig(textConfig);
           menu.controller.SetAudioController(audio);
           pause.controller.SetAudioController(audio);

           game.root.SetActive(false);
           reward.root.SetActive(false);
           message.root.SetActive(false);
           menu.root.SetActive(false);
           pause.root.SetActive(false);

           return new ComponentScene
           {
               director = director,
               briefing = briefing.controller,
               menu = menu.controller,
               pauseMenu = pause.controller,
               ui = game.ui,
               audio = audio,
                board = board,
                selection = selector,
                player = player,
                enemy = enemy,
                rewards = reward.controller,
                beamStartPoint = game.beamStartPoint,
                beamEndPoint = game.beamEndPoint
            };
        }

        private static SubspaceAudioController BuildAudio(Transform root)
        {
            var audioObject = CreateChild(root, "AudioManager");
            var musicSource = audioObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            var sfxSource = audioObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            var controller = audioObject.AddComponent<SubspaceAudioController>();
            controller.Configure(
                musicSource,
                sfxSource,
                LoadAudioClip($"{MusicFolder}/\u6e38\u620f\u80cc\u666f\u97f3\u4e50.m4a"),
                LoadAudioClip($"{SfxFolder}/\u9009\u62e9\u97f3\u6548.mp3"),
                LoadAudioClip($"{SfxFolder}/\u786e\u5b9a\u97f3\u6548.mp3"),
                LoadAudioClip($"{SfxFolder}/\u70b9\u51fb\u653b\u51fb\u97f3\u6548.mp3"),
                LoadAudioClip($"{SfxFolder}/\u73a9\u5bb6\u80dc\u5229\u9003\u8131\u97f3\u6548.mp3"),
                LoadAudioClip($"{SfxFolder}/\u602a\u7269\u6b7b\u4ea1\u97f3\u6548.mp3"),
                LoadAudioClip($"{SfxFolder}/\u98de\u8239\u7206\u70b8\u97f3\u6548.mp3"),
                LoadAudioClip($"{SfxFolder}/3\u90091\u51fa\u73b0\u97f3\u6548.mp3"));
            return controller;
        }

        private static BriefingParts BuildBriefing(Transform canvas, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
        {
            var root = CreatePanel(canvas, "Briefing Screen", artSet.backgroundColor, true);
            Stretch(root.rectTransform);

            var title = CreateText(root.transform, "Briefing Title", textConfig.briefingFallbackTitle, 48, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(title.rectTransform, 96f, 476f, 820f, 74f);

            var body = CreateText(root.transform, "Briefing Body", string.Empty, 26, new Color(0.92f, 0.95f, 1f, 1f), TextAnchor.UpperLeft);
            SetLowerLeft(body.rectTransform, 100f, 250f, 860f, 190f);

            var button = CreateButton(root.transform, "Continue Button", textConfig.briefingContinueButtonText, artSet.accentColor);
            SetLowerLeft(button.GetComponent<RectTransform>(), 100f, 142f, 240f, 66f);

            var controller = root.gameObject.AddComponent<SubspaceBriefingController>();
           controller.Configure(root.gameObject, root, title, body, button);
           return new BriefingParts { controller = controller };
       }

       private static MenuParts BuildMenu(Transform canvas, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
       {
           var root = CreatePanel(canvas, "Menu Screen", artSet.backgroundColor, true);
           Stretch(root.rectTransform);

           var title = CreateText(root.transform, "Menu Title", textConfig.menuTitle, 64, Color.white, TextAnchor.MiddleCenter);
           SetLowerLeft(title.rectTransform, 240f, 380f, 800f, 90f);

           var subtitle = CreateText(root.transform, "Menu Subtitle", textConfig.menuSubtitle, 28, new Color(0.7f, 0.78f, 0.86f, 1f), TextAnchor.MiddleCenter);
           SetLowerLeft(subtitle.rectTransform, 240f, 320f, 800f, 50f);

           var button = CreateButton(root.transform, "Start Button", textConfig.menuStartButtonText, artSet.accentColor);
           SetLowerLeft(button.GetComponent<RectTransform>(), 520f, 236f, 240f, 58f);

           var settingsButton = CreateButton(root.transform, "Settings Button", textConfig.menuSettingsButtonText, new Color(0.16f, 0.42f, 0.55f, 1f));
           SetLowerLeft(settingsButton.GetComponent<RectTransform>(), 520f, 164f, 240f, 58f);

           var exitButton = CreateButton(root.transform, "Exit Button", textConfig.menuExitButtonText, new Color(0.46f, 0.18f, 0.18f, 1f));
           SetLowerLeft(exitButton.GetComponent<RectTransform>(), 520f, 92f, 240f, 58f);

           var settingsPanel = CreatePanel(root.transform, "Settings Panel", new Color(0.06f, 0.07f, 0.08f, 0.94f), true);
           SetLowerLeft(settingsPanel.rectTransform, 425f, 205f, 430f, 310f);
           var settingsTitle = CreateText(settingsPanel.transform, "Settings Title", textConfig.settingsTitleText, 30, Color.white, TextAnchor.MiddleCenter);
           SetLowerLeft(settingsTitle.rectTransform, 45f, 242f, 340f, 46f);
           var musicLabel = CreateText(settingsPanel.transform, "Music Volume Label", textConfig.musicVolumeText, 20, Color.white, TextAnchor.MiddleLeft);
           SetLowerLeft(musicLabel.rectTransform, 46f, 174f, 130f, 34f);
           var musicSlider = CreateSlider(settingsPanel.transform, "Music Volume Slider", 0.8f, artSet.accentColor);
           SetLowerLeft(musicSlider.GetComponent<RectTransform>(), 176f, 180f, 210f, 22f);
           var sfxLabel = CreateText(settingsPanel.transform, "SFX Volume Label", textConfig.sfxVolumeText, 20, Color.white, TextAnchor.MiddleLeft);
           SetLowerLeft(sfxLabel.rectTransform, 46f, 110f, 130f, 34f);
           var sfxSlider = CreateSlider(settingsPanel.transform, "SFX Volume Slider", 0.8f, artSet.accentColor);
           SetLowerLeft(sfxSlider.GetComponent<RectTransform>(), 176f, 116f, 210f, 22f);
           var settingsClose = CreateButton(settingsPanel.transform, "Settings Close Button", textConfig.settingsCloseButtonText, new Color(0.16f, 0.42f, 0.55f, 1f));
           SetLowerLeft(settingsClose.GetComponent<RectTransform>(), 125f, 28f, 180f, 52f);
           settingsPanel.gameObject.SetActive(false);

           var controller = root.gameObject.AddComponent<SubspaceMenuController>();
           controller.Configure(root.gameObject, root, title, subtitle, button);
           controller.ConfigureButtons(settingsButton, exitButton);
           controller.ConfigureSettings(settingsPanel.gameObject, settingsTitle, musicLabel, musicSlider, sfxLabel, sfxSlider, settingsClose);
           return new MenuParts { root = root.gameObject, controller = controller };
       }

       private static PauseMenuParts BuildPauseMenu(Transform canvas, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
       {
           var root = CreatePanel(canvas, "Pause Options Menu", new Color(0.02f, 0.025f, 0.03f, 0.9f), true);
           Stretch(root.rectTransform);

           var panel = CreatePanel(root.transform, "Options Panel", new Color(0.09f, 0.1f, 0.12f, 0.96f), true);
           SetLowerLeft(panel.rectTransform, 445f, 190f, 390f, 340f);

           var title = CreateText(panel.transform, "Options Title", textConfig.pauseTitleText, 34, Color.white, TextAnchor.MiddleCenter);
           SetLowerLeft(title.rectTransform, 45f, 278f, 300f, 48f);

           var musicLabel = CreateText(panel.transform, "Music Volume Label", textConfig.musicVolumeText, 18, Color.white, TextAnchor.MiddleLeft);
           SetLowerLeft(musicLabel.rectTransform, 42f, 234f, 120f, 30f);
           var musicSlider = CreateSlider(panel.transform, "Music Volume Slider", 0.8f, artSet.accentColor);
           SetLowerLeft(musicSlider.GetComponent<RectTransform>(), 166f, 240f, 190f, 20f);

           var sfxLabel = CreateText(panel.transform, "SFX Volume Label", textConfig.sfxVolumeText, 18, Color.white, TextAnchor.MiddleLeft);
           SetLowerLeft(sfxLabel.rectTransform, 42f, 194f, 120f, 30f);
           var sfxSlider = CreateSlider(panel.transform, "SFX Volume Slider", 0.8f, artSet.accentColor);
           SetLowerLeft(sfxSlider.GetComponent<RectTransform>(), 166f, 200f, 190f, 20f);

           var resume = CreateButton(panel.transform, "Resume Button", textConfig.pauseResumeButtonText, artSet.accentColor);
           SetLowerLeft(resume.GetComponent<RectTransform>(), 75f, 124f, 240f, 52f);

           var mainMenu = CreateButton(panel.transform, "Main Menu Button", textConfig.pauseMainMenuButtonText, new Color(0.16f, 0.42f, 0.55f, 1f));
           SetLowerLeft(mainMenu.GetComponent<RectTransform>(), 75f, 64f, 240f, 52f);

           var exit = CreateButton(panel.transform, "Exit Game Button", textConfig.pauseExitButtonText, new Color(0.46f, 0.18f, 0.18f, 1f));
           SetLowerLeft(exit.GetComponent<RectTransform>(), 75f, 4f, 240f, 52f);

           var controller = root.gameObject.AddComponent<SubspacePauseMenuController>();
           controller.Configure(root.gameObject, title, mainMenu, exit, resume);
           controller.ConfigureSliders(musicLabel, musicSlider, sfxLabel, sfxSlider);
           return new PauseMenuParts { root = root.gameObject, controller = controller };
       }

       private static GameParts BuildGame(Transform canvas, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
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
            var enemyObject = CreateCharacter(topPanel.transform, "Enemy Actor", 915f, 6f, 250f, 78f, Color.white, textConfig.enemyLabel, out var enemyImage);
            var beamStartPoint = CreateBeamPoint(playerObject.transform, "Player Beam Start Point", 96f, 38f);
            var beamEndPoint = CreateBeamPoint(enemyObject.transform, "Enemy Beam Hit Point", 26f, 38f);

            var buffPanel = CreatePanel(root.transform, "Buff Item Panel", new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            SetLowerLeft(buffPanel.rectTransform, 20f, 286f, 210f, 234f);
            var buffTitle = CreateText(buffPanel.transform, "Buff Title", textConfig.buffPanelTitle, 24, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(buffTitle.rectTransform, 18f, 178f, 170f, 42f);
            var upgradeText = CreateText(buffPanel.transform, "Selected Upgrades Text", "\u9053\u5177/\u589e\u76ca\n-", 15, new Color(0.92f, 0.95f, 1f, 1f), TextAnchor.UpperLeft);
            SetLowerLeft(upgradeText.rectTransform, 18f, 20f, 174f, 158f);

            var playerPanel = CreatePanel(root.transform, "Player Animation Panel", new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            SetLowerLeft(playerPanel.rectTransform, 20f, 20f, 210f, 250f);
            var playerPanelTitle = CreateText(playerPanel.transform, "Player Animation Title", textConfig.playerAnimationPanelTitle, 24, Color.white, TextAnchor.MiddleLeft);
            SetLowerLeft(playerPanelTitle.rectTransform, 18f, 196f, 170f, 42f);
            var playerPortrait = CreatePanel(playerPanel.transform, "Player Portrait", Color.white, false);
            playerPortrait.preserveAspect = true;
            playerPortrait.raycastTarget = false;
            playerPortrait.color = Color.clear;
            SetLowerLeft(playerPortrait.rectTransform, 26f, 24f, 158f, 164f);

            var boardPanel = CreatePanel(root.transform, "Board Panel", artSet.boardColor, true);
            SetLowerLeft(boardPanel.rectTransform, 250f, 45f, 475f, 475f);
            var gridObject = CreateChild(boardPanel.transform, "Board Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            var gridRect = gridObject.GetComponent<RectTransform>();
            Stretch(gridRect);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.spacing = new Vector2(6f, 6f);

            var cellPrefab = CreateSymbolCellPrefab(boardPanel.transform, artSet);
            cellPrefab.gameObject.SetActive(false);

            var selectorObject = CreatePanel(boardPanel.transform, "Selection Box", WithAlpha(artSet.selectorColor, 0.33333334f), false);
            selectorObject.raycastTarget = false;
            var selectorOutline = selectorObject.gameObject.AddComponent<Outline>();
            selectorOutline.effectColor = WithAlpha(artSet.selectorOutlineColor, 0.33333334f);
            selectorOutline.effectDistance = new Vector2(3f, -3f);
            selectorOutline.useGraphicAlpha = false;
            selectorObject.transform.SetAsLastSibling();

            var rightPanel = CreatePanel(root.transform, "Right Control Panel", new Color(0.12f, 0.13f, 0.15f, 0.98f), true);
            SetLowerLeft(rightPanel.rectTransform, 985f, 45f, 275f, 475f);

            var scorePanel = CreatePanel(rightPanel.transform, "Score Panel", new Color(0.12f, 0.2f, 0.25f, 1f), true);
            SetLowerLeft(scorePanel.rectTransform, 32f, 350f, 210f, 88f);
            var scoreText = CreateText(scorePanel.transform, "Score Text", "\u4e0a\u6b21\u4f24\u5bb3\n-", 24, Color.white, TextAnchor.MiddleCenter);
            scoreText.raycastTarget = true;
            scoreText.gameObject.AddComponent<SubspaceDamageTooltipTarget>();
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

            var ui = root.gameObject.AddComponent<SubspaceUIController>();

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
                upgradeText = upgradeText,
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
                playerPortraitImage = playerPortrait,
                enemyObject = enemyObject,
                enemyImage = enemyImage,
                beamStartPoint = beamStartPoint,
                beamEndPoint = beamEndPoint
            };
        }

        private static RewardParts BuildRewards(Transform canvas, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
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

            var controller = root.gameObject.AddComponent<SubspaceRewardController>();
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

        private static MessageParts BuildMessage(Transform canvas, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
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
            image.preserveAspect = true;
            var text = CreateText(actor.transform, "Label", label, 22, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return actor.gameObject;
        }

        private static Transform CreateBeamPoint(Transform parent, string name, float x, float y)
        {
            var point = CreateChild(parent, name, typeof(RectTransform));
            var rect = point.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(12f, 12f);
            return point.transform;
        }

        private static SubspaceSpineActorView CreateEnemySpineView(Transform parent)
        {
            var spineObject = CreateChild(parent, "Enemy Spine Graphic", typeof(RectTransform), typeof(CanvasRenderer));
            var rect = spineObject.GetComponent<RectTransform>();
            Stretch(rect);
            rect.anchoredPosition = new Vector2(0f, -42f);
            rect.sizeDelta = new Vector2(180f, 180f);

            var components = SkeletonGraphic.AddSkeletonGraphicAnimationComponents(spineObject, LoadMonsterOneSkeleton(), LoadMonsterOneMaterial(), true);
            var graphic = components.skeletonRenderer;
            graphic.raycastTarget = false;
            graphic.skeletonDataAsset = LoadMonsterOneSkeleton();
            var material = LoadMonsterOneMaterial();
            if (material != null)
            {
                graphic.material = material;
            }

            var view = spineObject.AddComponent<SubspaceSpineActorView>();
            view.Configure(graphic, components.skeletonAnimation);
            spineObject.SetActive(false);
            return view;
        }

        private static SubspaceSymbolCellView CreateSymbolCellPrefab(Transform parent, SubspaceArtSet artSet)
        {
            var image = CreatePanel(parent, "Symbol Cell Prefab", Color.white, true);
            var label = CreateText(image.transform, "Value Text", string.Empty, 26, Color.black, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            var view = image.gameObject.AddComponent<SubspaceSymbolCellView>();
            view.Configure(image, label);
            return view;
        }

        private static SubspaceRewardOptionView CreateRewardOptionPrefab(Transform parent, SubspaceArtSet artSet, SubspaceTextConfig textConfig)
        {
            var button = CreateButton(parent, "Reward Option Prefab", "奖励", new Color(1f, 0.72f, 0.18f, 1f));
            SetLowerLeft(button.GetComponent<RectTransform>(), 0f, 0f, 176f, 210f);
            var iconObject = CreateChild(button.transform, "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var icon = iconObject.GetComponent<Image>();
            icon.color = Color.clear;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            SetLowerLeft(icon.rectTransform, 46f, 86f, 84f, 84f);
            var name = button.GetComponentInChildren<Text>();
            var score = CreateText(button.transform, "Score Text", textConfig.FormatRewardScore("奖励"), 20, Color.white, TextAnchor.MiddleCenter);
            SetLowerLeft(score.rectTransform, 16f, 26f, 144f, 32f);
            var view = button.gameObject.AddComponent<SubspaceRewardOptionView>();
            view.Configure(button, icon, name, score);
            view.SetTextConfig(textConfig);
            return view;
        }

        private static SubspaceArtRig CreateArtRig(Transform parent, SubspaceGameConfig config, SubspaceArtSet artSet, IReadOnlyList<SubspaceSymbolDefinition> symbols)
        {
            var rigObject = CreateChild(parent, "Subspace Art Rig");
            var rig = rigObject.AddComponent<SubspaceArtRig>();
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
            Undo.RegisterCreatedObjectUndo(root, "Create Subspace Component Prototype");
            return root;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var canvasObject = CreateChild(parent, "Subspace Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            image.gameObject.AddComponent<SubspaceButtonAudio>();
            var text = CreateText(image.transform, "Label", label, 24, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, float value, Color fillColor)
        {
            var sliderObject = CreateChild(parent, name, typeof(RectTransform), typeof(Slider));
            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);

            var background = CreatePanel(sliderObject.transform, "Background", new Color(0.16f, 0.18f, 0.2f, 1f), false);
            Stretch(background.rectTransform);
            background.raycastTarget = true;

            var fillArea = CreateChild(sliderObject.transform, "Fill Area", typeof(RectTransform));
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(6f, 0f);
            fillAreaRect.offsetMax = new Vector2(-6f, 0f);

            var fill = CreatePanel(fillArea.transform, "Fill", fillColor, false);
            Stretch(fill.rectTransform);

            var handleArea = CreateChild(sliderObject.transform, "Handle Slide Area", typeof(RectTransform));
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            Stretch(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(8f, -6f);
            handleAreaRect.offsetMax = new Vector2(-8f, 6f);

            var handle = CreatePanel(handleArea.transform, "Handle", Color.white, true);
            SetLowerLeft(handle.rectTransform, 0f, -6f, 18f, 34f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            return slider;
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
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Subspace Scene Object");
            return gameObject;
        }

        private static AudioClip LoadAudioClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
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

        private static SubspaceSymbolDefinition CreateElementSymbol(
            int value,
            string symbolId,
            string displayName,
            SubspaceSymbolKind kind,
            SubspaceElementCategory category,
            SubspaceElementRarity rarity,
            int baseScore,
            Color color)
        {
            return CreateOrUpdateAsset<SubspaceSymbolDefinition>(
                $"{SymbolsFolder}/{symbolId}.asset",
                asset =>
                {
                    asset.symbolId = symbolId;
                    asset.displayName = displayName;
                    asset.kind = kind;
                    asset.category = category;
                    asset.rarity = rarity;
                    asset.baseScore = baseScore;
                    asset.tintColor = color;
                    asset.artwork = LoadSymbolArtwork(displayName);
                    asset.effect = SubspaceSymbolEffect.None;
                    asset.effectMultiplier = 2;
                    asset.effectIncludesDiagonals = true;
                    asset.synergyTag = kind == SubspaceSymbolKind.Beacon || kind == SubspaceSymbolKind.EnergyCore ? SubspaceElementRules.CrystalEnergySynergyTag : string.Empty;
                    asset.synergyBonus = kind == SubspaceSymbolKind.Beacon || kind == SubspaceSymbolKind.EnergyCore ? 5 : 0;
                    asset.synergyWith = kind == SubspaceSymbolKind.Beacon ? "energy_core" : kind == SubspaceSymbolKind.EnergyCore ? "beacon" : string.Empty;
                });
        }

        private static Sprite LoadSymbolArtwork(string displayName)
        {
            var iconName = GetSymbolIconName(displayName);
            var path = $"{SymbolIconFolder}/{iconName}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static string GetSymbolIconName(string displayName)
        {
            switch (displayName)
            {
                case "信号加强点":
                    return "信号加强";
                case "信号加强器":
                    return "信号增强";
                case "现实链接":
                    return "现实连接";
                case "能量跃迁":
                    return "能量迁越";
                case "双重激发":
                    return "双重激光";
                case "信号献祭":
                    return "献祭信号";
                default:
                    return displayName;
            }
        }

        private static SubspaceLevelDefinition CreateLevel(
            string fileName,
            string id,
            string displayName,
            string briefing,
            int targetScore,
            int turns,
            string monsterId,
            string monsterDisplayName,
            SubspaceMonsterPressureType monsterPressureType,
            int monsterPressureAmount,
            SubspaceSymbolDefinition rewardA,
            SubspaceSymbolDefinition rewardB,
            SubspaceSymbolDefinition rewardC)
        {
            var path = $"{LevelsFolder}/{fileName}";
            var asset = AssetDatabase.LoadAssetAtPath<SubspaceLevelDefinition>(path);
            var isNew = asset == null;
            if (isNew)
            {
                asset = CreateInstance<SubspaceLevelDefinition>();
                AssetDatabase.CreateAsset(asset, path);
                asset.displayName = displayName;
                asset.briefingText = briefing;
            }

            if (string.IsNullOrWhiteSpace(asset.levelId))
            {
                asset.levelId = id;
            }

            asset.displayName = displayName;
            asset.briefingText = briefing;
            asset.enemyTargetScore = targetScore;
            asset.maxTurns = turns;
            asset.boardColumns = 6;
            asset.boardRows = 6;
            asset.selectionWidth = 2;
            asset.selectionHeight = 2;
            asset.monsterId = monsterId;
            asset.monsterDisplayName = monsterDisplayName;
            asset.monsterPressureType = monsterPressureType;
            asset.monsterPressureAmount = monsterPressureAmount;
            asset.enemyFailureAttackEffectOffset = asset.enemyFailureAttackEffectOffset == Vector2.zero ? new Vector2(0f, -48f) : asset.enemyFailureAttackEffectOffset;
            asset.enemyFailureAttackEffectSize = asset.enemyFailureAttackEffectSize == Vector2.zero ? new Vector2(180f, 180f) : asset.enemyFailureAttackEffectSize;
            asset.enemyFailureAttackEffectDuration = asset.enemyFailureAttackEffectDuration <= 0f ? 0.65f : asset.enemyFailureAttackEffectDuration;

            if (id == "level_01")
            {
                asset.enemySpineSkeleton = asset.enemySpineSkeleton == null ? LoadMonsterOneSkeleton() : asset.enemySpineSkeleton;
                asset.enemySpineMaterial = asset.enemySpineMaterial == null ? LoadMonsterOneMaterial() : asset.enemySpineMaterial;
                asset.enemySpineIdleAnimation = string.IsNullOrWhiteSpace(asset.enemySpineIdleAnimation) ? "stand" : asset.enemySpineIdleAnimation;
                asset.enemySpineAttackAnimation = string.IsNullOrWhiteSpace(asset.enemySpineAttackAnimation) ? "attack" : asset.enemySpineAttackAnimation;
                asset.enemySpineHitAnimation = string.IsNullOrWhiteSpace(asset.enemySpineHitAnimation) ? asset.enemySpineIdleAnimation : asset.enemySpineHitAnimation;
                asset.enemySpineDefeatedAnimation = string.IsNullOrWhiteSpace(asset.enemySpineDefeatedAnimation) ? asset.enemySpineIdleAnimation : asset.enemySpineDefeatedAnimation;
                asset.enemySpineAnchoredPosition = asset.enemySpineAnchoredPosition == Vector2.zero ? new Vector2(0f, -24f) : asset.enemySpineAnchoredPosition;
                asset.enemySpineSize = new Vector2(180f, 180f);
                asset.enemySpineScale = asset.enemySpineScale == Vector3.zero ? Vector3.one : asset.enemySpineScale;
            }
            else if (id == "level_03")
            {
                asset.enemySpineSkeleton = asset.enemySpineSkeleton == null ? LoadMonsterThreeSkeleton() : asset.enemySpineSkeleton;
                asset.enemySpineMaterial = asset.enemySpineMaterial == null ? LoadMonsterThreeMaterial() : asset.enemySpineMaterial;
                asset.enemySpineIdleAnimation = string.IsNullOrWhiteSpace(asset.enemySpineIdleAnimation) ? "stand" : asset.enemySpineIdleAnimation;
                asset.enemySpineAttackAnimation = string.IsNullOrWhiteSpace(asset.enemySpineAttackAnimation) ? "attack" : asset.enemySpineAttackAnimation;
                asset.enemySpineHitAnimation = string.IsNullOrWhiteSpace(asset.enemySpineHitAnimation) ? asset.enemySpineIdleAnimation : asset.enemySpineHitAnimation;
                asset.enemySpineDefeatedAnimation = string.IsNullOrWhiteSpace(asset.enemySpineDefeatedAnimation) ? asset.enemySpineIdleAnimation : asset.enemySpineDefeatedAnimation;
                asset.enemySpineAnchoredPosition = new Vector2(0f, -62f);
                asset.enemySpineSize = new Vector2(220f, 180f);
                asset.enemySpineScale = asset.enemySpineScale == Vector3.zero ? Vector3.one : asset.enemySpineScale;
                asset.enemyFailureAttackEffectPrefab = asset.enemyFailureAttackEffectPrefab == null ? LoadMonsterThreeAttackEffect() : asset.enemyFailureAttackEffectPrefab;
                asset.enemyFailureAttackEffectOffset = new Vector2(42f, 30f);
                asset.enemyFailureAttackEffectSize = new Vector2(210f, 190f);
                asset.enemyFailureAttackEffectDuration = 0.65f;
            }

            asset.rewardChoices = new List<SubspaceSymbolDefinition> { rewardA, rewardB, rewardC };

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static SkeletonDataAsset LoadMonsterOneSkeleton()
        {
            return AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(MonsterOneSkeletonPath);
        }

        private static Material LoadMonsterOneMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MonsterOneMaterialPath);
        }

        private static SkeletonDataAsset LoadMonsterThreeSkeleton()
        {
            return AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(MonsterThreeSkeletonPath);
        }

        private static Material LoadMonsterThreeMaterial()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MonsterThreeMaterialPath);
        }

        private static GameObject LoadMonsterThreeAttackEffect()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(MonsterThreeAttackEffectPath);
        }

        private static bool ContainsSameSymbols(IReadOnlyList<SubspaceSymbolDefinition> currentSymbols, IReadOnlyList<SubspaceSymbolDefinition> expectedSymbols, int expectedCount, int expectedStartIndex = 0)
        {
            if (currentSymbols == null || currentSymbols.Count != expectedCount)
            {
                return false;
            }

            for (int i = 0; i < expectedCount; i++)
            {
                int expectedIndex = expectedStartIndex + i;
                if (expectedIndex < 0 || expectedIndex >= expectedSymbols.Count || currentSymbols[i] != expectedSymbols[expectedIndex])
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
            EnsureFolder(ResourcesFolder, "Subspace");
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
           public SubspaceGameDirector director;
           public SubspaceBriefingController briefing;
           public SubspaceMenuController menu;
           public SubspacePauseMenuController pauseMenu;
           public SubspaceUIController ui;
           public SubspaceAudioController audio;
            public SubspaceBoardController board;
            public SubspaceSelectionController selection;
            public SubspaceActorController player;
            public SubspaceActorController enemy;
            public SubspaceRewardController rewards;
            public Transform beamStartPoint;
            public Transform beamEndPoint;
        }

       private sealed class BriefingParts
       {
           public SubspaceBriefingController controller;
       }

       private sealed class MenuParts
       {
           public GameObject root;
           public SubspaceMenuController controller;
       }

       private sealed class PauseMenuParts
       {
           public GameObject root;
           public SubspacePauseMenuController controller;
       }

       private sealed class GameParts
        {
            public GameObject root;
            public SubspaceUIController ui;
            public Text levelText;
            public Text scoreText;
            public Text targetText;
            public Text turnText;
            public Text roundScoreText;
            public Text detailText;
            public Text upgradeText;
            public Image hpFill;
            public Button attackButton;
            public GameObject boardObject;
            public RectTransform boardRect;
            public GridLayoutGroup grid;
            public SubspaceSymbolCellView cellPrefab;
            public GameObject selectorObject;
            public RectTransform selectorRect;
            public GameObject playerObject;
            public Image playerImage;
            public Image playerPortraitImage;
            public GameObject enemyObject;
            public Image enemyImage;
            public Transform beamStartPoint;
            public Transform beamEndPoint;
        }

        private sealed class RewardParts
        {
            public GameObject root;
            public SubspaceRewardController controller;
            public Text titleText;
            public Transform cardsRoot;
            public Button skipButton;
            public SubspaceRewardOptionView optionPrefab;
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
