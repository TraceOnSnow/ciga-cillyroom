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
           int total = 0;

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

               var symbol = SubspaceTileRulebook.GetSymbolData(tile.currentSymbol);
               int tileModifier = SubspaceTileRulebook.GetScoreModifier(tile);
               int finalScore = symbol.instantScore + tile.baseBonusScore + tileModifier;
               total += finalScore;

               string detail = $"即时 {symbol.instantScore}, 地块 {SubspaceTileRulebook.FormatSigned(tile.baseBonusScore)}, 状态 {SubspaceTileRulebook.FormatSigned(tileModifier)}";
               lines.Add($"{symbol.displayName}: {finalScore}");
               sources.Add(new SubspaceScoreSource(
                   symbol.displayName,
                   position,
                   symbol.instantScore,
                   finalScore,
                   1,
                   finalScore,
                   detail));

              SubspaceTileRulebook.ApplySymbolTileEffects(symbol, tile);
          }

          ApplySynergyBonuses(tiles, shape, context, total, lines, sources, ref total);

          return new SubspaceScoreResult(total, 0, lines, sources);
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
