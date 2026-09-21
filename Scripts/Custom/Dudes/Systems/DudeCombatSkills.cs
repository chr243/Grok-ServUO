using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Persistent Dude combat skills (Wrestling / Tactics / Anatomy / MagicResist).
    /// Stored on DudeData; skill cap scales by EvolutionStage (1→100, 2→110, 3→120).
    /// Applied on summon and while linked.
    /// </summary>
    public static class DudeCombatSkills
    {
        /// <summary>Stage-1 floor / legacy constant.</summary>
        public const double Cap = 100.0;
        public const int RollMin = 40;
        public const int RollMax = 60;

        public static double GetCap(int evolutionStage)
        {
            if (evolutionStage >= 3)
                return 120.0;
            if (evolutionStage == 2)
                return 110.0;
            return Cap;
        }

        public static double GetCap(DudeData data)
        {
            if (data == null)
                return Cap;

            int stage = data.EvolutionStage;
            if (stage < 1)
                stage = 1;

            return GetCap(stage);
        }

        public static double Roll()
        {
            return (double)Utility.RandomMinMax(RollMin, RollMax);
        }

        public static double Clamp(double value)
        {
            if (value < 0.0)
                return 0.0;
            if (value > Cap)
                return Cap;
            return value;
        }

        public static double Clamp(double value, int stage)
        {
            double cap = GetCap(stage);
            if (value < 0.0)
                return 0.0;
            if (value > cap)
                return cap;
            return value;
        }

        public static double Clamp(double value, DudeData data)
        {
            return Clamp(value, data == null ? 1 : data.EvolutionStage);
        }

        public static bool IsTracked(SkillName skill)
        {
            return skill == SkillName.Wrestling
                || skill == SkillName.Tactics
                || skill == SkillName.Anatomy
                || skill == SkillName.MagicResist;
        }

        /// <summary>
        /// Magery / EvalInt / Meditation / Parry on Dudes come from hat/shield copy-in only.
        /// Never raise Base via skill gain.
        /// </summary>
        public static bool IsGearCopyOnly(SkillName skill)
        {
            return skill == SkillName.Magery
                || skill == SkillName.EvalInt
                || skill == SkillName.Meditation
                || skill == SkillName.Parry;
        }

        /// <summary>
        /// Apply a gear-copied skill value and lock it against gains.
        /// </summary>
        public static void SetGearCopySkill(Mobile m, SkillName name, double value, double cap)
        {
            if (m == null || m.Skills == null)
                return;

            Skill skill = m.Skills[name];
            if (skill == null)
                return;

            if (cap < 0.0)
                cap = 0.0;
            if (skill.Cap < cap)
                skill.Cap = cap;

            double v = value;
            if (v < 0.0)
                v = 0.0;
            if (v > cap)
                v = cap;

            skill.Base = v;
            skill.SetLockNoRelay(SkillLock.Locked);
        }

        public static void EnsureRolled(DudeData data)
        {
            if (data == null)
                return;

            if (data.Wrestling <= 0.0)
                data.Wrestling = Roll();
            if (data.Tactics <= 0.0)
                data.Tactics = Roll();
            if (data.Anatomy <= 0.0)
                data.Anatomy = Roll();
            if (data.MagicResist <= 0.0)
                data.MagicResist = Roll();
        }

        public static void ApplyToMobile(Mobile m, DudeData data)
        {
            if (m == null || data == null)
                return;

            EnsureRolled(data);

            SetSkillValue(m, SkillName.Wrestling, data.Wrestling, data);
            SetSkillValue(m, SkillName.Tactics, data.Tactics, data);
            SetSkillValue(m, SkillName.Anatomy, data.Anatomy, data);
            SetSkillValue(m, SkillName.MagicResist, data.MagicResist, data);
        }

        public static void WriteFromMobile(Mobile m, DudeData data)
        {
            if (m == null || data == null)
                return;

            data.Wrestling = Clamp(GetSkillValue(m, SkillName.Wrestling), data);
            data.Tactics = Clamp(GetSkillValue(m, SkillName.Tactics), data);
            data.Anatomy = Clamp(GetSkillValue(m, SkillName.Anatomy), data);
            data.MagicResist = Clamp(GetSkillValue(m, SkillName.MagicResist), data);
        }

        public static void SetSkillValue(Mobile m, SkillName name, double value)
        {
            SetSkillValue(m, name, value, null);
        }

        public static void SetSkillValue(Mobile m, SkillName name, double value, DudeData data)
        {
            if (m == null || m.Skills == null)
                return;

            Skill skill = m.Skills[name];
            if (skill == null)
                return;

            double cap = GetCap(data);
            double v = Clamp(value, data);
            if (skill.Cap < cap)
                skill.Cap = cap;
            skill.Base = v;
        }

        public static double GetSkillValue(Mobile m, SkillName name)
        {
            if (m == null || m.Skills == null)
                return 0.0;

            Skill skill = m.Skills[name];
            if (skill == null)
                return 0.0;

            return skill.Base;
        }

        /// <summary>
        /// Raise Cap on the four tracked skills to the Dude's stage cap without changing Base.
        /// </summary>
        public static void RaiseCapsOnMobile(Mobile m, DudeData data)
        {
            if (m == null || m.Skills == null || data == null)
                return;

            double cap = GetCap(data);
            RaiseCapOne(m, SkillName.Wrestling, cap);
            RaiseCapOne(m, SkillName.Tactics, cap);
            RaiseCapOne(m, SkillName.Anatomy, cap);
            RaiseCapOne(m, SkillName.MagicResist, cap);
        }

        private static void RaiseCapOne(Mobile m, SkillName name, double cap)
        {
            Skill skill = m.Skills[name];
            if (skill == null)
                return;

            if (skill.Cap < cap)
                skill.Cap = cap;
        }

        /// <summary>
        /// Small combat gains on successful hit. Writes into DudeData and refreshes live mobile.
        /// </summary>
        public static void TryGainOnHit(DudeData data, DudeBall ball, Mobile live)
        {
            if (data == null)
                return;

            EnsureRolled(data);

            bool changed = false;
            changed |= TryGainOne(data, SkillName.Wrestling, 0.35, 0.1);
            changed |= TryGainOne(data, SkillName.Tactics, 0.35, 0.1);
            changed |= TryGainOne(data, SkillName.Anatomy, 0.25, 0.1);
            changed |= TryGainOne(data, SkillName.MagicResist, 0.15, 0.1);

            if (!changed)
                return;

            if (live != null && !live.Deleted)
                ApplyToMobile(live, data);

            // Gains land on most melee hits; don't rebuild/broadcast the ball tooltip every swing.
            if (ball != null && !ball.Deleted)
                ball.InvalidatePropertiesThrottled();
        }

        private static bool TryGainOne(DudeData data, SkillName skill, double chance, double amount)
        {
            if (Utility.RandomDouble() >= chance)
                return false;

            double before = GetDataSkill(data, skill);
            if (before >= GetCap(data))
                return false;

            double after = Clamp(before + amount, data);
            if (after <= before)
                return false;

            SetDataSkill(data, skill, after);
            return true;
        }

        public static double GetDataSkill(DudeData data, SkillName skill)
        {
            if (data == null)
                return 0.0;

            switch (skill)
            {
                case SkillName.Wrestling:
                    return data.Wrestling;
                case SkillName.Tactics:
                    return data.Tactics;
                case SkillName.Anatomy:
                    return data.Anatomy;
                case SkillName.MagicResist:
                    return data.MagicResist;
                default:
                    return 0.0;
            }
        }

        public static void SetDataSkill(DudeData data, SkillName skill, double value)
        {
            if (data == null)
                return;

            double v = Clamp(value, data);
            switch (skill)
            {
                case SkillName.Wrestling:
                    data.Wrestling = v;
                    break;
                case SkillName.Tactics:
                    data.Tactics = v;
                    break;
                case SkillName.Anatomy:
                    data.Anatomy = v;
                    break;
                case SkillName.MagicResist:
                    data.MagicResist = v;
                    break;
            }
        }

        /// <summary>
        /// Linked-player SkillGain: keep ball in sync and hard-cap at stage cap.
        /// </summary>
        public static void SyncGainToBall(Mobile from, Skill skill, DudeData data, DudeBall ball)
        {
            if (from == null || skill == null || data == null)
                return;

            if (IsGearCopyOnly(skill.SkillName))
                return;

            if (!IsTracked(skill.SkillName))
                return;

            double cap = GetCap(data);
            if (skill.Cap < cap)
                skill.Cap = cap;
            if (skill.Base > cap)
                skill.Base = cap;

            SetDataSkill(data, skill.SkillName, Clamp(skill.Base, data));
            // Do not InvalidateProperties here — OPL rebuild mid-combat spikes ping while linked.
        }
    }
}
