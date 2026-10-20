using System.Collections.Generic;

namespace RiskLoop.Core
{
    /// <summary>
    /// Varsayılan içerik: eşya tabanları ve canavarlar (sadece sayılar ve id'ler).
    /// İsimler ve görseller Game katmanında (FantasyTexts, MonsterArt, GearArt) id ile eşlenir.
    ///
    /// Yeni silah/canavar eklemek: buraya bir satır + Game katmanında isim ve görsel eşlemesi.
    /// Bölgeler (DepthTheme ile aynı): 10 katlık bölgeler (1-9 normal canavar havuzu, 10 boss),
    /// 90. kattan sonra bölge renkleri ve boss havuzları döngüyle tekrar eder.
    /// </summary>
    public static class DefaultContent
    {
        private static StatValue S(StatType t, double v) => new StatValue(t, v);

        /// <summary>Ek rastgele statların taban değerleri (nadirlik arttıkça eşyaya bunlardan eklenir).</summary>
        public static StatValue[] AffixPool() => new[]
        {
            S(StatType.Attack, 2), S(StatType.Health, 12), S(StatType.Defense, 3),
            S(StatType.CritChance, 0.02), S(StatType.CritDamage, 0.08), S(StatType.CaveResist, 0.015),
            S(StatType.FindChance, 0.015), S(StatType.Luck, 0.03), S(StatType.GoldBonus, 0.04),
            S(StatType.DigPower, 0.3),
        };

        public static GearBaseDefinition[] GearBases()
        {
            var list = new List<GearBaseDefinition>();

            // ---- Silahlar (18). Ağırlık derinle artar: derinde yeni silahlar daha sık düşer ----
            void W(string id, int minStep, params StatValue[] stats) =>
                list.Add(new GearBaseDefinition(id, GearSlot.Weapon, minStep, 1.0 + minStep / 10.0, 1.0, stats));

            W("dagger", 1, S(StatType.Attack, 4.2), S(StatType.CritChance, 0.06));
            W("shortsword", 1, S(StatType.Attack, 6));
            W("spiked_club", 1, S(StatType.Attack, 6.6));
            W("hand_axe", 6, S(StatType.Attack, 7.2), S(StatType.CritDamage, 0.15));
            W("war_mace", 10, S(StatType.Attack, 6.9), S(StatType.Defense, 2));
            W("spear", 10, S(StatType.Attack, 6.6), S(StatType.CritChance, 0.04));
            W("longbow", 16, S(StatType.Attack, 6), S(StatType.FindChance, 0.03));
            W("mage_staff", 16, S(StatType.Attack, 6), S(StatType.Luck, 0.05));
            W("katana", 24, S(StatType.Attack, 7.2), S(StatType.CritChance, 0.06));
            W("great_axe", 24, S(StatType.Attack, 9), S(StatType.CritDamage, 0.2));
            W("war_hammer", 32, S(StatType.Attack, 9.6));
            W("soul_scythe", 36, S(StatType.Attack, 7.8), S(StatType.CritDamage, 0.35));
            W("rune_sword", 40, S(StatType.Attack, 8.1), S(StatType.CritChance, 0.05), S(StatType.Luck, 0.03));
            W("crystal_staff", 44, S(StatType.Attack, 7.5), S(StatType.Luck, 0.08));
            W("dragon_spear", 52, S(StatType.Attack, 9.3), S(StatType.CritChance, 0.06));
            W("shadow_blades", 56, S(StatType.Attack, 7.2), S(StatType.CritChance, 0.12));
            W("volcanic_greatsword", 64, S(StatType.Attack, 10.8), S(StatType.CritDamage, 0.25));
            W("void_scythe", 72, S(StatType.Attack, 10.2), S(StatType.CritDamage, 0.4));

            // ---- Kazmalar (8): kazı gücü + derinde ek kazı statları. Yeni kazma = duvarlar daha az vuruşta yıkılır ----
            void P(string id, int minStep, params StatValue[] stats) =>
                list.Add(new GearBaseDefinition(id, GearSlot.Pickaxe, minStep, 1.0 + minStep / 10.0, 1.0, stats));

            P("rusty_pick", 1, S(StatType.DigPower, 1.0));
            P("iron_pick", 8, S(StatType.DigPower, 1.3));
            P("steel_pick", 18, S(StatType.DigPower, 1.6));
            P("dwarf_pick", 28, S(StatType.DigPower, 2.0), S(StatType.CaveResist, 0.02));
            P("mithril_pick", 38, S(StatType.DigPower, 2.4), S(StatType.Luck, 0.03));
            P("runic_pick", 48, S(StatType.DigPower, 2.8), S(StatType.FindChance, 0.02));
            P("dragonbone_pick", 58, S(StatType.DigPower, 3.3), S(StatType.GoldBonus, 0.05));
            P("star_pick", 68, S(StatType.DigPower, 4.0), S(StatType.CaveResist, 0.03));

            // ---- Zırh parçaları: 7 malzeme x 5 yuva ----
            var armorMaterials = new (string id, int minStep, double mult)[]
            {
                ("cloth", 1, 0.8), ("leather", 1, 1.0), ("chain", 10, 1.15), ("plate", 20, 1.3),
                ("mithril", 36, 1.5), ("dragonscale", 52, 1.75), ("void", 68, 2.0),
            };
            foreach (var m in armorMaterials)
            {
                double w = 1.0 + m.minStep / 10.0;
                list.Add(new GearBaseDefinition($"{m.id}_helmet", GearSlot.Helmet, m.minStep, w, m.mult,
                    S(StatType.Defense, 3), S(StatType.Health, 10), S(StatType.CaveResist, 0.02)));
                list.Add(new GearBaseDefinition($"{m.id}_armor", GearSlot.Armor, m.minStep, w, m.mult,
                    S(StatType.Defense, 6), S(StatType.Health, 25)));
                list.Add(new GearBaseDefinition($"{m.id}_pants", GearSlot.Pants, m.minStep, w, m.mult,
                    S(StatType.Defense, 4), S(StatType.Health, 18)));
                list.Add(new GearBaseDefinition($"{m.id}_gloves", GearSlot.Gloves, m.minStep, w, m.mult,
                    S(StatType.Attack, 2), S(StatType.CritChance, 0.02)));
                list.Add(new GearBaseDefinition($"{m.id}_boots", GearSlot.Boots, m.minStep, w, m.mult,
                    S(StatType.Defense, 2), S(StatType.Health, 10), S(StatType.FindChance, 0.015)));
            }

            // ---- Takılar: 6 malzeme x 2 yuva ----
            var jewelMaterials = new (string id, int minStep, double mult)[]
            {
                ("copper", 1, 0.8), ("silver", 10, 1.0), ("gold", 20, 1.2),
                ("moonstone", 36, 1.45), ("dragoneye", 52, 1.7), ("star", 68, 2.0),
            };
            foreach (var m in jewelMaterials)
            {
                double w = 1.0 + m.minStep / 10.0;
                list.Add(new GearBaseDefinition($"{m.id}_necklace", GearSlot.Necklace, m.minStep, w, m.mult,
                    S(StatType.Health, 12), S(StatType.Luck, 0.04), S(StatType.GoldBonus, 0.05)));
                list.Add(new GearBaseDefinition($"{m.id}_ring", GearSlot.Ring, m.minStep, w, m.mult,
                    S(StatType.Attack, 1.5), S(StatType.CritDamage, 0.10), S(StatType.GoldBonus, 0.04)));
            }

            return list.ToArray();
        }

        public static MonsterDefinition[] Monsters()
        {
            var list = new List<MonsterDefinition>();
            void N(string id, int min, int max, double hp, double atk) =>
                list.Add(new MonsterDefinition(id, MonsterKind.Normal, min, max, 1.0, hp, atk));
            void B(string id, MonsterKind kind, int step, double hp, double atk) =>
                list.Add(new MonsterDefinition(id, kind, step, step, 0.0, hp, atk));  // ağırlık 0: rastgele çıkmaz

            // Her bölgede 6 canavarlık havuz (o kat aralığında rastgele biri gelir)
            // Goblin Tünelleri (1-4)
            N("cave_rat", 1, 9, 0.7, 0.8); N("slime", 1, 9, 1.0, 0.7); N("bat", 1, 9, 0.6, 1.0);
            N("goblin", 1, 9, 1.0, 1.0); N("orc_scout", 1, 9, 1.1, 1.05); N("warg_pup", 1, 9, 0.8, 1.15);
            // Kayıp Cüce Karakolu (5-9)
            N("skeleton", 11, 19, 1.0, 1.0); N("cave_spider", 11, 19, 0.8, 1.2); N("stone_goblin", 11, 19, 1.3, 0.9);
            N("blind_worm", 11, 19, 1.5, 0.7); N("orc_warrior", 11, 19, 1.2, 1.1); N("barrow_wight", 11, 19, 1.0, 1.2);
            // Kristal Salonlar (10-14)
            N("crystal_slime", 21, 29, 1.2, 0.8); N("crystal_spider", 21, 29, 0.9, 1.2); N("wraith", 21, 29, 0.8, 1.3);
            N("myconid", 21, 29, 1.4, 0.8); N("cave_troll", 21, 29, 1.8, 1.0); N("dark_warg", 21, 29, 0.9, 1.3);
            // Gözcünün Gölü (15-19)
            N("naga", 31, 39, 1.1, 1.1); N("drowned", 31, 39, 1.2, 1.0); N("serpent", 31, 39, 0.9, 1.25);
            N("water_elemental", 31, 39, 1.4, 0.9); N("lake_tentacle", 31, 39, 1.3, 1.15); N("marsh_ghoul", 31, 39, 1.0, 1.2);
            // Cüce Kralın Demirhanesi (20-24)
            N("iron_golem", 41, 49, 1.6, 0.8); N("fire_imp", 41, 49, 0.8, 1.3); N("dwarf_ghost", 41, 49, 1.0, 1.1);
            N("coal_worm", 41, 49, 1.3, 1.0); N("black_orc", 41, 49, 1.3, 1.2); N("forge_troll", 41, 49, 1.9, 1.05);
            // Kadim Höyükler (25-29)
            N("bone_whelp", 51, 59, 1.1, 1.15); N("fossil_spider", 51, 59, 1.0, 1.2); N("mummy", 51, 59, 1.4, 0.9);
            N("sand_worm", 51, 59, 1.5, 0.95); N("giant_spider", 51, 59, 1.2, 1.3); N("cursed_dead", 51, 59, 1.1, 1.25);
            // Ejderin Altın Yatağı (30-34)
            N("dragon_whelp", 61, 69, 1.2, 1.2); N("kobold", 61, 69, 0.9, 1.1); N("flame_bat", 61, 69, 0.8, 1.35);
            N("dragon_cultist", 61, 69, 1.0, 1.25); N("fire_drake", 61, 69, 1.4, 1.3); N("war_troll", 61, 69, 2.0, 1.15);
            // Gölge ve Alev (35+)
            N("lava_elemental", 71, 999, 1.5, 1.1); N("magma_slime", 71, 999, 1.3, 1.0); N("demon", 71, 999, 1.1, 1.35);
            N("hellhound", 71, 999, 1.0, 1.3); N("shadow_rider", 71, 999, 1.2, 1.4); N("flame_wraith", 71, 999, 0.9, 1.5);

            // Keşif katı boss havuzları (her katta 5; BalanceData.milestones ile eşleşir)
            // 3. kat: mini bosslar
            B("goblin_chief", MonsterKind.Boss, 3, 3.0, 1.2); B("orc_captain", MonsterKind.Boss, 3, 3.2, 1.25);
            B("rat_king", MonsterKind.Boss, 3, 2.6, 1.3); B("slime_mother", MonsterKind.Boss, 3, 3.6, 1.05);
            B("warg_alpha", MonsterKind.Boss, 3, 2.8, 1.35);
            // 5. kat: mini bosslar
            B("bone_lord", MonsterKind.Boss, 5, 3.2, 1.25); B("spider_matron", MonsterKind.Boss, 5, 3.0, 1.35);
            B("wight_thane", MonsterKind.Boss, 5, 3.4, 1.2); B("orc_warlord", MonsterKind.Boss, 5, 3.5, 1.3);
            B("stone_troll", MonsterKind.Boss, 5, 4.2, 1.1);
            // 10. kat: bosslar
            B("crystal_golem", MonsterKind.Boss, 10, 6.0, 1.4); B("troll_king", MonsterKind.Boss, 10, 7.5, 1.35);
            B("crystal_wyrm", MonsterKind.Boss, 10, 6.2, 1.55); B("pale_queen", MonsterKind.Boss, 10, 5.5, 1.65);
            B("myconid_elder", MonsterKind.Boss, 10, 7.0, 1.3);
            // 15. kat: mini bosslar
            B("naga_queen", MonsterKind.Boss, 15, 3.5, 1.3); B("lake_watcher", MonsterKind.Boss, 15, 4.0, 1.25);
            B("drowned_king", MonsterKind.Boss, 15, 3.6, 1.3); B("storm_spirit", MonsterKind.Boss, 15, 3.0, 1.45);
            B("marsh_hag", MonsterKind.Boss, 15, 3.2, 1.4);
            // 20. kat: bosslar
            B("forge_king", MonsterKind.Boss, 20, 6.5, 1.5); B("black_orc_chief", MonsterKind.Boss, 20, 6.8, 1.55);
            B("iron_troll_lord", MonsterKind.Boss, 20, 8.0, 1.4); B("imp_lord", MonsterKind.Boss, 20, 5.8, 1.7);
            B("ghost_king", MonsterKind.Boss, 20, 6.2, 1.6);
            // 25. kat: mini bosslar
            B("fossil_wyrm", MonsterKind.Boss, 25, 4.0, 1.35); B("barrow_king", MonsterKind.Boss, 25, 3.8, 1.45);
            B("great_spider", MonsterKind.Boss, 25, 3.6, 1.5); B("sand_wurm", MonsterKind.Boss, 25, 4.6, 1.25);
            B("mummy_lord", MonsterKind.Boss, 25, 4.2, 1.35);
            // 30. kat: bosslar
            B("elder_dragon", MonsterKind.Boss, 30, 7.0, 1.6); B("gold_sleeper", MonsterKind.Boss, 30, 8.5, 1.5);
            B("dragon_priest", MonsterKind.Boss, 30, 6.0, 1.75); B("war_troll_king", MonsterKind.Boss, 30, 8.8, 1.45);
            B("drake_mother", MonsterKind.Boss, 30, 7.2, 1.6);
            // 35. kat: mini bosslar
            B("lava_titan", MonsterKind.Boss, 35, 4.5, 1.4); B("black_rider", MonsterKind.Boss, 35, 4.0, 1.6);
            B("hell_warg", MonsterKind.Boss, 35, 3.8, 1.6); B("magma_mother", MonsterKind.Boss, 35, 5.0, 1.3);
            B("demon_lord", MonsterKind.Boss, 35, 4.2, 1.55);
            // 40. kat: bosslar
            B("core_devourer", MonsterKind.Boss, 40, 8.0, 1.7); B("shadow_flame", MonsterKind.Boss, 40, 9.0, 1.75);
            B("world_serpent", MonsterKind.Boss, 40, 9.5, 1.6); B("root_watcher", MonsterKind.Boss, 40, 8.5, 1.7);
            B("ancient_lich", MonsterKind.Boss, 40, 7.5, 1.85);

            return list.ToArray();
        }
    }
}
