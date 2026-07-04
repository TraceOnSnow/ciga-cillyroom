using System;
using UnityEngine;

namespace CillyRoomPrototype
{
    [CreateAssetMenu(menuName = "CillyRoom/Text Config", fileName = "CillyRoomTextConfig")]
    public sealed class CillyRoomTextConfig : ScriptableObject
    {
        private static CillyRoomTextConfig runtimeDefault;

        [Header("Briefing")]
        public string briefingFallbackTitle = "任务简报";
        public string briefingContinueButtonText = "进入战斗";

        [Header("Game Labels")]
        public string initialLevelText = "第一关";
        public string playerLabel = "我方";
        public string enemyLabel = "敌人";
        public string buffPanelTitle = "道具";
        public string playerAnimationPanelTitle = "人物动画";
        public string attackButtonText = "攻击";

        [Header("Game UI Formats")]
        public string scoreFormat = "我方分数\n{0} / {1}";
        public string targetFormat = "血量条（总共要达到的分数） {0}";
        public string turnFormat = "剩余回合数\n{0}";
        public string roundScoreFormat = "本回合\n+{0}";
        public string roundScoreEmptyText = "本回合\n-";
        public string selectionHintText = "框选数字后点击攻击结算";

        [Header("Messages")]
        public string campaignCompleteTitle = "原型通关";
        [TextArea(2, 4)]
        public string campaignCompleteBody = "两个示例关卡已经完成。你可以重新开始，或继续扩展关卡配置。";
        public string campaignCompleteButtonText = "重新开始";
        public string failureTitle = "任务失败";
        [TextArea(2, 4)]
        public string failureBodyFormat = "还差 {0} 分。重新部署这一关，再试一次。";
        public string failureButtonText = "重试本关";
        public string messageContinueButtonText = "继续";

        [Header("Rewards")]
        public string rewardTitleFormat = "{0} 胜利奖励";
        public string rewardFallbackTitle = "胜利奖励";
        public string rewardSkipButtonText = "跳过奖励";
        public string rewardScoreFormat = "加入符号 {0}";

        public static CillyRoomTextConfig RuntimeDefault
        {
            get
            {
                if (runtimeDefault == null)
                {
                    runtimeDefault = CreateInstance<CillyRoomTextConfig>();
                    runtimeDefault.name = "Runtime CillyRoom Text Defaults";
                }

                return runtimeDefault;
            }
        }

        public string FormatScore(int current, int target) => SafeFormat(scoreFormat, "我方分数\n{0} / {1}", current, target);
        public string FormatTarget(int target) => SafeFormat(targetFormat, "血量条（总共要达到的分数） {0}", target);
        public string FormatTurns(int turns) => SafeFormat(turnFormat, "剩余回合数\n{0}", turns);
        public string FormatRoundScore(int score) => SafeFormat(roundScoreFormat, "本回合\n+{0}", score);
        public string FormatFailureBody(int missingScore) => SafeFormat(failureBodyFormat, "还差 {0} 分。重新部署这一关，再试一次。", missingScore);
        public string FormatRewardTitle(string levelName) => SafeFormat(rewardTitleFormat, "{0} 胜利奖励", levelName);
        public string FormatRewardScore(string symbolName) => SafeFormat(rewardScoreFormat, "加入符号 {0}", symbolName);

        private static string SafeFormat(string format, string fallback, params object[] args)
        {
            var template = string.IsNullOrWhiteSpace(format) ? fallback : format;
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return string.Format(fallback, args);
            }
        }
    }
}
