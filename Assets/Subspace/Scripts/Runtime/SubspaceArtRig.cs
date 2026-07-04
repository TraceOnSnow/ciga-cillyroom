using System;
using System.Collections.Generic;
using UnityEngine;

namespace Subspace
{
    [DisallowMultipleComponent]
    public sealed class SubspaceArtRig : MonoBehaviour
    {
        [Header("Linked Data")]
        public SubspaceGameConfig gameConfig;
        public SubspaceArtSet artSet;

        [Header("Backgrounds")]
        public BackgroundSprites backgrounds = new BackgroundSprites();

        [Header("Characters")]
        public CharacterSprites player = new CharacterSprites();
        public CharacterSprites enemy = new CharacterSprites();

        [Header("Effects")]
        public EffectSprites effects = new EffectSprites();

        [Header("UI Frames")]
        public UISprites ui = new UISprites();

        [Header("Board Symbols")]
        public List<SymbolSpriteBinding> symbolSprites = new List<SymbolSpriteBinding>();

        public Sprite GetBriefingBackground(SubspaceLevelDefinition level, SubspaceArtSet fallback)
        {
            if (level != null && level.briefingBackgroundOverride != null)
            {
                return level.briefingBackgroundOverride;
            }

            if (backgrounds.briefingBackground != null)
            {
                return backgrounds.briefingBackground;
            }

            return fallback != null ? fallback.briefingBackground : null;
        }

        public Sprite GetCombatBackground(SubspaceArtSet fallback)
        {
            if (backgrounds.combatBackground != null)
            {
                return backgrounds.combatBackground;
            }

            return fallback != null ? fallback.combatBackground : null;
        }

        public Sprite GetRewardBackground(SubspaceArtSet fallback)
        {
            if (backgrounds.rewardBackground != null)
            {
                return backgrounds.rewardBackground;
            }

            return fallback != null ? fallback.rewardBackground : null;
        }

        public Sprite GetPlayerIdle(SubspaceArtSet fallback)
        {
            if (player.idle != null)
            {
                return player.idle;
            }

            return fallback != null ? fallback.playerIdleSprite : null;
        }

        public Sprite GetPlayerAttack(SubspaceArtSet fallback)
        {
            if (player.attack != null)
            {
                return player.attack;
            }

            return fallback != null ? fallback.playerAttackSprite : null;
        }

        public Sprite GetEnemyIdle(SubspaceLevelDefinition level, SubspaceArtSet fallback)
        {
            if (level != null && level.enemySpriteOverride != null)
            {
                return level.enemySpriteOverride;
            }

            if (enemy.idle != null)
            {
                return enemy.idle;
            }

            return fallback != null ? fallback.enemyIdleSprite : null;
        }

        public Sprite GetEnemyDefeated(SubspaceArtSet fallback)
        {
            if (enemy.defeated != null)
            {
                return enemy.defeated;
            }

            return fallback != null ? fallback.enemyDefeatedSprite : null;
        }

        public Sprite GetAttackEffect(SubspaceArtSet fallback)
        {
            if (effects.attackEffect != null)
            {
                return effects.attackEffect;
            }

            return fallback != null ? fallback.attackEffectSprite : null;
        }

        public Sprite GetSymbolSprite(SubspaceSymbolDefinition symbol)
        {
            if (symbol == null)
            {
                return null;
            }

            for (int i = 0; i < symbolSprites.Count; i++)
            {
                var binding = symbolSprites[i];
                if (binding == null || binding.sprite == null)
                {
                    continue;
                }

                if (binding.symbol == symbol)
                {
                    return binding.sprite;
                }

                if (!string.IsNullOrWhiteSpace(binding.symbolId) && binding.symbolId == symbol.SafeId)
                {
                    return binding.sprite;
                }
            }

            return symbol.artwork;
        }

        public Sprite GetTopStagePanel(SubspaceArtSet fallback) => ui.topStagePanel != null ? ui.topStagePanel : fallback != null ? fallback.topStagePanelSprite : null;
        public Sprite GetBuffPanel(SubspaceArtSet fallback) => ui.buffPanel != null ? ui.buffPanel : fallback != null ? fallback.buffPanelSprite : null;
        public Sprite GetPlayerPanel(SubspaceArtSet fallback) => ui.playerPanel != null ? ui.playerPanel : fallback != null ? fallback.playerPanelSprite : null;
        public Sprite GetBoardPanel(SubspaceArtSet fallback) => ui.boardPanel != null ? ui.boardPanel : fallback != null ? fallback.boardPanelSprite : null;
        public Sprite GetRightPanel(SubspaceArtSet fallback) => ui.rightPanel != null ? ui.rightPanel : fallback != null ? fallback.rightPanelSprite : null;
        public Sprite GetScorePanel(SubspaceArtSet fallback) => ui.scorePanel != null ? ui.scorePanel : fallback != null ? fallback.scorePanelSprite : null;
        public Sprite GetTurnPanel(SubspaceArtSet fallback) => ui.turnPanel != null ? ui.turnPanel : fallback != null ? fallback.turnPanelSprite : null;
        public Sprite GetRoundScorePanel(SubspaceArtSet fallback) => ui.roundScorePanel != null ? ui.roundScorePanel : fallback != null ? fallback.roundScorePanelSprite : null;
        public Sprite GetAttackButton(SubspaceArtSet fallback) => ui.attackButton != null ? ui.attackButton : fallback != null ? fallback.attackButtonSprite : null;
        public Sprite GetRewardCard(SubspaceArtSet fallback) => ui.rewardCard != null ? ui.rewardCard : fallback != null ? fallback.rewardCardSprite : null;
    }

    [Serializable]
    public sealed class BackgroundSprites
    {
        public Sprite briefingBackground;
        public Sprite combatBackground;
        public Sprite rewardBackground;
    }

    [Serializable]
    public sealed class CharacterSprites
    {
        public Sprite idle;
        public Sprite attack;
        public Sprite defeated;
    }

    [Serializable]
    public sealed class EffectSprites
    {
        public Sprite attackEffect;
        public Sprite victoryEffect;
    }

    [Serializable]
    public sealed class UISprites
    {
        public Sprite topStagePanel;
        public Sprite buffPanel;
        public Sprite playerPanel;
        public Sprite boardPanel;
        public Sprite rightPanel;
        public Sprite scorePanel;
        public Sprite turnPanel;
        public Sprite roundScorePanel;
        public Sprite attackButton;
        public Sprite rewardCard;
    }

    [Serializable]
    public sealed class SymbolSpriteBinding
    {
        public string symbolId;
        public SubspaceSymbolDefinition symbol;
        public Sprite sprite;
    }
}
