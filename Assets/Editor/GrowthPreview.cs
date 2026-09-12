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
        public static void Start()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string directory=Path.Combine(Path.GetTempPath(),"BurgerGrowthPreview-"+Guid.NewGuid().ToString("N"));
            new LocalSaveStore(directory).Save(new RestaurantSaveData{version=9,coins=5000,completedSales=5,grillLevel=1,shopRank=6,upgradeStars=6,
                boughtExtraTable=true,boughtExtraGrill=true,extraGrillLevel=1,boughtExtraCounter=true,boughtBoxingStation=true,boughtDriveThru=true});
            SessionState.SetString(Previous,SessionState.GetString(RestaurantPersistence.EditorDirectoryKey,""));
            SessionState.SetString(RestaurantPersistence.EditorDirectoryKey,directory);SessionState.SetBool(Active,true);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");EditorApplication.isPlaying=true;
        }
    }
}
