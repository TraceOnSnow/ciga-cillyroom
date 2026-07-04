using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomBoardController : MonoBehaviour
    {
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private CillyRoomSymbolCellView cellPrefab;

        private readonly List<CillyRoomSymbolCellView> cells = new List<CillyRoomSymbolCellView>();
        private CillyRoomSymbolDefinition[,] board;
        private Func<CillyRoomSymbolDefinition, Sprite> spriteResolver;
        private System.Random random;
        private IReadOnlyList<CillyRoomSymbolDefinition> symbolPool;
        private int columns;
        private int rows;

        public RectTransform BoardRect => boardRect;
        public CillyRoomSymbolDefinition[,] Board => board;
        public int Columns => columns;
        public int Rows => rows;

        public void Configure(RectTransform rect, GridLayoutGroup layout, CillyRoomSymbolCellView prefab)
        {
            boardRect = rect;
            gridLayout = layout;
            cellPrefab = prefab;
        }

        public void Build(int newColumns, int newRows, IReadOnlyList<CillyRoomSymbolDefinition> pool, System.Random rng, Func<CillyRoomSymbolDefinition, Sprite> resolveSprite)
        {
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            symbolPool = pool;
            random = rng;
            spriteResolver = resolveSprite;
            board = new CillyRoomSymbolDefinition[columns, rows];

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
                    board[x, y] = GetRandomSymbol();
                }
            }
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

                    board[x, y] = GetRandomSymbol();
                }
            }

            RefreshAll();
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
                    cells[index].SetSymbol(symbol, spriteResolver != null ? spriteResolver(symbol) : symbol != null ? symbol.artwork : null);
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

        private CillyRoomSymbolDefinition GetRandomSymbol()
        {
            if (symbolPool == null || symbolPool.Count == 0)
            {
                return null;
            }

            return symbolPool[random.Next(0, symbolPool.Count)];
        }
    }
}
