using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    public sealed class SubspaceSelectionController : MonoBehaviour
    {
        private const float SelectorAlpha = 0.33333334f;

        [SerializeField] private RectTransform selectorRect;
        [SerializeField] private SubspaceBoardController boardController;

       private Vector2Int origin;
       private int width = 2;
       private int height = 2;
       private readonly SubspaceSelectionShape shape = new SubspaceSelectionShape();
       private bool useCustomShape;
       private bool isDragging;

       public RectInt CurrentSelection => new RectInt(origin.x, origin.y, width, height);
       public SubspaceSelectionShape CurrentShape => shape;

       public void Configure(RectTransform selector, SubspaceBoardController board)
        {
            selectorRect = selector;
            boardController = board;
            EnsureVisibleSelector();
        }

        private void Awake()
        {
            EnsureVisibleSelector();
        }

        public void ResetSelection(int selectionWidth, int selectionHeight)
        {
           width = Mathf.Clamp(selectionWidth, 1, boardController.Columns);
           height = Mathf.Clamp(selectionHeight, 1, boardController.Rows);
           useCustomShape = false;
           origin = new Vector2Int(
               Mathf.Max(0, (boardController.Columns - width) / 2),
               Mathf.Max(0, (boardController.Rows - height) / 2));
           shape.SetRectangular(origin, width, height);
           RefreshVisual();
       }

       public void ApplyCustomShape(List<Vector2Int> offsets)
       {
           useCustomShape = true;
           var bounds = ComputeBounds(offsets);
           width = bounds.width;
           height = bounds.height;
           origin = new Vector2Int(
               Mathf.Max(0, (boardController.Columns - width) / 2),
               Mathf.Max(0, (boardController.Rows - height) / 2));
           shape.SetCustom(origin, offsets);
           RefreshVisual();
       }

       private static RectInt ComputeBounds(IReadOnlyList<Vector2Int> offsets)
       {
           if (offsets == null || offsets.Count == 0)
           {
               return new RectInt(0, 0, 1, 1);
           }

           int minX = 0, maxX = 0, minY = 0, maxY = 0;
           for (int i = 0; i < offsets.Count; i++)
           {
               if (offsets[i].x < minX) minX = offsets[i].x;
               if (offsets[i].x > maxX) maxX = offsets[i].x;
               if (offsets[i].y < minY) minY = offsets[i].y;
               if (offsets[i].y > maxY) maxY = offsets[i].y;
           }

           return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
       }

       private void Update()
        {
            if (boardController == null || boardController.BoardRect == null)
            {
                return;
            }

            HandleKeyboard();
            HandleMouse();
            RefreshVisual();
        }

        private void HandleKeyboard()
        {
            int dx = 0;
            int dy = 0;

            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                dx = -1;
            }
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                dx = 1;
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                dy = -1;
            }
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                dy = 1;
            }

           if (dx != 0 || dy != 0)
           {
               origin = new Vector2Int(
                   Mathf.Clamp(origin.x + dx, 0, boardController.Columns - width),
                   Mathf.Clamp(origin.y + dy, 0, boardController.Rows - height));
               shape.MoveTo(origin);
           }
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0) && boardController.TryGetCellFromScreen(Input.mousePosition, out var cell))
            {
                isDragging = true;
                CenterOn(cell);
            }

            if (Input.GetMouseButton(0) && isDragging && boardController.TryGetCellFromScreen(Input.mousePosition, out cell))
            {
                CenterOn(cell);
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }

       private void CenterOn(Vector2Int cell)
       {
           origin = new Vector2Int(
               Mathf.Clamp(cell.x - width / 2, 0, boardController.Columns - width),
               Mathf.Clamp(cell.y - height / 2, 0, boardController.Rows - height));
           shape.MoveTo(origin);
       }

        private void RefreshVisual()
        {
            if (selectorRect == null || boardController == null || boardController.BoardRect == null)
            {
                return;
            }

           Vector2 cellSize = boardController.GetCellSize();
           selectorRect.anchorMin = new Vector2(0f, 1f);
           selectorRect.anchorMax = new Vector2(0f, 1f);
           selectorRect.pivot = new Vector2(0f, 1f);
           var bounds = shape.GetBounds();
           selectorRect.sizeDelta = new Vector2(cellSize.x * bounds.width, cellSize.y * bounds.height);
           selectorRect.anchoredPosition = new Vector2(bounds.x * cellSize.x, -bounds.y * cellSize.y);
       }

        private void EnsureVisibleSelector()
        {
            if (selectorRect == null)
            {
                return;
            }

            var outline = selectorRect.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                var color = outline.effectColor;
                color.a = SelectorAlpha;
                outline.effectColor = color;
                outline.useGraphicAlpha = false;
            }
        }
    }
}
