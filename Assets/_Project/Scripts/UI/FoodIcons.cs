using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    public enum FoodIcon { Burger, Box, Coin, Speed, Carry, Clean, Lock, Check, Phone, Gloves, Skates, EmptyBag, Bagged, Chair1, Chair2, Chair3, Chair4, Cola, RedParcel, Parts }

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
        static Color BurgerPixel(float x,float y)
        {
            Color bun=new Color(.96f,.63f,.22f), crust=new Color(.80f,.39f,.10f);
            float dx=(x-.5f)/.40f;
            if (y>=.54f && dx*dx+Mathf.Pow((y-.54f)/.36f,2)<=1)
            {
                if(Line(x,y,.29f,.69f,.31f,.73f,.009f)||Line(x,y,.46f,.78f,.49f,.81f,.009f)||
                    Line(x,y,.65f,.72f,.68f,.75f,.009f)||Line(x,y,.49f,.62f,.52f,.65f,.009f))
                    return new Color(1,.94f,.73f);
                return y<.58f?crust:Color.Lerp(bun,new Color(1,.77f,.39f),Mathf.Clamp01((y-.62f)*3));
            }
            if(Box(x,y,.12f,.48f,.88f,.55f))return new Color(.86f,.19f,.10f);
            if(x>.08f&&x<.92f&&y>.425f+Mathf.Sin(x*46)*.014f&&y<.485f+Mathf.Sin(x*46)*.014f)
                return new Color(.31f,.65f,.12f);
            if(Box(x,y,.09f,.355f,.91f,.43f)||(x>.58f&&x<.76f&&y>.30f+Mathf.Abs(x-.67f)*.65f&&y<.38f))
                return new Color(1,.78f,.12f);
            if(x>.1f&&x<.9f&&y>.265f&&y<.37f)return y>.33f?new Color(.40f,.20f,.11f):new Color(.27f,.12f,.07f);
            if(x>.12f&&x<.88f&&y>.17f+Mathf.Pow((x-.5f)*.9f,2)&&y<.28f)return y<.21f?crust:bun;
            return Color.clear;
        }

        static Color Pixel(FoodIcon icon,float x,float y)
        {
            if(icon==FoodIcon.RedParcel)
            {
                if(x<.12f||x>.88f||y<.20f||y>.80f)return Color.clear;
                return (x>.43f&&x<.56f)||(y>.44f&&y<.55f)?Color.white:new Color(.86f,.06f,.09f);
            }
            if(icon==FoodIcon.Parts)
            {
                float dx=x-.5f,dy=y-.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
                if(r>.40f)return Color.clear;
                if(r>.27f)return new Color(.75f,.8f,.84f);
                if(r<.10f)return new Color(.20f,.25f,.30f);
                return new Color(.15f,.40f,.85f);
            }

            if(icon==FoodIcon.Cola)
            {
                if(x>.60f&&x<.66f&&y>.65f&&y<.94f)return Color.white;
                if(x>.24f&&x<.76f&&y>.63f&&y<.72f)return new Color(.2f,.2f,.22f);
                if(y>.12f&&y<.65f&&x>.30f-(y-.12f)*.08f&&x<.70f+(y-.12f)*.08f)
                    return y>.35f&&y<.44f?Color.white:new Color(.8f,.12f,.17f);
                return Color.clear;
            }

            Color ink=HudChrome.Ink,gold=HudChrome.Gold,cream=HudChrome.Cream;
            switch(icon)
            {
                case FoodIcon.Burger:
                    return BurgerPixel(x,y);
                case FoodIcon.Box:
                    if(Box(x,y,.15f,.2f,.85f,.8f))
                    {if(Box(x,y,.36f,.37f,.64f,.65f))return cream;return new Color(.06f,.52f,.76f);}
                    break;
                case FoodIcon.Coin:
                    if(Box(x,y,.08f,.22f,.92f,.78f))return BurgerShop.Core.BanknoteLook.Pixel((x-.08f)/.84f,(y-.22f)/.56f);
                    break;
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
                    Color cushion=icon==FoodIcon.Chair1?BurgerShop.Core.RestaurantStyle.Blue:
                        icon==FoodIcon.Chair2?new Color(.82f,.62f,.22f):icon==FoodIcon.Chair3?cream:HudChrome.Tomato;
                    if(Box(x,y,.23f,.13f,.29f,.53f)||Box(x,y,.71f,.13f,.77f,.53f))return ink;
                    if(Box(x,y,.2f,.47f,.8f,.59f))return cushion;
                    if(Box(x,y,.2f,.41f,.8f,.47f)||Box(x,y,.22f,.58f,.28f,.88f)||Box(x,y,.72f,.58f,.78f,.88f))return ink;
                    if(Box(x,y,.28f,.64f,.72f,.87f))return y>.83f?Color.Lerp(cushion,Color.white,.3f):cushion;
                    if(Box(x,y,.22f,.87f,.78f,.91f))return ink;
                    break;
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
