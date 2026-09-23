using BurgerShop.Restaurant;
using UnityEngine.UI;
using UnityEngine;

namespace BurgerShop.UI
{
    // Chinese presentation of the existing goals/quotes, without owning progression or prices.
    public static class OpeningHudCopy
    {
        public static void Style(Text label,int size,bool fit=false)
        {
            label.font=HudChrome.ChineseFont();label.fontSize=size;
            label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            label.resizeTextForBestFit=fit;label.resizeTextMinSize=14;label.resizeTextMaxSize=size;
            label.lineSpacing=1.1f;
        }
        public static string Title(SessionGoalTracker g)
        {
            if(g.CanUpgrade)return $"可以升到 {g.Rank+1} 级";
            if(g.Rank==1)
            {
                switch(g.Opening?.Current)
                {
                    case OpeningGuide.Step.Stock:return "把汉堡放上柜台";
                    case OpeningGuide.Step.Serve:return "站到收银处，完成首单";
                    default:return "先取一个汉堡";
                }
            }
            if(g.TeachingPrerequisite!=null)
            {
                if(!MainHallExpansion.HasAccess)return "扩建小店，迎接员工";
                switch(g.Rank){case 3:return "雇用第一名员工";case 4:return "添置第二台汉堡机";case 5:case 6:return "添置蓝盒打包台";case 7:return "开设可乐区";}
            }
            return Translate(g.CapsuleTitle);
        }
        public static string Detail(SessionGoalTracker g)
        {
            if(g.CanUpgrade)return $"点击上方星条升级\n解锁：{Translate(ShopRanks.NextUnlock(g.Rank))}";
            if(g.Rank==1)return "完成首单 +2 星，即可升级开放堂食";
            if(g.TeachingPrerequisite!=null)return Translate(g.TeachingPrerequisite)+
                (g.Rank==3&&MainHallExpansion.HasAccess?"\n前往人事室雇用，员工会补货、收银和清洁":"\n点击查看商城");
            if(g.Rank==2&&!g.MilestoneComplete)return "顾客用餐后，清理整桌可得 +2 星";
            if(g.ShowsInvestment)return Translate(g.Investments?.Detail??"");
            if(g.IsCycle)return $"再赚 {g.MissingCoins:N0} 金币即可升级";
            return "体验新设施，完成后 +2 星\n投资设施也能获得星星";
        }
        public static string Translate(string text) => GameChinese.Translate(text);
    }
}
