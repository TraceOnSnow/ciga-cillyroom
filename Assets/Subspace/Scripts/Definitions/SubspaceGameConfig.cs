using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    [CreateAssetMenu(menuName = "Subspace/Game Config", fileName = "SubspaceGameConfig")]
    public sealed class SubspaceGameConfig : ScriptableObject
    {
        public SubspaceArtSet artSet;
        public SubspaceTextConfig textConfig;
        public bool useFixedSeed;
        public int randomSeed = 2026;
       public List<SubspaceSymbolDefinition> startingSymbols = new List<SubspaceSymbolDefinition>();
       public List<SubspaceLevelDefinition> levels = new List<SubspaceLevelDefinition>();
       public List<SubspaceUpgradeDefinition> upgradePool = new List<SubspaceUpgradeDefinition>();
   }
}
