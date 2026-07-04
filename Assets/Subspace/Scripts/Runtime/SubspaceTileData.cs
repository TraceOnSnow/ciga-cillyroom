using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Subspace
{
    public enum SubspaceTileBuffType
    {
        EnergyRich,
        SpacePollution
    }

    public enum SubspaceTileEffectType
    {
        AddBaseBonus,
        AddBuff,
        AddDebuff
    }

    [System.Serializable]
    public sealed class SubspaceTileEffect
    {
        public SubspaceTileEffectType effectType;
        public int amount;
        public SubspaceTileBuffType buffType;
        public string sourceName;

        public SubspaceTileEffect(SubspaceTileEffectType effectType, int amount = 0, SubspaceTileBuffType buffType = SubspaceTileBuffType.EnergyRich, string sourceName = "")
        {
            this.effectType = effectType;
            this.amount = amount;
            this.buffType = buffType;
            this.sourceName = sourceName;
        }
    }

    public sealed class SubspaceSymbolData
    {
        public SubspaceSymbolKind type;
        public string displayName;
        public string description;
        public int instantScore;
        public readonly List<SubspaceTileEffect> tileEffects = new List<SubspaceTileEffect>();

        public string TileEffectSummary
        {
            get
            {
                if (tileEffects.Count == 0)
                {
                    return SubspaceTileRulebook.Text.None;
                }

                var lines = new List<string>();
                foreach (var effect in tileEffects)
                {
                    switch (effect.effectType)
                    {
                        case SubspaceTileEffectType.AddBaseBonus:
                            lines.Add(string.Format(SubspaceTileRulebook.Text.AddBaseBonus, effect.amount));
                            break;
                        case SubspaceTileEffectType.AddBuff:
                            lines.Add(string.Format(SubspaceTileRulebook.Text.AddBuff, SubspaceTileRulebook.GetBuffData(effect.buffType).displayName));
                            break;
                        case SubspaceTileEffectType.AddDebuff:
                            lines.Add(string.Format(SubspaceTileRulebook.Text.AddDebuff, SubspaceTileRulebook.GetBuffData(effect.buffType).displayName));
                            break;
                    }
                }

                return string.Join("\n", lines);
            }
        }
    }

    public sealed class SubspaceTileBuffData
    {
        public SubspaceTileBuffType type;
        public string displayName;
        public string effectDescription;
        public int scoreModifier;
        public bool isDebuff;
        public Color color;
    }

    public sealed class SubspaceTileBuffInstance
    {
        public SubspaceTileBuffData data;
        public string sourceName;
        public int stacks = 1;

        public int ScoreModifier => data != null ? data.scoreModifier * Mathf.Max(1, stacks) : 0;
    }

    public sealed class SubspaceTileData
    {
        public int x;
        public int y;
        public SubspaceSymbolDefinition currentSymbol;
        public int baseBonusScore;
        public readonly List<SubspaceTileBuffInstance> buffs = new List<SubspaceTileBuffInstance>();
        public readonly List<SubspaceTileBuffInstance> debuffs = new List<SubspaceTileBuffInstance>();
        public readonly Dictionary<SubspaceSymbolKind, float> symbolWeightModifiers = new Dictionary<SubspaceSymbolKind, float>();

        public SubspaceTileData(int x, int y, SubspaceSymbolDefinition currentSymbol)
        {
            this.x = x;
            this.y = y;
            this.currentSymbol = currentSymbol;
        }
    }

    public static class SubspaceTileRulebook
    {
        public static SubspaceSymbolData GetSymbolData(SubspaceSymbolDefinition definition)
        {
            var data = new SubspaceSymbolData
            {
                type = definition != null ? definition.SafeKind : SubspaceSymbolKind.Generic,
                displayName = definition != null ? definition.SafeDisplayName : Text.EmptyTile,
                description = definition != null ? Text.PrototypeSymbolDescription : Text.EmptyTileDescription,
                instantScore = definition != null ? definition.SafeBaseScore : 0
            };

            switch (data.type)
            {
                case SubspaceSymbolKind.Beacon:
                    data.displayName = Text.Crystal;
                    data.description = Text.CrystalDescription;
                    data.instantScore = 20;
                    break;
                case SubspaceSymbolKind.Anchor:
                    data.displayName = Text.Anchor;
                    data.description = Text.AnchorDescription;
                    data.instantScore = 15;
                    data.tileEffects.Add(new SubspaceTileEffect(SubspaceTileEffectType.AddBaseBonus, 3, sourceName: data.displayName));
                    break;
                case SubspaceSymbolKind.LifeSignal:
                    data.displayName = Text.LifeSignal;
                    data.description = Text.LifeSignalDescription;
                    data.instantScore = 8;
                    data.tileEffects.Add(new SubspaceTileEffect(SubspaceTileEffectType.AddBaseBonus, 2, sourceName: data.displayName));
                    break;
                case SubspaceSymbolKind.EnergyCore:
                    data.displayName = Text.EnergyCore;
                    data.description = Text.EnergyCoreDescription;
                    data.instantScore = 25;
                    data.tileEffects.Add(new SubspaceTileEffect(SubspaceTileEffectType.AddBuff, buffType: SubspaceTileBuffType.EnergyRich, sourceName: data.displayName));
                    break;
                case SubspaceSymbolKind.Turbulence:
                    data.displayName = Text.Turbulence;
                    data.description = Text.TurbulenceDescription;
                    data.instantScore = 45;
                    data.tileEffects.Add(new SubspaceTileEffect(SubspaceTileEffectType.AddDebuff, buffType: SubspaceTileBuffType.SpacePollution, sourceName: data.displayName));
                    break;
                case SubspaceSymbolKind.SubspaceRift:
                    data.displayName = Text.SubspaceRift;
                    data.description = Text.SubspaceRiftDescription;
                    data.instantScore = -20;
                    data.tileEffects.Add(new SubspaceTileEffect(SubspaceTileEffectType.AddDebuff, buffType: SubspaceTileBuffType.SpacePollution, sourceName: data.displayName));
                    break;
                case SubspaceSymbolKind.CosmicDust:
                    data.displayName = Text.CosmicDust;
                    data.description = Text.CosmicDustDescription;
                    data.instantScore = 10;
                    break;
                case SubspaceSymbolKind.RealitySingularity:
                    data.displayName = Text.RealitySingularity;
                    data.description = Text.RealitySingularityDescription;
                    data.instantScore = 35;
                    break;
            }

            return data;
        }

        public static SubspaceTileBuffData GetBuffData(SubspaceTileBuffType type)
        {
            switch (type)
            {
                case SubspaceTileBuffType.SpacePollution:
                    return new SubspaceTileBuffData
                    {
                        type = type,
                        displayName = Text.SpacePollution,
                        effectDescription = Text.SpacePollutionEffect,
                        scoreModifier = -1,
                        isDebuff = true,
                        color = new Color(0.62f, 0.2f, 0.9f, 1f)
                    };
                default:
                    return new SubspaceTileBuffData
                    {
                        type = SubspaceTileBuffType.EnergyRich,
                        displayName = Text.EnergyRich,
                        effectDescription = Text.EnergyRichEffect,
                        scoreModifier = 1,
                        isDebuff = false,
                        color = new Color(0.24f, 0.85f, 0.38f, 1f)
                    };
            }
        }

        public static int GetScoreModifier(SubspaceTileData tile)
        {
            if (tile == null)
            {
                return 0;
            }

            int score = 0;
            foreach (var buff in tile.buffs)
            {
                score += buff.ScoreModifier;
            }

            foreach (var debuff in tile.debuffs)
            {
                score += debuff.ScoreModifier;
            }

            return score;
        }

        public static int CalculateTileScore(SubspaceTileData tile)
        {
            if (tile == null)
            {
                return 0;
            }

            var symbol = GetSymbolData(tile.currentSymbol);
            return symbol.instantScore + tile.baseBonusScore + GetScoreModifier(tile);
        }

        public static void ApplySymbolTileEffects(SubspaceSymbolData symbol, SubspaceTileData tile)
        {
            if (symbol == null || tile == null)
            {
                return;
            }

            foreach (var effect in symbol.tileEffects)
            {
                switch (effect.effectType)
                {
                    case SubspaceTileEffectType.AddBaseBonus:
                        tile.baseBonusScore += effect.amount;
                        break;
                    case SubspaceTileEffectType.AddBuff:
                        AddStack(tile.buffs, effect.buffType, effect.sourceName);
                        break;
                    case SubspaceTileEffectType.AddDebuff:
                        AddStack(tile.debuffs, effect.buffType, effect.sourceName);
                        break;
                }
            }
        }

        public static string BuildSymbolTooltip(SubspaceSymbolDefinition definition)
        {
            var symbol = GetSymbolData(definition);
            return string.Format(Text.SymbolTooltip, symbol.instantScore, symbol.TileEffectSummary, symbol.description);
        }

        public static string BuildTileTooltip(SubspaceTileData tile)
        {
            if (tile == null)
            {
                return string.Empty;
            }

            var symbol = GetSymbolData(tile.currentSymbol);
            var builder = new StringBuilder();
            builder.Append(Text.TilePrefix);
            builder.Append(tile.y + 1);
            builder.Append(Text.RowColumnSeparator);
            builder.Append(tile.x + 1);
            builder.AppendLine(Text.ColumnSuffix);
            builder.Append(Text.BaseBonus);
            builder.AppendLine(FormatSigned(tile.baseBonusScore));
            AppendBuffList(builder, "Buff", tile.buffs);
            AppendBuffList(builder, "Debuff", tile.debuffs);
            builder.Append(Text.CurrentSymbol);
            builder.AppendLine(symbol.displayName);
            builder.Append(Text.ExpectedScore);
            builder.Append(CalculateTileScore(tile));
            return builder.ToString();
        }

        public static string BuildBuffTooltip(SubspaceTileBuffInstance instance)
        {
            if (instance == null || instance.data == null)
            {
                return string.Empty;
            }

            string type = instance.data.isDebuff ? "Debuff" : "Buff";
            string source = string.IsNullOrWhiteSpace(instance.sourceName) ? Text.Unknown : instance.sourceName;
            return string.Format(Text.BuffTooltip, type, instance.data.effectDescription, source, Mathf.Max(1, instance.stacks));
        }

        public static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }

        private static void AddStack(List<SubspaceTileBuffInstance> list, SubspaceTileBuffType type, string sourceName)
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
                data = GetBuffData(type),
                sourceName = sourceName,
                stacks = 1
            });
        }

        private static void AppendBuffList(StringBuilder builder, string label, IReadOnlyList<SubspaceTileBuffInstance> instances)
        {
            builder.Append(label);
            builder.AppendLine(Text.Colon);
            if (instances == null || instances.Count == 0)
            {
                builder.AppendLine(Text.NoneListItem);
                return;
            }

            foreach (var instance in instances)
            {
                if (instance == null || instance.data == null)
                {
                    continue;
                }

                builder.Append("- ");
                builder.Append(instance.data.displayName);
                builder.Append(Text.Colon);
                builder.Append(instance.data.effectDescription);
                if (instance.stacks > 1)
                {
                    builder.Append(" x");
                    builder.Append(instance.stacks);
                }

                builder.AppendLine();
            }
        }

        internal static class Text
        {
            public const string None = "\u65e0";
            public const string Unknown = "\u672a\u77e5";
            public const string EmptyTile = "\u7a7a\u5730\u5757";
            public const string EmptyTileDescription = "\u5f53\u524d\u6ca1\u6709\u7b26\u53f7\u3002";
            public const string PrototypeSymbolDescription = "\u4e34\u65f6\u7b26\u53f7\u3002\u4e4b\u540e\u53ef\u4ee5\u8fc1\u79fb\u5230 ScriptableObject \u914d\u7f6e\u3002";
            public const string Anchor = "\u951a\u70b9";
            public const string Crystal = "\u6676\u4f53";
            public const string LifeSignal = "\u751f\u547d\u4fe1\u53f7";
            public const string EnergyCore = "\u80fd\u91cf\u6838\u5fc3";
            public const string Turbulence = "\u4e71\u6d41";
            public const string SubspaceRift = "\u4e9a\u7a7a\u95f4\u88c2\u7f1d";
            public const string CosmicDust = "\u5b87\u5b99\u5c18\u57c3";
            public const string RealitySingularity = "\u73b0\u5b9e\u5947\u70b9";
            public const string EnergyRich = "\u80fd\u91cf\u5bcc\u96c6";
            public const string SpacePollution = "\u7a7a\u95f4\u6c61\u67d3";
            public const string AnchorDescription = "\u7a33\u5b9a\u53ef\u9760\u7684\u5373\u65f6\u5f97\u5206\u7b26\u53f7\u3002";
            public const string CrystalDescription = "\u8d44\u6e90\u5143\u7d20\u3002\u4e0e\u80fd\u91cf\u6838\u5fc3\u540c\u65f6\u626b\u63cf\u65f6\u89e6\u53d1\u8054\u52a8\u3002";
            public const string LifeSignalDescription = "\u9002\u5408\u957f\u671f\u57f9\u517b\u5730\u5757\u3002";
            public const string EnergyCoreDescription = "\u8ba9\u6240\u5728\u5730\u5757\u83b7\u5f97\u6301\u7eed\u52a0\u5206\u3002";
            public const string TurbulenceDescription = "\u5373\u65f6\u5f97\u5206\u9ad8\uff0c\u4f46\u4f1a\u6c61\u67d3\u6240\u5728\u5730\u5757\u3002";
            public const string SubspaceRiftDescription = "\u538b\u529b\u5143\u7d20\u3002\u4f1a\u964d\u4f4e\u672c\u6b21\u5f97\u5206\u5e76\u6c61\u67d3\u5730\u5757\u3002";
            public const string CosmicDustDescription = "\u5e38\u89c1\u8d44\u6e90\u5143\u7d20\u3002";
            public const string RealitySingularityDescription = "\u7a00\u6709\u8d44\u6e90\u5143\u7d20\u3002";
            public const string EnergyRichEffect = "\u8be5\u5730\u5757\u7ed3\u7b97\u65f6 +1 \u5206";
            public const string SpacePollutionEffect = "\u8be5\u5730\u5757\u7ed3\u7b97\u65f6 -1 \u5206";
            public const string AddBaseBonus = "\u8be5\u5730\u5757\u57fa\u7840\u5206 +{0}";
            public const string AddBuff = "\u6dfb\u52a0 Buff\uff1a{0}";
            public const string AddDebuff = "\u6dfb\u52a0 Debuff\uff1a{0}";
            public const string SymbolTooltip = "\u5373\u65f6\u5f97\u5206\uff1a{0}\n\u5730\u5757\u57f9\u517b\uff1a{1}\n\u8bf4\u660e\uff1a{2}";
            public const string TilePrefix = "\u5730\u5757\uff1a\u7b2c ";
            public const string RowColumnSeparator = " \u884c\uff0c\u7b2c ";
            public const string ColumnSuffix = " \u5217";
            public const string BaseBonus = "\u57fa\u7840\u52a0\u6210\uff1a";
            public const string CurrentSymbol = "\u5f53\u524d\u7b26\u53f7\uff1a";
            public const string ExpectedScore = "\u9884\u8ba1\u672c\u683c\u5f97\u5206\uff1a";
            public const string BuffTooltip = "\u7c7b\u578b\uff1a{0}\n\u6548\u679c\uff1a{1}\n\u6765\u6e90\uff1a{2}\n\u5c42\u6570\uff1a{3}";
            public const string Colon = "\uff1a";
            public const string NoneListItem = "- \u65e0";
        }
    }
}
