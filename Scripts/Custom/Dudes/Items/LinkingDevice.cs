using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Blessed link item. Double-click in backpack to link/unlink an owned DudeBall.
    /// While linked the player takes the Dude's form (body/stats/skills) and uses 3 follower slots.
    /// </summary>
    public class LinkingDevice : Item
    {
        private bool m_IsLinked;
        private Serial m_LinkedBallSerial;
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

        [Constructable]
        public LinkingDevice()
            : base(0x2F58) // crystal
        {
            Name = "Linking Device";
            Weight = 1.0;
            Hue = 0x48D;
            LootType = LootType.Blessed;
        }

        public LinkingDevice(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsLinked
        {
            get { return m_IsLinked; }
        }

        public Serial LinkedBallSerial
        {
            get { return m_LinkedBallSerial; }
        }

        public DudeBall ResolveLinkedBall()
        {
            if (!m_IsLinked || m_LinkedBallSerial == Serial.MinusOne || m_LinkedBallSerial == Serial.Zero)
                return null;

            Item item = World.FindItem(m_LinkedBallSerial);
            return item as DudeBall;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_IsLinked)
            {
                DudeBall ball = ResolveLinkedBall();
                if (ball != null && ball.StoredDude != null)
                    list.Add("Linked: {0}", ball.StoredDude.DisplayName);
                else
                    list.Add("Linked (ball missing)");
                list.Add("Double-click to unlink.");
            }
            else
            {
                list.Add("Not linked");
                list.Add("Double-click and target an owned Dude Ball.");
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (!IsChildOf(from.Backpack) && RootParent != from)
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (m_IsLinked)
            {
                Revert(from, false, false);
                from.SendMessage(0x59, "You unlink from your Dude.");
                return;
            }

            from.SendMessage("Target an owned filled Dude Ball to link.");
            from.Target = new LinkTarget(this);
        }

        public override void OnDelete()
        {
            if (m_IsLinked)
            {
                Mobile holder = RootParent as Mobile;
                if (holder == null)
                    holder = Parent as Mobile;

                if (holder != null)
                    Revert(holder, false, false);
                else
                    ClearLinkFlags();
            }

            base.OnDelete();
        }

        private class LinkTarget : Target
        {
            private readonly LinkingDevice m_Device;

            public LinkTarget(LinkingDevice device)
                : base(8, false, TargetFlags.None)
            {
                m_Device = device;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Device == null || m_Device.Deleted || from == null)
                    return;

                if (!m_Device.IsChildOf(from.Backpack) && m_Device.RootParent != from)
                {
                    from.SendLocalizedMessage(1042001);
                    return;
                }

                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    from.SendMessage("That is not a Dude Ball.");
                    return;
                }

                m_Device.TryLink(from, ball);
            }
        }

        public void TryLink(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || Deleted)
                return;

            if (m_IsLinked || DudeLinkSystem.IsLinked(from))
            {
                from.SendMessage("You are already linked.");
                return;
            }

            if (!from.Alive)
            {
                from.SendMessage("You must be alive to link.");
                return;
            }

            if (!ball.IsChildOf(from.Backpack) && ball.RootParent != from)
            {
                from.SendMessage("That Dude Ball must be in your backpack.");
                return;
            }

            if (!ball.HasDude || ball.StoredDude == null)
            {
                from.SendMessage("That Dude Ball is empty.");
                return;
            }

            DudeData data = ball.StoredDude;

            if (data.IsFainted)
            {
                from.SendMessage("{0} is fainted and cannot be linked.", data.DisplayName);
                return;
            }

            if (ball.IsAssignedToJob)
            {
                from.SendMessage("{0} is on a job and cannot be linked.", data.DisplayName);
                return;
            }

            if (from.Followers > 2)
            {
                from.SendMessage("Other followers already use more than 2 slots.");
                return;
            }

            if (from.Followers + DudeLinkSystem.LinkFollowerSlots > from.FollowersMax)
            {
                from.SendMessage("You do not have enough free follower slots to link (need {0}).", DudeLinkSystem.LinkFollowerSlots);
                return;
            }

            // Recall summoned Dude first
            if (ball.IsSummoned)
                ball.Recall(from);

            // Dismount
            IMount mount = from.Mount;
            if (mount != null)
                mount.Rider = null;

            SnapshotBackup(from);

            DudeDefinition def = DudeRegistry.Get(data.DefinitionId);
            if (def == null)
            {
                from.SendMessage("That Dude species is unknown.");
                ClearBackup();
                return;
            }

            DudeCombatSkills.EnsureRolled(data);
            DudeExperience.EnsureEvolutionAbilities(data);

            // Apply Dude form
            from.BodyMod = def.Body;
            from.HueMod = def.Hue;

            from.RawStr = Math.Max(1, data.Str);
            from.RawDex = Math.Max(1, data.Dex);
            from.RawInt = Math.Max(1, data.Int);

            int hits = data.Hits;
            if (hits < 1)
                hits = 1;
            if (hits > from.HitsMax)
                hits = from.HitsMax;
            from.Hits = hits;

            DudeCombatSkills.ApplyToMobile(from, data);

            from.SendSpeedControl(SpeedControlType.MountSpeed);

            from.Followers += DudeLinkSystem.LinkFollowerSlots;
            m_FollowersHeld = true;

            m_IsLinked = true;
            m_LinkedBallSerial = ball.Serial;

            if (!DudeLinkSystem.TryRegister(from, this, ball))
            {
                // Roll back if register failed
                RestoreBackup(from, false);
                ClearLinkFlags();
                from.SendMessage("Link failed.");
                return;
            }

            DudeSummonEffects.Play(data.Type, from.Location, from.Map, data.DefinitionId);
            from.SendMessage(0x59, "You link with {0}!", data.DisplayName);
            InvalidateProperties();
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
                RestoreBackup(from, true);
                DudeLinkSystem.Unregister(from);
            }
            else
            {
                DudeLinkSystem.Unregister(from);
            }

            ClearLinkFlags();
            InvalidateProperties();
        }

        private void SnapshotBackup(Mobile from)
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

        private void RestoreBackup(Mobile from, bool releaseFollowers)
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

        private void ClearBackup()
        {
            m_HasBackup = false;
            m_BakSkillBases = null;
        }

        private void ClearLinkFlags()
        {
            m_IsLinked = false;
            m_LinkedBallSerial = Serial.MinusOne;
            m_FollowersHeld = false;
            ClearBackup();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
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

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
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

            // World load with link flag: Login handler will revert; also schedule safety revert.
            if (m_IsLinked)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                {
                    if (Deleted || !m_IsLinked)
                        return;

                    Mobile holder = RootParent as Mobile;
                    if (holder == null)
                        holder = Parent as Mobile;

                    if (holder != null)
                        Revert(holder, false, false);
                    else
                        ClearLinkFlags();
                });
            }
        }
    }
}
