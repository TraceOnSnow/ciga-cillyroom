using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    public static class SubspaceElementRules
    {
        public const string CrystalEnergySynergyTag = "crystal_energy";

        public static int GetInstantScore(SubspaceSymbolDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            switch (definition.SafeKind)
            {
                case SubspaceSymbolKind.SignalNode:
                case SubspaceSymbolKind.Data:
                case SubspaceSymbolKind.GravityFlow:
                case SubspaceSymbolKind.ChaosSignal:
                case SubspaceSymbolKind.VoidSignal:
                case SubspaceSymbolKind.TornSpace:
                case SubspaceSymbolKind.Overclock:
                case SubspaceSymbolKind.Prism:
                case SubspaceSymbolKind.EnergyShard:
                case SubspaceSymbolKind.MultidimensionalAnalysis:
                case SubspaceSymbolKind.ResonanceSignal:
                case SubspaceSymbolKind.BlockingSignal:
                case SubspaceSymbolKind.EnergyTransition:
                case SubspaceSymbolKind.RealityAnchor:
                case SubspaceSymbolKind.MagneticField:
                case SubspaceSymbolKind.SignalConversion:
                case SubspaceSymbolKind.SignalEnhancer:
                case SubspaceSymbolKind.SignalBoostPoint:
                case SubspaceSymbolKind.GrowthNode:
                case SubspaceSymbolKind.RealityLink:
                case SubspaceSymbolKind.SignalSacrifice:
                case SubspaceSymbolKind.DataFlow:
                case SubspaceSymbolKind.StableField:
                case SubspaceSymbolKind.HotCore:
                case SubspaceSymbolKind.ChaosField:
                case SubspaceSymbolKind.DoubleExcitation:
                case SubspaceSymbolKind.SpaceTurbulenceField:
                case SubspaceSymbolKind.EnergyElement:
                case SubspaceSymbolKind.CosmicPrism:
                case SubspaceSymbolKind.ChaosStance:
                    return definition.SafeBaseScore;
                case SubspaceSymbolKind.Beacon:
                    return 20;
                case SubspaceSymbolKind.LifeSignal:
                    return 8;
                case SubspaceSymbolKind.Anchor:
                    return 15;
                case SubspaceSymbolKind.EnergyCore:
                    return 25;
                case SubspaceSymbolKind.Turbulence:
                    return 45;
                case SubspaceSymbolKind.SubspaceRift:
                    return -20;
                case SubspaceSymbolKind.CosmicDust:
                    return 10;
                case SubspaceSymbolKind.RealitySingularity:
                    return 35;
                default:
                    return definition.SafeBaseScore;
            }
        }

        public static bool IsResource(SubspaceSymbolDefinition definition)
        {
            return definition != null && definition.SafeCategory == SubspaceElementCategory.Resource;
        }

        public static void ApplyTileEffects(SubspaceSymbolDefinition definition, SubspaceTileData tile)
        {
            if (definition == null || tile == null)
            {
                return;
            }

            switch (definition.SafeKind)
            {
                case SubspaceSymbolKind.RealityAnchor:
                    tile.baseBonusScore += 5;
                    break;
                case SubspaceSymbolKind.SignalBoostPoint:
                case SubspaceSymbolKind.SignalEnhancer:
                    tile.baseBonusScore += 3;
                    break;
                case SubspaceSymbolKind.DoubleExcitation:
                    tile.baseBonusScore += 4;
                    break;
                case SubspaceSymbolKind.GrowthNode:
                    tile.baseBonusScore += 2;
                    break;
                case SubspaceSymbolKind.RealityLink:
                    tile.baseBonusScore += Random.Range(1, 4);
                    break;
                case SubspaceSymbolKind.SpaceTurbulenceField:
                    tile.baseBonusScore -= 1;
                    AddStack(tile.buffs, SubspaceTileBuffType.EnergyRich, definition.SafeDisplayName);
                    break;
                case SubspaceSymbolKind.EnergyElement:
                case SubspaceSymbolKind.EnergyShard:
                    AddStack(tile.buffs, SubspaceTileBuffType.EnergyRich, definition.SafeDisplayName);
                    break;
                case SubspaceSymbolKind.StableField:
                    tile.debuffs.Clear();
                    break;
                case SubspaceSymbolKind.HotCore:
                    tile.baseBonusScore += Mathf.Max(0, definition.SafeBaseScore);
                    break;
                case SubspaceSymbolKind.ChaosField:
                case SubspaceSymbolKind.ChaosStance:
                    AddStack(tile.debuffs, SubspaceTileBuffType.SpacePollution, definition.SafeDisplayName);
                    break;
                case SubspaceSymbolKind.TornSpace:
                    tile.baseBonusScore = Mathf.Max(0, tile.baseBonusScore - 3);
                    break;
                case SubspaceSymbolKind.Overclock:
                    tile.baseBonusScore = Mathf.FloorToInt(tile.baseBonusScore * 0.5f);
                    break;
                case SubspaceSymbolKind.BlockingSignal:
                    AddStack(tile.debuffs, SubspaceTileBuffType.SpacePollution, definition.SafeDisplayName);
                    break;
                case SubspaceSymbolKind.LifeSignal:
                    tile.baseBonusScore += 2;
                    break;
                case SubspaceSymbolKind.Anchor:
                    tile.baseBonusScore += 3;
                    break;
                case SubspaceSymbolKind.EnergyCore:
                    AddStack(tile.buffs, SubspaceTileBuffType.EnergyRich, definition.SafeDisplayName);
                    break;
                case SubspaceSymbolKind.Turbulence:
                case SubspaceSymbolKind.SubspaceRift:
                    AddStack(tile.debuffs, SubspaceTileBuffType.SpacePollution, definition.SafeDisplayName);
                    break;
            }
        }

        public static int GetSynergyBonus(SubspaceSymbolDefinition a, SubspaceSymbolDefinition b)
        {
            if (a == null || b == null)
            {
                return 0;
            }

            bool hasCrystal = a.SafeKind == SubspaceSymbolKind.Beacon || b.SafeKind == SubspaceSymbolKind.Beacon;
            bool hasEnergy = a.SafeKind == SubspaceSymbolKind.EnergyCore || b.SafeKind == SubspaceSymbolKind.EnergyCore;
            return hasCrystal && hasEnergy ? 5 : 0;
        }

        public static void AddStack(List<SubspaceTileBuffInstance> list, SubspaceTileBuffType type, string sourceName)
        {
            foreach (var instance in list)
            {
                if (instance.data != null && instance.data.type == type)
                {
                    instance.stacks++;
                    return;
                }
            }

            list.Add(new SubspaceTileBuffInstance
            {
                data = SubspaceTileRulebook.GetBuffData(type),
                sourceName = sourceName,
                stacks = 1
            });
        }
    }
}
