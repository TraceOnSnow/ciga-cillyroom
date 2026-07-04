using UnityEngine;

namespace CillyRoomPrototype
{
    [CreateAssetMenu(menuName = "CillyRoom/Art Set", fileName = "CillyRoomArtSet")]
    public sealed class CillyRoomArtSet : ScriptableObject
    {
        [Header("Replaceable Sprites")]
        public Sprite briefingBackground;
        public Sprite combatBackground;
        public Sprite rewardBackground;
        public Sprite playerIdleSprite;
        public Sprite playerAttackSprite;
        public Sprite enemyIdleSprite;
        public Sprite enemyDefeatedSprite;
        public Sprite attackEffectSprite;
        public Sprite topStagePanelSprite;
        public Sprite buffPanelSprite;
        public Sprite playerPanelSprite;
        public Sprite boardPanelSprite;
        public Sprite rightPanelSprite;
        public Sprite scorePanelSprite;
        public Sprite turnPanelSprite;
        public Sprite roundScorePanelSprite;
        public Sprite attackButtonSprite;
        public Sprite rewardCardSprite;

        [Header("Fallback Colors")]
        public Color backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
        public Color panelColor = new Color(0.16f, 0.17f, 0.19f, 0.96f);
        public Color boardColor = new Color(0.09f, 0.1f, 0.12f, 1f);
        public Color playerColor = new Color(0.23f, 0.58f, 0.92f, 1f);
        public Color playerAttackColor = new Color(0.99f, 0.73f, 0.2f, 1f);
        public Color enemyColor = new Color(0.86f, 0.24f, 0.22f, 1f);
        public Color defeatedEnemyColor = new Color(0.32f, 0.32f, 0.36f, 1f);
        public Color selectorColor = new Color(1f, 0.56f, 0.18f, 0.24f);
        public Color selectorOutlineColor = new Color(1f, 0.43f, 0.12f, 1f);
        public Color accentColor = new Color(0.09f, 0.72f, 0.94f, 1f);
    }
}
