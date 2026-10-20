namespace RiskLoop.Core
{
    /// <summary>
    /// Oyundaki yükseltme türleri. Temadan bağımsız isimler kullanılıyor:
    /// ekranda ne yazacağı UI katmanındaki UIStrings sınıfında belirlenir.
    /// Yeni tür eklerken SONA ekle; mevcut isimleri değiştirme (kayıtlar isimle tutuluyor).
    /// UI'daki sıralama da bu sıradır.
    /// </summary>
    public enum UpgradeType
    {
        Earnings,     // Her adımın kazancını artırır
        Safety,       // Patlama (göçük) şansını azaltır
        Insurance,    // Patlamada tur kazancının bir kısmını kurtarır
        FindChance,   // Her adımda bulgu çıkma şansını artırır
        Luck,         // Bulguların nadirlik şansını artırır
        FindValue     // Para veren bulguların değerini artırır
    }
}
