using UnityEngine;

namespace BurgerShop.Economy
{
    public static class CoinSfx
    {
        public const float CoinSeconds = 0.12f;
        public const float SpendSeconds = 0.10f;

        public static AudioClip CreateCoin()
        {
            // A soft paper flick under a bright short chime. Local deterministic PCM, no audio asset dependency.
            const int sampleRate=22050;
            var samples=new float[Mathf.RoundToInt(sampleRate*CoinSeconds)];
            uint noise=731;
            float previous=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)sampleRate;
                noise=unchecked(noise*1664525u+1013904223u);
                float raw=(noise>>8)/16777215f*2-1;
                float paper=(raw-previous)*.12f*Mathf.Exp(-t*65);previous=raw;
                float bell=(Mathf.Sin(2*Mathf.PI*1046.5f*t)+.25f*Mathf.Sin(2*Mathf.PI*1568*t))*.32f*Mathf.Exp(-t*30);
                float envelope=Mathf.Clamp01(t/.003f)*Mathf.Clamp01((CoinSeconds-t)/.02f);
                samples[i]=(paper+bell)*envelope;
            }
            var clip=AudioClip.Create("BanknoteCollect",samples.Length,1,sampleRate,false);
            clip.SetData(samples,0);return clip;
        }

        public static AudioClip CreateSpend() => Build("CoinSpend", SpendSeconds, 392f, 293.66f, 0.38f);

        public static AudioClip CreateLight() => Build("SoftTransfer", .08f, 440f, 554f, .20f);
        public static AudioClip CreateSuccess() => Build("WarmSuccess", .24f, 523.25f, 783.99f, .42f);

        public static AudioClip CreateDump()
        {
            const int sampleRate = 22050;
            const float seconds = 0.14f;
            var samples = new float[Mathf.RoundToInt(sampleRate * seconds)];
            uint noise = 1901;
            float previous = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                noise = unchecked(noise * 1664525u + 1013904223u);
                float raw = (noise >> 8) / 16777215f * 2f - 1f;
                float paper = (raw - previous) * 0.28f * Mathf.Exp(-t * 26f);
                previous = raw;
                float thud = Mathf.Sin(2f * Mathf.PI * 88f * t) * Mathf.Exp(-Mathf.Max(0f, t - 0.018f) * 20f) * 0.38f;
                if (t < 0.018f) thud *= t / 0.018f;
                float envelope = Mathf.Clamp01(t / 0.004f) * Mathf.Clamp01((seconds - t) / 0.025f);
                samples[i] = (paper + thud) * envelope;
            }
            var clip = AudioClip.Create("TrashDump", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

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
