using UnityEngine;

namespace Subspace
{
    public enum SubspaceSymbolEffect
    {
        None,
        MultiplyAdjacentSelectedSymbols
    }

    public enum SubspaceSymbolKind
    {
        Generic,
        Beacon,
        LifeSignal,
        Anchor,
        EnergyCore,
        Turbulence,
        SubspaceRift,
        CosmicDust,
        RealitySingularity
    }

    [CreateAssetMenu(menuName = "Subspace/Symbol", fileName = "SubspaceSymbol")]
    public sealed class SubspaceSymbolDefinition : ScriptableObject
    {
        public string symbolId = "number_1";
        public string displayName = "1";
        public SubspaceSymbolKind kind = SubspaceSymbolKind.Generic;
        public int baseScore = 1;
        public Color tintColor = Color.white;
        public Sprite artwork;

       [Header("Prototype Effect Hook")]
       public SubspaceSymbolEffect effect = SubspaceSymbolEffect.None;
       public int effectMultiplier = 2;
       public bool effectIncludesDiagonals = true;

       [Header("Synergy")]
       public string synergyTag = string.Empty;
       public int synergyBonus = 0;
       public string synergyWith = string.Empty;

       public string SafeId => string.IsNullOrWhiteSpace(symbolId) ? name : symbolId;
        public string SafeDisplayName => string.IsNullOrWhiteSpace(displayName) ? SafeId : displayName;
        public SubspaceSymbolKind SafeKind => kind != SubspaceSymbolKind.Generic ? kind : GuessKind(SafeId);
        public int SafeBaseScore => baseScore;
        public int SafeMultiplier => Mathf.Max(1, effectMultiplier);
        public Color SafeTint => tintColor.a <= 0.01f ? Color.white : tintColor;

        private static SubspaceSymbolKind GuessKind(string id)
        {
            switch (id)
            {
                case "beacon":
                    return SubspaceSymbolKind.Beacon;
                case "life_signal":
                    return SubspaceSymbolKind.LifeSignal;
                case "anchor":
                    return SubspaceSymbolKind.Anchor;
                case "energy_core":
                    return SubspaceSymbolKind.EnergyCore;
                case "turbulence":
                    return SubspaceSymbolKind.Turbulence;
                case "subspace_rift":
                    return SubspaceSymbolKind.SubspaceRift;
                case "cosmic_dust":
                    return SubspaceSymbolKind.CosmicDust;
                case "reality_singularity":
                    return SubspaceSymbolKind.RealitySingularity;
                default:
                    return SubspaceSymbolKind.Generic;
            }
        }
    }
}
