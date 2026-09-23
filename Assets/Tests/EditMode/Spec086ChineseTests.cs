using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using BurgerShop.UI;
using BurgerShop.Building;
using BurgerShop.Restaurant;
using BurgerShop.Economy;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace BurgerShop.Tests.EditMode
{
 public sealed class Spec086ChineseTests : SaveIsolatedGameplayTest
 {
  static void Set(object target,string property,object value)=>target.GetType().GetProperty(property).SetValue(target,value);
  [TestCase("Continue", "继续")]
  [TestCase("Lv12", "12级")]
  [TestCase("Burger machine", "汉堡机")]
  [TestCase("+150", "+150")]
  public void CopyPreservesValues(string source,string expected)=>Assert.That(GameChinese.Translate(source),Is.EqualTo(expected));
  [TestCase("You were away for 0.9 minutes.\nHire an employee to earn while away.\n+0")]
  [TestCase("While you were away for 8.0 hours,\nyour 3 employees minded the shop.\n+123,456 cash added\nUp to 8 hours of after-hours earnings.")]
  [TestCase("Move mouse · Q/E rotate · click to place · Done to finish")]
  [TestCase("Delete Shop 2?\nThis shop will be removed.")]
  [TestCase("Upgrade unavailable — check cash and level")]
  [TestCase("No eligible upgrade found; check land and staff access. This is not a sell-more task.")]
  [TestCase("Choose · Paid · +2 ⭐")]
  [TestCase("Speed (u/s)\n20/20\n4.68 → 5.38")]
  [TestCase("SHOP 20\nCash +10%")]
  public void DynamicMessagesHaveNoEnglish(string source)=>Assert.That(Regex.IsMatch(GameChinese.Translate(source),"[A-Za-z]"),Is.False,GameChinese.Translate(source));
  [Test] public void WorldSignTranslatesWithoutRenamingAndFontContainsChineseAndRewardGlyphs()
  {
   var go=new GameObject("ShopRankCopy");
   try {
    var mesh=go.AddComponent<TextMesh>();mesh.text="SHOP 20\nCash +10%";
    ChineseWorldText.Ensure(mesh);
    Assert.That(go.name,Is.EqualTo("ShopRankCopy"));
    Assert.That(mesh.text,Is.EqualTo("店铺 20\n金币 +10%"));
    Assert.That(mesh.font,Is.EqualTo(HudChrome.ChineseFont()));
    foreach(char c in "店铺金币升级继续取消关闭★")Assert.That(mesh.font.HasCharacter(c),Is.True,c.ToString());
   } finally {Object.DestroyImmediate(go);}
  }
  [UnityTest] public IEnumerator LivePanelsAreChineseAndBounded()
  {
   EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");yield return new EnterPlayMode();
   for(int i=0;i<15;i++)yield return null;
   var layout=FacilityLayout.Current;var goals=layout.GetComponent<SessionGoalTracker>();
   layout.GetComponent<MainHallExpansion>().Restore(true,true,150);layout.Wallet.RestoreProgress(5000,0);
   var errors=new System.Collections.Generic.List<string>();
   var shop=Object.FindFirstObjectByType<FacilityShopHud>();
   var canvas=shop.GetComponentInParent<Canvas>();var camera=Camera.main;
   canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,1280)})
   {
    var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
    foreach(int rank in Enumerable.Range(1,15))
    {
     goals.Restore(rank,0,0);goals.ApplyUnlocks();layout.Discover();
     foreach(string panel in new[]{"hud","shop","expansions","owned","details","machine","offline","player","staff","saves","delete"})
     {
      if(panel=="shop" || panel=="expansions" || panel=="owned")shop.Open();
      if(panel=="expansions")shop.SendMessage("ShowExpansions");
      if(panel=="owned")shop.SendMessage("ShowCatalog",true);
      if(panel=="machine")Object.FindFirstObjectByType<FacilityDetailsHud>().Open(layout.Instances.First(f=>f.Kind==FacilityKind.BurgerMachine));
      if(panel=="offline") { var save=layout.GetComponent<BurgerShop.Persistence.RestaurantPersistence>(); Set(save,"OfflineVisible",true); Set(save,"OfflineStaffCount",3); Set(save,"OfflineTicks",System.TimeSpan.FromHours(8).Ticks); Set(save,"OfflineGrant",123456L); }
      if(panel=="details")Object.FindFirstObjectByType<FacilityDetailsHud>().Open(layout.Instances.First());
      if(panel=="player")Object.FindFirstObjectByType<PlayerUpgradeHud>().Open();
      if(panel=="staff")Object.FindFirstObjectByType<StaffUpgradeHud>().Open();
      if(panel=="saves" || panel=="delete")Object.FindFirstObjectByType<SaveSlotsHud>().Open();
      if(panel=="delete")Object.FindFirstObjectByType<SaveSlotsHud>().SendMessage("AskDelete",2);
      for(int f=0;f<3;f++)yield return null;Canvas.ForceUpdateCanvases();
      if(panel=="saves" || panel=="delete")
      {
       var sheet=GameObject.Find("SavesPanel").GetComponent<RectTransform>();
       var root=(RectTransform)sheet.parent;var corners=new Vector3[4];sheet.GetWorldCorners(corners);
       var lower=root.InverseTransformPoint(corners[0]);var upper=root.InverseTransformPoint(corners[2]);
       var bounds=Rect.MinMaxRect(lower.x,lower.y,upper.x,upper.y);
       Assert.That(bounds.xMin,Is.GreaterThanOrEqualTo(root.rect.xMin+19));
       Assert.That(bounds.xMax,Is.LessThanOrEqualTo(root.rect.xMax-19));
       Assert.That(bounds.yMin,Is.GreaterThanOrEqualTo(root.rect.yMin+29));
       Assert.That(bounds.yMax,Is.LessThanOrEqualTo(root.rect.yMax-29));
      }
      foreach(var label in Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t=>t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)))
      {
       if(Regex.IsMatch(label.text,"[A-Za-z]"))errors.Add(panel+"/"+label.name+": "+label.text);
       Assert.That(label.font,Is.EqualTo(HudChrome.ChineseFont()),label.name);
       Assert.That(label.horizontalOverflow,Is.EqualTo(HorizontalWrapMode.Wrap),label.name);
       Assert.That(label.verticalOverflow,Is.EqualTo(VerticalWrapMode.Truncate),label.name);
       // Compare the full generated text height at the fitted size, not merely the clipped mesh.
       var settings=label.GetGenerationSettings(label.rectTransform.rect.size);
       settings.resizeTextForBestFit=false;settings.fontSize=label.cachedTextGenerator.fontSizeUsedForBestFit;
       if(settings.fontSize<=0)settings.fontSize=label.fontSize;
       var generator=new TextGenerator();float height=generator.GetPreferredHeight(label.text,settings)/label.pixelsPerUnit;
       if(height>label.rectTransform.rect.height+2)errors.Add("CLIP "+panel+"/"+label.name+" "+height+">"+label.rectTransform.rect.height+": "+label.text);
      }
      if(rank==7){camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();System.IO.File.WriteAllBytes("/tmp/bs086-"+panel+"-"+size.x+".png",image.EncodeToPNG());RenderTexture.active=old;Object.Destroy(image);}
      if(panel=="shop" || panel=="expansions" || panel=="owned")shop.Close();
      if(panel=="details" || panel=="machine")Object.FindFirstObjectByType<FacilityDetailsHud>().Close();
      if(panel=="offline")Set(layout.GetComponent<BurgerShop.Persistence.RestaurantPersistence>(),"OfflineVisible",false);
      if(panel=="player")Object.FindFirstObjectByType<PlayerUpgradeHud>().ClickClose();
      if(panel=="staff")Object.FindFirstObjectByType<StaffUpgradeHud>().ClickClose();
      if(panel=="delete"){var cancel=GameObject.Find("CancelDelete").GetComponent<Button>();cancel.onClick.Invoke();Assert.That(cancel.transform.parent.gameObject.activeSelf,Is.False);}
      if(panel=="saves" || panel=="delete")Object.FindFirstObjectByType<SaveSlotsHud>().Close();
     }
    }
    camera.targetTexture=null;Object.Destroy(target);
   }
   Assert.That(errors.Distinct().ToArray(),Is.Empty,string.Join("\n",errors.Distinct()));
   LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
  }
 }
}
