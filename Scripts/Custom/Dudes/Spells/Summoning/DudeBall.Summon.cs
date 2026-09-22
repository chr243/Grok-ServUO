using System;
using Server.Custom.Dudes;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Dude summoning, the ball's equivalent of a summon spell: follower-slot and state checks,
    /// create or reuse the parked instance, take control, play the summon FX (DudeSummonEffects),
    /// and the recall path that parks the Dude back on Map.Internal after the despawn FX.
    /// </summary>
    public partial class DudeBall
    {
        private bool m_Recalling;

        public bool IsRecalling { get { return m_Recalling; } }

        private static readonly TimeSpan UseCooldown = TimeSpan.FromSeconds(5.0);

        private void MarkUsed()
        {
            m_NextUseUtc = DateTime.UtcNow + UseCooldown;
        }

        public void Summon(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (m_StoredDude == null)
            {
                from.SendMessage("The Dude Ball is empty.");
                return;
            }

            if (DudeRegistry.Get(m_StoredDude.DefinitionId) == null)
            {
                m_StoredDude = null;
                RefreshHue();
                InvalidateProperties();
                from.SendMessage("That Dude species no longer exists. The ball is empty.");
                return;
            }

            if (IsSummoned)
            {
                from.SendMessage("{0} is already summoned.", m_StoredDude.DisplayName);
                return;
            }

            if (m_StoredDude.IsFainted)
            {
                from.SendMessage("{0} is fainted. Use a Dude Revival Potion first.", m_StoredDude.DisplayName);
                return;
            }

            if (m_Recalling)
            {
                from.SendMessage("Your Dude is still returning to the ball.");
                return;
            }

            if (DudeLinkSystem.IsLinked(from))
            {
                DudeBall linked = DudeLinkSystem.GetLinkedBall(from);
                if (linked == this)
                {
                    from.SendMessage("You cannot summon the Dude you are linked with.");
                    return;
                }
            }

            int slots = DudeRegistry.GetControlSlots(m_StoredDude);

            if (DudeLinkSystem.IsLinked(from) && slots > 2)
            {
                from.SendMessage("While linked, you cannot summon a Dude that requires more than 2 follower slots.");
                return;
            }

            if (from.Followers + slots > from.FollowersMax)
            {
                from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                return;
            }

            Point3D loc = from.Location;
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            // Reuse parked instance so the client status bar keeps the same serial.
            DudeCreature dude = m_SummonedDude;
            bool created = false;

            if (dude == null || dude.Deleted)
            {
                dude = new DudeCreature(m_StoredDude.DefinitionId, false);
                created = true;
            }

            dude.BoundBall = this;
            dude.ApplyData(m_StoredDude, false);
            m_StoredDude.Hits = dude.Hits;

            dude.MoveToWorld(loc, map);

            if (!dude.SetControlMaster(from))
            {
                from.SendMessage("You cannot control this Dude right now.");
                if (created)
                {
                    dude.BoundBall = null;
                    dude.Delete();
                    m_SummonedDude = null;
                }
                else
                {
                    ParkDude(dude);
                }
                return;
            }

            // Match owner standing for karma-based systems; staff pets use ForceNotoriety above.
            dude.Karma = from.Karma;
            dude.Fame = from.Fame;

            dude.ControlTarget = from;
            dude.ControlOrder = OrderType.Guard;
            m_SummonedDude = dude;
            InvalidateProperties();

            from.SendMessage(0x59, "{0} emerges from the Dude Ball!", m_StoredDude.DisplayName);
            DudeSummonEffects.PlayForStage(m_StoredDude.Type, dude.Location, dude.Map, m_StoredDude.EvolutionStage);
            MarkUsed();
        }

        public void Recall(Mobile from)
        {
            RecallInternal(from, true);
        }

        private void RecallInternal(Mobile from, bool notify)
        {
            DudeCreature dude = SummonedDude;
            if (dude == null)
            {
                if (notify && from != null)
                    from.SendMessage("No Dude is currently summoned from this ball.");
                return;
            }

            if (m_Recalling)
            {
                if (notify && from != null)
                    from.SendMessage("Your Dude is already returning to the ball.");
                return;
            }

            dude.SyncToBall();

            if (m_StoredDude != null)
                m_StoredDude.IsFainted = false;

            Point3D loc = dude.Location;
            Map map = dude.Map;
            DudeType fxType = m_StoredDude != null ? m_StoredDude.Type : DudeType.Fire;
            int fxStage = m_StoredDude != null ? m_StoredDude.EvolutionStage : 1;

            // Keep ControlMaster through the FX so the Dude never goes "wild" / guard-candidate.
            // Pacify + bless during the animation; park only after it finishes.
            m_Recalling = true;
            dude.BeginDespawnSequence();

            TimeSpan delay = TimeSpan.Zero;
            if (map != null && map != Map.Internal)
            {
                DudeSummonEffects.PlayDespawnForStage(fxType, loc, map, fxStage);
                delay = DudeSummonEffects.GetDespawnDurationForStage(fxStage);
            }

            MarkUsed();

            Mobile notifyMobile = from;
            bool doNotify = notify;
            DudeCreature parkTarget = dude;

            Timer.DelayCall(delay, () =>
            {
                FinishRecallPark(parkTarget, notifyMobile, doNotify);
            });
        }

        private void FinishRecallPark(DudeCreature dude, Mobile from, bool notify)
        {
            m_Recalling = false;

            // Deleted while the recall FX played (e.g. the ball was emptied via ClearDude): nothing to
            // park or re-link, and nothing "returns to the ball".
            if (dude == null || dude.Deleted)
                return;

            dude.EndDespawnSequence();
            dude.SetControlMaster(null);
            ParkDude(dude);
            m_SummonedDude = dude;

            InvalidateProperties();

            if (notify && from != null && !from.Deleted)
            {
                from.SendMessage(0x59, "{0} returns to the Dude Ball.", m_StoredDude != null ? m_StoredDude.DisplayName : "Your Dude");
                from.PlaySound(0x1F1);
            }
        }

        private static void ParkDude(DudeCreature dude)
        {
            if (dude == null || dude.Deleted)
                return;

            dude.ClearPoisonForPark();
            dude.Combatant = null;
            dude.Warmode = false;
            dude.Internalize();
        }
    }
}
