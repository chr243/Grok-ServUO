using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Persistent per-player link backup (formerly stored on LinkingDevice).
    /// Attached to PlayerMobile and written in PlayerMobile.Serialize.
    /// </summary>
    public class DudeLinkState
    {
        private bool m_IsLinked;
        private Serial m_LinkedBallSerial = Serial.MinusOne;
        private bool m_HasBackup;
        private int m_BakBody;
        private int m_BakBodyMod;
        private int m_BakHue;
        private int m_BakHueMod;
        private int m_BakRawStr;
        private int m_BakRawDex;
        private int m_BakRawInt;
        private int m_BakHits;
        private int m_BakStam;
        private int m_BakMana;
        private double[] m_BakSkillBases;
        private bool m_FollowersHeld;

        public bool IsLinked
        {
            get { return m_IsLinked; }
        }

        public Serial LinkedBallSerial
        {
            get { return m_LinkedBallSerial; }
        }

        public bool FollowersHeld
        {
            get { return m_FollowersHeld; }
        }

        public static DudeLinkState Get(Mobile m)
        {
            PlayerMobile pm = m as PlayerMobile;
            if (pm == null)
                return null;
            return pm.DudeLinkState;
        }

        public DudeBall ResolveLinkedBall()
        {
            if (!m_IsLinked || m_LinkedBallSerial == Serial.MinusOne || m_LinkedBallSerial == Serial.Zero)
                return null;

            Item item = World.FindItem(m_LinkedBallSerial);
            return item as DudeBall;
        }

        public void SnapshotBackup(Mobile from)
        {
            m_HasBackup = true;
            m_BakBody = from.Body;
            m_BakBodyMod = from.BodyMod;
            m_BakHue = from.Hue;
            m_BakHueMod = from.HueMod;
            m_BakRawStr = from.RawStr;
            m_BakRawDex = from.RawDex;
            m_BakRawInt = from.RawInt;
            m_BakHits = from.Hits;
            m_BakStam = from.Stam;
            m_BakMana = from.Mana;

            int len = from.Skills != null ? from.Skills.Length : 0;
            m_BakSkillBases = new double[len];
            for (int i = 0; i < len; i++)
            {
                Skill sk = from.Skills[i];
                m_BakSkillBases[i] = sk != null ? sk.Base : 0.0;
            }
        }

        public void RestoreBackup(Mobile from, bool releaseFollowers)
        {
            if (from == null || from.Deleted)
                return;

            if (m_HasBackup)
            {
                from.BodyMod = m_BakBodyMod;
                from.HueMod = m_BakHueMod;
                // Prefer BodyMod clear for transform; Body itself usually stays race body.
                if (from.BodyMod == 0 && m_BakBody > 0)
                    from.Body = m_BakBody;

                from.RawStr = Math.Max(1, m_BakRawStr);
                from.RawDex = Math.Max(1, m_BakRawDex);
                from.RawInt = Math.Max(1, m_BakRawInt);

                if (m_BakSkillBases != null && from.Skills != null)
                {
                    int len = Math.Min(m_BakSkillBases.Length, from.Skills.Length);
                    for (int i = 0; i < len; i++)
                    {
                        Skill sk = from.Skills[i];
                        if (sk != null)
                            sk.Base = m_BakSkillBases[i];
                    }
                }

                int hits = m_BakHits;
                if (hits < 0)
                    hits = 0;
                if (from.Alive)
                {
                    if (hits < 1)
                        hits = 1;
                    if (hits > from.HitsMax)
                        hits = from.HitsMax;
                    from.Hits = hits;
                }

                from.Stam = Math.Min(from.StamMax, Math.Max(0, m_BakStam));
                from.Mana = Math.Min(from.ManaMax, Math.Max(0, m_BakMana));
            }
            else
            {
                from.BodyMod = 0;
                from.HueMod = -1;
            }

            from.SendSpeedControl(SpeedControlType.Disable);

            if (releaseFollowers && m_FollowersHeld)
            {
                from.Followers -= DudeLinkSystem.LinkFollowerSlots;
                if (from.Followers < 0)
                    from.Followers = 0;
                m_FollowersHeld = false;
            }

            ClearBackup();
        }

        public void ClearBackup()
        {
            m_HasBackup = false;
            m_BakSkillBases = null;
        }

        public void ClearLinkFlags()
        {
            m_IsLinked = false;
            m_LinkedBallSerial = Serial.MinusOne;
            m_FollowersHeld = false;
            ClearBackup();
        }

        public void SetLinked(DudeBall ball, bool followersHeld)
        {
            m_IsLinked = true;
            m_LinkedBallSerial = ball != null ? ball.Serial : Serial.MinusOne;
            m_FollowersHeld = followersHeld;
        }

        /// <summary>
        /// Revert player to backup human form. Writes Hits + combat skills back to the ball.
        /// </summary>
        public void Revert(Mobile from, bool fromDeath, bool faintBall)
        {
            if (!m_IsLinked)
                return;

            DudeBall ball = ResolveLinkedBall();
            DudeData data = ball != null ? ball.StoredDude : null;

            if (from != null && !from.Deleted && data != null)
            {
                // Persist current combat state onto the ball before restoring human skills/stats.
                data.Hits = Math.Max(0, from.Hits);
                DudeCombatSkills.WriteFromMobile(from, data);

                if (fromDeath || faintBall || from.Hits <= 0 || !from.Alive)
                {
                    data.IsFainted = true;
                    data.Hits = 0;
                }

                if (ball != null)
                    ball.InvalidateProperties();
            }
            else if (data != null && (fromDeath || faintBall))
            {
                data.IsFainted = true;
                data.Hits = 0;
                if (ball != null)
                    ball.InvalidateProperties();
            }

            if (from != null && !from.Deleted)
            {
                // Unlink FX (same family as recall despawn) before restoring human form.
                if (data != null)
                    DudeSummonEffects.PlayDespawn(data.Type, from.Location, from.Map, data.DefinitionId);
                else if (ball != null && ball.StoredDude != null)
                    DudeSummonEffects.PlayDespawn(ball.StoredDude.Type, from.Location, from.Map, ball.StoredDude.DefinitionId);

                RestoreBackup(from, true);
                DudeLinkSystem.Unregister(from);
            }
            else
            {
                DudeLinkSystem.Unregister(from);
            }

            ClearLinkFlags();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write(m_IsLinked);
            writer.Write((int)m_LinkedBallSerial);
            writer.Write(m_FollowersHeld);

            writer.Write(m_HasBackup);
            if (m_HasBackup)
            {
                writer.Write(m_BakBody);
                writer.Write(m_BakBodyMod);
                writer.Write(m_BakHue);
                writer.Write(m_BakHueMod);
                writer.Write(m_BakRawStr);
                writer.Write(m_BakRawDex);
                writer.Write(m_BakRawInt);
                writer.Write(m_BakHits);
                writer.Write(m_BakStam);
                writer.Write(m_BakMana);

                int len = m_BakSkillBases != null ? m_BakSkillBases.Length : 0;
                writer.Write(len);
                for (int i = 0; i < len; i++)
                    writer.Write(m_BakSkillBases[i]);
            }
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            m_IsLinked = reader.ReadBool();
            m_LinkedBallSerial = (Serial)reader.ReadInt();
            m_FollowersHeld = reader.ReadBool();

            m_HasBackup = reader.ReadBool();
            if (m_HasBackup)
            {
                m_BakBody = reader.ReadInt();
                m_BakBodyMod = reader.ReadInt();
                m_BakHue = reader.ReadInt();
                m_BakHueMod = reader.ReadInt();
                m_BakRawStr = reader.ReadInt();
                m_BakRawDex = reader.ReadInt();
                m_BakRawInt = reader.ReadInt();
                m_BakHits = reader.ReadInt();
                m_BakStam = reader.ReadInt();
                m_BakMana = reader.ReadInt();

                int len = reader.ReadInt();
                m_BakSkillBases = new double[len];
                for (int i = 0; i < len; i++)
                    m_BakSkillBases[i] = reader.ReadDouble();
            }
        }
    }
}
