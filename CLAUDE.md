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
   - Hayatta kalırsan derinlik +1, tur kazancına `o katın ödülü` eklenir, XP kazanılır, sonra bulgu zarı atılır.
   - Göçükte tur kazancı gider, sigorta bir kısmını kurtarır, tur biter. (Baret YOK, 7. turda kaldırıldı.)
3. "Yüzeye dön": tur kazancının tamamı toplam paraya eklenir, tur biter.
4. Ekranda her zaman: göçük yüzdesi + renkli risk çubuğu, sonraki kat ödülü + bulgu şansı, sigorta oranı,
   **turun değeri** (sayarak artar) + "risk altında / cepte" dökümü, bu turun etkileri (kazanç bonusu, risk çarpanı),
   sonraki boss katı ve oraya ulaşma ihtimali, **bu turun bulgu koleksiyonu** (kart ızgarası), üst barda seviye + XP.

## Kazı sahnesi (birinci şahıs, `UI/DigView`)
- **Tur ekranı TAM EKRAN:** kazı sahnesi tüm tuvali kaplar. Üstünde yarı saydam HUD panelleri (`CreateHudPanel`, içeriğe göre yükseklik):
  sol üst istatistik (karakter, derinlik, değer, 12 stat kartı, risk çubuğu, kısa satırlar), sağ üst bulgu koleksiyonu,
  sağ alt KAZ / Yüzeye dön, alt orta mesaj, en üstte yarı saydam üst bar. Ortası (duvar, canavar, bulgu) hep açık.
  Kazma ve silah HUD'ların iç kenarında (`HudSideWidth`), oyuncu can barı üst barın altında. HUD arka planı tıklamayı yutar (yanlış vuruş yok).
- Sahne: iç içe 4 tünel halkası (içe koyulaşır), ortada kaya duvarı (+çatlak katmanı, "12. KAT" kazınmış yazı), sağ altta kazma,
  kenar karartma (fener titremesi). Kaya rengi bölgeye göre değişir (`Art/DepthTheme`: toprak, taş, kristal, göl, demirhane, fosil, ejder, lav;
  bölgeler keşif katlarında başlar). Her bölgede 4 doku çeşidi dönüşümlü.
- **Bir kat = kazı gücüne göre 1-8 kazma vuruşu** (başta 3; bkz. kazma yuvası). Her vuruş: geri çekil → hızla in → kıvılcım + moloz + toz, duvar sarsılır, çatlak büyür.
  Tuş basılı tutulursa art arda vurur (yorucu olmasın). Sahneye tıklamak da vurur. Zar SADECE son vuruşta atılır.
- Son vuruş: duvar titrer `0.12 + 0.7*risk` sn (gerilim) → sonuç:
  duvar parçalanır, delik açılır →
  bulgu delikten fırlar (arkasında nadirlik renginde dönen hüzme) → pencere / hızlı kart / uçan yazı (bkz. Hissiyat) → koleksiyona uçar →
  tünelde ilerlenir (halkalar büyür, yeni duvar karanlıktan yaklaşır) | göçükte tavan çöker, taşlar dipte YIĞILIR, sahne kararır.
- Risk sahnede görünür: tavan çatlağı riskle koyulaşır, tavandan riskle sıklaşan çakıl düşer.

## Bölgeler, seviye, yetenekler (7. tur kararları)
Kullanıcı isteği: tam ekran HUD, oto-kazı ve baret tamamen kalksın, boss her 10 katta, takılınca bölgeyi farmlayıp bossu kesme tatmini,
nadirlik açılışı sadece ilk seferde uzun, özellikler seviyeyle açılsın, yetenek ağacı (tasarımı bana bırakıldı).
- **Bölgeler (`zoneSize` 10):** her 10. kat bölge bossu (10, 20, ... 90; sonrası havuzlar döner, `RiskMath.MilestoneAt`). Boss katında göçük YOK.
  Risk bölge İÇİNDE kat kat artar: `base + bustChancePerZoneFloor*(f-1) + bustChancePerZone*(z-1)` (taban da aynı mantık, `bustFloorPerZone*`).
  Bossu yenince yeni bölgenin başında risk biraz düşer: derinlik kapısı göçük değil **boss gücü**. Bossu yenemeyen oyuncu 1-9. katları
  farmlar (para + eşya + XP), güçlenir, tekrar dener. Geçit (seviye 5) yenilen bossun katından başlatır.
- **Canavarlar:** normal canavar havuzları bölge içine (1-9, 11-19, ...). Tüm keşif bossları `MonsterKind.Boss`. Ölçek 1.16 can / 1.15 saldırı.
  Silah/kazma/malzeme `minStep`'leri ikiye katlandı (en derin 72). Güç göstergesi = bölge bossunu sağ yenebildiğin en derin kat.
- **Seviye ve XP (`ProgressionBalance`, `GameSession.Progression.cs`):** kat başı `xpPerFloor*bölge`, öldürme `xpPerKill*bölge`,
  boss `xpPerBoss*bölge`. Gereken XP `xpBase*xpGrowth^(seviye-1)`. Seviye ve yetenekler Yeni Çağda da KALIR. Kadim Bilgelik XP çarpanı.
- **Özellik kilitleri (`Feature`):** Yetenekler 2, Ekipman/ganimet 3, Sigorta + Dedektör 4, Geçitler 5, Güçlendirme 6, Fener + Terazi 7,
  Özel bulgular (harita/kristal/put) 8, Yeni Çağ 12 (+ 30. kat rekoru). **Kilitli şey HİÇ görünmez** (8. tur kararı): sekme, yükseltme satırı,
  stat kartı (sigorta), güçlendirme notu ve ipuçlarındaki bahisler (`RunTooltips.If`) açılana kadar gizli. Açıldığında oyuna yeni bir boyut
  gelmiş hissi. Seviye penceresi yeni özelliği yazar ve okunana kadar bekler. Yükseltme ekranında sadece "Seviye N'de yeni bir şey açılacak..."
  (ne olduğu söylenmez: merak + hedef).
- **Yetenek ağacı (`TalentBalance`, 21 düğüm, her seviye 1 puan):** 3 dal x 4 sıra; bir dala 5 puan harcayınca sonraki sıra açılır,
  en alttaki tek düğüm dalın imzası. Cüce Madenci (kazı gücü, risk, kat ödülü, bulgu, sigorta, enkaz; imza Dağın Kalbi: bölgenin ilk 5 katında
  risk -%50), Kılıç Ustası (saldırı, can, kritik, savunma, can çalma, öfke yavaşlatma; imza Kral Katili: bosslara +%40), Ejder Avcısı
  (nadirlik, hazine değeri, ganimet, satış, Pırıltı +1 nadirlik, telafi; imza Ejder Gözü: %12 ikiz bulgu). Sıfırlama ücretli `100*seviye²`.
  Neden: her seviye anında hissedilen bir seçim, dal imzası uzun vadeli hedef, sıfırlama farklı build denemeye davet.
- **İlk-kez açılış:** `PlayerProgress.seenTiers`. Bir nadirlik (lacivert+) ilk kez gelince uzun renk tırmanışı + "İLK ...!" (kapatana kadar bekler).
  Sonrakiler: sarı+ ya da mavi+ ganimet hızlı kart (`ShowFindQuick`, ~0.9 sn, tıkla geç), diğerleri uçan yazı. Keşif katı her zaman uzun.
- **Göçük cezası ağırlaştı (baret yok):** telafi (+%12 nadirlik/kademe, Talih Çarkı ile artar), enkazdan kurtarma (%50 + Enkaz Avcısı),
  sigorta (+Payanda Ustası, sınır %90) ve Dağın Kalbi yumuşatır.
- **Boss öfkesi:** boss 8 sn'de, mini boss 12 sn'de bir öfkelenir: saldırı +%20/kademe. Uzayan savaş = ölüm. Hasar yetmezse takılırsın:
  eşya kasmak, güçlendirmek anlamlı. Zamanlayıcı `GameController`'da, görsel öfke çubuğu can barının altında.
- **Göçük moral bozmasın:** Talihsizlik telafisi (her göçük/yenilgi +%12 nadirlik, maks 5 kademe, başarılı dönüşte sıfır) +
  Enkazdan kurtarma (turun en nadir eşyası %50 ihtimalle envantere).
- **Arayüz sadeleşti (9. tur):** yeni oyuncu sayı yağmuruna boğulmasın. Sol HUD = karakter + derinlik + turun değeri + en fazla 4 kart:
  KUDRET (ekipman açılınca), "N. KAT BOSS" ulaşma % (savaşta "BU SAVAŞ" tehdidi), SİGORTA (açılınca), TELAFİ (sadece birikmişse)
  + tur etkileri satırı (sadece varsa). Ayrıntı fareyle (`TooltipTrigger` → `TooltipView`, metinler `RunTooltips.cs`).
- **Kumar terazisi (`RiskGaugeView`, alt orta):** risk/ödül döngüsünün kalbi sol köşede bir stat olmasın diye ortaya alındı. Büyük göçük yüzdesi
  (yeşil → sarı → kırmızı, risk belirgin artınca "pat"), çubuk, "KAZANIRSAN +X" | "KAYBEDERSEN -Y", yorum (GÜVENLİ / DEĞER / KUMAR / ÇILGINLIK).
  Savaşta: tehdit, "KAÇARSAN X cebe" | "ÖLÜRSEN -Y". **Kalp atışı:** risk %8'den %45'e çıktıkça ekran kenarı kırmızı nabız gibi atar
  (60 → 150 bpm, çift vuruş), kaybedecek bir şey yoksa atmaz; panel de nabızla hafifçe büyür. KAZ butonunun altında da "göçük %X" yazar.
- **Kudret (eşya puanı, gear score):** eşyanın "eşdeğer seviyesi" * 10: `itemLevel + ln(malzeme * 1.22^tier * güçlendirme) / ln(1.14) + 1.5 * ek stat`
  (`RiskMath.GearKudret`, `kudretPerLevel`, `kudretPerAffix`). Toplam = kuşanılanlar. Doğrusal: iki eşya tek bakışta karşılaştırılır.
  Eşya detayında "Kudret 132, toplam 940 → 985 ▲+45", ekipman sekmesinde ve sol HUD'da toplam.
- **Karakter (`AvatarView`, `Art/CharacterArt`):** 24x32 pixel art, katmanlı. Giyilenler malzeme renginde üstünde görünür,
  yeşil+ kaskta boynuz / zırhta omuzluk, kırmızı+ zırhta pelerin, en nadir parçanın renginde aura (kırmızı+ dönen hüzme), elde silah,
  sırtta kazma, nefes salınımı. Ekipman sekmesinde büyük, kazıda küçük.
- **Hissiyat:** hit-stop (kritik 0.05 sn, öldürme 0.07, boss 0.15; `RunFeedbackDirector` FxTime.Speed'i tek yerden yönetir),
  altınlar sayaca kavisle uçar (`FxLayerView.CoinsTo`), sarı+ bulguda ganimet ışını, titreyen meşale ışığı, süzülen toz.

## Sınıflar, büyüler, görsel zenginlik (10. tur kararları)
Kullanıcı isteği: daha sofistike karakter, nadirlik/Kudret görünüşe yansısın, güçlendirme ödüllendirici olsun, Diablo/WoW gibi eşya sevenlere
hitap etsin, yükseltme/yetenekler ikonlu olsun, class sistemi + spell book (seviyeyle yeni büyü, büyüler savaş dışı şeylere de etki etsin).
- **Sınıflar (`Core/Classes.cs`, `HeroClass`, kayıtta sayı, `saveVersion` 7):** ilk girişte ve her Yeni Çağda seçim ekranı (`ClassSelectView`).
  Savaşçı (can x1.3, savunma x1.35), Büyücü (saldırı x1.2, şans +0.06, mana +50, can x0.9), Okçu (kritik +%8, bulgu +%3, kazı x1.15).
  **Silahlar sınıfa özel:** 27 silah, sınıf başına 9 (`GearBaseDefinition.heroClass`). Sadece kendi sınıfının silahı düşer ve kuşanılır.
  Yeni şablonlar: değnek, küre, kitap, arbalet.
- **Mana ve büyüler (`ClassBalance`, `GameSession.Spells.cs`):** mana 100 (+sınıf), tura yarı dolu başlar, kat başı +12, öldürme +18.
  Derine inmek = büyü yakıtı (risk almayı ödüllendirir). Süreler saniye değil kat/vuruş sayısı (Core zaman bilmez). Her sınıf 6 büyü, Sv 1/3/5/8/11/14.
  Savaşçı: Yarma Darbesi (x3 vuruş), Demir İrade (%40 can), Kaya Omuz (3 kat risk -%40), Savaş Narası (boss öfkesi sıfır), Yağma Hırsı
  (2 kat ödül x2), Titan Kalkanı (4 vuruş -%80). Büyücü: Ateş Topu, **Kehanet** (sonraki katın göçük zarı ŞİMDİ atılır, sonuç söylenir),
  Altın Simyası (risk altındakinin %25'i cebe), Buz Kalkanı, Arkan Göz (garantili bulgu +2 nadirlik), Zamanı Bük (2 kat risk -%60).
  Okçu: Ok Yağmuru (4 vuruş), İzci Gözü (2 kat garantili bulgu), Kartal Gözü (5 kesin kritik), Yol Bulucu (4 kat risk -%30), Ganimet Avı
  (sonraki zaferde eşya, +1 nadirlik), Ölüm Atışı (x5). Büyü çubuğu sol alt HUD (kısayol 1-6), Büyü Kitabı sekmesi (öğrenilmemiş: kara siluet + "???").
  Seviye penceresi yeni büyüyü yazar. Kehanet sonucu Kumar terazisinde yazar. Büyü etkileri tur etkileri satırında (mor).
- **Görünüş (Diablo/WoW hissi):** `GearArt.Decorate` hem ikonlara hem karakter katmanlarına uygulanır: lacivert+ süs taşları nadirlik renginde,
  turuncu+ kontur parlar, +4 altın parıltı, +7 cilalı metal, +10 altın kaplama. Mavi+ eşya ilk ek statından sıfat alır ("Destansı Keskin Kısa Kılıç").
  Karakter: sınıf kıyafeti (cübbe + sivri şapka / başlık + sadak / alın bandı, kask-zırh yokken), silah arkasında güçlendirme parıltısı,
  **Kudret rütbesi** (Çaylak → Madenci → Yiğit → Kahraman → Efsane → Yarı Tanrı → Ölümsüz, `UIStrings.KudretRank`) ayak altı ışık halkası ve
  yükselen kıvılcımlar (toplam güçlendirme 40+ ise altın). Kudret kartı rütbeyi yazar.
- **Güçlendirme ödüllendirici:** örs vuruşları (seviyeyle uzayan gerilim, kıvılcım), başarıda ikon yeni görünüşüne geçer, büyük "+N" patlar,
  eşiklerde (4/7/10) ekranda başlık; kırılınca parçalanma. Eşya kutularında güçlendirme parıltısı + köşede "+N".
- **İkonlar (`Art/IconArt`):** 30 piktogram (kazma, kalkan, göz, alev...) x renk = yükseltme, yetenek, büyü, sınıf, Kadim ikonları.
- **Config uyarısı:** silah listesi `BalanceConfig.asset`'e yazılı olduğundan yeni sınıf silahları için asset **Reset** gerekir.

## Bağımlılık ve uzun oyun sistemleri (5. tur kararları)
- **Oto-kazı 7. turda KALDIRILDI** (kullanıcı isteği; `AutoDigView` silindi, kayıtta `autoDig` yok).
- **Geçitler:** Keşif katının bossu yenilince (`conqueredMilestones`) sonraki turlara o kattan başlanabilir (atlanan katların ödülü yok).
  Yükseltme ekranında "Başlangıç: ..." butonu döngüyle seçer.
- **Güç/risk göstergesi (`GameSession.Insight.cs`):** "Savaş gücün: N. kat seviyesi" (normal canavar Orta'dan kolay VE mini boss ölümcül değil
  olan en derin kat), tehdit (Kolay/Orta/Zor/Ölümcül = canavarı öldürene kadar kaybedilen can oranı <0.2/<0.5/<1/>=1),
  göçük hesabıyla sonraki boss katına kadar ortalama kaç kat ve oraya ulaşma ihtimali, kumar satırı (kazanırsan / kaybedersen / %, yorum: Güvenli adım, Değer,
  Kumar, Çılgınlık). Eşya detayında "Savaş gücü 14 → 17" ve kazmada "Duvar 5 → 3 vuruş".
- **Kazma yuvası + kazı gücü:** `GearSlot.Pickaxe`, `StatType.DigPower`. Gereken vuruş = ceil(3 * 1.07^(kat-1) / kazı gücü), 1..8.
  8 kazma (Paslı → Yıldız Demiri). Eldeki kazma görseli kuşanılanla değişir.
- **Nadirlik farkı büyütüldü:** ağırlıklar 100/30/9/2.7/0.8/0.24/0.07/0.02/0.006 (~3.3x), güç 1/1.6/2.5/4/6.5/10/16/26/45,
  eşya nadirlik çarpanı `1.22^tier` (mor x4.9), luckPerStep 0.025, tur şansı sınırı 1.5. Kırmızı/mor eşyalar efsanevi isim alır.
- **Güçlendirme (Shaiya tarzı):** +1..+10, şans %95 → %15, her seviye statlar +%10. +4'ten sonra başarısızlık seviye düşürür,
  +7'den sonra eşya KIRILIR. Ücret satış değeri x2 x1.6^seviye (para yutağı). Ruletvari animasyon.
- **Yeni Çağ (prestij):** seviye 12 + 30. kat rekoru. Yadigar = floor(rekor^1.6 / 10). Sıfırlanır: para, yükseltmeler, eşyalar, rekor, geçitler.
  Kalır: çağ, yadigar, seviye, XP, yetenekler, görülen nadirlikler, 5 Kadim yükseltme (Kadim Altın, Yıldızların Lütfu, Kadim Bilgelik (XP x1.2/sv),
  Kralların Kanı, Ejder Hazinesi).
- **Yükseltme maks seviyeleri yükseltildi** (tek çağda dolmasın): Kazanç 40, Güvenlik 25, Sigorta 15, Dedektör 20, Fener 25, Terazi 30.
- **İçerik:** bölge başına 6 canavarlık havuz (48), her boss katında 5 bossluk havuz (45), 19 şablon. LOTR havası ama **özgün isimler**
  (Steam'de telif/marka riski olmasın: Balrog/Nazgûl/Moria gibi birebir isim YOK). Boss katları (10..90): Goblin Tünelleri, Kayıp Cüce Karakolu,
  Kristal Salonlar, Gözcünün Gölü, Cüce Kralın Demirhanesi, Kadim Höyükler, Ejderin Altın Yatağı, Gölge ve Alev Köprüsü, Dünyanın Kökleri
  (100+ "Derin ... 2" diye döner, `UIStrings.MilestoneName`). Kaya rengi de 10 katta bir değişir (`DepthTheme`, 9 renk döner).
- **Çantayı sat:** ekipman sekmesinde, iki basış ister.
- **Config uyarısı:** `BalanceConfig.asset` Inspector'da bir kez düzenlenirse tüm alanlar asset'e yazılır ve koddaki yeni varsayılanlar
  artık uygulanmaz. Yeni varsayılanları almak için asset'in ⋮ menüsünden Reset.

## Savaş, canavarlar, ekipman (RPG katmanı)
Kararlar (kullanıcıyla): **tıklayarak vur**, **yenilgi = göçük gibi tur biter**, **eşyalar yüzeye dönünce kalıcı**, **savaş + kazı statları**.
- Duvar yıkılınca canavar çıkabilir: `min(0.15 + 0.012*(step-1), 0.5)`. Boss katında (her 10.) boss garantili (zar yok).
  Canavarlı katta bulgu zarı **zafere ertelenir** (canavar katın hazinesini korur).
- Savaşta kazı tuşu = **SALDIR** (basılı tut, `playerAttackCooldown` 0.16 sn). Canavar `monsterAttackInterval` (1.6 sn, boss x0.85) aralıkla vurur;
  sayacı `GameController` tutar (Core zaman bilmez), uzun sahnelerde (pencere vs.) durur. İlk vuruştan önce 0.8 sn tolerans.
- Oyuncu canı tur başında `Stats.Health` ile dolar; tur içinde kalıcı, savaşsız her katta %8 dolar. Can 0: tur biter
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
  - Envanter işlemleri sadece turlar arası: Kuşan / Çıkar / Sat / Çantayı sat. "En iyileri kuşan" 8. turda KALDIRILDI:
    oyuncu düşen eşyanın gücünü kendisi değerlendirsin (detayda "Savaş gücü 14 → 17" karşılaştırması kalır).
- Görsel: kazı sahnesinde canavar delikten büyüyerek çıkar (boss: aura, kükreme sarsıntısı), üstünde isim + can barı.
  Sol altta elde silah (kazmanın aynası), sahnenin üstünde oyuncu can barı. Vuruş: silah sallanır, canavar kızarır/sarsılır, hasar sayısı
  (kritik: sarı "KRİTİK!"). Canavar saldırısı: kameraya atılır, ekran kızarır, -hasar. Ölüm: parçalanır, ödül, bulgu ve ganimet tek tek açılır.
- Ekipman ekranı: yükseltme ekranında sekme (Yükseltmeler | Ekipman). Sağda stat özeti + 8 yuva + 8x4 envanter, solda seçili eşya detayı
  (statlar ve kuşanılıya göre +/- fark, Kuşan/Çıkar, Sat).
- Denge notu: 10. kat bossu (Azurion ~1060 can) ekipmansız ~18 sn sürer ve öldürür; ekipman toplamayı zorunlu kılan ilk duvar. Python ile ölçülmeli.

## Görseller (kodla üretilen pixel art, dosya yok)
- `Art/PixelArt`: metin ızgarasından sprite (`FromRows`), seed'li kaya dokusu, birikimli çatlak (aynı seed + büyüyen stage),
  hüzme, parıltı, vignette. Hepsi Point filtreli ve önbellekli (bellek sızmaz).
- `Art/SpriteLibrary`: 16x16 ikonlar metin olarak (harf = renk, tablo dosyanın başında). Kazma ve her bulgu türü.
  Çeşitlilik: Hazine nadirlikle değişir (para yığını → altın külçe → sandık → taç), Kese (kese → çuval).
  Kristal gri çizilip nadirlik rengine boyanır. İkonlar koleksiyon kartlarında, sahnede ve pencerede.
- Pencerede ikon önce **siyah siluet** (ne olduğu belli, ne kadar iyi olduğu değil), renk tırmanışı biter, sonra renklenir.

## Hissiyat (juice) ve spam önleme
- Her kat bir sahne: `RunFeedbackDirector` sonucu oynatır. `GameController`: vuruş sırasında `_busy` (butonlar sönmez),
  sonuç sahnesinde `SetBusy(true)` (butonlar da kilitli).
  2. Kat ödülü: derinlik "pat", turun değerinden uçan +X. Ödül turun o ana kadarki değerinin %25'i/%50'si üstündeyse açık mavi/altın.
  3. Bulgu: nadirlik İLK KEZ görülüyorsa (lacivert+) ya da boss katıysa uzun pencere (görülmüşse hızlı kart / uçan yazı, 7. tur): çerçeve beyazdan gerçek nadirliğe kadar renk renk tırmanır
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
  | LuckCharm | Uğur tılsımı | Tur boyu `runLuck += baseValue*power` (daha nadir bulgular) |
  | FindStreak | Eski harita | Sonraki `floor(1 + 0.5*tier)` katta bulgu garantili (zar 2 atılmaz). En az açık mavi |
  | Multiplier | Parlak kristal | Risk altındaki kazanç `*= 1 + min(0.1*power, 2)` anında (en fazla x3). En az mavi |
  | CursedTreasure | Lanetli put | Büyük hazine (baseValue 4) risk altında + tur riski x1.15 (toplam en fazla x1.6). En az mavi |
- Tür ağırlıkları: Hazine 40, Kese 20, Damar 18, Kaya 15, Tılsım 10, Harita 6, Kristal 5, Put 5.
- Boss katları (keşif katları) her 10 katta bir, adları `UIStrings.MilestoneName` (bkz. 7. tur kararları).
  Garantili bulgu çıkar, her katta en düşük nadirlik daha yüksek. Keşfedilmemiş katın adı "???" görünür (merak).
  İlk kez ulaşınca ayrıca güvenli ilk keşif ödülü (`o katın ödülü * firstReachRewardSteps`) doğrudan cebe girer.
  Amaç: oyuncunun kendine "şu kata kadar in, dön" hedefi koyması (geliştiricinin kendi stratejisi). Keşif katı bu hedefi doğal kılıyor,
  bir önceki katta "bir tane daha" gerilimi yaratıyor.
- Koleksiyon: aynı tür + aynı nadirlik tek kartta birikir (×adet, toplam etki), en nadir solda. 9 kutu, fazlası "+N daha". Ekipmanlar yuvaya göre de ayrılır.
  Kartın üstüne fareyle gelince grubun toplam etkisi ve risk altında mı / cepte mi yazar (`UIStrings.FindGroupTooltip`). Amaç: kristal ya da
  damar gelince oyuncu turun değerini görüp risk almaya istekli olsun. Kristaller birleşik çarpanla gösterilir.

## Yükseltmeler (6)
| Tür | UI adı | Etki | Birikim |
|---|---|---|---|
| Earnings | Daha iyi kazma | Kat ödülü | `(1+e)^lvl` |
| Safety | Destek kirişleri | Göçük riski | `(1-e)^lvl` |
| Insurance | Sigorta | Göçükte kurtarılan | `1-(1-e)^lvl` |
| FindChance | Maden dedektörü | Bulgu şansı | `+e*lvl` (üst sınır `maxFindChance`) |
| Luck | Uğurlu fener | Nadirlik | `luck += e*lvl` |
| FindValue | Kuyumcu terazisi | Hazine/Kese değeri | `(1+e)^lvl`, e=0.12 (maks x9.6) |

## Alınan kararlar
| Konu | Karar |
|---|---|
| Kazanç modeli | Kat başı ödül × çarpan, tur 0'dan başlar, giriş ücreti yok |
| Risk modeli | Her iniş bağımsız zar, olasılık derinlikle doğrusal artar |
| Ekonomi patlaması | İlk sürümde her şey maksimumdayken risk %1'e iniyor, derinlik sınırsızlaşıp kazanç 10^15'e çıkıyordu (performans değil, denge sorunu: double 10^308'e kadar gider). Çözüm: **derinlikle artan taban risk** `minBustChance + bustFloorPerStep*(step-1)` (30. katta ≥ %30), tur bonus sınırları (`maxRunProfitBonus` +%200, `minRunSafetyFactor` 0.5), Kuyumcu terazisi 0.20→0.12. Hesap: maks yükseltme + yarıya inmiş riskle 20. kata ulaşma ~%7, 30. kata ~%0.2. 7. turda risk bölge içinde artan modele geçti (bkz. 7. tur kararları) |
| Sayı gösterimi | `UIStrings.Num`: 10.000 altı tam sayı, üstü 12.3K / 4.56M / 7.8B / T / Qa..., liste biterse 1.23e45 |
| Tur değeri | `RunState.TotalValue` = risk altındaki (`Earnings`) + cepte (`Banked`: Kese + ilk keşif ödülü). `bestRunValue` kalıcı rekor |
| Yükseltme birikimi | Çoğu çarpımsal. Seviyeler kalıcı, eski ilerleme boşa gitmez |
| Sigorta | `1 - (1-e)^level`, %100'e asla ulaşmaz. Sadece risk altındaki kazanca uygulanır |
| Baret | 7. turda kaldırıldı. Göçük cezasını telafi, enkaz, sigorta ve Dağın Kalbi yumuşatır |
| Fiyatlar | `ceil(baseCost * costGrowth^level)`, tam sayı |
| Para tipi | `double` (idle oyunlar gibi büyük sayılara hazır). Gösterimde aşağı yuvarlanır |
| Kayıt | JsonUtility ile `Application.persistentDataPath/save.json` (saveVersion 6: seviye, XP, yetenekler, görülen nadirlikler; envanter ve kuşanılan eşyalar; GearSlot/StatType sayı olarak, SADECE SONA ekle). Tur ortası kaydedilmez (risk altındaki kazanç kaybolur, Kese/rekor ödülü anında kaydedilir) |
| Kaydı sıfırla | Üst barın sağında (kazı butonlarından uzakta). Tur sırasında gizli, iki basış ister (3 sn içinde) |
| Sahne | `RiskLoop > Build Prototype Scene` menüsü sahneyi koddan kurar ve bağlar. Config'i NewScene'den SONRA yükler (önce yüklenirse referans null düşer) |
| Platform önceliği | Önce **PC (yatay 16:9)**. Mobil sonra (yatay düzen telefonda da çalışabilir, ayrıca ele alınacak) |
| UI | uGUI + TextMeshPro (zengin metinde `<color>` ile nadirlik renkleri). CanvasScaler 1920x1080 Expand. Tur ekranı: sol istatistik / sağ bulgu+butonlar. Yükseltme ekranı: sol sonuç+başla / sağ 7 yükseltme |
| Klavye | ↓/S/Boşluk kaz (basılı tut = art arda vuruş; yükseltme ekranında YENİ basışla kazıya başla), ↑/W yüzeye dön. Tur bitince 0.6 sn tuş kilidi. Butonların UI navigasyonu kapalı |
| Layout tuzağı | Dikey düzenin içindeki yatay grupta `childForceExpandHeight = true` OLMAZ: grup esnek yükseklik ister ve ekranı kaplar. Yan yana dizilimde metin genişliği `preferredWidth = 0` ile sıfırlanır (TMP tek satır genişliğini bildirir, sütunu taşırır) |
| Tema | Mantık temadan bağımsız (`Step`, `Bust`, `Find`, `Zone`). Tema metinleri `UIStrings.cs` + `RarityStyle.cs` içinde |

## Mimari
```
Assets/Scripts/
  Core/        RiskLoop.Core.asmdef (noEngineReferences: true). Unity'ye BAĞIMLI OLAMAZ
    BalanceData.cs     Tüm denge sayıları (yükseltme, nadirlik, bulgu türü, hazine odası tanımları)
    RiskMath.cs        Saf formüller (durumsuz, zar dışarıdan). Python'a birebir taşınacak dosya
    GameSession.cs     Kurallar (partial): tur, kazı, bulgular, yükseltmeler
    GameSession.Combat.cs  Canavar çıkışı, Attack / MonsterAttack, zafer ve ganimet
    GameSession.Gear.cs    Eşya üretimi (RollGearItem), envanter, kuşan/çıkar/sat, çantayı sat, güçlendirme
    GameSession.Prestige.cs  Yeni Çağ, Yadigar, Kadim yükseltmeler
    GameSession.Insight.cs   Savaş gücü, tehdit, boss katına ulaşma, kumar oranı (hiçbir şeyi değiştirmez)
    GameSession.Progression.cs  XP, seviye, özellik kilitleri (IsUnlocked), yetenek ağacı (öğren, sıfırla, toplam bonus)
    GameSession.Spells.cs  Sınıf seçimi, sınıf statları, mana, büyü kitabı, Cast (büyü etkileri)
    Classes.cs         HeroClass, SpellEffect, SpellDefinition, ClassDefinition, ClassBalance (3 sınıf, 18 büyü), SpellResult
    Progression.cs     Feature, ProgressionBalance, TalentBranch/Effect/Definition, TalentBalance (21 düğüm), TalentBonuses
    Prestige.cs        PrestigeType, PrestigeBalance
    Gear.cs            GearSlot, StatType, StatValue, GearItem
    CombatData.cs      CombatBalance (savaş/ekipman sayıları), MonsterDefinition, GearBaseDefinition, sonuç tipleri
    DefaultContent.cs  Varsayılan içerik: 27 silah (sınıf başına 9), 47 zırh/takı tabanı, 41 canavar
    CharacterStats.cs  Taban + kuşanılan eşyalar = toplam statlar (sınırlarla)
    RunState.cs        Tek turun durumu (tur bonusları, bulgular, can) + AdvanceResult
    FindType.cs        Bulgu türleri + FindResult
    PlayerProgress.cs  Kalıcı veri (para, yükseltme seviyeleri, istatistik, rekor)
    IRandomSource.cs   Rastgelelik arayüzü (seed verilebilir, testte sahte kaynak)
    UpgradeType.cs
  Game/        Unity katmanı (Assembly-CSharp)
    BalanceConfig.cs   ScriptableObject kabı. Context menu: Export/Import JSON -> Balance/balance.json
    GameController.cs  Köprü: UI olayları -> GameSession -> UI yenile, kaydet. İnişleri coroutine ile sıralar (_busy kilidi)
    RunFeedbackDirector.cs  Sonucu sahneler: kazı gerilimi, ödül, bulgu (ilk kez uzun / sonra hızlı), XP + seviye penceresi, göçük, dönüş efektleri
    SaveSystem.cs
    UIStrings.cs       Ekrandaki tüm metinler (tema/çeviri burada)
    RarityStyle.cs     Nadirlik isimleri ve renkleri
    FantasyTexts.cs    Canavar/boss/silah/malzeme/yuva/stat adları (id -> Türkçe), eşya stat metinleri
    Art/               IconArt (yükseltme/yetenek/büyü/sınıf ikonları), CharacterArt (karakter katmanları + sınıf kıyafeti),
    Art/               PixelArt (sprite/doku üretimi), SpriteLibrary (bulgu ikonları), DepthTheme (bölge renkleri),
                       MonsterArt (14 şablon x renk), GearArt (10 silah şablonu, 7 zırh/takı şablonu x malzeme rengi)
    UI/                TopBarView, RunPanelView, FindCollectionView + FindMiniCardView, UpgradePanelView, UpgradeRowView:
                       sadece gösterir, olay yayar. DigView (kazı + savaş sahnesi), UIParticleSpawner, FxLayerView, RevealPopupView, UIFx: efektler
                       EquipmentView + GearSlotView + ItemDetailView: ekipman sekmesi. PrestigePanelView + PrestigeRowView: Yeni Çağ sekmesi.
                       ClassSelectView: sınıf seçimi. SpellBarView + SpellButtonView: büyü çubuğu. SpellBookView: Büyü Kitabı sekmesi.
                       RiskGaugeView: Kumar terazisi.
                       TalentTreeView + TalentNodeView: yetenek sekmesi. FxTime: animasyon hız çarpanı (hit-stop)
                       ClassSelectView: sınıf seçimi. SpellBarView + SpellButtonView: büyü çubuğu. SpellBookView: Büyü Kitabı.
                       RiskGaugeView: Kumar terazisi.
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
zone(step) z    = (step-1)/zoneSize + 1,   f = (step-1)%zoneSize + 1,   f == zoneSize -> boss katı, bustChance = 0
bustChance(step)= clamp((baseBustChance + bustChancePerZoneFloor*(f-1) + bustChancePerZone*(z-1)) * (1-safety.e)^safetyLvl
                        * runSafetyFactor * (1-gear CaveResist) * (1-talent RiskReduction), floor(step), max)
floor(step)     = minBustChance + bustFloorPerZoneFloor*(f-1) + bustFloorPerZone*(z-1)
xpToNext(lvl)   = ceil(xpBase * xpGrowth^(lvl-1));  talent bonus = rank * perRank (TalentBonuses)
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
- [ ] Unity'de derleme ve oynanış testi (7. tur: tam ekran HUD, bölgeler, seviye/yetenek ağacı, ilk-kez açılış henüz Unity'de denenmedi; kod Unity Roslyn'iyle hatasız derlendi)
- [ ] Python simülasyonu: Yeni Çağ döngüsü süresi, güçlendirme para yutağı, seviye hızı (XP eğrisi 1.28), bölge bossu duvarları ve farm süresi
- [ ] Savaş dengesi: boss süreleri, ekipman ölçeği (1.14) vs canavar ölçeği (1.18/1.16), düşme oranları
- [ ] Sonra: silah türüne özel vuruş efekti, canavar türüne özel saldırı (zehir, büyü), set bonusları, eşya karşılaştırma ipucu (tooltip)
- [ ] Pixel art'ı gerçek bir sanatçı/asset ile değiştirmek istenirse: SpriteLibrary.FindIcon tek giriş noktası
- [ ] Geç oyun para yutağı (money sink): tüm yükseltmeler dolunca paranın anlamı kalmıyor. Aday: prestij / yeni maden (sıfırla, kalıcı çarpan kazan)
- [ ] Ses: kazı, bulgu (nadirliğe göre), seviye atlama, göçük
- [ ] Windows build ayarları: Player Settings'te pencere modu/çözünürlük (ör. 1600x900 pencereli, yeniden boyutlandırılabilir)
- [ ] Python simülasyonu: `Balance/balance.json` oku, `RiskMath`'i port et, stratejiler (sabit hedef katta dön, hazine odasında dön, EV>0 iken devam) için tur başı kazanç, yükseltme hızı ve nadirlik dağılımını ölç
- [ ] Core için EditMode testleri (Unity Test Framework kurulu): RiskMath formülleri, sahte IRandomSource ile Advance/CashOut/bölge riski/yetenekler
- [ ] git init + Unity `.gitignore` (Library/, Temp/, Logs/, UserSettings/ hariç)
- [ ] Sonra: bulgu kartı animasyonu (nadirliğe göre parlama, ses), görsel tema, sayı kısaltma (1.2K, 3.4M), yerelleştirme, mobil güvenli alan (safe area), yükseltme listesi uzarsa ScrollRect
