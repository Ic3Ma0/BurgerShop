using UnityEngine;
namespace BurgerShop.Core
{
    public static class BanknoteLook
    {
        public static readonly Color Green=new Color(.40f,.78f,.12f), Ink=new Color(.75f,.96f,.40f);
        static Texture2D face;
        public static Color Pixel(float x,float y)
        {
            bool border=(x>.05f&&x<.95f&&y>.07f&&y<.93f)&&(x<.075f||x>.925f||y<.105f||y>.895f);
            bool symbol=(x>.47f&&x<.50f&&y>.22f&&y<.78f)||
                (x>.39f&&x<.61f&&((y>.68f&&y<.72f)||(y>.48f&&y<.52f)||(y>.28f&&y<.32f)))||
                (x>.37f&&x<.41f&&y>.49f&&y<.7f)||(x>.59f&&x<.63f&&y>.3f&&y<.51f);
            return border||symbol?Ink:Green;
        }
        public static Texture2D Texture
        {
            get
            {
                if(face!=null)return face;
                face=new Texture2D(128,80,TextureFormat.RGB24,true){name="GreenBanknote",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
                for(int y=0;y<80;y++)for(int x=0;x<128;x++)face.SetPixel(x,y,Pixel((x+.5f)/128,(y+.5f)/80));
                face.Apply(true,true);return face;
            }
        }
    }
}
