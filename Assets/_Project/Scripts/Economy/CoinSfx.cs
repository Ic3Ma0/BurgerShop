using UnityEngine;

namespace BurgerShop.Economy
{
    public static class CoinSfx
    {
        public const float CoinSeconds = 0.12f;
        public const float SpendSeconds = 0.10f;

        public static AudioClip CreateCoin() => Build("CoinDing", CoinSeconds, 1174.66f, 1567.98f, 0.52f);

        public static AudioClip CreateSpend() => Build("CoinSpend", SpendSeconds, 392f, 293.66f, 0.38f);

        static AudioClip Build(string name, float seconds, float startHz, float endHz, float gain)
        {
            const int sampleRate = 22050;
            int count = Mathf.Max(1, Mathf.RoundToInt(sampleRate * seconds));
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)sampleRate;
                float n = count == 1 ? 1f : i / (float)(count - 1);
                float freq = Mathf.Lerp(startHz, endHz, n);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * time);
                float square = sine >= 0f ? 1f : -1f;
                float envelope = Mathf.Clamp01(time / 0.004f) * Mathf.Exp(-time * 32f);
                samples[i] = (sine * 0.82f + square * 0.18f) * envelope * gain;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
