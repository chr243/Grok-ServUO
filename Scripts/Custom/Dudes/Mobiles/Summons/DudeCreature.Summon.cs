using System;
using Server.Custom.Dudes;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// Summon lifecycle on the creature side: recall/despawn FX sequence, syncing back to the
    /// ball, fainting into the ball instead of dying, and parking on Map.Internal.
    /// The ball side (summon / recall) is Spells/Summoning/DudeBall.Summon.cs.
    /// </summary>
    public partial class DudeCreature
    {
        private bool m_SyncingDeath;
        private bool m_Fainting;
        private bool m_Despawning;
        private bool m_BlessedBeforeDespawn;

        /// <summary>True while recall/despawn FX plays — no aggro, not a guard candidate.</summary>
        public bool IsDespawning
        {
            get { return m_Despawning; }
        }

        public void BeginDespawnSequence()
        {
            m_Despawning = true;
            m_BlessedBeforeDespawn = Blessed;
            Blessed = true;
            Combatant = null;
            Warmode = false;
            Frozen = true;
            Criminal = false;
            ControlOrder = OrderType.Stay;
            if (ControlMaster != null)
                ControlTarget = ControlMaster;
            FocusMob = null;
        }

        public void EndDespawnSequence()
        {
            m_Despawning = false;
            Blessed = m_BlessedBeforeDespawn;
            Frozen = false;
            Combatant = null;
            Warmode = false;
        }

        public void SyncToBall()
        {
            if (m_BoundBall == null || m_BoundBall.Deleted || m_BoundBall.StoredDude == null)
                return;

            DudeData data = m_BoundBall.StoredDude;
            data.Hits = Hits;
            // Persist seed, not HitsMax property (seed + Str offset), to avoid inflation.
            data.HitsMax = HitsMaxSeed > 0 ? HitsMaxSeed : HitsMax;
            data.Str = RawStr;
            data.Dex = RawDex;
            data.Int = RawInt;
            data.MinDamage = DamageMin;
            data.MaxDamage = DamageMax;
            data.VirtualArmor = VirtualArmor;
            data.Level = m_DudeLevel;
            data.CustomName = Name;
            if (Hue > 0)
                data.SkinHue = Hue;
            DudeCombatSkills.WriteFromMobile(this, data);
            m_BoundBall.InvalidateProperties();
        }

        public override bool OnBeforeDeath()
        {
            if (!m_IsWild && m_BoundBall != null && !m_BoundBall.Deleted)
            {
                // Faint: play despawn FX, then park on Internal so serial survives.
                if (m_Fainting)
                    return false;

                m_SyncingDeath = true;
                m_Fainting = true;
                SyncToBall();

                DudeData data = m_BoundBall.StoredDude;
                if (data != null)
                {
                    data.IsFainted = true;
                    data.Hits = 0;
                }

                Point3D loc = Location;
                Map map = Map;
                DudeType fxType = data != null ? data.Type : DudeType.Fire;
                string defId = data != null ? data.DefinitionId : m_DefinitionId;

                Mobile master = ControlMaster;
                SetControlMaster(null);
                Combatant = null;
                Warmode = false;
                Frozen = true;

                // Keep visible at 1 HP so death stays cancelled while FX plays.
                if (Hits < 1)
                    Hits = 1;

                TimeSpan delay = TimeSpan.Zero;
                if (map != null && map != Map.Internal)
                {
                    DudeSummonEffects.PlayDespawn(fxType, loc, map, defId);
                    delay = DudeSummonEffects.GetDespawnDuration(fxType, defId);
                }

                DudeBall ball = m_BoundBall;
                string dudeName = Name;

                Timer.DelayCall(delay, () =>
                {
                    FinishFaintPark(ball, master, dudeName);
                });

                m_SyncingDeath = false;
                return false;
            }

            return base.OnBeforeDeath();
        }

        /// <summary>
        /// Clear poison before parking (recall / faint) so Map.Internal Dudes stay cured.
        /// </summary>
        public void ClearPoisonForPark()
        {
            CurePoison(this);
            Poison = null;
        }

        private void FinishFaintPark(DudeBall ball, Mobile master, string dudeName)
        {
            m_Fainting = false;
            Frozen = false;

            if (Deleted)
                return;

            ClearPoisonForPark();

            m_SyncingDeath = true;
            Internalize();
            m_SyncingDeath = false;

            if (ball != null && !ball.Deleted)
                ball.InvalidateProperties();

            if (master != null && !master.Deleted)
                master.SendMessage(0x22, "{0} fainted and returned to the Dude Ball!", dudeName);
        }

        public override void OnDelete()
        {
            if (!m_SyncingDeath && m_BoundBall != null && !m_BoundBall.Deleted)
            {
                if (m_BoundBall.SummonedDude == this)
                    m_BoundBall.ClearSummonLink();
            }

            base.OnDelete();
        }
    }
}
