using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    public enum SubspaceUpgradeType
    {
        ScannerShape,
        AnchorEffectBoost,
        ResourceScoreBoost,
        FirstScanDouble,
        PollutionReduction,
        ExtraScan,
        SignalStabilization,
        Overload,
        LimitScan,
        PreserveOutside,
        TimeRewind,
        EnergySurvey,
        CleanerRobot,
        ChaosConversion,
        DamageControl,
        LastStand
    }

    [CreateAssetMenu(menuName = "Subspace/Upgrade", fileName = "SubspaceUpgrade")]
    public sealed class SubspaceUpgradeDefinition : ScriptableObject
    {
        public string upgradeId = "upgrade_1";
        public string displayName = "Upgrade";
        [TextArea] public string description = string.Empty;
        public SubspaceUpgradeType type = SubspaceUpgradeType.ScannerShape;

        [Header("Scanner Shape")]
        public List<Vector2Int> shapeOffsets = new List<Vector2Int>();

        [Header("Numeric Parameters")]
        public float floatParam = 0f;
        public int intParam = 0;

        public string SafeId => string.IsNullOrWhiteSpace(upgradeId) ? name : upgradeId;
    }
}
