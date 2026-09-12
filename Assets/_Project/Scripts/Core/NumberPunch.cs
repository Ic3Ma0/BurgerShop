using UnityEngine;

namespace BurgerShop.Core
{
    public sealed class NumberPunch
    {
        public const float Duration = 0.28f;
        public const float Peak = 0.22f;
        float elapsed;
        bool playing;

        public bool IsActive => playing;
        public float Scale => playing ? 1f + Peak * Mathf.Sin(Mathf.Clamp01(elapsed / Duration) * Mathf.PI) : 1f;

        public void Play()
        {
            elapsed = 0f;
            playing = true;
        }

        public void Advance(float deltaTime)
        {
            if (!playing || deltaTime <= 0f) return;
            elapsed += deltaTime;
            if (elapsed >= Duration) playing = false;
        }
    }

    public sealed class CoinRoll
    {
        public const float Duration = 0.25f;
        long from;
        long to;
        float elapsed;
        bool playing;

        public bool IsActive => playing;
        public long Value
        {
            get
            {
                if (!playing) return to;
                float t = Mathf.Clamp01(elapsed / Duration);
                return from + (long)Mathf.Round((to - from) * t);
            }
        }

        public void Play(long start, long end)
        {
            from = start;
            to = end;
            elapsed = 0f;
            playing = start != end;
        }

        public void Snap(long value)
        {
            from = to = value;
            elapsed = 0f;
            playing = false;
        }

        public void Advance(float deltaTime)
        {
            if (!playing || deltaTime <= 0f) return;
            elapsed += deltaTime;
            if (elapsed >= Duration) playing = false;
        }
    }
}
