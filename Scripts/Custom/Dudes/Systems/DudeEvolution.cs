using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Ascension: a Dude grows within its own type. No core cost, no cross-type, no sacrifice.
    /// Level-gated (L10 → stage 2, L20 → stage 3), and the Dude must be unsummoned and
    /// inside its Dude Ball. Stage raises follower slots, gear slots and skill caps.
    /// </summary>
    public static class DudeEvolution
    {
        /// <summary>Ascension is free. Kept for gump text / legacy callers.</summary>
        public static int GetCoreCost(int currentStage)
        {
            return 0;
        }

        public static bool CanAscend(DudeData data)
        {
            if (data == null)
                return false;

            int stage = data.EvolutionStage;
            if (stage >= DudeStage.MaxStage)
                return false;

            int required = DudeStage.AscendLevel(stage);
            return required > 0 && data.Level >= required;
        }

        /// <summary>Back-compat alias.</summary>
        public static bool CanEvolve(DudeData data)
        {
            return CanAscend(data);
        }

        // NOTE: elemental essences (Ember/Tide/Stone/GaleCore) are reserved for a future
        // progression system and are NOT involved in ascension. See DudeEssence.cs.

        /// <summary>Ascension happens from the ball, in the owner's pack, while unsummoned.</summary>
        public static bool CanPlayerAscend(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted || !ball.HasDude)
                return false;

            if (!ball.IsChildOf(from.Backpack))
                return false;

            if (ball.IsSummoned)
                return false;

            return CanAscend(ball.StoredDude);
        }

        /// <summary>Back-compat alias.</summary>
        public static bool CanPlayerEvolveBall(Mobile from, DudeBall ball)
        {
            return CanPlayerAscend(from, ball);
        }

        public static bool TryAscend(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted || !ball.HasDude)
                return false;

            if (!ball.IsChildOf(from.Backpack))
            {
                from.SendMessage("The Dude Ball must be in your backpack to ascend.");
                return false;
            }

            if (ball.IsSummoned)
            {
                from.SendMessage("Recall the Dude before it can ascend.");
                return false;
            }

            DudeData data = ball.StoredDude;
            if (data == null)
                return false;

            int stage = data.EvolutionStage;
            if (stage >= DudeStage.MaxStage)
            {
                from.SendMessage("{0} is already at the final ascension.", data.DisplayName);
                return false;
            }

            int required = DudeStage.AscendLevel(stage);
            if (required <= 0 || data.Level < required)
            {
                from.SendMessage("{0} must reach level {1} before ascending.", data.DisplayName, required);
                return false;
            }

            string oldName = data.DisplayName;

            // Ascension never changes type or name: it raises the ceiling.
            data.EvolutionStage = stage + 1;

            // Stage change raises the skill cap; re-clamp and derive on the new stage.
            data.Wrestling = data.Wrestling;
            data.Tactics = data.Tactics;
            data.Anatomy = data.Anatomy;
            data.MagicResist = data.MagicResist;
            data.RecomputeStats();

            data.EXPToNext = DudeExperience.GetExpRequiredForLevel(data.Level);
            data.Hits = data.HitsMax;
            data.IsFainted = false;

            ball.RefreshHue();
            ball.InvalidateProperties();

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted)
            {
                live.ApplyData(data, false);
                DudeCombatSkills.RaiseCapsOnMobile(live, data);
            }

            if (DudeLinkSystem.IsLinked(from))
            {
                DudeBall linked = DudeLinkSystem.GetLinkedBall(from);
                if (linked == ball)
                    DudeCombatSkills.RaiseCapsOnMobile(from, data);
            }

            if (from.Map != null && from.Map != Map.Internal)
                DudeSummonEffects.PlayForStage(data.Type, from.Location, from.Map, data.EvolutionStage);

            from.SendMessage(0x44, "{0} ascended to stage {1}!", oldName, data.EvolutionStage);
            from.PlaySound(0x208);

            return true;
        }

        }
}