using BurgerShop.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public sealed class PartsHud : MonoBehaviour
    {
        PartsWallet wallet;Text value;Image card;
        public static void Build(Transform parent,PartsWallet currency)
        {
            var root=new GameObject("PartsHud");root.transform.SetParent(parent,false);
            var hud=root.AddComponent<PartsHud>();hud.wallet=currency;
            hud.card=HudChrome.Panel(parent,"PartsBalance",Vector2.one,Vector2.one,new Vector2(-32,-128),new Vector2(180,56),HudChrome.Cream);
            HudChrome.Icon(hud.card.transform,"PartsIcon",FoodIcons.Get(FoodIcon.Parts),new Vector2(0,.5f),Vector2.one*.5f,new Vector2(28,0),new Vector2(32,32),Color.white);
            hud.value=HudChrome.Label(hud.card.transform,"Value",Vector2.zero,Vector2.one,Vector2.one*.5f,new Vector2(20,0),new Vector2(-55,0),26,HudChrome.Ink,TextAnchor.MiddleRight,true,false);
            hud.Refresh();
        }
        void LateUpdate()=>Refresh();
        void Refresh(){if(wallet==null||card==null)return;card.gameObject.SetActive(wallet.Balance>0);value.text=wallet.Balance.ToString();}
    }
}
