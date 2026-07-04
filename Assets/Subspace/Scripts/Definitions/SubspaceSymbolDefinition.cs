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
        RealitySingularity,
        SignalNode,
        TornSpace,
        Overclock,
        Data,
        Prism,
        GravityFlow,
        EnergyShard,
        MultidimensionalAnalysis,
        EnergyTransition,
        ResonanceSignal,
        ChaosSignal,
        VoidSignal,
        BlockingSignal,
        RealityAnchor,
        SignalBoostPoint,
        DoubleExcitation,
        GrowthNode,
        RealityLink,
        SpaceTurbulenceField,
        EnergyElement,
        SignalSacrifice,
        DataFlow,
        CosmicPrism,
        StableField,
        HotCore,
        MagneticField,
        ChaosField,
        SignalConversion,
        SignalEnhancer,
        ChaosStance
    }

    public enum SubspaceElementCategory
    {
        Resource,
        Anchor,
        Threat
    }

    public enum SubspaceElementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    [CreateAssetMenu(menuName = "Subspace/Symbol", fileName = "SubspaceSymbol")]
    public sealed class SubspaceSymbolDefinition : ScriptableObject
    {
        public string symbolId = "number_1";
        public string displayName = "1";
        public SubspaceSymbolKind kind = SubspaceSymbolKind.Generic;
        public SubspaceElementCategory category = SubspaceElementCategory.Resource;
        public SubspaceElementRarity rarity = SubspaceElementRarity.Common;
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
        public SubspaceElementCategory SafeCategory => category;
        public SubspaceElementRarity SafeRarity => rarity;
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
                case "signal_node":
                    return SubspaceSymbolKind.SignalNode;
                case "torn_space":
                    return SubspaceSymbolKind.TornSpace;
                case "overclock":
                    return SubspaceSymbolKind.Overclock;
                case "data":
                    return SubspaceSymbolKind.Data;
                case "prism":
                    return SubspaceSymbolKind.Prism;
                case "gravity_flow":
                    return SubspaceSymbolKind.GravityFlow;
                case "energy_shard":
                    return SubspaceSymbolKind.EnergyShard;
                case "multidimensional_analysis":
                    return SubspaceSymbolKind.MultidimensionalAnalysis;
                case "energy_transition":
                    return SubspaceSymbolKind.EnergyTransition;
                case "resonance_signal":
                    return SubspaceSymbolKind.ResonanceSignal;
                case "chaos_signal":
                    return SubspaceSymbolKind.ChaosSignal;
                case "void_signal":
                    return SubspaceSymbolKind.VoidSignal;
                case "blocking_signal":
                    return SubspaceSymbolKind.BlockingSignal;
                case "reality_anchor":
                    return SubspaceSymbolKind.RealityAnchor;
                case "signal_boost_point":
                    return SubspaceSymbolKind.SignalBoostPoint;
                case "double_excitation":
                    return SubspaceSymbolKind.DoubleExcitation;
                case "growth_node":
                    return SubspaceSymbolKind.GrowthNode;
                case "reality_link":
                    return SubspaceSymbolKind.RealityLink;
                case "space_turbulence_field":
                    return SubspaceSymbolKind.SpaceTurbulenceField;
                case "energy_element":
                    return SubspaceSymbolKind.EnergyElement;
                case "signal_sacrifice":
                    return SubspaceSymbolKind.SignalSacrifice;
                case "data_flow":
                    return SubspaceSymbolKind.DataFlow;
                case "cosmic_prism":
                    return SubspaceSymbolKind.CosmicPrism;
                case "stable_field":
                    return SubspaceSymbolKind.StableField;
                case "hot_core":
                    return SubspaceSymbolKind.HotCore;
                case "magnetic_field":
                    return SubspaceSymbolKind.MagneticField;
                case "chaos_field":
                    return SubspaceSymbolKind.ChaosField;
                case "signal_conversion":
                    return SubspaceSymbolKind.SignalConversion;
                case "signal_enhancer":
                    return SubspaceSymbolKind.SignalEnhancer;
                case "chaos_stance":
                    return SubspaceSymbolKind.ChaosStance;
                default:
                    return SubspaceSymbolKind.Generic;
            }
        }
    }
}
