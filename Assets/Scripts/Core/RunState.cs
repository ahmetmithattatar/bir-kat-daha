using System.Collections.Generic;

namespace RiskLoop.Core
{
    public enum RunStatus
    {
        Active,     // Tur devam ediyor
        CashedOut,  // Oyuncu kazancı cebe attı (yüzeye döndü)
        Busted      // Patlama (göçük) oldu
    }

    /// <summary>Tek bir turun durumu. Tur bitince atılır, kalıcı değildir.</summary>
    public class RunState
    {
        public int Step { get; internal set; }            // Mevcut derinlik (0 = henüz inmedi)
        public int StartStep { get; internal set; }       // Geçitten başlandıysa başlangıç katı (0 = yüzey)
        public double Earnings { get; internal set; }     // Bu turda biriken, RİSK ALTINDAKİ kazanç
        public double Banked { get; internal set; }       // Bu turda doğrudan cebe giren (güvenli) para
        public RunStatus Status { get; internal set; } = RunStatus.Active;
        public double Payout { get; internal set; }       // Tur sonunda risk altından cebe giren miktar

        // Bu turun bulgu etkileri (tur bitince sıfırlanır)
        public double ProfitBonus { get; internal set; }           // Adım ödüllerine eklenen oran (0.1 = +%10)
        public double SafetyFactor { get; internal set; } = 1.0;   // Patlama riskinin çarpanı (lanet 1'in üstüne çıkarır)
        public double RunLuck { get; internal set; }               // Tılsımlardan gelen ek şans
        public int GuaranteedFinds { get; internal set; }          // Haritadan: kalan garantili bulgu adımı
        public double MultiplierGained { get; internal set; }      // Kristallerin kazandırdığı toplam (özet için)

        // Bu turda bulunanlar (koleksiyon şeridi ve tur sonu özeti için)
        public List<FindResult> Finds { get; } = new List<FindResult>();

        // ---- Savaş ----
        public double PlayerHealth { get; internal set; }
        public double PlayerMaxHealth { get; internal set; }
        public MonsterInstance Monster { get; internal set; }      // Şu an savaşılan canavar (yoksa null)
        public int Kills { get; internal set; }
        public bool DiedToMonster { get; internal set; }           // Tur canavar yüzünden mi bitti?

        // ---- Büyüler (sınıf sistemi) ----
        public double Mana { get; internal set; }
        public double MaxMana { get; internal set; }
        public int SafeFloors { get; internal set; }              // Kalan korumalı kat (risk azalır)
        public double SafeFloorsFactor { get; internal set; }     // Korumalı katta risk x(1 - bu)
        public int RewardFloors { get; internal set; }            // Kalan ödüllü kat
        public double RewardFloorsMult { get; internal set; } = 1.0;
        public int FindBoostTiers { get; internal set; }          // Sonraki bulguya eklenecek nadirlik (0 = yok)
        public int WardHits { get; internal set; }                // Kalan korunan canavar vuruşu
        public double WardFactor { get; internal set; }           // Korunan vuruşta hasar x(1 - bu)
        public int CritCharges { get; internal set; }             // Kalan kesin kritik vuruş
        public int LootHuntTiers { get; internal set; } = -1;     // Sonraki zaferde garantili ganimet (+nadirlik), -1 = yok
        internal double ForesightRoll = -1;                       // Kehanet: önceden atılmış göçük zarı (-1 = yok)
        public bool HasForesight => ForesightRoll >= 0;

        // Turda düşen eşyalar: risk altında. Yüzeye dönünce envantere girer, göçük/ölümde kaybolur
        public List<GearItem> PendingLoot { get; } = new List<GearItem>();
        public int LootAutoSold { get; internal set; }             // Envanter doluydu, otomatik satıldı
        public double LootAutoSoldValue { get; internal set; }

        // Canavarlı katta bulgu zarı zafere ertelenir; o katın kilometre taşı (varsa) burada bekler
        internal MilestoneDefinition PendingMilestone;
        internal double PendingStepReward;

        // Tur özeti: bulgulardan gelenlerin toplamı
        public double TreasureTotal { get; internal set; }        // Hazinelerden tur kazancına eklenen (risk altında)
        public double CacheTotal { get; internal set; }           // Keselerden cebe giren
        public double MilestoneBonusTotal { get; internal set; }  // İlk keşif ödülleri (cebe giren)
        public bool IsNewBestRun { get; internal set; }           // Tur bitince: en değerli tur rekoru mu?

        /// <summary>
        /// Turun şu anki toplam değeri = risk altındaki kazanç + zaten cebe girmiş olan.
        /// Tur bittikten sonra = gerçekte kazanılan (Payout + Banked).
        /// </summary>
        public double TotalValue => Status == RunStatus.Active ? Earnings + Banked : Payout + Banked;
    }

    /// <summary>Advance() çağrısının sonucu. UI bunu mesaj göstermek için kullanır.</summary>
    public struct AdvanceResult
    {
        public bool Busted;           // Patlama oldu mu?
        public int Step;              // İnilmeye çalışılan adım
        public double BustChance;     // O adımın patlama olasılığı
        public double Roll;           // Atılan zar (0..1). Roll < BustChance ise patlama
        public double RewardGained;   // Hayatta kalındıysa eklenen kazanç
        public double LostEarnings;   // Patlamada kaybedilen miktar
        public double InsurancePayout;// Patlamada sigortanın kurtardığı miktar
        public double XpGained;       // Bu inişte kazanılan deneyim
        public int LevelsGained;      // Bu inişte atlanan seviye sayısı (yetenek puanı + yeni özellikler)
        public GearItem SalvagedItem; // Göçükte enkazdan kurtarılan eşya (yoksa null)

        public bool HasFind;          // Bu adımda bulgu çıktı mı?
        public FindResult Find;

        public MonsterInstance Encounter; // Bu katta canavar çıktıysa (bulgu zafere ertelenir)
        public double HealthRegen;        // Savaşsız katta dolan can

        public bool IsMilestone;      // Bu adım bir kilometre taşı mı?
        public bool FirstMilestoneReach; // Bu kilometre taşına ilk kez mi ulaşıldı?
        public double MilestoneBonus; // İlk ulaşma ödülü (doğrudan cebe)
        public bool NewRecord;        // Derinlik rekoru kırıldı mı?
    }
}
