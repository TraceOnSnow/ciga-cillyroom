using System.Collections.Generic;
using UnityEngine;

namespace CillyRoomPrototype
{
    public readonly struct CillyRoomScoreResult
    {
        public readonly int total;
        public readonly int turnDelta;
        public readonly List<string> lines;
        public readonly List<CillyRoomScoreSource> sources;

        public CillyRoomScoreResult(int total, int turnDelta, List<string> lines, List<CillyRoomScoreSource> sources = null)
        {
            this.total = total;
            this.turnDelta = turnDelta;
            this.lines = lines;
            this.sources = sources ?? new List<CillyRoomScoreSource>();
        }
    }

    public readonly struct CillyRoomScoreSource
    {
        public readonly string displayName;
        public readonly Vector2Int position;
        public readonly int originalScore;
        public readonly int baseScore;
        public readonly int multiplier;
        public readonly int finalScore;
        public readonly string detail;

        public CillyRoomScoreSource(string displayName, Vector2Int position, int originalScore, int baseScore, int multiplier, int finalScore, string detail = "")
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

    public static class CillyRoomScoreResolver
    {
        private static readonly Vector2Int BonusScoreKey = new Vector2Int(int.MinValue, int.MinValue);

        public static CillyRoomScoreResult Calculate(CillyRoomSymbolDefinition[,] board, RectInt selection, System.Random random = null)
        {
            var baseScores = new Dictionary<Vector2Int, int>();
            var originalScores = new Dictionary<Vector2Int, int>();
            var multipliers = new Dictionary<Vector2Int, int>();
            var sourceDetails = new Dictionary<Vector2Int, List<string>>();
            var positions = new List<Vector2Int>();
            var energyCorePositions = new List<Vector2Int>();
            var lines = new List<string>();
            var sources = new List<CillyRoomScoreSource>();

            if (board == null)
            {
                return new CillyRoomScoreResult(0, 0, lines, sources);
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
                        case CillyRoomSymbolKind.LifeSignal:
                            score = RollInclusive(random, 1, 6);
                            AddDetail(sourceDetails, position, $"生命信号随机得分 {score}");
                            lifeSignalCount++;
                            break;
                        case CillyRoomSymbolKind.RealitySingularity:
                            score = RollInclusive(random, -2, 5);
                            AddDetail(sourceDetails, position, $"现实奇点随机基础分 {score}");
                            break;
                        case CillyRoomSymbolKind.Anchor:
                            hasSelectedAnchor = true;
                            break;
                        case CillyRoomSymbolKind.EnergyCore:
                            energyCorePositions.Add(position);
                            energyCoreCount++;
                            break;
                        case CillyRoomSymbolKind.CosmicDust:
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
                sources.Add(new CillyRoomScoreSource("生命体检测", BonusScoreKey, 25, 25, 1, 25, "5 个生命信号在同一框内触发一次性奖励"));
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
                sources.Add(new CillyRoomScoreSource(symbol.SafeDisplayName, entry.Key, originalScore, entry.Value, multiplier, finalScore, detail));
            }

            return new CillyRoomScoreResult(total, turnDelta, lines, sources);
        }

        private static void ApplyAnchorEffects(
            CillyRoomSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, List<string>> sourceDetails,
            IReadOnlyList<Vector2Int> positions,
            int cosmicDustCount,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeKind != CillyRoomSymbolKind.Anchor)
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
                    if (adjacentSymbol.SafeKind == CillyRoomSymbolKind.Beacon)
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
            CillyRoomSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, List<string>> sourceDetails,
            IReadOnlyList<Vector2Int> positions,
            bool hasSelectedAnchor,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeKind != CillyRoomSymbolKind.SubspaceRift)
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
                    if (baseScores.ContainsKey(adjacent) && board[adjacent.x, adjacent.y].SafeKind == CillyRoomSymbolKind.Beacon)
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
            CillyRoomSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, int> multipliers,
            Dictionary<Vector2Int, List<string>> sourceDetails,
            IReadOnlyList<Vector2Int> positions,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeKind != CillyRoomSymbolKind.RealitySingularity)
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
