using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// EXP award and leveling. Scaling knobs live on DudeScalingConfig (live-tunable).
    /// </summary>
    public static class DudeExperience
    {
        /// <summary>
        /// Legacy soft max (stage 1). Prefer GetMaxLevel(DudeData) for evolution-aware caps.
        /// </summary>
        public static int MaxLevel = 10;

        /// <summary>Base EXP granted per kill before level scaling.</summary>
        public static int BaseKillExp = 25;

        /// <summary>Bonus EXP per band of victim HitsMax.</summary>
        public static int KillExpPerVictimHits = 1;

        public static int GetMaxLevel(DudeData data)
        {
            int stage = data != null ? data.EvolutionStage : 1;
            return GetMaxLevel(stage);
        }


        /// <summary>
        /// Gear slots unlocked by evolution stage: stage1=2, stage2=3, stage3+=4.
        /// </summary>
        public static int GetGearSlots(int stage)
        {
            if (stage <= 1)
                return 2;
            if (stage == 2)
                return 3;
            return 4;
        }

        public static int GetGearSlots(DudeData data)
        {
            int stage = data != null ? data.EvolutionStage : 1;
            return GetGearSlots(stage);
        }

        /// <summary>
        /// Legacy no-op. Evolution unlocks gear slots, not kit abilities.
        /// Combat abilities will come from equipped gear (next step).
        /// </summary>
        public static void EnsureEvolutionAbilities(DudeData data)
        {
            // Intentionally empty — stage no longer grants kit abilities.
        }

        public static int GetMaxLevel(int evolutionStage)
        {
            if (evolutionStage <= 1)
                return 10;
            if (evolutionStage == 2)
                return 20;
            return 30;
        }

        public static int GetExpRequiredForLevel(int level)
        {
            if (level < 1)
                level = 1;

            DudeScalingConfig.EnsureLoaded();

            // Simple curve: 100 * level^2 (continues through L11–30), scaled live.
            double raw = 100.0 * level * level * DudeScalingConfig.ExpScale;
            int required = (int)Math.Round(raw);
            if (required < 1)
                required = 1;
            return required;
        }

        public static int CalculateKillExp(DudeData data, Mobile victim)
        {
            if (data == null || victim == null)
                return 0;

            int exp = BaseKillExp;

            BaseCreature bc = victim as BaseCreature;
            if (bc != null)
            {
                exp += Math.Max(0, bc.HitsMax / 10) * KillExpPerVictimHits;
            }

            return exp;
        }

        /// <summary>
        /// Combat skill cap by evolution stage: stage1=100, stage2=110, stage3=120.
        /// NOT level-based.
        /// </summary>
        public static double GetCombatSkillCap(DudeData data)
        {
            int stage = data != null ? data.EvolutionStage : 1;
            return GetCombatSkillCap(stage);
        }

        public static double GetCombatSkillCap(int evolutionStage)
        {
            if (evolutionStage <= 1)
                return 100.0;
            if (evolutionStage == 2)
                return 110.0;
            return 120.0;
        }

        /// <summary>
        /// Legacy GatherSkill migration helper only — NOT for combat skills.
        /// Level 1 → 50, Level 10 → 100.
        /// </summary>
        public static double GetSkillCapForLevel(int level)
        {
            if (level < 1)
                level = 1;

            double skill = 50.0 + ((level - 1) * (50.0 / 9.0));
            if (skill > 100.0)
                skill = 100.0;
            return skill;
        }

        /// <summary>
        /// Awards EXP to the Dude stored in the ball (and live creature if present).
        /// Handles multi-level-ups and classic Str/Dex/Int/Hits bumps.
        /// </summary>
        public static void AwardExperience(DudeBall ball, int amount)
        {
            AwardExperience(ball, amount, null);
        }

        public static void AwardExperience(DudeBall ball, int amount, Mobile notify)
        {
            AwardExperience(ball, amount, notify, false);
        }

        /// <param name="linkedQuiet">
        /// Linked combat path: skip OPL rebuild mid-kill (ApplyData still only on real level-up).
        /// </param>
        public static void AwardExperience(DudeBall ball, int amount, Mobile notify, bool linkedQuiet)
        {
            if (ball == null || ball.StoredDude == null || amount <= 0)
                return;

            DudeData data = ball.StoredDude;
            Mobile owner = notify;
            if (owner == null)
                owner = ball.RootParent as Mobile;

            DudeCreature live = ball.SummonedDude;

            // Root-cause fix: StoredDude.Hits is only synced on recall/death. Without syncing
            // current combat HP here, ApplyData after a kill restores stale (often full) Hits.
            if (live != null && !live.Deleted && live.Map != null && live.Map != Map.Internal)
                data.Hits = Math.Max(0, live.Hits);

            int oldLevel = data.Level;

            data.CurrentEXP += amount;

            if (!linkedQuiet && live != null && !live.Deleted && live.Map != null && live.Map != Map.Internal)
            {
                live.PublicOverheadMessage(MessageType.Regular, 0x59, false,
                    string.Format("+{0} EXP", amount));
            }

            if (owner != null)
                owner.SendMessage(0x59, "{0} gained {1} EXP.", data.DisplayName, amount);

            // Daily Training: count EXP while live Dude OR owner is in the assigned region.
            if (owner != null)
            {
                Point3D expLoc;
                Map expMap;
                if (live != null && !live.Deleted && live.Map != null && live.Map != Map.Internal)
                {
                    expLoc = live.Location;
                    expMap = live.Map;
                }
                else
                {
                    expLoc = owner.Location;
                    expMap = owner.Map;
                }
                DudeDailySystem.OnExp(owner, amount, expLoc, expMap);
            }

            int maxLevel = GetMaxLevel(data);
            int safety = 0;
            while (data.CurrentEXP >= data.EXPToNext && data.EXPToNext > 0 && data.Level < maxLevel && safety < 50)
            {
                data.CurrentEXP -= data.EXPToNext;
                LevelUp(data, owner);
                if (data.EXPToNext < 1)
                    data.EXPToNext = GetExpRequiredForLevel(data.Level);
                safety++;
                maxLevel = GetMaxLevel(data);
            }

            // At cap: keep the bar filled (evolution gate) but do not overflow endlessly.
            if (data.Level >= maxLevel && data.CurrentEXP > data.EXPToNext)
                data.CurrentEXP = data.EXPToNext;

            if (!linkedQuiet)
            {
                ball.InvalidateProperties();

                // Refresh summoned Dude mouseover Level/EXP bar after EXP changes.
                if (live != null && !live.Deleted)
                    live.InvalidateProperties();
            }

            // Only rewrite live creature stats/skills/speeds on a real level-up (not every XP tick).
            if (live != null && !live.Deleted && data.Level != oldLevel)
                live.ApplyData(data, false);

            // Gear EXP: same kill amount to every equipped DudeGear on the live Dude (full, not split).
            // Linked path with no live dude: skip gear EXP for now.
            if (live != null && !live.Deleted && live.Map != null && live.Map != Map.Internal)
            {
                for (int i = 0; i < live.Items.Count; i++)
                {
                    DudeGear gear = live.Items[i] as DudeGear;
                    if (gear == null || gear.Deleted)
                        continue;
                    gear.AwardGearExp(amount);
                }
            }
        }

        /// <summary>
        /// Hits gain on reaching this level. Tuned so Embit (base 50) ≈ 200 @10 / 500 @20 / 1000 @30.
        /// Values come from DudeScalingConfig (live-tunable).
        /// </summary>
        public static int GetHitsGainForLevel(int newLevel)
        {
            DudeScalingConfig.EnsureLoaded();

            if (newLevel <= 10)
                return DudeScalingConfig.HitsGainL2to10; // L2–10 default 17
            if (newLevel <= 20)
                return DudeScalingConfig.HitsGainL11to20; // L11–20 default 30
            return DudeScalingConfig.HitsGainL21to30;     // L21–30 default 50
        }

        public static void LevelUp(DudeData data, Mobile owner)
        {
            if (data == null)
                return;

            if (data.Level >= GetMaxLevel(data))
                return;

            DudeScalingConfig.EnsureLoaded();

            data.Level++;
            data.EXPToNext = GetExpRequiredForLevel(data.Level);
            if (data.EXPToNext < 1)
                data.EXPToNext = 1;

            // Shared Fire-baseline gains (Str/Dex/Int/Damage). Hits/Armor may be overridden by type.
            // Defaults: Str+5 / Dex+6 / Int+2 / Damage+MeleeDamagePerLevel.
            data.Str += DudeScalingConfig.StrGainPerLevel;
            data.Dex += DudeScalingConfig.DexGainPerLevel;
            data.Int += DudeScalingConfig.IntGainPerLevel;
            data.MinDamage += DudeScalingConfig.MeleeDamagePerLevel;
            data.MaxDamage += DudeScalingConfig.MeleeDamagePerLevel;

            bool isEarth = data.Type == DudeType.Earth;

            // Hits: Fire/Water/Air use band table; Earth uses +50% (rounded) on every band.
            if (isEarth)
                data.HitsMax += GetEarthHitsGainForLevel(data.Level);
            else
                data.HitsMax += GetHitsGainForLevel(data.Level);

            // Armor: Fire/Water/Air keep +1 every 3 levels; Earth +2 VirtualArmor every level-up.
            if (isEarth)
                data.VirtualArmor += 2;
            else if (data.Level % 3 == 0)
                data.VirtualArmor += 1;

            // Type specialty extras (Fire: none). Water Int×2 total; Air Dex+3 extra.
            ApplyTypeLevelUpExtras(data);

            data.Hits = data.HitsMax;
            data.IsFainted = false;

            if (owner != null)
            {
                owner.SendMessage(0x44, "{0} reached level {1}!", data.DisplayName, data.Level);
                owner.PlaySound(0x1F2);
            }
        }

        /// <summary>
        /// Earth Hits gain: +50% of Fire band values, rounded (defaults +26 / +45 / +75).
        /// Pebble base 55 → ~1489 @30 (~1500 target).
        /// </summary>
        public static int GetEarthHitsGainForLevel(int newLevel)
        {
            return (int)Math.Round(GetHitsGainForLevel(newLevel) * 1.5);
        }

        /// <summary>
        /// Per-type LevelUp extras after shared Fire baseline + Earth hits/armor overrides.
        /// Fire: none.
        /// Earth: hits/armor already applied in LevelUp (no further extras here).
        /// Water: +IntGainPerLevel again → Int +4 total (double Fire).
        /// Air: +3 Dex → Dex +9 total (Fire 6 + 3).
        /// Does not respec existing characters — only future level-ups.
        /// </summary>
        private static void ApplyTypeLevelUpExtras(DudeData data)
        {
            if (data == null)
                return;

            switch (data.Type)
            {
                case DudeType.Water:
                    // Caster: double Int gain (IntGainPerLevel already applied → one more).
                    data.Int += DudeScalingConfig.IntGainPerLevel;
                    break;

                case DudeType.Air:
                    // Fast: DexGainPerLevel + 3.
                    data.Dex += 3;
                    break;

                // Fire / Earth / default: no further extras (Earth handled above).
            }
        }

        /// <summary>
        /// Blast damage formula shared by Blast, Ring of Fire, and Infernox passive.
        /// </summary>
        public static int GetBlastDamage(int level)
        {
            if (level < 1)
                level = 1;

            DudeScalingConfig.EnsureLoaded();
            DudeAbilityConfig.EnsureLoaded();

            int damage = DudeAbilityConfig.BlastBase + (level * DudeAbilityConfig.BlastPerLevel);
            damage = (int)Math.Round(damage * DudeScalingConfig.AbilityDamageMultiplier);
            if (damage < 1)
                damage = 1;
            return damage;
        }
    }
}
