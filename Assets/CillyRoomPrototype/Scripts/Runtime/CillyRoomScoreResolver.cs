using System.Collections.Generic;
using UnityEngine;

namespace CillyRoomPrototype
{
    public readonly struct CillyRoomScoreResult
    {
        public readonly int total;
        public readonly int turnDelta;
        public readonly List<string> lines;

        public CillyRoomScoreResult(int total, int turnDelta, List<string> lines)
        {
            this.total = total;
            this.turnDelta = turnDelta;
            this.lines = lines;
        }
    }

    public static class CillyRoomScoreResolver
    {
        private static readonly Vector2Int BonusScoreKey = new Vector2Int(int.MinValue, int.MinValue);

        public static CillyRoomScoreResult Calculate(CillyRoomSymbolDefinition[,] board, RectInt selection, System.Random random = null)
        {
            var baseScores = new Dictionary<Vector2Int, int>();
            var multipliers = new Dictionary<Vector2Int, int>();
            var positions = new List<Vector2Int>();
            var lines = new List<string>();

            if (board == null)
            {
                return new CillyRoomScoreResult(0, 0, lines);
            }

            int columns = board.GetLength(0);
            int rows = board.GetLength(1);
            int lifeSignalCount = 0;
            int energyCoreCount = 0;
            int cosmicDustCount = 0;

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
                    switch (symbol.SafeId)
                    {
                        case "life_signal":
                            score = RollInclusive(random, 1, 6);
                            lifeSignalCount++;
                            break;
                        case "reality_singularity":
                            score = RollInclusive(random, -2, 5);
                            break;
                        case "energy_core":
                            energyCoreCount++;
                            break;
                        case "cosmic_dust":
                            cosmicDustCount++;
                            break;
                    }

                    baseScores[position] = score;
                }
            }

            ApplyAnchorEffects(board, baseScores, positions, cosmicDustCount, lines);
            ApplySubspaceRifts(board, baseScores, positions, lines);
            ApplyRealitySingularities(board, baseScores, multipliers, positions, lines);

            int turnDelta = 0;
            if (lifeSignalCount >= 5)
            {
                baseScores[BonusScoreKey] = 25;
                multipliers[BonusScoreKey] = 1;
                lines.Add("生命体检测: +25");
            }

            if (energyCoreCount >= 5)
            {
                turnDelta += 1;
                lines.Add("能量核心: 回合 +1");
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
            }

            return new CillyRoomScoreResult(total, turnDelta, lines);
        }

        private static void ApplyAnchorEffects(
            CillyRoomSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            IReadOnlyList<Vector2Int> positions,
            int cosmicDustCount,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeId != "anchor")
                {
                    continue;
                }

                int adjacentBeaconCount = 0;
                bool connectedToRift = false;
                foreach (var adjacent in GetAdjacentPositions(position, true))
                {
                    if (!baseScores.ContainsKey(adjacent))
                    {
                        continue;
                    }

                    var adjacentSymbol = board[adjacent.x, adjacent.y];
                    if (adjacentSymbol.SafeId == "beacon")
                    {
                        adjacentBeaconCount++;
                    }
                    else if (adjacentSymbol.SafeId == "subspace_rift")
                    {
                        connectedToRift = true;
                    }
                }

                int beaconBonus = adjacentBeaconCount * 5;
                int dustPenalty = adjacentBeaconCount * cosmicDustCount;
                int finalBonus = beaconBonus - dustPenalty;
                baseScores[position] += finalBonus;

                if (beaconBonus > 0)
                {
                    lines.Add($"锚点: 相邻信标 +{beaconBonus}");
                }

                if (dustPenalty > 0)
                {
                    lines.Add($"宇宙尘埃: 锚点数值 -{dustPenalty}");
                }

                if (connectedToRift)
                {
                    lines.Add("锚点: 抵消相邻亚空间裂缝");
                }
            }
        }

        private static void ApplySubspaceRifts(
            CillyRoomSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            IReadOnlyList<Vector2Int> positions,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeId != "subspace_rift" || IsRiftCancelledByAnchor(board, baseScores, position))
                {
                    continue;
                }

                foreach (var adjacent in GetAdjacentPositions(position, true))
                {
                    if (baseScores.ContainsKey(adjacent) && board[adjacent.x, adjacent.y].SafeId == "beacon")
                    {
                        baseScores[adjacent] = 0;
                        lines.Add("亚空间裂缝: 相邻信标失效");
                    }
                }
            }
        }

        private static void ApplyRealitySingularities(
            CillyRoomSymbolDefinition[,] board,
            Dictionary<Vector2Int, int> baseScores,
            Dictionary<Vector2Int, int> multipliers,
            IReadOnlyList<Vector2Int> positions,
            List<string> lines)
        {
            foreach (var position in positions)
            {
                var symbol = board[position.x, position.y];
                if (symbol.SafeId != "reality_singularity")
                {
                    continue;
                }

                foreach (var adjacent in GetAdjacentPositions(position, true))
                {
                    if (baseScores.ContainsKey(adjacent))
                    {
                        multipliers[adjacent] *= 2;
                    }
                }

                lines.Add("现实奇点: 相邻元素数值 x2");
            }
        }

        private static bool IsRiftCancelledByAnchor(CillyRoomSymbolDefinition[,] board, Dictionary<Vector2Int, int> baseScores, Vector2Int riftPosition)
        {
            foreach (var adjacent in GetAdjacentPositions(riftPosition, true))
            {
                if (baseScores.ContainsKey(adjacent) && board[adjacent.x, adjacent.y].SafeId == "anchor")
                {
                    return true;
                }
            }

            return false;
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
