using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Modular capture-chance provider. v1 is always 100% for valid wild Dudes.
    /// Swap or extend Calculate for rarity / HP / ball-type formulas later.
    /// Catch plays a ball-throw MovingEffect, then fanfare on success.
    /// </summary>
    public static class DudeCapture
    {
        private static readonly TimeSpan ThrowDelay = TimeSpan.FromSeconds(1.0);

        // Classic-friendly SFX: throw whoosh (snowball), catch flourish (Arcane Empowerment).
        private const int ThrowSound = 0x145;
        private const int CatchFanfareSound = 0x5C1;
        private const int BallItemId = 0xF0F; // round gem art
        private const int CatchParticles = 0x373A;

        /// <summary>
        /// Why this target cannot be caught, or null if capture may proceed.
        /// Blocks DudeBoss (and any DudeCreature.CanBeCaught == false).
        /// </summary>
        public static string GetCaptureBlockReason(Mobile thrower, object targeted, DudeBall ball)
        {
            if (thrower == null || ball == null)
                return "You cannot catch that.";

            if (ball.HasDude)
                return "That Dude Ball is already occupied.";

            DudeBoss boss = targeted as DudeBoss;
            if (boss != null)
                return string.Format("{0} cannot be caught in a Dude Ball!", boss.Name);

            DudeCreature dude = targeted as DudeCreature;
            if (dude == null || dude.Deleted)
                return "That is not a wild Dude.";

            if (!dude.CanBeCaught)
                return string.Format("{0} cannot be caught.", dude.Name);

            return null;
        }

        public static double Calculate(Mobile thrower, DudeCreature wild, DudeBall ball)
        {
            if (thrower == null || wild == null || ball == null)
                return 0.0;

            if (wild.Deleted || !wild.CanBeCaught)
                return 0.0;

            if (ball.HasDude)
                return 0.0;

            // v1: guaranteed capture
            return 1.0;
        }

        /// <summary>
        /// Starts the throw animation, then finishes capture after a short delay.
        /// </summary>
        public static bool BeginCapture(Mobile thrower, DudeCreature wild, DudeBall ball)
        {
            string blocked = GetCaptureBlockReason(thrower, wild, ball);
            if (blocked != null)
            {
                if (thrower != null)
                    thrower.SendMessage(blocked);
                return false;
            }

            if (thrower == null || wild == null || ball == null)
                return false;

            if (!thrower.BeginAction(typeof(DudeCapture)))
            {
                thrower.SendMessage("You are already throwing a Dude Ball.");
                return false;
            }

            thrower.Direction = thrower.GetDirectionTo(wild);
            thrower.PlaySound(ThrowSound);
            thrower.Animate(9, 1, 1, true, false, 0);

            Effects.SendMovingEffect(thrower, wild, BallItemId, 7, 0, false, false, ball.Hue, 0);

            Timer.DelayCall(ThrowDelay, new TimerStateCallback(FinishCapture), new object[] { thrower, wild, ball });
            return true;
        }

        private static void FinishCapture(object state)
        {
            object[] states = state as object[];
            if (states == null || states.Length < 3)
                return;

            Mobile thrower = states[0] as Mobile;
            DudeCreature wild = states[1] as DudeCreature;
            DudeBall ball = states[2] as DudeBall;

            if (thrower != null)
                thrower.EndAction(typeof(DudeCapture));

            if (thrower == null || thrower.Deleted || !thrower.Alive)
                return;

            if (ball == null || ball.Deleted)
                return;

            if (!ball.IsChildOf(thrower.Backpack) && ball.RootParent != thrower)
            {
                thrower.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (wild == null || wild.Deleted)
            {
                thrower.SendMessage("The Dude got away before the ball landed.");
                return;
            }

            if (!thrower.InRange(wild, 12) || !thrower.CanSee(wild))
            {
                thrower.SendMessage("You lost sight of the Dude.");
                return;
            }

            string blocked = GetCaptureBlockReason(thrower, wild, ball);
            if (blocked != null)
            {
                thrower.SendMessage(blocked);
                return;
            }

            TryCapture(thrower, wild, ball);
        }

        public static bool TryCapture(Mobile thrower, DudeCreature wild, DudeBall ball)
        {
            double chance = Calculate(thrower, wild, ball);

            if (chance <= 0.0)
                return false;

            if (Utility.RandomDouble() > chance)
                return false;

            DudeDefinition def = DudeRegistry.Get(wild.DefinitionId);
            if (def == null)
                return false;

            DudeData data = DudeData.FromDefinition(def, thrower);

            // Preserve live HP ratio into storage (UOR-simple).
            if (wild.HitsMax > 0)
            {
                data.Hits = Math.Max(1, (int)(data.HitsMax * ((double)wild.Hits / wild.HitsMax)));
            }

            Point3D loc = wild.Location;
            Map map = wild.Map;

            ball.StoreDude(data);
            wild.Delete();

            PlayCatchFanfare(thrower, loc, map);

            thrower.SendMessage(0x59, "You caught {0}!", data.DisplayName);

            return true;
        }

        private static void PlayCatchFanfare(Mobile thrower, Point3D loc, Map map)
        {
            if (thrower != null && !thrower.Deleted)
                thrower.PlaySound(CatchFanfareSound);

            if (map == null || map == Map.Internal)
                return;

            Effects.PlaySound(loc, map, CatchFanfareSound);
            Effects.SendLocationParticles(
                EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                CatchParticles, 10, 30, 5052);
            Effects.SendLocationParticles(
                EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                0x376A, 9, 32, 5008);
        }
    }
}
