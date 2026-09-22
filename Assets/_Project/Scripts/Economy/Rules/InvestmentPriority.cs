using System;
using System.Collections.Generic;

namespace BurgerShop.Economy
{
    public enum InvestmentNeed { None, Production, Transport, Service, Seats }

    // Session-only hysteresis. Callers supply operating time, observations and real offer costs.
    public sealed class InvestmentPriority
    {
        public const float ObservationSeconds = 10f;
        sealed class Signal { public bool Raw, Stable; public float Since; }
        readonly Dictionary<string, Signal> signals = new Dictionary<string, Signal>();
        static string Key(InvestmentNeed need, string line) => need + ":" + line;
        public void Observe(InvestmentNeed need, string line, bool active, float now)
        {
            string key = Key(need, line);
            if (!signals.TryGetValue(key, out var signal))
            { signal = new Signal { Raw = active, Since = now }; signals.Add(key, signal); }
            if (signal.Raw != active || now < signal.Since) { signal.Raw = active; signal.Since = now; }
            if (now - signal.Since >= ObservationSeconds) signal.Stable = active;
        }
        public bool Persistent(InvestmentNeed need, string line) =>
            signals.TryGetValue(Key(need, line), out var signal) && signal.Stable;
        public int Priority(InvestmentNeed need, string line, bool prerequisite)
        {
            if (prerequisite) return 0;
            if (Persistent(need, line)) return 1;
            if (need == InvestmentNeed.Production && (Persistent(InvestmentNeed.Service, line)
                || Persistent(InvestmentNeed.Transport, line) || Persistent(InvestmentNeed.Seats, "dining"))) return 3;
            return 2;
        }
    }
}
