using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Persistent Dude combat skills (Wrestling / Tactics / Anatomy / MagicResist).
    /// Stored on DudeData, capped at 100. Applied on summon and while linked.
    /// </summary>
    public static class DudeCombatSkills
    {
        public const double Cap = 100.0;
        public const int RollMin = 40;
        public const int RollMax = 60;

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

        public static bool IsTracked(SkillName skill)
        {
            return skill == SkillName.Wrestling
                || skill == SkillName.Tactics
                || skill == SkillName.Anatomy
                || skill == SkillName.MagicResist;
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

            SetSkillValue(m, SkillName.Wrestling, data.Wrestling);
            SetSkillValue(m, SkillName.Tactics, data.Tactics);
            SetSkillValue(m, SkillName.Anatomy, data.Anatomy);
            SetSkillValue(m, SkillName.MagicResist, data.MagicResist);
        }

        public static void WriteFromMobile(Mobile m, DudeData data)
        {
            if (m == null || data == null)
                return;

            data.Wrestling = Clamp(GetSkillValue(m, SkillName.Wrestling));
            data.Tactics = Clamp(GetSkillValue(m, SkillName.Tactics));
            data.Anatomy = Clamp(GetSkillValue(m, SkillName.Anatomy));
            data.MagicResist = Clamp(GetSkillValue(m, SkillName.MagicResist));
        }

        public static void SetSkillValue(Mobile m, SkillName name, double value)
        {
            if (m == null || m.Skills == null)
                return;

            Skill skill = m.Skills[name];
            if (skill == null)
                return;

            double v = Clamp(value);
            if (skill.Cap < v)
                skill.Cap = Cap;
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

            if (ball != null && !ball.Deleted)
                ball.InvalidateProperties();
        }

        private static bool TryGainOne(DudeData data, SkillName skill, double chance, double amount)
        {
            if (Utility.RandomDouble() >= chance)
                return false;

            double before = GetDataSkill(data, skill);
            if (before >= Cap)
                return false;

            double after = Clamp(before + amount);
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

            double v = Clamp(value);
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
        /// Linked-player SkillGain: keep ball in sync and hard-cap at 100.
        /// </summary>
        public static void SyncGainToBall(Mobile from, Skill skill, DudeData data, DudeBall ball)
        {
            if (from == null || skill == null || data == null)
                return;

            if (!IsTracked(skill.SkillName))
                return;

            double v = Clamp(skill.Base);
            if (skill.Base > Cap)
                skill.Base = Cap;

            SetDataSkill(data, skill.SkillName, v);

            if (ball != null && !ball.Deleted)
                ball.InvalidateProperties();
        }
    }
}
