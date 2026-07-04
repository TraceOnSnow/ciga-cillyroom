using UnityEngine;

namespace CillyRoomPrototype
{
    public enum CillyRoomSymbolEffect
    {
        None,
        MultiplyAdjacentSelectedSymbols
    }

    public enum CillyRoomSymbolKind
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

    [CreateAssetMenu(menuName = "CillyRoom/Symbol", fileName = "CillyRoomSymbol")]
    public sealed class CillyRoomSymbolDefinition : ScriptableObject
    {
        public string symbolId = "number_1";
        public string displayName = "1";
        public CillyRoomSymbolKind kind = CillyRoomSymbolKind.Generic;
        public int baseScore = 1;
        public Color tintColor = Color.white;
        public Sprite artwork;

        [Header("Prototype Effect Hook")]
        public CillyRoomSymbolEffect effect = CillyRoomSymbolEffect.None;
        public int effectMultiplier = 2;
        public bool effectIncludesDiagonals = true;

        public string SafeId => string.IsNullOrWhiteSpace(symbolId) ? name : symbolId;
        public string SafeDisplayName => string.IsNullOrWhiteSpace(displayName) ? SafeId : displayName;
        public CillyRoomSymbolKind SafeKind => kind != CillyRoomSymbolKind.Generic ? kind : GuessKind(SafeId);
        public int SafeBaseScore => baseScore;
        public int SafeMultiplier => Mathf.Max(1, effectMultiplier);
        public Color SafeTint => tintColor.a <= 0.01f ? Color.white : tintColor;

        private static CillyRoomSymbolKind GuessKind(string id)
        {
            switch (id)
            {
                case "beacon":
                    return CillyRoomSymbolKind.Beacon;
                case "life_signal":
                    return CillyRoomSymbolKind.LifeSignal;
                case "anchor":
                    return CillyRoomSymbolKind.Anchor;
                case "energy_core":
                    return CillyRoomSymbolKind.EnergyCore;
                case "turbulence":
                    return CillyRoomSymbolKind.Turbulence;
                case "subspace_rift":
                    return CillyRoomSymbolKind.SubspaceRift;
                case "cosmic_dust":
                    return CillyRoomSymbolKind.CosmicDust;
                case "reality_singularity":
                    return CillyRoomSymbolKind.RealitySingularity;
                default:
                    return CillyRoomSymbolKind.Generic;
            }
        }
    }
}
