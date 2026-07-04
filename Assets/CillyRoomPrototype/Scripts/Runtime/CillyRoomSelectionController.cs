using UnityEngine;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomSelectionController : MonoBehaviour
    {
        private const float SelectorAlpha = 0.33333334f;

        [SerializeField] private RectTransform selectorRect;
        [SerializeField] private CillyRoomBoardController boardController;

        private Vector2Int origin;
        private int width = 2;
        private int height = 2;
        private bool isDragging;

        public RectInt CurrentSelection => new RectInt(origin.x, origin.y, width, height);

        public void Configure(RectTransform selector, CillyRoomBoardController board)
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
            origin = new Vector2Int(
                Mathf.Max(0, (boardController.Columns - width) / 2),
                Mathf.Max(0, (boardController.Rows - height) / 2));
            RefreshVisual();
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
            selectorRect.sizeDelta = new Vector2(cellSize.x * width, cellSize.y * height);
            selectorRect.anchoredPosition = new Vector2(origin.x * cellSize.x, -origin.y * cellSize.y);
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
