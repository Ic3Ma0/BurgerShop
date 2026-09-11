using UnityEngine;

namespace BurgerShop.Core
{
    public static class MobileRuntimeSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Configure()
        {
            if (Application.platform != RuntimePlatform.Android) return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
