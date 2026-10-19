using System;

namespace RiskLoop.Core
{
    /// <summary>
    /// Rastgele sayı kaynağı. Arayüz olarak tanımlı ki testlerde sabit sayılar
    /// veren sahte bir kaynak verebilelim.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>0 (dahil) ile 1 (hariç) arası sayı.</summary>
        double NextDouble();
    }

    /// <summary>System.Random kullanan standart kaynak. Aynı seed = aynı sonuçlar.</summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() { _random = new Random(); }
        public SystemRandomSource(int seed) { _random = new Random(seed); }

        public double NextDouble() => _random.NextDouble();
    }
}
