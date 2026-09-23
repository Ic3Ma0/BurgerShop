using UnityEngine;
using UnityEngine.UI;

namespace BurgerShop.UI
{
    // Localize only the display boundary; IDs and gameplay strings remain unchanged.
    public sealed class LocalizedText : Text
    {
        string source, rendered;
        public string Source => source ?? base.text;
        public override string text
        {
            get => base.text;
            set
            {
                if(source==value && base.text==rendered)return;
                source=value;rendered=GameChinese.Translate(value);
                ApplyTypography();base.text=rendered;
            }
        }
        void LateUpdate() => ApplyTypography();
        void ApplyTypography()
        {
            var chinese=HudChrome.ChineseFont();if(font!=chinese)font=chinese;
            if(horizontalOverflow!=HorizontalWrapMode.Wrap)horizontalOverflow=HorizontalWrapMode.Wrap;
            if(verticalOverflow!=VerticalWrapMode.Truncate)verticalOverflow=VerticalWrapMode.Truncate;
            if(!resizeTextForBestFit)resizeTextForBestFit=true;
            int ceiling=Mathf.Clamp(fontSize,14,44);
            if(resizeTextMaxSize!=ceiling)resizeTextMaxSize=ceiling;
            int minimum=Mathf.Min(16,ceiling);
            if(resizeTextMinSize!=minimum)resizeTextMinSize=minimum;
        }
    }
}
