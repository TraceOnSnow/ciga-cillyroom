using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Subspace
{
    public sealed class SubspaceBoardController : MonoBehaviour
    {
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private SubspaceSymbolCellView cellPrefab;
        [Header("Refresh Animation")]
        [SerializeField] private GameObject disappearPrefab;
        [SerializeField] private float disappearLifetime = 0.8f;
        [SerializeField] private Vector3 disappearOffset;
        [SerializeField] private Vector2 disappearScaleMultiplier = Vector2.one;
        [SerializeField] private bool logDisappearEffect;

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

        public void SetDisappearPrefab(GameObject prefab)
        {
            disappearPrefab = prefab;
        }

        private void Awake()
        {
            AutoAssignDisappearPrefabInEditor();
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
                   PlayDisappearEffect(x, y);
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

                    PlayDisappearEffect(x, y);
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

        private void PlayDisappearEffect(int x, int y)
        {
            AutoAssignDisappearPrefabInEditor();
            if (disappearPrefab == null)
            {
                if (logDisappearEffect)
                {
                    Debug.LogWarning("[Subspace Board] Disappear effect skipped: prefab is missing.");
                }

                return;
            }

            if (disappearPrefab.GetComponent<RectTransform>() != null || disappearPrefab.GetComponentInChildren<Graphic>(true) != null)
            {
                PlayUiPrefabDisappearEffect(x, y);
                return;
            }

            PlaySpritePrefabAsUiDisappearEffect(x, y);
        }

        private RectTransform GetCellRect(int x, int y)
        {
            RefreshCellSize();
            Canvas.ForceUpdateCanvases();
            if (gridLayout != null)
            {
                var gridRect = gridLayout.transform as RectTransform;
                if (gridRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
                }
            }

            int index = y * columns + x;
            if (index >= 0 && index < cells.Count && cells[index] != null)
            {
                return cells[index].GetComponent<RectTransform>();
            }

            return null;
        }

        private Vector2 GetFallbackCellLocalPosition(int x, int y)
        {
            Vector2 cellSize = GetCellSize();
            Rect rect = boardRect.rect;
            float localX = rect.xMin + cellSize.x * (x + 0.5f);
            float localY = rect.yMax - cellSize.y * (y + 0.5f);
            return new Vector2(localX, localY);
        }

        private void PlayUiPrefabDisappearEffect(int x, int y)
        {
            RectTransform cellRect = GetCellRect(x, y);
            Transform parent = cellRect != null ? cellRect : boardRect;
            if (parent == null)
            {
                return;
            }

            var instance = Instantiate(disappearPrefab, parent);
            instance.name = $"{disappearPrefab.name} ({x}, {y})";

            var rect = instance.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = instance.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = cellRect != null ? (Vector2)disappearOffset : GetFallbackCellLocalPosition(x, y) + (Vector2)disappearOffset;
            Vector2 cellSize = cellRect != null ? cellRect.rect.size : GetCellSize();
            rect.sizeDelta = new Vector2(
                cellSize.x * Mathf.Max(0.01f, disappearScaleMultiplier.x),
                cellSize.y * Mathf.Max(0.01f, disappearScaleMultiplier.y));
            instance.transform.SetAsLastSibling();
            Destroy(instance, Mathf.Max(0.05f, disappearLifetime));
        }

        private void PlaySpritePrefabAsUiDisappearEffect(int x, int y)
        {
            RectTransform cellRect = GetCellRect(x, y);
            Transform parent = cellRect != null ? cellRect : boardRect;
            if (parent == null)
            {
                return;
            }

            var renderer = disappearPrefab.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            var effectObject = new GameObject($"{disappearPrefab.name} UI ({x}, {y})", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            effectObject.transform.SetParent(parent, false);
            effectObject.transform.SetAsLastSibling();

            var rect = effectObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = cellRect != null ? (Vector2)disappearOffset : GetFallbackCellLocalPosition(x, y) + (Vector2)disappearOffset;

            Vector2 cellSize = cellRect != null ? cellRect.rect.size : GetCellSize();
            rect.sizeDelta = new Vector2(
                cellSize.x * Mathf.Max(0.01f, disappearScaleMultiplier.x),
                cellSize.y * Mathf.Max(0.01f, disappearScaleMultiplier.y));

            var image = effectObject.GetComponent<Image>();
            image.sprite = renderer.sprite;
            image.color = renderer.color;
            image.raycastTarget = false;
            image.preserveAspect = true;

            StartCoroutine(PlayUiDisappearFallback(effectObject, image));
            if (logDisappearEffect)
            {
                Debug.Log($"[Subspace Board] Played UI disappear effect at ({x}, {y}) under {(cellRect != null ? cellRect.name : parent.name)}.");
            }
        }

        private IEnumerator PlayUiDisappearFallback(GameObject effectObject, Image image)
        {
            float duration = Mathf.Max(0.05f, disappearLifetime);
            float elapsed = 0f;
            Vector3 startScale = effectObject.transform.localScale;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(1f, 0f, t);
                image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
                effectObject.transform.localScale = Vector3.Lerp(startScale, startScale * 1.12f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(effectObject);
        }

        private void AutoAssignDisappearPrefabInEditor()
        {
#if UNITY_EDITOR
            if (disappearPrefab == null)
            {
                disappearPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Land/Disappear.prefab");
            }
#endif
        }
    }
}
