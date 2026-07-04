using UnityEngine;
using UnityEngine.EventSystems;

namespace Subspace
{
    public sealed class SubspaceDamageTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        private string title = string.Empty;
        private string body = string.Empty;

        public void SetTooltip(string tooltipTitle, string tooltipBody)
        {
            title = tooltipTitle ?? string.Empty;
            body = tooltipBody ?? string.Empty;
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
            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            SubspaceTooltipManager.ShowTooltip(title, body, screenPosition);
        }
    }
}
