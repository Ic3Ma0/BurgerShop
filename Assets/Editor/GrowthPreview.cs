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
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Active,false))return;
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey,SessionState.GetString(Previous,""));
            SessionState.SetBool(Active,false);
        };
        [MenuItem("BurgerShop/Growth Preview (isolated save)")]
        public static void Start() => StartPreview(false);
        [MenuItem("BurgerShop/Counter appearance preview (isolated save)")]
        public static void CounterPreview() => StartPreview(true);
        static void StartPreview(bool counters)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string directory=Path.Combine(Path.GetTempPath(),"BurgerGrowthPreview-"+Guid.NewGuid().ToString("N"));
            var data = new RestaurantSaveData{version=RestaurantSaveData.CurrentVersion,coins=5000,completedSales=5,grillLevel=1,shopRank=6,upgradeStars=6,
                boughtExtraTable=true,boughtExtraGrill=true,extraGrillLevel=1,boughtExtraCounter=true,boughtBoxingStation=true,boughtDriveThru=true};
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
