using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    public readonly struct SubspaceScoreResult
    {
        public readonly int total;
        public readonly int turnDelta;
        public readonly List<string> lines;
        public readonly List<SubspaceScoreSource> sources;

        public SubspaceScoreResult(int total, int turnDelta, List<string> lines, List<SubspaceScoreSource> sources = null)
        {
            this.total = total;
            this.turnDelta = turnDelta;
            this.lines = lines;
            this.sources = sources ?? new List<SubspaceScoreSource>();
        }
    }

    public readonly struct SubspaceScoreSource
    {
        public readonly string displayName;
        public readonly Vector2Int position;
        public readonly int originalScore;
        public readonly int baseScore;
        public readonly int multiplier;
        public readonly int finalScore;
        public readonly string detail;

        public SubspaceScoreSource(string displayName, Vector2Int position, int originalScore, int baseScore, int multiplier, int finalScore, string detail = "")
        {
            this.displayName = displayName;
            this.position = position;
            this.originalScore = originalScore;
            this.baseScore = baseScore;
            this.multiplier = multiplier;
            this.finalScore = finalScore;
            this.detail = detail;
        }
    }

   public static class SubspaceScoreResolver
   {
       private static readonly Vector2Int BonusScoreKey = new Vector2Int(int.MinValue, int.MinValue);
       private const float ResourceScoreBoostFallback = 0.2f;
       private const float AnchorEffectBoostFallback = 0.5f;
       private const float PollutionReductionFallback = 0.5f;

       private sealed class TileScoreEntry
       {
           public SubspaceTileData tile;
           public SubspaceSymbolDefinition symbol;
           public Vector2Int position;
           public string displayName;
           public int originalScore;
           public int baseScore;
           public int multiplier = 1;
           public readonly List<string> details = new List<string>();

           public int FinalScore => baseScore * multiplier;
       }

       public static SubspaceScoreResult Calculate(SubspaceTileData[,] tiles, SubspaceSelectionShape shape, SubspaceScoreContext context, System.Random random = null)
       {
           var lines = new List<string>();
           var sources = new List<SubspaceScoreSource>();
           if (tiles == null || shape == null || shape.Count == 0)
           {
               return new SubspaceScoreResult(0, 0, lines, sources);
           }

           int columns = tiles.GetLength(0);
           int rows = tiles.GetLength(1);
           var entries = new List<TileScoreEntry>();
           var selectedSymbols = new List<SubspaceSymbolDefinition>();

           foreach (var position in shape.Cells)
           {
               if (!IsInside(position.x, position.y, columns, rows))
               {
                   continue;
               }

               var tile = tiles[position.x, position.y];
               if (tile == null || tile.currentSymbol == null)
               {
                   continue;
               }

               ApplySignalStabilization(tile, context);
               var symbolDefinition = tile.currentSymbol;
               if (symbolDefinition == null)
               {
                   continue;
               }

               var symbol = SubspaceTileRulebook.GetSymbolData(symbolDefinition);
               int baseInstantScore = GetRolledInstantScore(symbolDefinition, random);
               int instantScore = ApplyResourceScoreBoost(symbolDefinition, baseInstantScore, context, out var resourceBoostDetail);
               int tileModifier = GetScoreModifier(tile, context, out var pollutionReductionDetail);
               int tileEffectScore = ApplyAnchorEffectBoost(tile.baseBonusScore + tileModifier, context, out var anchorBoostDetail);
               int baseScore = instantScore + tileEffectScore;

               if (tile.realityAnchor)
               {
                   baseScore += 5;
               }

               var entry = new TileScoreEntry
               {
                   tile = tile,
                   symbol = symbolDefinition,
                   position = position,
                   displayName = symbol.displayName,
                   originalScore = baseInstantScore,
                   baseScore = baseScore
               };

               entry.details.Add($"即时 {baseInstantScore}, 地块 {SubspaceTileRulebook.FormatSigned(tile.baseBonusScore)}, 状态 {SubspaceTileRulebook.FormatSigned(tileModifier)}");
               if (tile.realityAnchor)
               {
                   entry.details.Add("现实锚点 +5");
               }

               if (!string.IsNullOrEmpty(resourceBoostDetail))
               {
                   entry.details.Add(resourceBoostDetail);
               }

               if (!string.IsNullOrEmpty(anchorBoostDetail))
               {
                   entry.details.Add(anchorBoostDetail);
               }

               if (!string.IsNullOrEmpty(pollutionReductionDetail))
               {
                   entry.details.Add(pollutionReductionDetail);
               }

               ApplyPersistentTileScoreEffects(entry, entries, random);
               entries.Add(entry);
               selectedSymbols.Add(symbolDefinition);
          }

           ApplySelectionWideTileEffects(entries, random);

           int total = 0;
           int turnDelta = 0;
           foreach (var entry in entries)
           {
               int finalScore = entry.FinalScore;
               total += finalScore;
               if (entry.tile.energyElement && finalScore >= 5)
               {
                   turnDelta += 1;
                   entry.details.Add("能量元素：回合 +1");
               }

               lines.Add($"{entry.displayName}: {finalScore}");
               sources.Add(new SubspaceScoreSource(
                   entry.displayName,
                   entry.position,
                   entry.originalScore,
                   entry.baseScore,
                   entry.multiplier,
                   finalScore,
                   string.Join("; ", entry.details)));
           }

           ApplyElementTableBonuses(entries, selectedSymbols, context, lines, sources, ref total);
           ApplyElementSynergyBonuses(selectedSymbols, lines, sources, ref total);
           ApplyRewardUpgradeBonuses(entries, selectedSymbols, context, lines, sources, random, ref total);
           ApplySynergyBonuses(tiles, shape, context, total, lines, sources, ref total);
           ApplyFirstScanDouble(context, lines, sources, ref total);

           foreach (var entry in entries)
           {
               int times = entry.tile.doubleExcitation ? 2 : 1;
               if (entry.symbol.SafeKind == SubspaceSymbolKind.DoubleExcitation)
               {
                   times = Mathf.Max(times, 2);
               }

               for (int i = 0; i < times; i++)
               {
                   ApplyTileEffects(entry.symbol, entry.tile, entries, context, random);
               }
           }

          return new SubspaceScoreResult(total, turnDelta, lines, sources);
     }

     private static int GetRolledInstantScore(SubspaceSymbolDefinition symbol, System.Random random)
     {
         if (symbol == null)
         {
             return 0;
         }

         if (symbol.SafeKind == SubspaceSymbolKind.ChaosSignal)
         {
             return random != null ? random.Next(-10, 31) : Random.Range(-10, 31);
         }

         return SubspaceElementRules.GetInstantScore(symbol);
     }

     private static void ApplySignalStabilization(SubspaceTileData tile, SubspaceScoreContext context)
     {
         if (tile == null || tile.currentSymbol == null || !context.HasUpgrade(SubspaceUpgradeType.SignalStabilization))
         {
             return;
         }

         var kind = tile.currentSymbol.SafeKind;
         if (kind != SubspaceSymbolKind.ChaosSignal && kind != SubspaceSymbolKind.VoidSignal)
         {
             return;
         }

         var signal = context.FindSymbol(SubspaceSymbolKind.SignalNode);
         if (signal != null)
         {
             tile.currentSymbol = signal;
         }
     }

     private static void ApplyPersistentTileScoreEffects(TileScoreEntry entry, IReadOnlyList<TileScoreEntry> previousEntries, System.Random random)
     {
         if (entry == null || entry.tile == null || entry.symbol == null)
         {
             return;
         }

         if (entry.tile.signalBoostPoint && entry.symbol.SafeKind == SubspaceSymbolKind.SignalNode)
         {
             entry.multiplier *= 2;
             entry.details.Add("信号加强点 x2");
         }

         if (entry.tile.realityLink && entry.symbol.SafeKind == SubspaceSymbolKind.SignalNode)
         {
             int roll = random != null ? random.Next(1, 7) : Random.Range(1, 7);
             float multiplier = roll * 0.5f;
             int before = entry.baseScore;
             entry.baseScore = Mathf.RoundToInt(entry.baseScore * multiplier);
             entry.details.Add($"现实链接 x{multiplier:0.0}: {before} -> {entry.baseScore}");
         }

         if (entry.tile.spaceTurbulence)
         {
             entry.multiplier *= 2;
             entry.details.Add("空间乱流 x2");
         }

         if (entry.tile.cosmicPrism || entry.symbol.SafeKind == SubspaceSymbolKind.Prism || entry.symbol.SafeKind == SubspaceSymbolKind.CosmicPrism)
         {
             var left = FindEntry(previousEntries, entry.position + Vector2Int.left);
             if (left != null)
             {
                 int copied = left.FinalScore;
                 entry.baseScore += copied;
                 entry.details.Add($"棱镜复制左侧 +{copied}");
             }
         }
     }

     private static void ApplySelectionWideTileEffects(IReadOnlyList<TileScoreEntry> entries, System.Random random)
     {
         bool hasChaosStance = false;
         int signalEnhancerCount = 0;
         int signalNodeCount = 0;

         for (int i = 0; i < entries.Count; i++)
         {
             var entry = entries[i];
             if (entry.tile.chaosStance || entry.symbol.SafeKind == SubspaceSymbolKind.ChaosStance)
             {
                 hasChaosStance = true;
             }

             if (entry.tile.signalEnhancer || entry.symbol.SafeKind == SubspaceSymbolKind.SignalEnhancer)
             {
                 signalEnhancerCount++;
             }

             if (entry.symbol.SafeKind == SubspaceSymbolKind.SignalNode)
             {
                 signalNodeCount++;
             }
         }

         for (int i = 0; i < entries.Count; i++)
         {
             var entry = entries[i];
             if (hasChaosStance && entry.symbol.SafeKind == SubspaceSymbolKind.ChaosSignal)
             {
                 entry.multiplier *= 3;
                 entry.details.Add("混乱立场 x3");
             }

             if (entry.symbol.SafeKind == SubspaceSymbolKind.GravityFlow && signalNodeCount > 0)
             {
                 int bonus = signalNodeCount * 10;
                 entry.baseScore += bonus;
                 entry.details.Add($"引力流吸收信号 +{bonus}");
             }
         }

         if (signalEnhancerCount > 0)
         {
             int signalNameCount = 0;
             for (int i = 0; i < entries.Count; i++)
             {
                 if (IsSignalName(entries[i].displayName))
                 {
                     signalNameCount++;
                 }
             }

             int bonus = signalNameCount * signalEnhancerCount * 5;
             for (int i = 0; i < entries.Count; i++)
             {
                 if (IsSignalName(entries[i].displayName))
                 {
                     entries[i].baseScore += signalEnhancerCount * 5;
                     entries[i].details.Add($"信号加强器 +{signalEnhancerCount * 5}");
                 }
             }
         }

         for (int i = 0; i < entries.Count; i++)
         {
             var center = entries[i];
             if (!center.tile.signalSacrifice && center.symbol.SafeKind != SubspaceSymbolKind.SignalSacrifice)
             {
                 continue;
             }

             int before = center.baseScore;
             center.baseScore = Mathf.RoundToInt(center.baseScore / 8f);
             center.details.Add($"信号献祭中心 /8: {before} -> {center.baseScore}");

             for (int j = 0; j < entries.Count; j++)
             {
                 if (i == j)
                 {
                     continue;
                 }

                 var adjacent = entries[j];
                 int distance = Mathf.Abs(adjacent.position.x - center.position.x) + Mathf.Abs(adjacent.position.y - center.position.y);
                 if (distance == 1)
                 {
                     adjacent.multiplier *= 2;
                     adjacent.details.Add("信号献祭相邻 x2");
                 }
             }
         }
     }

     private static void ApplyElementTableBonuses(
         IReadOnlyList<TileScoreEntry> entries,
         IReadOnlyList<SubspaceSymbolDefinition> selectedSymbols,
         SubspaceScoreContext context,
         List<string> lines,
         List<SubspaceScoreSource> sources,
         ref int total)
     {
         if (selectedSymbols == null || selectedSymbols.Count == 0)
         {
             return;
         }

         int dataCount = 0;
         int energyShardCount = 0;
         int analysisCount = 0;
         int resonanceCount = 0;
         int energyTransitionCount = 0;
         var kindCounts = new Dictionary<SubspaceSymbolKind, int>();

         for (int i = 0; i < selectedSymbols.Count; i++)
         {
             var symbol = selectedSymbols[i];
             if (symbol == null)
             {
                 continue;
             }

             kindCounts.TryGetValue(symbol.SafeKind, out var count);
             kindCounts[symbol.SafeKind] = count + 1;

             switch (symbol.SafeKind)
             {
                 case SubspaceSymbolKind.Data:
                     dataCount++;
                     break;
                 case SubspaceSymbolKind.EnergyShard:
                     energyShardCount++;
                     break;
                 case SubspaceSymbolKind.MultidimensionalAnalysis:
                     analysisCount++;
                     break;
                 case SubspaceSymbolKind.ResonanceSignal:
                     resonanceCount++;
                     break;
                 case SubspaceSymbolKind.EnergyTransition:
                     energyTransitionCount++;
                     break;
             }
         }

         if (dataCount > 1)
         {
             AddBonus("数据", dataCount * dataCount * 2, lines, sources, ref total, "框中数据数量越多，数据额外加分越高。");
         }

         if (energyShardCount > 0)
         {
             AddBonus("能量碎片", energyShardCount * selectedSymbols.Count * 5, lines, sources, ref total, "给予框内所有元素 +5 分。");
         }

         if (analysisCount > 0)
         {
             AddBonus("多维分析", analysisCount * kindCounts.Count * 3, lines, sources, ref total, "框中每有一个不同元素，+3。");
         }

         if (energyTransitionCount > 0)
         {
             int tileBonus = 0;
             for (int i = 0; i < entries.Count; i++)
             {
                 if (entries[i].tile != null)
                 {
                     tileBonus += Mathf.Max(0, entries[i].tile.baseBonusScore + SubspaceTileRulebook.GetScoreModifier(entries[i].tile));
                 }
             }

             AddBonus("能量跃迁", tileBonus * energyTransitionCount, lines, sources, ref total, "获得框中所有地块的正向加成。");
         }

         if (resonanceCount > 0)
         {
             foreach (var entry in kindCounts)
             {
                 if (entry.Value * 2 >= selectedSymbols.Count)
                 {
                     AddBonus("共振信号", resonanceCount * 20, lines, sources, ref total, "框中至少 50% 为同一元素。");
                     break;
                 }
             }
         }
     }

     private static void ApplyRewardUpgradeBonuses(
         IReadOnlyList<TileScoreEntry> entries,
         IReadOnlyList<SubspaceSymbolDefinition> selectedSymbols,
         SubspaceScoreContext context,
         List<string> lines,
         List<SubspaceScoreSource> sources,
         System.Random random,
         ref int total)
     {
         if (context.HasUpgrade(SubspaceUpgradeType.ChaosConversion))
         {
             int debuffCount = 0;
             for (int i = 0; i < entries.Count; i++)
             {
                 debuffCount += entries[i].tile != null ? entries[i].tile.debuffs.Count : 0;
             }

             AddBonus("混沌转化", debuffCount * 5, lines, sources, ref total, "地块上每有一个 defull/debuff，结算 +5。");
         }

         if (context.HasUpgrade(SubspaceUpgradeType.LimitScan) && selectedSymbols != null && selectedSymbols.Count > 0)
         {
             var kinds = new HashSet<SubspaceSymbolKind>();
             for (int i = 0; i < selectedSymbols.Count; i++)
             {
                 if (selectedSymbols[i] != null)
                 {
                     kinds.Add(selectedSymbols[i].SafeKind);
                 }
             }

             int before = total;
             if (kinds.Count == 1)
             {
                 total *= 2;
             }
             else if (kinds.Count == 2)
             {
                 total += 10;
             }
             else if (kinds.Count > 3)
             {
                 total -= 20;
             }

             if (total != before)
             {
                 int delta = total - before;
                 lines.Add($"极限扫描: {SubspaceTileRulebook.FormatSigned(delta)}");
                 sources.Add(new SubspaceScoreSource("极限扫描", BonusScoreKey, delta, delta, 1, delta, $"元素种类 {kinds.Count}。"));
             }
         }

         if (total > 30 && context.HasUpgrade(SubspaceUpgradeType.Overload))
         {
             int bonus = Mathf.RoundToInt(total * 0.1f);
             AddBonus("过载", bonus, lines, sources, ref total, "结算分数 >30，分数 +10%。");
         }

         if (context.HasUpgrade(SubspaceUpgradeType.LastStand) && context.remainingTurns <= 1)
         {
             int bonus = total;
             AddBonus("孤注一掷", bonus, lines, sources, ref total, "剩余 1 回合时，最后一次结算 +2 倍。");
         }

         if (context.HasUpgrade(SubspaceUpgradeType.CleanerRobot))
         {
             int roll = random != null ? random.Next(0, 100) : Random.Range(0, 100);
             if (roll < 30 && ClearRandomDebuff(entries, random))
             {
                 lines.Add("清洁机器人: 清除 1 个 defull");
             }
         }
     }

     private static void ApplyTileEffects(
         SubspaceSymbolDefinition definition,
         SubspaceTileData tile,
         IReadOnlyList<TileScoreEntry> selectedEntries,
         SubspaceScoreContext context,
         System.Random random)
     {
         if (definition == null || tile == null)
         {
             return;
         }

         bool canAddTileEffect = !tile.blocksTileEffects;
         switch (definition.SafeKind)
         {
             case SubspaceSymbolKind.RealityAnchor:
                 if (canAddTileEffect) tile.realityAnchor = true;
                 break;
             case SubspaceSymbolKind.SignalBoostPoint:
                 if (canAddTileEffect) tile.signalBoostPoint = true;
                 break;
             case SubspaceSymbolKind.DoubleExcitation:
                 if (canAddTileEffect) tile.doubleExcitation = true;
                 break;
             case SubspaceSymbolKind.GrowthNode:
                 if (canAddTileEffect) tile.growthNode = true;
                 break;
             case SubspaceSymbolKind.RealityLink:
                 if (canAddTileEffect) tile.realityLink = true;
                 break;
             case SubspaceSymbolKind.SpaceTurbulenceField:
                 if (canAddTileEffect) tile.spaceTurbulence = true;
                 break;
             case SubspaceSymbolKind.EnergyElement:
                 if (canAddTileEffect) tile.energyElement = true;
                 break;
             case SubspaceSymbolKind.SignalSacrifice:
                 if (canAddTileEffect) tile.signalSacrifice = true;
                 break;
             case SubspaceSymbolKind.DataFlow:
                 if (canAddTileEffect)
                 {
                     tile.dataFlow = true;
                     TryConvertRandomSelectedSymbol(selectedEntries, context, SubspaceSymbolKind.Data, 20, random);
                 }
                 break;
             case SubspaceSymbolKind.CosmicPrism:
                 if (canAddTileEffect) tile.cosmicPrism = true;
                 break;
             case SubspaceSymbolKind.StableField:
                 if (canAddTileEffect) tile.stableField = true;
                 tile.debuffs.Clear();
                 break;
             case SubspaceSymbolKind.HotCore:
                 if (canAddTileEffect)
                 {
                     tile.hotCore = true;
                     ConsumeLowestBaseSymbolIntoTile(tile, selectedEntries);
                 }
                 break;
             case SubspaceSymbolKind.MagneticField:
                 if (canAddTileEffect)
                 {
                     tile.magneticField = true;
                     PullAdjacentSymbol(tile, context, random);
                 }
                 break;
             case SubspaceSymbolKind.ChaosField:
                 if (canAddTileEffect)
                 {
                     tile.chaosField = true;
                     tile.chaosFieldCounter++;
                     if (tile.chaosFieldCounter >= 3)
                     {
                         tile.chaosFieldCounter = 0;
                         SpawnChaosSignals(selectedEntries, context, random);
                     }
                 }
                 break;
             case SubspaceSymbolKind.SignalConversion:
                 if (canAddTileEffect)
                 {
                     tile.signalConversion = true;
                     TryConvertRandomSelectedSymbol(selectedEntries, context, SubspaceSymbolKind.SignalNode, 100, random);
                 }
                 break;
             case SubspaceSymbolKind.SignalEnhancer:
                 if (canAddTileEffect) tile.signalEnhancer = true;
                 break;
             case SubspaceSymbolKind.ChaosStance:
                 if (canAddTileEffect) tile.chaosStance = true;
                 break;
             case SubspaceSymbolKind.BlockingSignal:
                 tile.blocksTileEffects = true;
                 break;
             case SubspaceSymbolKind.TornSpace:
                 DestroyRandomTileBonus(selectedEntries, random);
                 break;
             case SubspaceSymbolKind.Overclock:
                 if (!tile.stableField)
                 {
                     tile.baseBonusScore = Mathf.FloorToInt(tile.baseBonusScore * 0.5f);
                 }
                 break;
             case SubspaceSymbolKind.VoidSignal:
                 ConsumeRandomAdjacentSymbol(tile, context, random);
                 break;
         }

         if (tile.growthNode)
         {
             tile.baseBonusScore += 2;
         }

         if (tile.spaceTurbulence && !tile.stableField)
         {
             tile.baseBonusScore -= 1;
         }
     }

     private static TileScoreEntry FindEntry(IReadOnlyList<TileScoreEntry> entries, Vector2Int position)
     {
         if (entries == null)
         {
             return null;
         }

         for (int i = 0; i < entries.Count; i++)
         {
             if (entries[i] != null && entries[i].position == position)
             {
                 return entries[i];
             }
         }

         return null;
     }

     private static bool IsSignalName(string displayName)
     {
         return !string.IsNullOrEmpty(displayName) && displayName.Contains("信号");
     }

     private static bool ClearRandomDebuff(IReadOnlyList<TileScoreEntry> entries, System.Random random)
     {
         var candidates = new List<SubspaceTileData>();
         for (int i = 0; i < entries.Count; i++)
         {
             if (entries[i].tile != null && entries[i].tile.debuffs.Count > 0)
             {
                 candidates.Add(entries[i].tile);
             }
         }

         if (candidates.Count == 0)
         {
             return false;
         }

         var tile = candidates[random != null ? random.Next(0, candidates.Count) : Random.Range(0, candidates.Count)];
         tile.debuffs.RemoveAt(0);
         return true;
     }

     private static void TryConvertRandomSelectedSymbol(
         IReadOnlyList<TileScoreEntry> entries,
         SubspaceScoreContext context,
         SubspaceSymbolKind targetKind,
         int chancePercent,
         System.Random random)
     {
         int roll = random != null ? random.Next(0, 100) : Random.Range(0, 100);
         if (roll >= chancePercent)
         {
             return;
         }

         var target = context.FindSymbol(targetKind);
         if (target == null)
         {
             return;
         }

         var candidates = new List<TileScoreEntry>();
         for (int i = 0; i < entries.Count; i++)
         {
             if (entries[i].tile != null && entries[i].symbol != null && entries[i].symbol.SafeKind != targetKind)
             {
                 candidates.Add(entries[i]);
             }
         }

         if (candidates.Count == 0)
         {
             return;
         }

         var chosen = candidates[random != null ? random.Next(0, candidates.Count) : Random.Range(0, candidates.Count)];
         chosen.tile.currentSymbol = target;
     }

     private static void ConsumeLowestBaseSymbolIntoTile(SubspaceTileData targetTile, IReadOnlyList<TileScoreEntry> entries)
     {
         TileScoreEntry lowest = null;
         for (int i = 0; i < entries.Count; i++)
         {
             var entry = entries[i];
             if (entry == null || entry.tile == null || entry.symbol == null || entry.tile == targetTile)
             {
                 continue;
             }

             if (lowest == null || SubspaceElementRules.GetInstantScore(entry.symbol) < SubspaceElementRules.GetInstantScore(lowest.symbol))
             {
                 lowest = entry;
             }
         }

         if (lowest == null)
         {
             return;
         }

         targetTile.baseBonusScore += Mathf.Max(0, SubspaceElementRules.GetInstantScore(lowest.symbol));
         lowest.tile.currentSymbol = null;
     }

     private static void PullAdjacentSymbol(SubspaceTileData tile, SubspaceScoreContext context, System.Random random)
     {
         var candidates = new List<SubspaceTileData>();
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, 1, 0));
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, -1, 0));
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, 0, 1));
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, 0, -1));
         if (candidates.Count == 0)
         {
             return;
         }

         var source = candidates[random != null ? random.Next(0, candidates.Count) : Random.Range(0, candidates.Count)];
         tile.currentSymbol = source.currentSymbol;
         source.currentSymbol = null;
     }

     private static void AddAdjacentCandidate(List<SubspaceTileData> candidates, SubspaceTileData tile)
     {
         if (tile != null && tile.currentSymbol != null)
         {
             candidates.Add(tile);
         }
     }

     private static void SpawnChaosSignals(IReadOnlyList<TileScoreEntry> entries, SubspaceScoreContext context, System.Random random)
     {
         var chaos = context.FindSymbol(SubspaceSymbolKind.ChaosSignal);
         if (chaos == null || entries.Count == 0)
         {
             return;
         }

         var shuffled = new List<TileScoreEntry>(entries);
         for (int i = 0; i < shuffled.Count; i++)
         {
             int j = random != null ? random.Next(i, shuffled.Count) : Random.Range(i, shuffled.Count);
             (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
         }

         int count = Mathf.Min(3, shuffled.Count);
         for (int i = 0; i < count; i++)
         {
             if (shuffled[i].tile != null)
             {
                 shuffled[i].tile.currentSymbol = chaos;
             }
         }
     }

     private static void DestroyRandomTileBonus(IReadOnlyList<TileScoreEntry> entries, System.Random random)
     {
         var candidates = new List<SubspaceTileData>();
         for (int i = 0; i < entries.Count; i++)
         {
             if (entries[i].tile != null && entries[i].tile.baseBonusScore > 0)
             {
                 candidates.Add(entries[i].tile);
             }
         }

         if (candidates.Count == 0)
         {
             return;
         }

         var target = candidates[random != null ? random.Next(0, candidates.Count) : Random.Range(0, candidates.Count)];
         target.baseBonusScore = 0;
     }

     private static void ConsumeRandomAdjacentSymbol(SubspaceTileData tile, SubspaceScoreContext context, System.Random random)
     {
         var candidates = new List<SubspaceTileData>();
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, 1, 0));
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, -1, 0));
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, 0, 1));
         AddAdjacentCandidate(candidates, context.GetAdjacent(tile, 0, -1));
         if (candidates.Count == 0)
         {
             return;
         }

         var target = candidates[random != null ? random.Next(0, candidates.Count) : Random.Range(0, candidates.Count)];
         target.currentSymbol = null;
     }

     private static int ApplyResourceScoreBoost(SubspaceSymbolDefinition symbol, int score, SubspaceScoreContext context, out string detail)
     {
         detail = string.Empty;
         if (symbol == null)
         {
             return 0;
         }

         if (score == 0 || !context.HasUpgrade(SubspaceUpgradeType.ResourceScoreBoost) || !SubspaceElementRules.IsResource(symbol))
         {
             return score;
         }

         float bonus = context.GetUpgradeFloat(SubspaceUpgradeType.ResourceScoreBoost, ResourceScoreBoostFallback);
         int boosted = Mathf.RoundToInt(score * (1f + bonus));
         detail = $"ResourceScoreBoost x{1f + bonus:0.##}: {score} -> {boosted}";
         return boosted;
     }

     private static int ApplyAnchorEffectBoost(int tileEffectScore, SubspaceScoreContext context, out string detail)
     {
         detail = string.Empty;
         if (tileEffectScore == 0 || !context.HasUpgrade(SubspaceUpgradeType.AnchorEffectBoost))
         {
             return tileEffectScore;
         }

         float bonus = context.GetUpgradeFloat(SubspaceUpgradeType.AnchorEffectBoost, AnchorEffectBoostFallback);
         int boosted = Mathf.RoundToInt(tileEffectScore * (1f + bonus));
         detail = $"AnchorEffectBoost x{1f + bonus:0.##}: {tileEffectScore} -> {boosted}";
         return boosted;
     }

     private static int GetScoreModifier(SubspaceTileData tile, SubspaceScoreContext context, out string detail)
     {
         detail = string.Empty;
         if (tile == null)
         {
             return 0;
         }

         int score = 0;
         foreach (var buff in tile.buffs)
         {
             score += buff.ScoreModifier;
         }

         int debuffScore = 0;
         foreach (var debuff in tile.debuffs)
         {
             debuffScore += debuff.ScoreModifier;
         }

         if (debuffScore != 0 && (context.HasUpgrade(SubspaceUpgradeType.PollutionReduction) || context.HasUpgrade(SubspaceUpgradeType.DamageControl)))
         {
             int originalDebuffScore = debuffScore;
             float reduction = context.HasUpgrade(SubspaceUpgradeType.DamageControl)
                 ? context.GetUpgradeFloat(SubspaceUpgradeType.DamageControl, PollutionReductionFallback)
                 : context.GetUpgradeFloat(SubspaceUpgradeType.PollutionReduction, PollutionReductionFallback);
             debuffScore = Mathf.RoundToInt(debuffScore * Mathf.Clamp01(1f - reduction));
             detail = $"PollutionReduction {reduction:P0}: debuff {originalDebuffScore} -> {debuffScore}";
         }

         return score + debuffScore;
     }

     private static void ApplyFirstScanDouble(SubspaceScoreContext context, List<string> lines, List<SubspaceScoreSource> sources, ref int total)
     {
         if (total == 0 || !context.isFirstScanThisLevel || !context.HasUpgrade(SubspaceUpgradeType.FirstScanDouble))
         {
             return;
         }

         int bonus = total;
         total += bonus;
         lines.Add($"FirstScanDouble: +{bonus}");
         sources.Add(new SubspaceScoreSource(
             "FirstScanDouble",
             BonusScoreKey,
             bonus,
             bonus,
             1,
             bonus,
             $"First scan this level doubled score from {bonus} to {total}."));
     }

     private static void ApplyElementSynergyBonuses(IReadOnlyList<SubspaceSymbolDefinition> selectedSymbols, List<string> lines, List<SubspaceScoreSource> sources, ref int total)
     {
         if (selectedSymbols == null || selectedSymbols.Count < 2)
         {
             return;
         }

         bool hasSignal = false;
         bool hasEnergy = false;
         for (int i = 0; i < selectedSymbols.Count; i++)
         {
             var symbol = selectedSymbols[i];
             if (symbol == null)
             {
                 continue;
             }

             hasSignal |= symbol.SafeKind == SubspaceSymbolKind.SignalNode;
             hasEnergy |= symbol.SafeKind == SubspaceSymbolKind.EnergyShard;
         }

         if (!hasSignal || !hasEnergy)
         {
             return;
         }

         const int bonus = 5;
         total += bonus;
         lines.Add("信号节点 + 能量碎片: +5");
         sources.Add(new SubspaceScoreSource("信号节点 + 能量碎片", BonusScoreKey, bonus, bonus, 1, bonus, "Resource synergy"));
     }

     private static void ApplyElementTableBonuses(IReadOnlyList<SubspaceSymbolDefinition> selectedSymbols, List<string> lines, List<SubspaceScoreSource> sources, ref int total)
     {
         if (selectedSymbols == null || selectedSymbols.Count == 0)
         {
             return;
         }

         int dataCount = 0;
         int energyShardCount = 0;
         int analysisCount = 0;
         int resonanceCount = 0;
         var kindCounts = new Dictionary<SubspaceSymbolKind, int>();

         foreach (var symbol in selectedSymbols)
         {
             if (symbol == null)
             {
                 continue;
             }

             kindCounts.TryGetValue(symbol.SafeKind, out var count);
             kindCounts[symbol.SafeKind] = count + 1;

             if (symbol.SafeKind == SubspaceSymbolKind.Data)
             {
                 dataCount++;
             }
             else if (symbol.SafeKind == SubspaceSymbolKind.EnergyShard)
             {
                 energyShardCount++;
             }
             else if (symbol.SafeKind == SubspaceSymbolKind.MultidimensionalAnalysis)
             {
                 analysisCount++;
             }
             else if (symbol.SafeKind == SubspaceSymbolKind.ResonanceSignal)
             {
                 resonanceCount++;
             }
         }

         if (dataCount > 1)
         {
             AddBonus("数据", dataCount * dataCount * 2, lines, sources, ref total, "Data count scaling.");
         }

         if (energyShardCount > 0)
         {
             AddBonus("能量碎片", energyShardCount * selectedSymbols.Count * 5, lines, sources, ref total, "Each Energy Shard gives every selected element +5.");
         }

         if (analysisCount > 0)
         {
             AddBonus("多维分析", analysisCount * kindCounts.Count * 3, lines, sources, ref total, "Each different selected element gives +3.");
         }

         if (resonanceCount > 0)
         {
             foreach (var entry in kindCounts)
             {
                 if (entry.Value * 2 >= selectedSymbols.Count)
                 {
                     AddBonus("共振信号", resonanceCount * 20, lines, sources, ref total, "At least half of the selected elements share one frequency.");
                     break;
                 }
             }
         }
     }

     private static void AddBonus(string name, int bonus, List<string> lines, List<SubspaceScoreSource> sources, ref int total, string detail)
     {
         if (bonus == 0)
         {
             return;
         }

         total += bonus;
         lines.Add($"{name}: +{bonus}");
         sources.Add(new SubspaceScoreSource(name, BonusScoreKey, bonus, bonus, 1, bonus, detail));
     }

     private static void ApplySynergyBonuses(SubspaceTileData[,] tiles, SubspaceSelectionShape shape, SubspaceScoreContext context, int currentTotal, List<string> lines, List<SubspaceScoreSource> sources, ref int total)
     {
         if (tiles == null || shape == null || shape.Count == 0)
         {
             return;
         }

         int columns = tiles.GetLength(0);
         int rows = tiles.GetLength(1);
         var synergyMap = new Dictionary<string, List<SubspaceSymbolDefinition>>();

         foreach (var position in shape.Cells)
         {
             if (!IsInside(position.x, position.y, columns, rows))
             {
                 continue;
             }

             var tile = tiles[position.x, position.y];
             if (tile == null || tile.currentSymbol == null)
             {
                 continue;
             }

             var tag = tile.currentSymbol.synergyTag;
             if (string.IsNullOrEmpty(tag))
             {
                 continue;
             }

             if (!synergyMap.TryGetValue(tag, out var list))
             {
                 list = new List<SubspaceSymbolDefinition>();
                 synergyMap[tag] = list;
             }

             list.Add(tile.currentSymbol);
         }

         foreach (var entry in synergyMap)
         {
             if (entry.Value.Count < 2)
             {
                 continue;
             }

             int bestBonus = 0;
             string bestTag = entry.Key;
             foreach (var symbol in entry.Value)
             {
                 if (symbol.synergyBonus > bestBonus)
                 {
                     bestBonus = symbol.synergyBonus;
                 }
             }

             if (bestBonus > 0)
             {
                 total += bestBonus;
                 lines.Add($"联动: {bestTag} +{bestBonus}");
                 sources.Add(new SubspaceScoreSource(
                     $"联动({bestTag})",
                     BonusScoreKey,
                     bestBonus,
                     bestBonus,
                     1,
                     bestBonus,
                     $"选区内 {entry.Value.Count} 个 {bestTag} 联动"));
             }
         }
     }

     public static SubspaceScoreResult Calculate(SubspaceTileData[,] tiles, RectInt selection, System.Random random = null)
        {
            var lines = new List<string>();
            var sources = new List<SubspaceScoreSource>();
            if (tiles == null)
            {
                return new SubspaceScoreResult(0, 0, lines, sources);
            }

            int columns = tiles.GetLength(0);
            int rows = tiles.GetLength(1);
            int total = 0;

            for (int y = selection.yMin; y < selection.yMax; y++)
            {
                for (int x = selection.xMin; x < selection.xMax; x++)
                {
                    if (!IsInside(x, y, columns, rows))
                    {
                        continue;
                    }

                    var tile = tiles[x, y];
                    if (tile == null || tile.currentSymbol == null)
                    {
                        continue;
                    }

                    var symbol = SubspaceTileRulebook.GetSymbolData(tile.currentSymbol);
                    int tileModifier = SubspaceTileRulebook.GetScoreModifier(tile);
                    int finalScore = symbol.instantScore + tile.baseBonusScore + tileModifier;
                    total += finalScore;

                    string detail = $"即时 {symbol.instantScore}, 地块 {SubspaceTileRulebook.FormatSigned(tile.baseBonusScore)}, 状态 {SubspaceTileRulebook.FormatSigned(tileModifier)}";
                    lines.Add($"{symbol.displayName}: {finalScore}");
                    sources.Add(new SubspaceScoreSource(
                        symbol.displayName,
                        new Vector2Int(x, y),
                        symbol.instantScore,
                        finalScore,
                        1,
                        finalScore,
                        detail));

                    SubspaceTileRulebook.ApplySymbolTileEffects(symbol, tile);
                }
            }

            return new SubspaceScoreResult(total, 0, lines, sources);
        }

        public static SubspaceScoreResult Calculate(SubspaceSymbolDefinition[,] board, RectInt selection, System.Random random = null)
        {
            var baseScores = new Dictionary<Vector2Int, int>();
            var originalScores = new Dictionary<Vector2Int, int>();
            var multipliers = new Dictionary<Vector2Int, int>();
            var sourceDetails = new Dictionary<Vector2Int, List<string>>();
            var positions = new List<Vector2Int>();
            var energyCorePositions = new List<Vector2Int>();
            var lines = new List<string>();
            var sources = new List<SubspaceScoreSource>();

            if (board == null)
            {
                return new SubspaceScoreResult(0, 0, lines, sources);
            }

            int columns = board.GetLength(0);
            int rows = board.GetLength(1);
            int lifeSignalCount = 0;
            int energyCoreCount = 0;
            int cosmicDustCount = 0;
            bool hasSelectedAnchor = false;

            for (int y = selection.yMin; y < selection.yMax; y++)
            {
                for (int x = selection.xMin; x < selection.xMax; x++)
                {
                    if (!IsInside(x, y, columns, rows) || board[x, y] == null)
                    {
                        continue;
                    }

                    var position = new Vector2Int(x, y);
                    var symbol = board[x, y];
                    positions.Add(position);
                    multipliers[position] = 1;

                    int score = symbol.SafeBaseScore;
                    switch (symbol.SafeKind)
                    {
                        case SubspaceSymbolKind.LifeSignal:
                            score = RollInclusive(random, 1, 6);
                            AddDetail(sourceDetails, position, $"生命信号随机得分 {score}");
                            lifeSignalCount++;
                            break;
                        case SubspaceSymbolKind.RealitySingularity:
                            score = RollInclusive(random, -2, 5);
                            AddDetail(sourceDetails, position, $"现实奇点随机基础分 {score}");
                            break;
                        case SubspaceSymbolKind.Anchor:
                            hasSelectedAnchor = true;
                            break;
                        case SubspaceSymbolKind.EnergyCore:
                            energyCorePositions.Add(position);
                            energyCoreCount++;
                            break;
                        case SubspaceSymbolKind.CosmicDust:
                            cosmicDustCount++;
                            break;
                    }

                    originalScores[position] = score;
                    baseScores[position] = score;
                }
            }

            ApplyAnchorEffects(board, baseScores, sourceDetails, positions, cosmicDustCount, lines);
            ApplySubspaceRifts(board, baseScores, sourceDetails, positions, hasSelectedAnchor, lines);
            ApplyRealitySingularities(board, baseScores, multipliers, sourceDetails, positions, lines);

            int turnDelta = 0;
            if (lifeSignalCount >= 5)
            {
                baseScores[BonusScoreKey] = 25;
                originalScores[BonusScoreKey] = 25;
                multipliers[BonusScoreKey] = 1;
                lines.Add("生命体检测: +25");
                sources.Add(new SubspaceScoreSource("生命体检测", BonusScoreKey, 25, 25, 1, 25, "5 个生命信号在同一框内触发一次性奖励"));
            }

            if (energyCoreCount >= 5)
            {
                turnDelta += 1;
                lines.Add("能量核心: 回合 +1");
                foreach (var position in energyCorePositions)
                {
                    AddDetail(sourceDetails, position, "5 个能量核心在同一框内，额外获得 1 次出牌机会");
                }
            }

            int total = 0;
            foreach (var entry in baseScores)
            {
                int multiplier = multipliers.TryGetValue(entry.Key, out var value) ? value : 1;
                int finalScore = entry.Value * multiplier;
                total += finalScore;

                if (!IsInside(entry.Key.x, entry.Key.y, columns, rows))
                {
                    continue;
                }

                var symbol = board[entry.Key.x, entry.Key.y];
                string suffix = multiplier != 1 ? $" x{multiplier}" : string.Empty;
                lines.Add($"{symbol.SafeDisplayName}: {entry.Value}{suffix}");
                int originalScore = originalScores.TryGetValue(entry.Key, out var original) ? original : entry.Value;
                string detail = sourceDetails.TryGetValue(entry.Key, out var details) ? string.Join("; ", details) : string.Empty;
                sources.Add(new SubspaceScoreSource(symbol.SafeDisplayName, entry.Key, originalScore, entry.Value, multiplier, finalScore, detail));
            }

            return new SubspaceScoreResult(total, turnDelta, lines, sources);
        }

        private static void ApplyAnchorEffects(
            SubspaceSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, List<string>> sourceDetails,
            IReadOnlyList<Vector2Int> positions,
            int cosmicDustCount,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeKind != SubspaceSymbolKind.Anchor)
                {
                    continue;
                }

                int adjacentBeaconCount = 0;
                foreach (var adjacent in GetAdjacentPositions(position, true))
                {
                    if (!baseScores.ContainsKey(adjacent))
                    {
                        continue;
                    }

                    var adjacentSymbol = board[adjacent.x, adjacent.y];
                    if (adjacentSymbol.SafeKind == SubspaceSymbolKind.Beacon)
                    {
                        adjacentBeaconCount++;
                    }
                }

                int beaconBonus = adjacentBeaconCount * 5;
                int dustPenalty = adjacentBeaconCount * cosmicDustCount;
                int finalBonus = beaconBonus - dustPenalty;
                baseScores[position] += finalBonus;

                if (beaconBonus > 0)
                {
                    lines.Add($"锚点: 相邻信标 +{beaconBonus}");
                    AddDetail(sourceDetails, position, $"相邻信标 {adjacentBeaconCount} 个，锚点 +{beaconBonus}");
                }

                if (dustPenalty > 0)
                {
                    lines.Add($"宇宙尘埃: 锚点数值 -{dustPenalty}");
                    AddDetail(sourceDetails, position, $"框内宇宙尘埃 {cosmicDustCount} 个，锚点 -{dustPenalty}");
                }

            }
        }

        private static void ApplySubspaceRifts(
            SubspaceSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, List<string>> sourceDetails,
            IReadOnlyList<Vector2Int> positions,
            bool hasSelectedAnchor,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeKind != SubspaceSymbolKind.SubspaceRift)
                {
                    continue;
                }

                if (hasSelectedAnchor)
                {
                    lines.Add("锚点: 抵消同框亚空间裂缝");
                    AddDetail(sourceDetails, position, "同一框内存在锚点，亚空间裂缝效果被抵消");
                    continue;
                }

                foreach (var adjacent in GetAdjacentPositions(position, true))
                {
                    if (baseScores.ContainsKey(adjacent) && board[adjacent.x, adjacent.y].SafeKind == SubspaceSymbolKind.Beacon)
                    {
                        baseScores[adjacent] = 0;
                        lines.Add("亚空间裂缝: 相邻信标失效");
                        AddDetail(sourceDetails, adjacent, "被相邻亚空间裂缝影响，基础数值变为 0");
                        AddDetail(sourceDetails, position, $"使相邻信标 ({adjacent.x}, {adjacent.y}) 基础数值变为 0");
                    }
                }
            }
        }

        private static void ApplyRealitySingularities(
            SubspaceSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, int> multipliers,
            Dictionary<Vector2Int, List<string>> sourceDetails,
            IReadOnlyList<Vector2Int> positions,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeKind != SubspaceSymbolKind.RealitySingularity)
                {
                    continue;
                }

                foreach (var adjacent in GetAdjacentPositions(position, true))
                {
                    if (baseScores.ContainsKey(adjacent))
                    {
                        multipliers[adjacent] *= 2;
                        AddDetail(sourceDetails, adjacent, $"被相邻现实奇点影响，倍率 x2");
                    }
                }

                lines.Add("现实奇点: 相邻元素数值 x2");
                AddDetail(sourceDetails, position, "使相邻元素数值 x2");
            }
        }

        private static void AddDetail(Dictionary<Vector2Int, List<string>> details, Vector2Int position, string detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return;
            }

            if (!details.TryGetValue(position, out var list))
            {
                list = new List<string>();
                details[position] = list;
            }

            list.Add(detail);
        }

        private static int RollInclusive(System.Random random, int minInclusive, int maxInclusive)
        {
            return random != null ? random.Next(minInclusive, maxInclusive + 1) : Random.Range(minInclusive, maxInclusive + 1);
        }

        private static bool IsInside(int x, int y, int columns, int rows)
        {
            return x >= 0 && y >= 0 && x < columns && y < rows;
        }

        private static IEnumerable<Vector2Int> GetAdjacentPositions(Vector2Int center, bool includeDiagonals)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (!includeDiagonals && Mathf.Abs(x) + Mathf.Abs(y) > 1)
                    {
                        continue;
                    }

                    yield return new Vector2Int(center.x + x, center.y + y);
                }
            }
        }
    }
}
