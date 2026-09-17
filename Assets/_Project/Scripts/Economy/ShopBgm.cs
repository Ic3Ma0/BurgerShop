using UnityEngine;

namespace BurgerShop.Economy
{
    public static class ShopBgm
    {
        public const int SampleRate = 22050;
        public const float Bpm = 96f;
        public const int Bars = 4;
        public const float LoopSeconds = Bars * 4f * 60f / Bpm;
        public const float Volume = 0.09f;

        public static bool ShouldPlay(bool soundEnabled, bool decorationsEnabled, bool paused, float timeScale)
            => soundEnabled && decorationsEnabled && !paused && timeScale > 0f;

        static readonly int[] Bass = { 48, 45, 41, 43 };
        static readonly int[][] Pad =
        {
            new[] { 60, 64, 67 },
            new[] { 57, 60, 64 },
            new[] { 53, 57, 60 },
            new[] { 55, 59, 62 }
        };
        static readonly int[] Melody =
        {
            72, 76, 79, 76, 72, 67, 69, 72,
            76, 72, 69, 72, 76, 79, 76, 72,
            77, 72, 69, 72, 77, 81, 77, 72,
            79, 76, 74, 71, 72, 74, 76, 72
        };

        public static AudioClip CreateLoop()
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(SampleRate * LoopSeconds));
            var samples = new float[count];
            float peak = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float sample = Tone(t);
                samples[i] = sample;
                float abs = Mathf.Abs(sample);
                if (abs > peak) peak = abs;
            }
            if (peak > 0.001f)
            {
                float gain = Volume / peak;
                for (int i = 0; i < count; i++) samples[i] *= gain;
            }
            CrossfadeSeam(samples, Mathf.Min(512, count / 8));
            var clip = AudioClip.Create("ShopBgmLoop", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Tone(float t)
        {
            float beat = t * (Bpm / 60f);
            int bar = Mathf.Clamp(Mathf.FloorToInt(beat / 4f), 0, Bars - 1);
            float eighth = beat * 2f;
            int note = Mathf.Clamp(Mathf.FloorToInt(eighth), 0, Melody.Length - 1);
            float notePos = eighth - note;
            float noteFade = Mathf.Clamp01(notePos / 0.08f) * Mathf.Clamp01((1f - notePos) / 0.08f);
            float barPos = (beat % 4f) / 4f;
            float barFade = Mathf.Clamp01(barPos / 0.04f) * Mathf.Clamp01((1f - barPos) / 0.04f);
            float bass = Sine(Midi(Bass[bar]), t) * 0.42f * barFade;
            float pad = 0f;
            for (int p = 0; p < Pad[bar].Length; p++)
                pad += Sine(Midi(Pad[bar][p]), t) * 0.16f * barFade;
            float lead = (Sine(Midi(Melody[note]), t) * 0.72f + Triangle(Midi(Melody[note]), t) * 0.28f) * 0.20f * noteFade;
            return bass + pad + lead;
        }

        static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);
        static float Sine(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
        static float Triangle(float hz, float t)
        {
            float phase = (hz * t) % 1f;
            return phase < 0.5f ? phase * 4f - 1f : 3f - phase * 4f;
        }

        static void CrossfadeSeam(float[] samples, int fade)
        {
            if (fade <= 0 || samples.Length <= fade * 2) return;
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                float start = samples[i];
                int end = samples.Length - fade + i;
                samples[end] = samples[end] * (1f - w) + start * w;
            }
        }
    }
}
