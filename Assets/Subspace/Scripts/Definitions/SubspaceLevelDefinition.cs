using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

namespace Subspace
{
    public enum SubspaceMonsterPressureType
    {
        None,
        SpreadPollution,
        ErodeStrongestTile,
        JamScanner,
        CollapseAnchors
    }

    [CreateAssetMenu(menuName = "Subspace/Level", fileName = "SubspaceLevel")]
    public sealed class SubspaceLevelDefinition : ScriptableObject
    {
        public string levelId = "level_01";
        public string displayName = "第一关";

        [TextArea(3, 7)]
        public string briefingText = "任务简报：在有限回合内框选数字，累计达到敌人血条要求。";

        public int enemyTargetScore = 30;
        public int maxTurns = 5;
        public int boardColumns = 9;
        public int boardRows = 5;
        public int selectionWidth = 2;
        public int selectionHeight = 2;

        [Header("Monster")]
        public string monsterId = "subspace_larva";
        public string monsterDisplayName = "\u4e9a\u7a7a\u95f4\u5e7c\u4f53";
        public SubspaceMonsterPressureType monsterPressureType = SubspaceMonsterPressureType.SpreadPollution;
        public int monsterPressureAmount = 1;
        public GameObject enemyFailureAttackEffectPrefab;
        public Vector2 enemyFailureAttackEffectOffset = new Vector2(0f, -48f);
        public Vector2 enemyFailureAttackEffectSize = new Vector2(180f, 180f);
        public float enemyFailureAttackEffectDuration = 0.65f;

        [Header("Optional Art Overrides")]
        public Sprite briefingBackgroundOverride;
        public Sprite enemySpriteOverride;

        [Header("Enemy Spine Override")]
        public SkeletonDataAsset enemySpineSkeleton;
        public Material enemySpineMaterial;
        public string enemySpineIdleAnimation = "stand";
        public string enemySpineAttackAnimation = "attack";
        public string enemySpineHitAnimation = "stand";
        public string enemySpineDefeatedAnimation = "stand";
        public Vector2 enemySpineAnchoredPosition = new Vector2(0f, -24f);
        public Vector2 enemySpineSize = new Vector2(220f, 150f);
        public Vector3 enemySpineScale = Vector3.one;

        [Header("Reward Choices")]
        public List<SubspaceSymbolDefinition> rewardChoices = new List<SubspaceSymbolDefinition>();

        public int SafeColumns => Mathf.Max(1, boardColumns);
        public int SafeRows => Mathf.Max(1, boardRows);
        public int SafeSelectionWidth => Mathf.Clamp(selectionWidth, 1, SafeColumns);
        public int SafeSelectionHeight => Mathf.Clamp(selectionHeight, 1, SafeRows);
        public int SafeTurns => Mathf.Max(1, maxTurns);
        public int SafeTargetScore => Mathf.Max(1, enemyTargetScore);
    }
}
