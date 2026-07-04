using UnityEngine;
using UnityEngine.UI;

namespace CillyRoomPrototype
{
    public sealed class CillyRoomSymbolCellView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;

        public void Configure(Image iconImage, Text textLabel)
        {
            icon = iconImage;
            label = textLabel;
        }

        public void SetSymbol(CillyRoomSymbolDefinition symbol, Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.color = sprite != null ? Color.white : symbol != null ? symbol.SafeTint : Color.white;
            }

            if (label != null)
            {
                label.text = symbol != null ? symbol.SafeDisplayName : string.Empty;
                label.color = symbol != null && symbol.SafeTint.grayscale > 0.68f ? Color.black : Color.white;
            }
        }
    }
}
