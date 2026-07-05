using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceSymbolCellView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private const int MaxBuffIcons = 3;
        private const float BackgroundAlpha = 0.38f;
        private static readonly Color CellOutlineColor = new Color(0f, 0f, 0f, 0.9f);

        [SerializeField] private Image icon;
        [SerializeField] private Image symbolIcon;
        [SerializeField] private Text label;
        [SerializeField] private RectTransform buffContainer;
        [SerializeField] private Text baseBonusText;
        [SerializeField] private Outline cellOutline;

        private readonly System.Collections.Generic.List<SubspaceTileBuffIconView> buffIcons = new System.Collections.Generic.List<SubspaceTileBuffIconView>();
        private SubspaceTileData tile;

        public void Configure(Image iconImage, Text textLabel)
        {
            icon = iconImage;
            label = textLabel;
            EnsureCultivationVisuals();
        }

        public void SetSymbol(SubspaceSymbolDefinition symbol, Sprite sprite)
        {
            SetTile(new SubspaceTileData(0, 0, symbol), sprite);
        }

        public void SetTile(SubspaceTileData tileData, Sprite sprite)
        {
            tile = tileData;
            var symbol = tile != null ? tile.currentSymbol : null;
            EnsureCultivationVisuals();

            if (icon != null)
            {
                icon.sprite = null;
                Color backgroundColor = symbol != null ? Color.Lerp(new Color(0.07f, 0.08f, 0.1f, 1f), symbol.SafeTint, 0.28f) : Color.white;
                backgroundColor.a = symbol != null ? BackgroundAlpha : 0f;
                icon.color = backgroundColor;
                icon.raycastTarget = true;
            }

            if (symbolIcon != null)
            {
                symbolIcon.gameObject.SetActive(sprite != null);
                symbolIcon.sprite = sprite;
                symbolIcon.color = sprite != null ? Color.white : Color.clear;
                symbolIcon.preserveAspect = true;
                symbolIcon.raycastTarget = false;
            }

            if (label != null)
            {
                label.text = sprite == null && symbol != null ? SubspaceTileRulebook.GetSymbolData(symbol).displayName : string.Empty;
                label.color = symbol != null && symbol.SafeTint.grayscale > 0.68f ? Color.black : Color.white;
                label.raycastTarget = false;
            }

            RefreshCultivationVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip(eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            ShowTooltip(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SubspaceTooltipManager.HideTooltip();
        }

        private void ShowTooltip(Vector2 screenPosition)
        {
            if (tile == null)
            {
                return;
            }

            if (IsPointerOverBuffArea(screenPosition))
            {
                return;
            }

            if (IsPointerOverSymbolArea(screenPosition))
            {
                var symbol = SubspaceTileRulebook.GetSymbolData(tile.currentSymbol);
                SubspaceTooltipManager.ShowTooltip(symbol.displayName, SubspaceTileRulebook.BuildSymbolTooltip(tile.currentSymbol), screenPosition);
                return;
            }

            SubspaceTooltipManager.ShowTooltip("\u5730\u5757\u4fe1\u606f", SubspaceTileRulebook.BuildTileTooltip(tile), screenPosition);
        }

        private void EnsureCultivationVisuals()
        {
            if (icon == null)
            {
                icon = GetComponent<Image>();
            }

            if (cellOutline == null)
            {
                cellOutline = GetComponent<Outline>();
                if (cellOutline == null)
                {
                    cellOutline = gameObject.AddComponent<Outline>();
                }

                cellOutline.effectColor = CellOutlineColor;
                cellOutline.effectDistance = new Vector2(1.5f, -1.5f);
                cellOutline.useGraphicAlpha = false;
            }

            if (symbolIcon == null)
            {
                var iconObject = new GameObject("Symbol Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(transform, false);
                symbolIcon = iconObject.GetComponent<Image>();
                symbolIcon.preserveAspect = true;
                symbolIcon.raycastTarget = false;

                var iconRect = symbolIcon.rectTransform;
                iconRect.anchorMin = new Vector2(0.16f, 0.16f);
                iconRect.anchorMax = new Vector2(0.84f, 0.84f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                iconRect.pivot = new Vector2(0.5f, 0.5f);
            }

            if (buffContainer == null)
            {
                var containerObject = new GameObject("Buff Icon Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                containerObject.transform.SetParent(transform, false);
                buffContainer = containerObject.GetComponent<RectTransform>();
                buffContainer.anchorMin = new Vector2(0f, 1f);
                buffContainer.anchorMax = new Vector2(0f, 1f);
                buffContainer.pivot = new Vector2(0f, 1f);
                buffContainer.anchoredPosition = new Vector2(4f, -4f);
                buffContainer.sizeDelta = new Vector2(86f, 20f);

                var layout = containerObject.GetComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 3f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            if (baseBonusText == null)
            {
                var textObject = new GameObject("Base Bonus Text", typeof(RectTransform), typeof(Text), typeof(Outline));
                textObject.transform.SetParent(transform, false);
                baseBonusText = textObject.GetComponent<Text>();
                baseBonusText.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 14);
                baseBonusText.fontSize = 14;
                baseBonusText.fontStyle = FontStyle.Bold;
                baseBonusText.alignment = TextAnchor.UpperRight;
                baseBonusText.color = new Color(1f, 0.86f, 0.18f, 1f);
                baseBonusText.raycastTarget = false;

                var outline = textObject.GetComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                outline.effectDistance = new Vector2(1f, -1f);

                var rect = baseBonusText.rectTransform;
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-5f, -4f);
                rect.sizeDelta = new Vector2(44f, 20f);
            }
        }

        private void RefreshCultivationVisuals()
        {
            if (baseBonusText != null)
            {
                baseBonusText.text = tile != null && tile.baseBonusScore > 0 ? $"+{tile.baseBonusScore}" : string.Empty;
            }

            if (buffContainer == null)
            {
                return;
            }

            int needed = tile != null ? Mathf.Min(MaxBuffIcons, tile.buffs.Count + tile.debuffs.Count) : 0;
            while (buffIcons.Count < needed)
            {
                buffIcons.Add(CreateBuffIcon());
            }

            int index = 0;
            if (tile != null)
            {
                for (int i = 0; i < tile.buffs.Count && index < MaxBuffIcons; i++)
                {
                    SetIcon(index++, tile.buffs[i]);
                }

                for (int i = 0; i < tile.debuffs.Count && index < MaxBuffIcons; i++)
                {
                    SetIcon(index++, tile.debuffs[i]);
                }
            }

            for (int i = index; i < buffIcons.Count; i++)
            {
                buffIcons[i].gameObject.SetActive(false);
            }
        }

        private SubspaceTileBuffIconView CreateBuffIcon()
        {
            var iconObject = new GameObject("Buff Icon", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(SubspaceTileBuffIconView));
            iconObject.transform.SetParent(buffContainer, false);

            var rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(16f, 16f);

            var image = iconObject.GetComponent<Image>();
            image.raycastTarget = true;

            var outline = iconObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.72f);
            outline.effectDistance = new Vector2(1f, -1f);

            var stackTextObject = new GameObject("Stack Text", typeof(RectTransform), typeof(Text));
            stackTextObject.transform.SetParent(iconObject.transform, false);
            var stackText = stackTextObject.GetComponent<Text>();
            stackText.font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 10);
            stackText.fontSize = 10;
            stackText.alignment = TextAnchor.MiddleCenter;
            stackText.color = Color.white;
            stackText.raycastTarget = false;
            Stretch(stackText.rectTransform);

            var view = iconObject.GetComponent<SubspaceTileBuffIconView>();
            view.Configure(image, stackText);
            return view;
        }

        private void SetIcon(int index, SubspaceTileBuffInstance instance)
        {
            buffIcons[index].gameObject.SetActive(true);
            buffIcons[index].SetBuff(instance);
        }

        private bool IsPointerOverSymbolArea(Vector2 screenPosition)
        {
            var rectTransform = transform as RectTransform;
            if (rectTransform == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, null, out var localPoint))
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            Rect symbolRect = new Rect(
                rect.xMin + rect.width * 0.22f,
                rect.yMin + rect.height * 0.22f,
                rect.width * 0.56f,
                rect.height * 0.56f);
            return symbolRect.Contains(localPoint);
        }

        private bool IsPointerOverBuffArea(Vector2 screenPosition)
        {
            if (buffContainer == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(buffContainer, screenPosition, null);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
