using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Subspace
{
    public sealed class SubspaceTileBuffIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;

        private SubspaceTileBuffInstance instance;

        public void Configure(Image iconImage, Text textLabel)
        {
            icon = iconImage;
            label = textLabel;
        }

        public void SetBuff(SubspaceTileBuffInstance buffInstance)
        {
            instance = buffInstance;
            if (icon == null)
            {
                icon = GetComponent<Image>();
            }

            if (icon != null)
            {
                icon.color = instance != null && instance.data != null ? instance.data.color : Color.white;
                icon.raycastTarget = true;
            }

            if (label != null)
            {
                label.text = instance != null && instance.stacks > 1 ? instance.stacks.ToString() : string.Empty;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Show(eventData.position);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            Show(eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SubspaceTooltipManager.HideTooltip();
        }

        private void Show(Vector2 position)
        {
            if (instance == null || instance.data == null)
            {
                return;
            }

            SubspaceTooltipManager.ShowTooltip(instance.data.displayName, SubspaceTileRulebook.BuildBuffTooltip(instance), position);
        }
    }
}
