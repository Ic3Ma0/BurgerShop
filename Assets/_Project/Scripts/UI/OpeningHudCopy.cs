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
        public static string Translate(string text)
        {
            if(string.IsNullOrEmpty(text))return "";
            string[,] copy={
                {"Own space: machine ","自有空间：机器 "},{"Optional wing ","可选侧翼 "},{" · total "," · 合计 "},
                {"Production is short of demand","持续缺货：先改善生产"},{"Stock is waiting for transport","机上有货：先改善搬运"},{"Stock ready · customers are waiting","柜台有货：改善出餐速度"},{"Clean seats are full","干净座位已满：增加座位"},
                {"Service interval ","出餐间隔 "},{"Hire staff first","先雇用员工"},
                {"No valid star source · progression needs review","暂无可用投资"},
                {"No eligible upgrade found; check land and staff access. This is not a sell-more task.","请检查扩建与员工是否已开放"},
                {"Tap to locate · other investments also earn stars","点击定位，也可选择其他投资"},
                {"Expand main hall & hire staff","扩建主厅并雇用员工"},
                {"Upgrade burger machine","升级汉堡机"},{"Upgrade cola machine","升级可乐机"},
                {"Upgrade player speed","提升移动速度"},{"Upgrade player carry","提升携带能力"},
                {"Upgrade staff speed","提升员工速度"},{"Upgrade staff carry","提升员工携带"},
                {"Upgrade table · ","更换餐桌 · "},{"Expand main hall","扩建主厅"},
                {"More workspace · staff access · land ","增加空间，开放员工 · 土地 "},{" + first staff "," + 首位员工 "},
                {"Hire your first employee · ","雇用首位员工 · "},{" then hire staff "," 再雇员工 "},
                {"Second burger machine · ","第二台汉堡机 · "},{"Blue-box packing · ","蓝盒打包台 · "},
                {"Car orders need blue boxes · build packing","汽车订单需要蓝盒，请先建打包台"},
                {"Cola opening: land ","可乐区：土地 "},{" + machine "," + 机器 "},{" + counter "," + 柜台 "},
                {"Clear a used dining table","清理一张餐桌"},{"Let staff complete an order","让员工完成一单"},
                {"Produce on the second grill","用第二台机器生产汉堡"},{"Pack a blue box","打包一个蓝盒"},
                {"Complete a drive-thru order","完成一单汽车订单"},{"Sell a cup of cola","卖出一杯可乐"},
                {"Complete a courier order","完成一单骑手订单"},{"Fulfil an automated courier order","完成一单自动配送"},
                {"Grow cash income","提升营业收入"},{"Choose an investment","选择一项经营投资"},
                {"Table & chairs","餐桌椅"},{"Dining counter","堂食柜台"},{"Boxing table","打包台"},
                {"Bistro","小馆款"},{"Diner","快餐款"},{"Patio","庭院款"},
                {"Item every ","出餐间隔 "},{"Packing ","打包时间 "},{"Style ","款式 "},{"Tip ","小费 "},
                {"Production ","出餐 "},{"Speed ","速度 "},{"Carry ","携带 "},{"Meal pay ","餐费 "},{"eat ","用餐 "},
                {"+3% empty speed","空手速度 +3%"},{" stars to upgrade"," 星可升级"},{" stars"," 星"},
                {" coins"," 金币"},{"Earn ","还差 "},{" more",""},{"supermarket","商城"},
                {"Dining, cleaning & trash bins","堂食、清桌和垃圾桶"},
                {"Second grill, burger counter & restroom","第二台汉堡机、柜台和卫生间"},
                {"Cola lounge, four-seat & square tables","可乐区、四人桌和方桌"},
                {"Courier tray & red-box packing","骑手取餐与红盒打包"},
                {"Conveyor automation","传送带自动生产"},{"West bag machines & pickup","外卖打包与取餐"},
                {"cash income & shop sign","营业收入与店铺招牌"},{"Dining tables","堂食餐桌"},{"Dining","堂食"},{"Hire staff","雇用员工"},
                {"Second grill","第二台汉堡机"},{"Blue-box packing","蓝盒打包"},{"Drive-thru","汽车取餐"},
                {"Cola wing","可乐区"},{"Courier","骑手配送"},{"Automation","自动生产"},
                {"Staff speed","员工速度"},{"Staff carry","员工携带"},{"Player speed","玩家速度"},{"Player carry","玩家携带"},
                {"Pair table","双人桌"},{"Four-seat table","四人桌"},{"Square table","方桌"},
                {"Classic","经典款"},{"Modern","现代款"},{"Premium","高级款"},{"Rustic","田园款"},
                {"West wing","外卖区"},{"Cash income","营业收入"},{"Upgrade ","升级 "}
            };
            for(int i=0;i<copy.GetLength(0);i++)text=text.Replace(copy[i,0],copy[i,1]);
            return text;
        }
    }
}
