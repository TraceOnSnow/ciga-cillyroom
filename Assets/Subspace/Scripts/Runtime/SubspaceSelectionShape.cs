using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    /// <summary>
    /// Arbitrary selection shape on the board. Replaces RectInt so we can support
    /// cross, L-shape, full-column, and other non-rectangular scanners.
    /// All positions are board-space (x=column, y=row, origin bottom-left).
    /// </summary>
    public sealed class SubspaceSelectionShape
    {
        private readonly List<Vector2Int> cells;

        public Vector2Int Origin { get; private set; }
        public IReadOnlyList<Vector2Int> Cells => cells;

        public int Count => cells.Count;

        public SubspaceSelectionShape()
        {
            cells = new List<Vector2Int>();
            Origin = Vector2Int.zero;
        }

        public SubspaceSelectionShape(Vector2Int origin, IEnumerable<Vector2Int> offsets)
        {
            Origin = origin;
            cells = new List<Vector2Int>();
            foreach (var offset in offsets)
            {
                cells.Add(origin + offset);
            }
        }

        public void SetRectangular(Vector2Int origin, int width, int height)
        {
            Origin = origin;
            cells.Clear();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells.Add(new Vector2Int(origin.x + x, origin.y + y));
                }
            }
        }

        public void SetCustom(Vector2Int origin, IEnumerable<Vector2Int> offsets)
        {
            Origin = origin;
            cells.Clear();
            foreach (var offset in offsets)
            {
                cells.Add(origin + offset);
            }
        }

        public void MoveTo(Vector2Int newOrigin)
        {
            var delta = newOrigin - Origin;
            Origin = newOrigin;
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i] += delta;
            }
        }

        public bool Contains(Vector2Int position)
        {
            return cells.Contains(position);
        }

        public RectInt GetBounds()
        {
            if (cells.Count == 0)
            {
                return new RectInt(0, 0, 0, 0);
            }

            int minX = cells[0].x, maxX = cells[0].x;
            int minY = cells[0].y, maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                if (cells[i].x < minX) minX = cells[i].x;
                if (cells[i].x > maxX) maxX = cells[i].x;
                if (cells[i].y < minY) minY = cells[i].y;
                if (cells[i].y > maxY) maxY = cells[i].y;
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        public List<Vector2Int> GetClampedPositions(int columns, int rows)
        {
            var result = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                if (cell.x >= 0 && cell.y >= 0 && cell.x < columns && cell.y < rows)
                {
                    result.Add(cell);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Context passed to SubspaceScoreResolver so individual tile scoring logic
    /// can query adjacent tiles, player upgrades, and other global state.
    /// </summary>
    public readonly struct SubspaceScoreContext
    {
        public readonly SubspaceTileData[,] tiles;
        public readonly int columns;
        public readonly int rows;
        public readonly IReadOnlyList<string> playerUpgrades;

        public SubspaceScoreContext(SubspaceTileData[,] boardTiles, IReadOnlyList<string> upgrades = null)
        {
            tiles = boardTiles;
            columns = boardTiles != null ? boardTiles.GetLength(0) : 0;
            rows = boardTiles != null ? boardTiles.GetLength(1) : 0;
            playerUpgrades = upgrades ?? new List<string>();
        }

        public SubspaceTileData GetTile(int x, int y)
        {
            if (tiles == null || x < 0 || y < 0 || x >= columns || y >= rows)
            {
                return null;
            }

            return tiles[x, y];
        }

        public SubspaceTileData GetAdjacent(SubspaceTileData tile, int dx, int dy)
        {
            if (tile == null)
            {
                return null;
            }

            return GetTile(tile.x + dx, tile.y + dy);
        }

        public bool HasUpgrade(string upgradeId)
        {
            for (int i = 0; i < playerUpgrades.Count; i++)
            {
                if (playerUpgrades[i] == upgradeId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}