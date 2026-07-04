using System.Collections.Generic;
using UnityEngine;

namespace CillyRoomPrototype
{
    [CreateAssetMenu(menuName = "CillyRoom/Level", fileName = "CillyRoomLevel")]
    public sealed class CillyRoomLevelDefinition : ScriptableObject
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

        [Header("Optional Art Overrides")]
        public Sprite briefingBackgroundOverride;
        public Sprite enemySpriteOverride;

        [Header("Reward Choices")]
        public List<CillyRoomSymbolDefinition> rewardChoices = new List<CillyRoomSymbolDefinition>();

        public int SafeColumns => Mathf.Max(1, boardColumns);
        public int SafeRows => Mathf.Max(1, boardRows);
        public int SafeSelectionWidth => Mathf.Clamp(selectionWidth, 1, SafeColumns);
        public int SafeSelectionHeight => Mathf.Clamp(selectionHeight, 1, SafeRows);
        public int SafeTurns => Mathf.Max(1, maxTurns);
        public int SafeTargetScore => Mathf.Max(1, enemyTargetScore);
    }
}
