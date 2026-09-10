using BurgerShop.Restaurant;
using UnityEngine;

namespace BurgerShop.Economy
{
    public sealed class SaleFeedback : MonoBehaviour
    {
        RestaurantWallet wallet;
        AudioSource source;
        AudioClip chime;

        public void Configure(RestaurantWallet earnings)
        {
            if (wallet != null) wallet.SaleRecorded -= Play;
            wallet = earnings;
            source = GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0.25f;
            if (chime == null)
            {
                const int sampleRate = 22050;
                float[] samples = new float[(int)(sampleRate * 0.28f)];
                for (int i = 0; i < samples.Length; i++)
                {
                    float time = (float)i / sampleRate;
                    float noteTime = time < 0.12f ? time : time - 0.12f;
                    float frequency = time < 0.12f ? 659.25f : 987.77f;
                    float envelope = Mathf.Min(1f, noteTime / 0.005f) * Mathf.Exp(-noteTime * 28f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * noteTime) * envelope * 0.5f;
                }
                chime = AudioClip.Create("SaleChime", samples.Length, 1, sampleRate, false);
                chime.SetData(samples, 0);
            }
            source.clip = chime;
            if (wallet != null) wallet.SaleRecorded += Play;
        }

        void Play(int amount)
        {
            if (isActiveAndEnabled && source != null) source.PlayOneShot(chime);
        }

        void OnDestroy()
        {
            if (wallet != null) wallet.SaleRecorded -= Play;
            if (chime != null) BurgerVisual.Release(chime);
        }
    }
}
