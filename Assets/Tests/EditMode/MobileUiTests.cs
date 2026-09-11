using BurgerShop.UI;
using NUnit.Framework;
using UnityEngine;

namespace BurgerShop.Tests.EditMode
{
    public sealed class MobileUiTests
    {
        [TestCase(0, 60, 1080, 2220, 1080, 2400)]
        [TestCase(100, 0, 2100, 1080, 2400, 1080)]
        public void UiFitsNotchAndGestureInsets(float x, float y, float w, float h, int screenW, int screenH)
        {
            var root = new GameObject("SafeArea", typeof(RectTransform));
            try
            {
                var fitter = root.AddComponent<SafeAreaFitter>();
                fitter.Apply(new Rect(x, y, w, h), screenW, screenH);
                var rect = (RectTransform)root.transform;
                Assert.That(rect.anchorMin.x, Is.EqualTo(x / screenW).Within(0.0001f));
                Assert.That(rect.anchorMin.y, Is.EqualTo(y / screenH).Within(0.0001f));
                Assert.That(rect.anchorMax.x, Is.EqualTo((x + w) / screenW).Within(0.0001f));
                Assert.That(rect.anchorMax.y, Is.EqualTo((y + h) / screenH).Within(0.0001f));
                Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void EmptySafeAreaFallsBackAndZeroResolutionDoesNotDivide()
        {
            var root = new GameObject("SafeArea", typeof(RectTransform));
            try
            {
                var fitter = root.AddComponent<SafeAreaFitter>();
                fitter.Apply(Rect.zero, 1080, 1920);
                fitter.Apply(Rect.zero, 0, 0);
                var rect = (RectTransform)root.transform;
                Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
