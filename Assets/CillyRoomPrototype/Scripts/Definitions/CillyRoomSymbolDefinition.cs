using UnityEngine;

namespace CillyRoomPrototype
{
    public enum CillyRoomSymbolEffect
    {
        None,
        MultiplyAdjacentSelectedSymbols
    }

    [CreateAssetMenu(menuName = "CillyRoom/Symbol", fileName = "CillyRoomSymbol")]
    public sealed class CillyRoomSymbolDefinition : ScriptableObject
    {
        public string symbolId = "number_1";
        public string displayName = "1";
        public int baseScore = 1;
        public Color tintColor = Color.white;
        public Sprite artwork;

        [Header("Prototype Effect Hook")]
        public CillyRoomSymbolEffect effect = CillyRoomSymbolEffect.None;
        public int effectMultiplier = 2;
        public bool effectIncludesDiagonals = true;

        public string SafeId => string.IsNullOrWhiteSpace(symbolId) ? name : symbolId;
        public string SafeDisplayName => string.IsNullOrWhiteSpace(displayName) ? SafeId : displayName;
        public int SafeBaseScore => baseScore;
        public int SafeMultiplier => Mathf.Max(1, effectMultiplier);
        public Color SafeTint => tintColor.a <= 0.01f ? Color.white : tintColor;
    }
}
