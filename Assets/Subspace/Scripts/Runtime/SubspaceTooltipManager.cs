using UnityEngine;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceTooltipManager : MonoBehaviour
    {
        private const float Padding = 14f;

        private static SubspaceTooltipManager instance;

        [SerializeField] private RectTransform panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        private Canvas canvas;
        private Vector2 lastScreenPosition;
        private bool isVisible;

        public static SubspaceTooltipManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<SubspaceTooltipManager>(true);
                }

                if (instance == null)
                {
                    instance = CreateRuntimeInstance();
                }

                return instance;
            }
        }

        public static void ShowTooltip(string title, string body, Vector2 screenPosition)
        {
            Instance.Show(title, body, screenPosition);
        }

        public static void HideTooltip()
        {
            if (instance != null)
            {
                instance.Hide();
            }
        }

        public void Show(string title, string body, Vector2 screenPosition)
        {
            EnsureBuilt();
            lastScreenPosition = screenPosition;
            isVisible = true;

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = body;
            }

            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                panel.SetAsLastSibling();
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
                PositionPanel();
            }
        }

        public void Hide()
        {
            isVisible = false;
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureBuilt();
            Hide();
        }

        private void Update()
        {
            if (!isVisible || panel == null)
            {
                return;
            }

            lastScreenPosition = Input.mousePosition;
            PositionPanel();
        }

        private void EnsureBuilt()
        {
            if (panel != null && titleText != null && bodyText != null)
            {
                return;
            }

            canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                var canvasObject = new GameObject("Subspace Tooltip Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            transform.SetParent(canvas.transform, false);

            var panelObject = new GameObject("Tooltip Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObject.transform.SetParent(transform, false);
            panel = panelObject.GetComponent<RectTransform>();
            panel.pivot = new Vector2(0f, 1f);
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.sizeDelta = new Vector2(260f, 120f);

            var group = panelObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var image = panelObject.GetComponent<Image>();
            image.color = new Color(0.02f, 0.025f, 0.03f, 0.92f);

            var layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleText = CreateText(panel, "Title", 18, FontStyle.Bold, Color.white);
            bodyText = CreateText(panel, "Body", 15, FontStyle.Normal, new Color(0.9f, 0.94f, 1f, 1f));
            panel.gameObject.SetActive(false);
        }

        private void PositionPanel()
        {
            if (panel == null || canvas == null)
            {
                return;
            }

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, lastScreenPosition, null, out var localPoint);
            var size = panel.rect.size;
            float x = localPoint.x + Padding;
            float y = localPoint.y - Padding;

            float minX = canvasRect.rect.xMin + Padding;
            float maxX = canvasRect.rect.xMax - size.x - Padding;
            float minY = canvasRect.rect.yMin + size.y + Padding;
            float maxY = canvasRect.rect.yMax - Padding;

            panel.anchoredPosition = new Vector2(Mathf.Clamp(x, minX, maxX), Mathf.Clamp(y, minY, maxY));
        }

        private static SubspaceTooltipManager CreateRuntimeInstance()
        {
            var gameObject = new GameObject("Subspace Tooltip Manager", typeof(SubspaceTooltipManager));
            return gameObject.GetComponent<SubspaceTooltipManager>();
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, fontSize);
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var layout = textObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 300f;
            return text;
        }
    }
}
