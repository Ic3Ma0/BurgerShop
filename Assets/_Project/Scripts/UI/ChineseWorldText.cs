using UnityEngine;

namespace BurgerShop.UI
{
    // Only the few directly rendered signs use this; order cards already use LocalizedText.
    [DefaultExecutionOrder(150)]
    public sealed class ChineseWorldText : MonoBehaviour
    {
        TextMesh label;
        Renderer surface;
        string rendered;
        float originalSize, maxWidth;
        public static void Ensure(TextMesh text)
        {
            var item=text.GetComponent<ChineseWorldText>();
            if(item==null)
            {
                item=text.gameObject.AddComponent<ChineseWorldText>();item.label=text;
                item.surface=text.GetComponent<Renderer>();item.originalSize=text.characterSize;
                item.maxWidth=text.name=="ShopRankCopy"?4f:text.name=="CourierRoadMark"?3.5f:5f;
            }
            item.Refresh();
        }
        void LateUpdate()=>Refresh();
        void Refresh()
        {
            if(label==null)return;
            if(label.text!=rendered)
            {
                rendered=GameChinese.Translate(label.text);label.text=rendered;
                label.font=HudChrome.ChineseFont();surface.sharedMaterial=label.font.material;
                label.characterSize=originalSize;
            }
            // Font glyph geometry can refresh after the setter; bound again next frame.
            float width=surface.localBounds.size.x;
            if(width>maxWidth&&width>0)label.characterSize*=maxWidth/width;
        }
    }
}
