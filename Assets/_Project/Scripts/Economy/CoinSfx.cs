using UnityEngine;

namespace BurgerShop.Economy
{
    public static class CoinSfx
    {
        public const float CoinSeconds = 0.12f;
        public const float SpendSeconds = 0.10f;

        public static AudioClip CreateCoin() => Build("CoinDing", CoinSeconds, 784f, 1046.5f, 0.40f);

        public static AudioClip CreateSpend() => Build("CoinSpend", SpendSeconds, 392f, 293.66f, 0.38f);

        public static AudioClip CreateLight() => Build("SoftTransfer", .08f, 440f, 554f, .20f);
        public static AudioClip CreateSuccess() => Build("WarmSuccess", .24f, 523.25f, 783.99f, .42f);

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
                float sine = Mathf.Sin(2f * Mathf.PI * (startHz*time + .5f*(endHz-startHz)*time*time/seconds));
                float square = sine >= 0f ? 1f : -1f;
                float envelope = Mathf.Clamp01(time / 0.004f) * Mathf.Exp(-time * 18f) * Mathf.Clamp01((seconds-time)/.02f);
                samples[i] = sine * envelope * gain;
            }

            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
