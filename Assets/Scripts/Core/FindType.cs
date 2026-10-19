namespace RiskLoop.Core
{
    /// <summary>
    /// Bir adımda denk gelinebilecek bulgu türleri (temadan bağımsız isimler).
    /// Yeni tür eklerken SONA ekle; BalanceData.GetFindType, RiskMath.FindValue, GameSession.ApplyFind ve UIStrings'e de ekle.
    /// </summary>
    public enum FindType
    {
        Treasure,     // Tur kazancına eklenir. Patlamada kaybedilebilir (risk altında)
        SafeCache,    // Doğrudan toplam paraya eklenir. Güvende
        ProfitBoost,  // Bu tur boyunca adım ödüllerini artırır
        Safety,       // Bu tur boyunca patlama riskini azaltır
        Shield,       // Bir patlamayı engeller
        LuckCharm,    // Bu tur boyunca nadirlik şansını artırır
        FindStreak,   // Sonraki N adımda bulgu garantili
        Multiplier,   // Risk altındaki tur kazancını anında çarpar
        CursedTreasure,// Büyük hazine (risk altında) ama bu tur patlama riski artar
        Gear          // Ekipman (kayadan ya da canavardan). Yüzeye dönünce envantere girer
    }

    /// <summary>Bir bulgunun sonucu.</summary>
    public struct FindResult
    {
        public FindType Type;
        public int Tier;           // 0 = en sıradan ... BalanceData.rarityTiers.Length-1 = en iyi
        public double Value;       // Türe göre anlamı değişir: para, oran ya da adet (RiskMath.FindValue)
        public bool FromMilestone; // Kilometre taşının garantili bulgusu mu?
        public GearItem Item;      // Type == Gear ise eşya (diğerlerinde null)
        public bool FromMonster;   // Canavardan mı düştü?
    }
}
