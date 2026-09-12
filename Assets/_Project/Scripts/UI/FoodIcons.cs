using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public enum FoodIcon { Burger, Box, Coin, Speed, Carry, Clean, Lock, Check, Phone, Gloves, Skates, EmptyBag, Bagged, Chair1, Chair2, Chair3, Chair4 }

    // Original, code-drawn icons. Supersampled at creation; cached for the whole session.
    public static class FoodIcons
    {
        static readonly Dictionary<FoodIcon, Sprite> cache = new Dictionary<FoodIcon, Sprite>();
        public static Image Add(Transform parent, FoodIcon icon, Vector2 position, float size) =>
            HudChrome.Icon(parent, icon + "Icon", Get(icon), Vector2.one*.5f, Vector2.one*.5f,
                position, Vector2.one*size, Color.white);

        public static Sprite Get(FoodIcon icon)
        {
            if (cache.TryGetValue(icon, out var sprite) && sprite != null) return sprite;
            const int size = 96;
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false) { wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Bilinear };
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                Color sum=Color.clear;
                for(int sy=0;sy<2;sy++) for(int sx=0;sx<2;sx++)
                    sum += Pixel(icon,(x+(sx+.5f)/2)/size,(y+(sy+.5f)/2)/size)*.25f;
                texture.SetPixel(x,y,sum);
            }
            texture.Apply(false,true);
            sprite=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,96);
            cache[icon]=sprite; return sprite;
        }
        static bool Box(float x,float y,float l,float b,float r,float t) => x>=l && x<=r && y>=b && y<=t;
        static bool Disc(float x,float y,float cx,float cy,float r) => (x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r;
        static bool Line(float x,float y,float ax,float ay,float bx,float by,float w)
        {
            var a=new Vector2(ax,ay);var d=new Vector2(bx-ax,by-ay);var p=new Vector2(x,y)-a;
            return (p-d*Mathf.Clamp01(Vector2.Dot(p,d)/d.sqrMagnitude)).sqrMagnitude<=w*w;
        }
        static Color Pixel(FoodIcon icon,float x,float y)
        {
            Color ink=HudChrome.Ink,gold=HudChrome.Gold,cream=HudChrome.Cream;
            switch(icon)
            {
                case FoodIcon.Burger:
                    if(Box(x,y,.12f,.20f,.88f,.30f))return gold;
                    if(Box(x,y,.1f,.32f,.9f,.44f))return ink;
                    if(Box(x,y,.12f,.45f,.88f,.50f))return HudChrome.Green;
                    if(Box(x,y,.13f,.52f,.87f,.58f))return HudChrome.Tomato;
                    if(y>.6f && Disc(x,y,.5f,.59f,.38f))
                    { if(Disc(x,y,.35f,.76f,.022f)||Disc(x,y,.62f,.79f,.022f))return cream;return gold; }
                    break;
                case FoodIcon.Box:
                    if(Box(x,y,.18f,.18f,.82f,.76f))
                    {if(y>.68f||x<.23f||x>.77f||y<.23f)return ink;return gold;}
                    if(Line(x,y,.12f,.65f,.3f,.85f,.04f)||Line(x,y,.3f,.85f,.76f,.85f,.04f))return ink;
                    if(Box(x,y,.46f,.46f,.54f,.7f))return cream;break;
                case FoodIcon.Coin:
                    if(Disc(x,y,.5f,.5f,.42f))
                    {if(!Disc(x,y,.5f,.5f,.33f))return new Color32(181,118,31,255);
                     if(Box(x,y,.46f,.3f,.54f,.7f))return ink;return gold;}break;
                case FoodIcon.Speed:
                    if(Line(x,y,.33f,.24f,.73f,.54f,.07f)||Line(x,y,.73f,.54f,.52f,.65f,.07f)||Line(x,y,.52f,.65f,.72f,.86f,.07f))return gold;
                    if(Line(x,y,.12f,.45f,.34f,.45f,.035f)||Line(x,y,.12f,.65f,.3f,.65f,.035f))return ink;break;
                case FoodIcon.Carry:
                    if(Box(x,y,.16f,.16f,.84f,.66f)){if(x<.22f||x>.78f||y<.22f)return ink;return gold;}
                    if(y>.58f && Disc(x,y,.5f,.6f,.23f)&&!Disc(x,y,.5f,.6f,.16f))return ink;break;
                case FoodIcon.Clean:
                    if(Line(x,y,.48f,.48f,.78f,.88f,.045f))return ink;
                    if(y>.15f&&y<.5f&&x>.15f&&x<.68f&&x+y>.45f)return gold;break;
                case FoodIcon.Lock:
                    if(y>.5f&&Disc(x,y,.5f,.6f,.24f)&&!Disc(x,y,.5f,.6f,.15f))return ink;
                    if(Box(x,y,.2f,.18f,.8f,.56f))return ink;
                    if(Disc(x,y,.5f,.4f,.06f))return gold;break;
                case FoodIcon.Chair1:
                case FoodIcon.Chair2:
                case FoodIcon.Chair3:
                case FoodIcon.Chair4:
                    if(Box(x,y,.22f,.2f,.3f,.53f)||Box(x,y,.7f,.2f,.78f,.53f))return ink;
                    if(Box(x,y,.2f,.45f,.8f,.56f)||Box(x,y,.22f,.6f,.78f,.88f))return icon==FoodIcon.Chair1?gold:new Color(.15f,.44f,.8f);
                    if(icon>=FoodIcon.Chair3&&y>.55f&&Disc(x,y,.5f,.72f,.27f))return cream;
                    if(icon==FoodIcon.Chair4&&(Box(x,y,.12f,.51f,.2f,.72f)||Box(x,y,.8f,.51f,.88f,.72f)))return ink;break;
                case FoodIcon.Phone:
                    if(Box(x,y,.28f,.12f,.72f,.9f)){if(Box(x,y,.34f,.27f,.66f,.78f))return cream;return ink;}break;
                case FoodIcon.Gloves:
                    if(Box(x,y,.3f,.15f,.75f,.6f)||Box(x,y,.32f,.48f,.43f,.89f)||Box(x,y,.45f,.48f,.56f,.94f)||Box(x,y,.58f,.48f,.69f,.88f)||Line(x,y,.34f,.38f,.16f,.58f,.08f))return gold;break;
                case FoodIcon.Skates:
                    if(Disc(x,y,.32f,.18f,.09f)||Disc(x,y,.7f,.18f,.09f))return ink;
                    if(Box(x,y,.22f,.31f,.82f,.48f)||Box(x,y,.22f,.4f,.48f,.84f))return HudChrome.Tomato;break;
                case FoodIcon.EmptyBag:
                case FoodIcon.Bagged:
                    if(y>.62f&&Disc(x,y,.5f,.68f,.22f)&&!Disc(x,y,.5f,.68f,.14f))return ink;
                    if(Box(x,y,.2f,.12f,.8f,.67f))return icon==FoodIcon.Bagged&&y>.3f&&y<.48f?HudChrome.Tomato:gold;break;
                case FoodIcon.Check:
                    if(Line(x,y,.2f,.5f,.43f,.28f,.065f)||Line(x,y,.43f,.28f,.82f,.78f,.065f))return HudChrome.Green;break;
            }
            return Color.clear;
        }
    }
}
