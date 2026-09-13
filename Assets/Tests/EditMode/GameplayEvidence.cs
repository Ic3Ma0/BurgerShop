using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace BurgerShop.Tests.EditMode
{
    // Deterministic portrait render from the running SampleScene, including its real HUD.
    // This is Editor evidence, never a substitute for device touch acceptance.
    public static class GameplayEvidence
    {
        public static void Capture(string filename)
        {
            var camera = Camera.main;
            if (camera == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            var target = new RenderTexture(720, 1280, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            float previousAspect = camera.aspect;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            var distances = new float[canvases.Length];
            var pixels = new Texture2D(720, 1280, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.aspect = 720f / 1280f;
                for (int i = 0; i < canvases.Length; i++)
                {
                    modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera;
                    distances[i] = canvases[i].planeDistance;
                    if (!canvases[i].isRootCanvas || modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = 1f;
                }
                Canvas.ForceUpdateCanvases();
                foreach(var hud in Object.FindObjectsByType<BurgerShop.UI.WorldLabelHud>(FindObjectsSortMode.None))hud.RefreshNow();
                foreach(var hud in Object.FindObjectsByType<BurgerShop.UI.CarryHud>(FindObjectsSortMode.None))hud.Refresh(0);
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0);
                pixels.Apply();
                string path = Path.GetFullPath("Logs/spec023-025/" + filename);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++)
                {
                    canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i];
                    canvases[i].planeDistance = distances[i];
                }
                camera.targetTexture = previousTarget;
                camera.aspect = previousAspect;
                RenderTexture.active = previousActive;
                Object.Destroy(pixels); Object.Destroy(target);
            }
        }
    }
}
