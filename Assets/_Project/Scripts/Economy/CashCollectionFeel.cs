using System;

namespace BurgerShop.Economy
{
    // Presentation cadence only. Never multiply the monetary value by these settings.
    public static class CashCollectionFeel
    {
        public const int MinimumPacket = 10;
        public const int MaximumPackets = 32;
        public const float Interval = .07f;
        public const float FlightSeconds = .30f;
        public const float SoundInterval = .055f;
        public const float SoundResetSeconds = .35f;
        public const float PitchStep = .025f;
        public const float MaximumPitch = 1.30f;
        public const int HudParticleLimit = 8;
        public static readonly UnityEngine.Vector3 ReceiverOffset=new UnityEngine.Vector3(0,.25f,.25f);
        public const float ArcHeight=.9f;
        public const float ArcTowardCamera=.45f;
        public static readonly UnityEngine.Vector3 PacketScale=new UnityEngine.Vector3(1f,.4f,1f);

        public static int PacketValue(long available)
            => (int)Math.Min(int.MaxValue, Math.Max(MinimumPacket,
                available / MaximumPackets + (available % MaximumPackets == 0 ? 0 : 1)));
    }

    public sealed class CashSoundSequence
    {
        float last = float.NegativeInfinity;
        float pitch = 1f;
        public float Next(float time)
        {
            pitch = time - last > CashCollectionFeel.SoundResetSeconds || time < last
                ? 1f : Math.Min(CashCollectionFeel.MaximumPitch, pitch + CashCollectionFeel.PitchStep);
            last = time;
            return pitch;
        }
    }
}
