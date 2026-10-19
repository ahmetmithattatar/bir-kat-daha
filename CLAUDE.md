# RiskLoop — Kazı temalı risk/ödül prototipi

## Proje bağlamı
- Unity 6.6 (6000.6.6f1), 2D URP şablonu, Windows. Yeni Input System aktif (`activeInputHandler: 1`).
- Geliştirici C# konusunda yeni (Python biliyor). Kod **sade ve bol yorumlu** olmalı, yorumlar Türkçe.
- Uzun vadeli hedef: Steam + mobil yayın, portfolyo kalitesinde temiz ve genişletilebilir kod.
- Her şey oyun içi sanal para. **Gerçek para ya da gerçek parayla şans mekaniği yok** (loot box vb. eklenmeyecek).
- Tasarım hedefi: "bir tur daha" dedirten döngü. Geliştirici denge/bağlılık kararlarını bana bırakıyor; kararları buraya yaz.

## Oyun döngüsü
1. Turlar arası ekranda yükseltme al, "Kazıya başla".
2. "Derine in": bir sonraki katın göçük şansıyla zar atılır.
   - Hayatta kalırsan derinlik +1, tur kazancına `o katın ödülü` eklenir, sonra bulgu zarı atılır.
   - Göçükte baret varsa baret harcanır ve tur devam eder. Yoksa tur kazancı gider, sigorta bir kısmını kurtarır, tur biter.
3. "Yüzeye dön": tur kazancının tamamı toplam paraya eklenir, tur biter.
4. Ekranda her zaman: göçük yüzdesi + renkli risk çubuğu, sonraki kat ödülü + bulgu şansı, sigorta oranı,
   **turun değeri** (sayarak artar) + "risk altında / cepte" dökümü, bu turun etkileri (baret, kazanç bonusu, risk çarpanı),
   sonraki keşif katı ve kalan kat, **bu turun bulgu koleksiyonu** (kart ızgarası).

## Kazı sahnesi (birinci şahıs, `UI/DigView`)
- Tur ekranı 3 sütun: sol istatistik | **orta kazı sahnesi** + mesaj | sağ koleksiyon + butonlar.
- Sahne: iç içe 4 tünel halkası (içe koyulaşır), ortada kaya duvarı (+çatlak katmanı, "12. KAT" kazınmış yazı), sağ altta kazma,
  kenar karartma (fener titremesi). Kaya rengi bölgeye göre değişir (`Art/DepthTheme`: toprak, taş, kristal, göl, demirhane, fosil, ejder, lav;
  bölgeler keşif katlarında başlar). Her bölgede 4 doku çeşidi dönüşümlü.
- **Bir kat = kazı gücüne göre 1-8 kazma vuruşu** (başta 3; bkz. kazma yuvası). Her vuruş: geri çekil → hızla in → kıvılcım + moloz + toz, duvar sarsılır, çatlak büyür.
  Tuş basılı tutulursa art arda vurur (yorucu olmasın). Sahneye tıklamak da vurur. Zar SADECE son vuruşta atılır.
- Son vuruş: duvar titrer `0.12 + 0.7*risk` sn (gerilim) → sonuç:
  baret kurtardıysa sahnede baret görünür, üstüne taş yağar, kırılıp düşer + pencere | duvar parçalanır, delik açılır →
  bulgu delikten fırlar (arkasında nadirlik renginde dönen hüzme) → nadirse pencere, değilse adıyla uçan yazı → koleksiyona uçar →
  tünelde ilerlenir (halkalar büyür, yeni duvar karanlıktan yaklaşır) | göçükte tavan çöker, taşlar dipte YIĞILIR, sahne kararır.
- Risk sahnede görünür: tavan çatlağı riskle koyulaşır, tavandan riskle sıklaşan çakıl düşer.

## Bağımlılık ve uzun oyun sistemleri (5. tur kararları)
- **Oto-kazı (slot "oto çevir" mantığı):** A tuşu / buton. Kazıyı kendi sürdürür, animasyonlar `FxTime.Speed` = 2.5x.
  Sıradan bulgular pencere açmadan koleksiyona uçar. Durma kuralları (`PlayerProgress.autoDig`, kayda girer):
  baret bitince (oto başlarken baret varsa), risk %X+ (10..75), nadirlik >= seçilen (varsayılan yeşil), oto savaş açık/kapalı.
  Ayrıca her zaman durur: boss, ölümcül tehdit, can < %30. Durunca banner + flaş: kumar anında kontrol oyuncuya geçer.
  Amaç: baret varken sıkıcı katları hızla geçmek, risk anında oyuncuyu çağırmak.
- **Geçitler:** Keşif katının bossu yenilince (`conqueredMilestones`) sonraki turlara o kattan başlanabilir (atlanan katların ödülü yok).
  Yükseltme ekranında "Başlangıç: ..." butonu döngüyle seçer.
- **Güç/risk göstergesi (`GameSession.Insight.cs`):** "Savaş gücün: N. kat seviyesi" (normal canavar Orta'dan kolay VE mini boss ölümcül değil
  olan en derin kat), tehdit (Kolay/Orta/Zor/Ölümcül = canavarı öldürene kadar kaybedilen can oranı <0.2/<0.5/<1/>=1),
  göçük hesabıyla ortalama kaç kat daha (baret sayısı üzerinden DP), kumar satırı (kazanırsan / kaybedersen / %, yorum: Güvenli adım, Değer,
  Kumar, Çılgınlık). Eşya detayında "Savaş gücü 14 → 17" ve kazmada "Duvar 5 → 3 vuruş".
- **Kazma yuvası + kazı gücü:** `GearSlot.Pickaxe`, `StatType.DigPower`. Gereken vuruş = ceil(3 * 1.07^(kat-1) / kazı gücü), 1..8.
  8 kazma (Paslı → Yıldız Demiri). Eldeki kazma görseli kuşanılanla değişir.
- **Nadirlik farkı büyütüldü:** ağırlıklar 100/30/9/2.7/0.8/0.24/0.07/0.02/0.006 (~3.3x), güç 1/1.6/2.5/4/6.5/10/16/26/45,
  eşya nadirlik çarpanı `1.22^tier` (mor x4.9), luckPerStep 0.025, tur şansı sınırı 1.5. Kırmızı/mor eşyalar efsanevi isim alır.
- **Güçlendirme (Shaiya tarzı):** +1..+10, şans %95 → %15, her seviye statlar +%10. +4'ten sonra başarısızlık seviye düşürür,
  +7'den sonra eşya KIRILIR. Ücret satış değeri x2 x1.6^seviye (para yutağı). Ruletvari animasyon.
- **Yeni Çağ (prestij):** 15. kattan sonra açılır. Yadigar = floor(rekor^1.6 / 10). Sıfırlanır: para, yükseltmeler, eşyalar, rekor, geçitler.
  Kalır: çağ, yadigar, 5 Kadim yükseltme (Kadim Altın, Yıldızların Lütfu, Cüce Miğferi, Kralların Kanı, Ejder Hazinesi), oto-kazı ayarları.
- **Yükseltme maks seviyeleri yükseltildi** (tek çağda dolmasın): Kazanç 40, Güvenlik 25, Sigorta 15, Dedektör 20, Fener 25, Terazi 30.
- **İçerik:** bölge başına 6 canavarlık havuz (48), her keşif katında 5 bossluk havuz (45), 19 şablon. LOTR havası ama **özgün isimler**
  (Steam'de telif/marka riski olmasın: Balrog/Nazgûl/Moria gibi birebir isim YOK). Keşif katları: Goblin Tünelleri, Kayıp Cüce Karakolu,
  Kristal Salonlar, Gözcünün Gölü, Cüce Kralın Demirhanesi, Kadim Höyükler, Ejderin Altın Yatağı, Gölge ve Alev Köprüsü, Dünyanın Kökleri.
- **Çantayı sat:** ekipman sekmesinde, iki basış ister.
- **Config uyarısı:** `BalanceConfig.asset` Inspector'da bir kez düzenlenirse tüm alanlar asset'e yazılır ve koddaki yeni varsayılanlar
  artık uygulanmaz. Yeni varsayılanları almak için asset'in ⋮ menüsünden Reset.

## Savaş, canavarlar, ekipman (RPG katmanı)
Kararlar (kullanıcıyla): **tıklayarak vur**, **yenilgi = göçük gibi tur biter**, **eşyalar yüzeye dönünce kalıcı**, **savaş + kazı statları**.
- Duvar yıkılınca canavar çıkabilir: `min(0.15 + 0.012*(step-1), 0.5)`. Keşif katında boss garantili (zar yok).
  Canavarlı katta bulgu zarı **zafere ertelenir** (canavar katın hazinesini korur).
- Savaşta kazı tuşu = **SALDIR** (basılı tut, `playerAttackCooldown` 0.16 sn). Canavar `monsterAttackInterval` (1.6 sn, boss x0.85) aralıkla vurur;
  sayacı `GameController` tutar (Core zaman bilmez), uzun sahnelerde (pencere vs.) durur. İlk vuruştan önce 0.8 sn tolerans.
- Oyuncu canı tur başında `Stats.Health` ile dolar; tur içinde kalıcı, savaşsız her katta %8 dolar. Can 0: baret varsa %30 canla kalkar, yoksa tur biter
  (sigorta çalışır, turun eşyaları kaybolur). "Yüzeye dön" savaşta **kaçış**: kazanç ve eşyalar korunur, canavar ödülü gider.
- Hasar: oyuncu `Attack * ±%10 * (kritikse CritDamage)`. Canavar: `atk * (1 - def/(def + 5*atk))` (ölçekten bağımsız savunma).
- Canavar: `hp = 40 * 1.18^(step-1) * hpMult`, `atk = 6 * 1.16^(step-1) * atkMult`. Öldürme ödülü `kat ödülü * 1.5 * hpMult` (risk altında).
- Ganimet: normal canavar %40 / 1 eşya, mini boss 1 (en az keşif nadirliği-1), boss 2 (en az keşif nadirliği). Kayadan "Ekipman" bulgusu da çıkar (ağırlık 12).
- 32 normal canavar (bölge başına 4) + 9 isimli boss (keşif katlarında sırayla mini/büyük): Grubnak, Varkoth, Azurion, Sssira, Thorgrim,
  Ossarax, Vyrmathor, Ignar, Nihil. Veriler `Core/DefaultContent`, isimler `Game/FantasyTexts`, görseller `Game/Art/MonsterArt`.
- **Ekipman:** 8 yuva (Silah, Kask, Zırh, Pantolon, Eldiven, Çizme, Kolye, Yüzük). 18 silah (minStep'le derinde açılır),
  zırh 7 malzeme x 5 yuva (Bez, Deri, Zincir, Plaka, Mithril, Ejder Pulu, Hiçlik), takı 6 malzeme x 2 (Bakır ... Yıldız).
  - Düz stat = `taban * malzeme * (1 + 0.25*tier) * 1.14^(seviye-1)`, yüzde stat = `taban * malzeme * (1+0.25*tier) * (1 + 0.02*(seviye-1))`.
  - Ek rastgele stat sayısı = `tier / 2` (mor = 4), havuzdan tekrarsız. Seviye = düştüğü kat.
  - Statlar: Saldırı, Can, Savunma, Kritik şansı (maks %60), Kritik hasarı, Göçük direnci (maks %40, taban riskin altına inemez),
    Bulgu şansı, Şans, Altın bonusu (maks +%300). Kazı statları `GameSession`'da kazıya uygulanır.
  - Turda düşen eşya `RunState.PendingLoot` (risk altında) → yüzeye dönünce envantere (32 yer; doluysa otomatik satılır).
  - Envanter işlemleri sadece turlar arası: Kuşan / Çıkar / Sat / "En iyileri kuşan" (`RiskMath.GearPowerScore`).
- Görsel: kazı sahnesinde canavar delikten büyüyerek çıkar (boss: aura, kükreme sarsıntısı), üstünde isim + can barı.
  Sol altta elde silah (kazmanın aynası), sahnenin üstünde oyuncu can barı. Vuruş: silah sallanır, canavar kızarır/sarsılır, hasar sayısı
  (kritik: sarı "KRİTİK!"). Canavar saldırısı: kameraya atılır, ekran kızarır, -hasar. Ölüm: parçalanır, ödül, bulgu ve ganimet tek tek açılır.
- Ekipman ekranı: yükseltme ekranında sekme (Yükseltmeler | Ekipman). Sağda stat özeti + 8 yuva + 8x4 envanter, solda seçili eşya detayı
  (statlar ve kuşanılıya göre +/- fark, Kuşan/Çıkar, Sat).
- Denge notu: 10. kat bossu (Azurion ~1060 can) ekipmansız ~18 sn sürer ve öldürür; ekipman toplamayı zorunlu kılan ilk duvar. Python ile ölçülmeli.

## Görseller (kodla üretilen pixel art, dosya yok)
- `Art/PixelArt`: metin ızgarasından sprite (`FromRows`), seed'li kaya dokusu, birikimli çatlak (aynı seed + büyüyen stage),
  hüzme, parıltı, vignette. Hepsi Point filtreli ve önbellekli (bellek sızmaz).
- `Art/SpriteLibrary`: 16x16 ikonlar metin olarak (harf = renk, tablo dosyanın başında). Kazma, baret ve her bulgu türü.
  Çeşitlilik: Hazine nadirlikle değişir (para yığını → altın külçe → sandık → taç), Kese (kese → çuval).
  Kristal gri çizilip nadirlik rengine boyanır. İkonlar koleksiyon kartlarında, sahnede ve pencerede.
- Pencerede ikon önce **siyah siluet** (ne olduğu belli, ne kadar iyi olduğu değil), renk tırmanışı biter, sonra renklenir.

## Hissiyat (juice) ve spam önleme
- Her kat bir sahne: `RunFeedbackDirector` sonucu oynatır. `GameController`: vuruş sırasında `_busy` (butonlar sönmez),
  sonuç sahnesinde `SetBusy(true)` (butonlar da kilitli).
  1. Baret kırıldıysa: kırmızı flaş, sahnede baret sekansı, sonra **kapatılması gereken** "BARET KIRILDI!" penceresi.
  2. Kat ödülü: derinlik "pat", turun değerinden uçan +X. Ödül turun o ana kadarki değerinin %25'i/%50'si üstündeyse açık mavi/altın.
  3. Bulgu: nadirlik ≥ 3 (lacivert) ya da keşif katıysa açılır pencere: çerçeve beyazdan gerçek nadirliğe kadar renk renk tırmanır
     (yükseldikçe yavaşlar), sonra isim ve ikon açılır, parçacık + flaş (+ 6 ve üstünde sarsıntı). Kapatmak için tık/tuş, en az `0.35 + 0.08*tier` sn.
  4. Tur değeri `bestRunValue`'yu geçerse turda bir kez "EN DEĞERLİ TURUN!" banner'ı.
  5. Göçük: çok sert sarsıntı, kırmızı ekran, rastgele başlık (GÖÇÜK!, TAVAN ÇÖKTÜ!...), tavan çöker, sonra sonuç ekranı.
- Tıklanan buton seçili kalıp Boşluk/Enter ile tekrar tetiklenmesin diye `GameController.Update` seçimi her karede temizler.
- Metinler `enableAutoSizing` ile kutuya sığmazsa küçülür (en fazla %40).
- Aynı olaya hep aynı cümle çıkmasın diye `UIStrings` havuzlardan rastgele seçer (`Pick`, sunum rastgeleliği; Core'un zarı değil).
- Efekt parçaları: `UI/FxLayerView` (sarsıntı, flaş, uçan yazı, parçacık, banner), `UI/RevealPopupView`, `UI/UIFx` (coroutine yardımcıları).
  Hepsi basit UI şekilleri, sprite/asset yok. Zamanlamalar Inspector'dan ayarlanabilir (Director ve Popup bileşenleri).

## Bulgular ve nadirlik
- 9 nadirlik (Core'da 0..8, renk/isim `Game/RarityStyle.cs`):
  beyaz Sıradan, açık mavi Kaliteli, mavi Nadir, lacivert Değerli, yeşil Seçkin, sarı Destansı, turuncu Efsanevi, kırmızı Mitik, mor Kadim.
- Her seviyenin `weight` (sıklık) ve `power` (güç) değeri var: nadir = düşük weight, yüksek power.
- Şans `luck = luckPerStep*step + luckUpgrade`, ağırlıkları `weight*(1+luck)^tier` ile yüksek seviyelere kaydırır. Yani derine inmek daha nadir bulgu demek.
- Bulgu türleri (`FindType`):
  | Tür | UI adı | Etki |
  |---|---|---|
  | Treasure | Hazine | Tur kazancına eklenir, **risk altında** (göçükte gider) |
  | SafeCache | Kese | Doğrudan cebe, güvende |
  | ProfitBoost | Zengin damar | Tur boyu kat ödülleri +% (toplanarak birikir) |
  | Safety | Sağlam kaya | Tur boyu göçük riski çarpanı azalır (çarpımsal) |
  | Shield | Baret | Bir göçüğü yutar. En az lacivert seviyede çıkar, kırmızı ve üstünde 2 adet |
  | LuckCharm | Uğur tılsımı | Tur boyu `runLuck += baseValue*power` (daha nadir bulgular) |
  | FindStreak | Eski harita | Sonraki `floor(1 + 0.5*tier)` katta bulgu garantili (zar 2 atılmaz). En az açık mavi |
  | Multiplier | Parlak kristal | Risk altındaki kazanç `*= 1 + min(0.1*power, 2)` anında (en fazla x3). En az mavi |
  | CursedTreasure | Lanetli put | Büyük hazine (baseValue 4) risk altında + tur riski x1.15 (toplam en fazla x1.6). En az mavi |
- Tür ağırlıkları: Hazine 40, Kese 20, Damar 18, Kaya 15, Tılsım 10, Baret 7, Harita 6, Kristal 5, Put 5.
- Keşif katları (kilometre taşları), her birinin adı var (`UIStrings.MilestoneName`):
  3 Paslı Sandık, 5 Kayıp Madenci Kampı, 10 Kristal Mağarası, 15 Yeraltı Gölü, 20 Cüce Demirhanesi,
  25 Dev Fosil Mezarlığı, 30 Ejderin İni, 35 Lav Kalbi, 40 Dünyanın Çekirdeği.
  Garantili bulgu çıkar, her katta en düşük nadirlik daha yüksek. Keşfedilmemiş katın adı "???" görünür (merak).
  İlk kez ulaşınca ayrıca güvenli ilk keşif ödülü (`o katın ödülü * firstReachRewardSteps`) doğrudan cebe girer.
  Amaç: oyuncunun kendine "şu kata kadar in, dön" hedefi koyması (geliştiricinin kendi stratejisi). Keşif katı bu hedefi doğal kılıyor,
  bir önceki katta "bir tane daha" gerilimi yaratıyor.
- Koleksiyon: aynı tür + aynı nadirlik tek kartta birikir (×adet, toplam etki), en nadir solda. 9 kutu, fazlası "+N daha". Ekipmanlar yuvaya göre de ayrılır.

## Yükseltmeler (7)
| Tür | UI adı | Etki | Birikim |
|---|---|---|---|
| Earnings | Daha iyi kazma | Kat ödülü | `(1+e)^lvl` |
| Safety | Destek kirişleri | Göçük riski | `(1-e)^lvl` |
| Insurance | Sigorta | Göçükte kurtarılan | `1-(1-e)^lvl` |
| FindChance | Maden dedektörü | Bulgu şansı | `+e*lvl` (üst sınır `maxFindChance`) |
| Luck | Uğurlu fener | Nadirlik | `luck += e*lvl` |
| FindValue | Kuyumcu terazisi | Hazine/Kese değeri | `(1+e)^lvl`, e=0.12 (maks x9.6) |
| StartShield | Yedek baret | Tura baretle başla | `round(e*lvl)` adet, maks 3, çok pahalı |

## Alınan kararlar
| Konu | Karar |
|---|---|
| Kazanç modeli | Kat başı ödül × çarpan, tur 0'dan başlar, giriş ücreti yok |
| Risk modeli | Her iniş bağımsız zar, olasılık derinlikle doğrusal artar |
| Ekonomi patlaması | İlk sürümde her şey maksimumdayken risk %1'e iniyor, derinlik sınırsızlaşıp kazanç 10^15'e çıkıyordu (performans değil, denge sorunu: double 10^308'e kadar gider). Çözüm: **derinlikle artan taban risk** `minBustChance + bustFloorPerStep*(step-1)` (30. katta ≥ %30), tur bonus sınırları (`maxRunProfitBonus` +%200, `minRunSafetyFactor` 0.5), Kuyumcu terazisi 0.20→0.12. Hesap: maks yükseltme + yarıya inmiş riskle 20. kata ulaşma ~%7, 30. kata ~%0.2 (baretsiz) |
| Sayı gösterimi | `UIStrings.Num`: 10.000 altı tam sayı, üstü 12.3K / 4.56M / 7.8B / T / Qa..., liste biterse 1.23e45 |
| Tur değeri | `RunState.TotalValue` = risk altındaki (`Earnings`) + cepte (`Banked`: Kese + ilk keşif ödülü). `bestRunValue` kalıcı rekor |
| Yükseltme birikimi | Çoğu çarpımsal. Seviyeler kalıcı, eski ilerleme boşa gitmez |
| Sigorta | `1 - (1-e)^level`, %100'e asla ulaşmaz. Sadece risk altındaki kazanca uygulanır |
| Baret | Göçüğü yutar, adım normal sayılır (ödül + bulgu zarı). Göçük yüzdesini değiştirmez |
| Fiyatlar | `ceil(baseCost * costGrowth^level)`, tam sayı |
| Para tipi | `double` (idle oyunlar gibi büyük sayılara hazır). Gösterimde aşağı yuvarlanır |
| Kayıt | JsonUtility ile `Application.persistentDataPath/save.json` (saveVersion 4: envanter ve kuşanılan eşyalar; GearSlot/StatType sayı olarak, SADECE SONA ekle). Tur ortası kaydedilmez (risk altındaki kazanç kaybolur, Kese/rekor ödülü anında kaydedilir) |
| Kaydı sıfırla | Üst barın sağında (kazı butonlarından uzakta). Tur sırasında gizli, iki basış ister (3 sn içinde) |
| Sahne | `RiskLoop > Build Prototype Scene` menüsü sahneyi koddan kurar ve bağlar. Config'i NewScene'den SONRA yükler (önce yüklenirse referans null düşer) |
| Platform önceliği | Önce **PC (yatay 16:9)**. Mobil sonra (yatay düzen telefonda da çalışabilir, ayrıca ele alınacak) |
| UI | uGUI + TextMeshPro (zengin metinde `<color>` ile nadirlik renkleri). CanvasScaler 1920x1080 Expand. Tur ekranı: sol istatistik / sağ bulgu+butonlar. Yükseltme ekranı: sol sonuç+başla / sağ 7 yükseltme |
| Klavye | ↓/S/Boşluk kaz (basılı tut = art arda vuruş; yükseltme ekranında YENİ basışla kazıya başla), ↑/W yüzeye dön. Tur bitince 0.6 sn tuş kilidi. Butonların UI navigasyonu kapalı |
| Layout tuzağı | Dikey düzenin içindeki yatay grupta `childForceExpandHeight = true` OLMAZ: grup esnek yükseklik ister ve ekranı kaplar. Yan yana dizilimde metin genişliği `preferredWidth = 0` ile sıfırlanır (TMP tek satır genişliğini bildirir, sütunu taşırır) |
| Tema | Mantık temadan bağımsız (`Step`, `Bust`, `Find`, `Shield`). Tema metinleri `UIStrings.cs` + `RarityStyle.cs` içinde |

## Mimari
```
Assets/Scripts/
  Core/        RiskLoop.Core.asmdef (noEngineReferences: true). Unity'ye BAĞIMLI OLAMAZ
    BalanceData.cs     Tüm denge sayıları (yükseltme, nadirlik, bulgu türü, hazine odası tanımları)
    RiskMath.cs        Saf formüller (durumsuz, zar dışarıdan). Python'a birebir taşınacak dosya
    GameSession.cs     Kurallar (partial): tur, kazı, bulgular, yükseltmeler
    GameSession.Combat.cs  Canavar çıkışı, Attack / MonsterAttack, zafer ve ganimet
    GameSession.Gear.cs    Eşya üretimi (RollGearItem), envanter, kuşan/çıkar/sat, otomatik kuşan, çantayı sat, güçlendirme
    GameSession.Prestige.cs  Yeni Çağ, Yadigar, Kadim yükseltmeler
    GameSession.Insight.cs   Savaş gücü, tehdit, kalan kat (DP), kumar oranı (hiçbir şeyi değiştirmez)
    Prestige.cs        PrestigeType, PrestigeBalance
    Gear.cs            GearSlot, StatType, StatValue, GearItem
    CombatData.cs      CombatBalance (savaş/ekipman sayıları), MonsterDefinition, GearBaseDefinition, sonuç tipleri
    DefaultContent.cs  Varsayılan içerik: 18 silah, 47 zırh/takı tabanı, 41 canavar
    CharacterStats.cs  Taban + kuşanılan eşyalar = toplam statlar (sınırlarla)
    RunState.cs        Tek turun durumu (baret, tur bonusları, bulgular) + AdvanceResult
    FindType.cs        Bulgu türleri + FindResult
    PlayerProgress.cs  Kalıcı veri (para, yükseltme seviyeleri, istatistik, rekor)
    IRandomSource.cs   Rastgelelik arayüzü (seed verilebilir, testte sahte kaynak)
    UpgradeType.cs
  Game/        Unity katmanı (Assembly-CSharp)
    BalanceConfig.cs   ScriptableObject kabı. Context menu: Export/Import JSON -> Balance/balance.json
    GameController.cs  Köprü: UI olayları -> GameSession -> UI yenile, kaydet. İnişleri coroutine ile sıralar (_busy kilidi)
    RunFeedbackDirector.cs  Sonucu sahneler: kazı gerilimi, baret, ödül, bulgu, göçük, dönüş efektleri
    SaveSystem.cs
    UIStrings.cs       Ekrandaki tüm metinler (tema/çeviri burada)
    RarityStyle.cs     Nadirlik isimleri ve renkleri
    FantasyTexts.cs    Canavar/boss/silah/malzeme/yuva/stat adları (id -> Türkçe), eşya stat metinleri
    Art/               PixelArt (sprite/doku üretimi), SpriteLibrary (bulgu ikonları), DepthTheme (bölge renkleri),
                       MonsterArt (14 şablon x renk), GearArt (10 silah şablonu, 7 zırh/takı şablonu x malzeme rengi)
    UI/                TopBarView, RunPanelView, FindCollectionView + FindMiniCardView, UpgradePanelView, UpgradeRowView:
                       sadece gösterir, olay yayar. DigView (kazı + savaş sahnesi), UIParticleSpawner, FxLayerView, RevealPopupView, UIFx: efektler
                       EquipmentView + GearSlotView + ItemDetailView: ekipman sekmesi. PrestigePanelView + PrestigeRowView: Yeni Çağ sekmesi.
                       AutoDigView: oto-kazı paneli. FxTime: animasyon hız çarpanı (oto-kazıda hızlanır)
  Editor/
    PrototypeSceneBuilder.cs
Assets/Config/BalanceConfig.asset   (menü ilk çalıştığında oluşur)
Assets/Scenes/Prototype.unity       (menü oluşturur, Build Settings'te ilk sahne)
```
Kurallar:
- Oyun kuralı sadece `Core` içinde. View'lar karar vermez, GameController sadece köprü.
- Yeni denge sayısı = `BalanceData`'ya alan + `RiskMath`'e formül. Koda sihirli sayı gömme.
- `BalanceData` alan adları = JSON anahtarları = Python tarafı. Ad değişirse Python'u da güncelle.
- `UpgradeType` kayıtta string olarak tutuluyor. Enum'a SONA ekleme serbest, yeniden adlandırma eski kayıtları bozar.
  Yeni yükseltme: enum + `BalanceData` alanı + `GetUpgrade` + `UIStrings.UpgradeName/UpgradeEffectValue`. Satırı sahne kurucu otomatik ekler.
- Yeni bulgu türü: `FindType` (sona) + `BalanceData` alanı + `GetFindType` + `RiskMath.FindValue` + `GameSession.ApplyFind` + `UIStrings`.
- Kayıt formatı değişirse `PlayerProgress.saveVersion`'ı artır.
- Sahne kurucuyu değiştirince sahneyi menüden yeniden kur.

## Formüller (varsayılan sayılar `BalanceData.cs` içinde)
```
reward(step)    = baseStepReward * stepMultiplierGrowth^(step-1) * (1+earnings.e)^earningsLvl * (1 + runProfitBonus)
bustChance(step)= clamp((baseBustChance + bustChancePerStep*(step-1)) * (1-safety.e)^safetyLvl * runSafetyFactor, floor(step), max)
floor(step)     = minBustChance + bustFloorPerStep*(step-1)
runProfitBonus  <= maxRunProfitBonus,   runSafetyFactor >= minRunSafetyFactor
insurance       = 1 - (1-insurance.e)^insuranceLvl      -> göçükte kurtarılan = runEarnings * insurance
findChance(step)= clamp(findChanceBase + findChancePerStep*(step-1) + findChance.e*lvl, 0, maxFindChance)   (hazine odasında 1)
luck            = luckPerStep*step + luck.e*lvl
tierWeight(t)   = rarityTiers[t].weight * (1+luck)^t    (t < minTier ise 0)
Treasure/Kese   = reward(step) * baseValue * power(t) * (1+findValue.e)^lvl
ProfitBoost     = baseValue * power(t)                    -> runProfitBonus += değer
Safety          = min(baseValue * power(t), maxSafetyFindReduction) -> runSafetyFactor *= (1 - değer)
cost(level)     = ceil(baseCost * costGrowth^level)
```
luck (bulgu anında) += runLuck (tılsımlar) + gear Luck;  findChance += gear FindChance;  reward *= (1 + gear GoldBonus)
bustChance içinde runSafetyFactor *= (1 - gear CaveResist)   (taban risk yine geçerli)
Zar sırası: GameSession.cs başındaki yorumda (Advance, Attack, MonsterAttack, zafer, RollGearItem). Python'da aynı sırayı koru.
Varsayılanlarla kaba tahmin (bulgusuz): yükseltmesiz optimal durma noktası ~6. kat. Bulgular eklendi, Python simülasyonuyla yeniden ölçülmeli.

## Yapılacaklar
- [ ] Unity'de derleme ve oynanış testi (oto-kazı, geçit, güçlendirme, Yeni Çağ, kazma, güç göstergesi sürümü henüz denenmedi)
- [ ] Python simülasyonu: Yeni Çağ döngüsü süresi, güçlendirme para yutağı, oto-kazı ile tur başına süre
- [ ] Savaş dengesi: boss süreleri, ekipman ölçeği (1.14) vs canavar ölçeği (1.18/1.16), düşme oranları
- [ ] Sonra: silah türüne özel vuruş efekti, canavar türüne özel saldırı (zehir, büyü), set bonusları, eşya karşılaştırma ipucu (tooltip)
- [ ] Pixel art'ı gerçek bir sanatçı/asset ile değiştirmek istenirse: SpriteLibrary.FindIcon tek giriş noktası
- [ ] Geç oyun para yutağı (money sink): tüm yükseltmeler dolunca paranın anlamı kalmıyor. Aday: prestij / yeni maden (sıfırla, kalıcı çarpan kazan)
- [ ] Ses: kazı, bulgu (nadirliğe göre), baret kırılması, göçük
- [ ] Windows build ayarları: Player Settings'te pencere modu/çözünürlük (ör. 1600x900 pencereli, yeniden boyutlandırılabilir)
- [ ] Python simülasyonu: `Balance/balance.json` oku, `RiskMath`'i port et, stratejiler (sabit hedef katta dön, hazine odasında dön, EV>0 iken devam) için tur başı kazanç, yükseltme hızı ve nadirlik dağılımını ölç
- [ ] Core için EditMode testleri (Unity Test Framework kurulu): RiskMath formülleri, sahte IRandomSource ile Advance/CashOut/baret/hazine odası
- [ ] git init + Unity `.gitignore` (Library/, Temp/, Logs/, UserSettings/ hariç)
- [ ] Sonra: bulgu kartı animasyonu (nadirliğe göre parlama, ses), görsel tema, sayı kısaltma (1.2K, 3.4M), yerelleştirme, mobil güvenli alan (safe area), yükseltme listesi uzarsa ScrollRect
