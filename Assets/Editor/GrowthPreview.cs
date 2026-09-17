using System;
using System.IO;
using BurgerShop.Persistence;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BurgerShop.EditorTools
{
    // Explicit editor-only demo entry. Normal Play keeps the user's real save untouched.
    [InitializeOnLoad]
    public static class GrowthPreview
    {
        const string Active="BurgerShop.GrowthPreview.Active",Previous="BurgerShop.GrowthPreview.Previous";
        static GrowthPreview()=>EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&(SessionState.GetBool("BurgerShop.WingPreview",false)||SessionState.GetBool("BurgerShop.CourierPreview",false)||SessionState.GetBool("BurgerShop.BlueCounterPreview",false)))
            {
                bool wing=SessionState.GetBool("BurgerShop.WingPreview",false);
                SessionState.SetBool("BurgerShop.WingPreview",false);
                bool blueCounters=SessionState.GetBool("BurgerShop.BlueCounterPreview",false);
                SessionState.SetBool("BurgerShop.BlueCounterPreview",false);
                SessionState.SetBool("BurgerShop.CourierPreview",false);
                EditorApplication.delayCall+=()=>
                {
                    var player=UnityEngine.Object.FindFirstObjectByType<BurgerShop.Player.PlayerMotor>();
                    if(player==null)return;
                    player.transform.position=wing?new UnityEngine.Vector3(12,1.1f,-4.5f):blueCounters?new UnityEngine.Vector3(-8,1.1f,-10.5f):new UnityEngine.Vector3(-1,1.1f,20);
                    UnityEngine.Camera.main.GetComponent<BurgerShop.Player.CameraFollow>().Snap();
                };
            }
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active,false))return;
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey,SessionState.GetString(Previous,""));
            SessionState.SetBool(Active,false);
        };
        [MenuItem("BurgerShop/Growth Preview (isolated save)")]
        public static void Start() => StartPreview(false);
        [MenuItem("BurgerShop/Counter appearance preview (isolated save)")]
        public static void CounterPreview() => StartPreview(true);
        [MenuItem("BurgerShop/Courier preview (isolated save)")]
        public static void CourierPreview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("BurgerShop.CourierPreview",true);StartPreview(false);
        }
        [MenuItem("BurgerShop/Blue drive-thru preview (isolated save)")]
        public static void BlueCounterPreview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("BurgerShop.BlueCounterPreview",true);StartPreview(false);
        }
        [MenuItem("BurgerShop/Side wing purchase preview (isolated save)")]
        [MenuItem("BurgerShop/Expansion planning preview (isolated save)")]
        public static void WingPreview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("BurgerShop.WingPreview",true);StartPreview(false, true);
        }
        static void StartPreview(bool counters, bool wing=false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string directory=Path.Combine(Path.GetTempPath(),"BurgerGrowthPreview-"+Guid.NewGuid().ToString("N"));
            var data = new RestaurantSaveData{version=RestaurantSaveData.CurrentVersion,coins=5000,completedSales=5,grillLevel=1,shopRank=6,upgradeStars=6,
                boughtExtraTable=true,boughtExtraGrill=true,extraGrillLevel=1,boughtExtraCounter=true,boughtBoxingStation=true,boughtDriveThru=true};
            if(wing) data = new RestaurantSaveData {version=RestaurantSaveData.CurrentVersion,grillLevel=1,coins=1500,shopRank=7};
            if(counters)
            {
                data.colaLevel=3;
                data.westExpanded=data.bagMachineBuilt=data.bagTableBuilt=data.bagCounterBuilt=true;
                data.facilityLevels=new[]{
                    new BurgerShop.Restaurant.FacilityLevelRecord{id="counter-main",level=1},
                    new BurgerShop.Restaurant.FacilityLevelRecord{id="counter-extra",level=2},
                    new BurgerShop.Restaurant.FacilityLevelRecord{id="boxing",level=3},
                    new BurgerShop.Restaurant.FacilityLevelRecord{id="bag-table",level=2},
                    new BurgerShop.Restaurant.FacilityLevelRecord{id="bag-counter",level=3}};
            }
            new LocalSaveStore(directory).Save(data);
            SessionState.SetString(Previous,SessionState.GetString(RestaurantPersistence.EditorDirectoryKey,""));
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey,directory);SessionState.SetBool(Active,true);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
        }
    }
}
