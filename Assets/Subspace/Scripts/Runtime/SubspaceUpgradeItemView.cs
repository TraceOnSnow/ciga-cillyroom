using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceUpgradeItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        private SubspaceUpgradeDefinition upgrade;

        public void Configure(Image iconImage, Text labelText)
        {
            icon = iconImage;
            label = labelText;
        }

        public void SetUpgrade(SubspaceUpgradeDefinition definition)
        {
            upgrade = definition;
            if (icon != null)
            {
                icon.color = upgrade != null ? new Color(0.98f, 0.84f, 0.22f, 1f) : Color.clear;
                icon.raycastTarget = upgrade != null;
            }

            if (label != null)
            {
                label.text = upgrade != null && !string.IsNullOrWhiteSpace(upgrade.displayName)
                    ? upgrade.displayName.Substring(0, Mathf.Min(1, upgrade.displayName.Length))
                    : string.Empty;
            }
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
            if (upgrade == null)
            {
                return;
            }

            var title = string.IsNullOrWhiteSpace(upgrade.displayName) ? upgrade.SafeId : upgrade.displayName;
            var body = string.IsNullOrWhiteSpace(upgrade.description) ? "\u5df2\u83b7\u5f97\u7684\u9053\u5177/\u589e\u76ca\u3002" : upgrade.description;
            SubspaceTooltipManager.ShowTooltip(title, body, screenPosition);
        }
    }
}
