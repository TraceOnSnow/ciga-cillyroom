using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceBoardController : MonoBehaviour
    {
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private SubspaceSymbolCellView cellPrefab;

        private readonly List<SubspaceSymbolCellView> cells = new List<SubspaceSymbolCellView>();
        private SubspaceSymbolDefinition[,] board;
        private SubspaceTileData[,] tiles;
        private Func<SubspaceSymbolDefinition, Sprite> spriteResolver;
        private System.Random random;
        private IReadOnlyList<SubspaceSymbolDefinition> symbolPool;
        private int columns;
        private int rows;

        public RectTransform BoardRect => boardRect;
        public SubspaceSymbolDefinition[,] Board => board;
        public SubspaceTileData[,] Tiles => tiles;
        public int Columns => columns;
        public int Rows => rows;

        public void Configure(RectTransform rect, GridLayoutGroup layout, SubspaceSymbolCellView prefab)
        {
            boardRect = rect;
            gridLayout = layout;
            cellPrefab = prefab;
        }

        public void Build(int newColumns, int newRows, IReadOnlyList<SubspaceSymbolDefinition> pool, System.Random rng, Func<SubspaceSymbolDefinition, Sprite> resolveSprite)
        {
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            symbolPool = pool;
            random = rng;
            spriteResolver = resolveSprite;
            board = new SubspaceSymbolDefinition[columns, rows];
            tiles = new SubspaceTileData[columns, rows];

            EnsureCells();
            FillAll();
            RefreshAll();
        }

       public void FillAll()
       {
           for (int y = 0; y < rows; y++)
           {
               for (int x = 0; x < columns; x++)
               {
                   SetCurrentSymbol(x, y, GetRandomSymbol(), true);
               }
           }
       }

       public void RerollAllSymbols()
       {
           for (int y = 0; y < rows; y++)
           {
               for (int x = 0; x < columns; x++)
               {
                   var symbol = GetRandomSymbol();
                   board[x, y] = symbol;
                   if (tiles[x, y] == null)
                   {
                       tiles[x, y] = new SubspaceTileData(x, y, symbol);
                   }
                   else
                   {
                       tiles[x, y].currentSymbol = symbol;
                   }
               }
           }

           RefreshAll();
       }

       public void RerollOutside(RectInt protectedSelection)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (protectedSelection.Contains(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    SetCurrentSymbol(x, y, GetRandomSymbol(), false);
                }
            }

            RefreshAll();
        }

        public SubspaceTileData GetTile(int x, int y)
        {
            if (tiles == null || x < 0 || y < 0 || x >= columns || y >= rows)
            {
                return null;
            }

            return tiles[x, y];
        }

        public void RefreshAll()
        {
            RefreshCellSize();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    var symbol = board[x, y];
                    cells[index].SetTile(tiles[x, y], spriteResolver != null ? spriteResolver(symbol) : symbol != null ? symbol.artwork : null);
                }
            }
        }

        public bool TryGetCellFromScreen(Vector2 screenPosition, out Vector2Int cell)
        {
            cell = default;
            if (boardRect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, screenPosition, null, out var localPoint))
            {
                return false;
            }

            Rect rect = boardRect.rect;
            if (!rect.Contains(localPoint))
            {
                return false;
            }

            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float normalizedY = Mathf.InverseLerp(rect.yMax, rect.yMin, localPoint.y);
            cell = new Vector2Int(
                Mathf.Clamp(Mathf.FloorToInt(normalizedX * columns), 0, columns - 1),
                Mathf.Clamp(Mathf.FloorToInt(normalizedY * rows), 0, rows - 1));
            return true;
        }

        public Vector2 GetCellSize()
        {
            if (gridLayout == null)
            {
                return Vector2.one;
            }

            RefreshCellSize();
            return gridLayout.cellSize;
        }

        private void EnsureCells()
        {
            for (int i = cells.Count - 1; i >= columns * rows; i--)
            {
                if (cells[i] != null)
                {
                    Destroy(cells[i].gameObject);
                }

                cells.RemoveAt(i);
            }

            while (cells.Count < columns * rows)
            {
                var cell = Instantiate(cellPrefab, gridLayout.transform);
                cell.name = $"Symbol Cell {cells.Count + 1}";
                cell.gameObject.SetActive(true);
                cells.Add(cell);
            }

            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = columns;
            }
        }

        private void RefreshCellSize()
        {
            if (gridLayout == null || boardRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            float width = boardRect.rect.width > 1f ? boardRect.rect.width : boardRect.sizeDelta.x;
            float height = boardRect.rect.height > 1f ? boardRect.rect.height : boardRect.sizeDelta.y;
            gridLayout.cellSize = new Vector2(width / columns, height / rows);
            gridLayout.spacing = Vector2.zero;
        }

        private SubspaceSymbolDefinition GetRandomSymbol()
        {
            if (symbolPool == null || symbolPool.Count == 0)
            {
                return null;
            }

            return symbolPool[random.Next(0, symbolPool.Count)];
        }

        private void SetCurrentSymbol(int x, int y, SubspaceSymbolDefinition symbol, bool createTileIfMissing)
        {
            board[x, y] = symbol;
            if (tiles[x, y] == null)
            {
                if (!createTileIfMissing)
                {
                    tiles[x, y] = new SubspaceTileData(x, y, symbol);
                    return;
                }

                tiles[x, y] = new SubspaceTileData(x, y, symbol);
            }
            else
            {
                tiles[x, y].currentSymbol = symbol;
            }
        }
    }
}
