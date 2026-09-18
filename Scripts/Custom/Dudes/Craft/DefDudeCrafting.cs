using Server;
using System;
using Server.Items;

namespace Server.Engines.Craft
{
    /// <summary>
    /// Placeholder CraftSystem for Dude Ball crafting.
    /// Recipe is intentionally simple and easy to retune.
    /// Loop: catch → Mixer → DudeDust → craft empty Dude Ball.
    /// </summary>
    public class DefDudeCrafting : CraftSystem
    {
        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefDudeCrafting();

                return m_CraftSystem;
            }
        }

        public override SkillName MainSkill
        {
            get { return SkillName.Tinkering; }
        }

        public override string GumpTitleString
        {
            get { return "DUDE CRAFTING"; }
        }

        private DefDudeCrafting()
            : base(1, 1, 1.25)
        {
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 1.0;
        }

        public override int CanCraft(Mobile from, ITool tool, Type itemType)
        {
            if (tool == null || tool.Deleted || tool.UsesRemaining <= 0)
                return 1044038; // You have worn out your tool!

            int num = 0;
            if (!tool.CheckAccessible(from, ref num))
                return num;

            return 0;
        }

        public override void PlayCraftEffect(Mobile from)
        {
            from.PlaySound(0x241);
        }

        public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038);

            if (failed)
            {
                if (lostMaterial)
                    return 1044043; // You failed to create the item, and some of your materials are lost.
                return 1044157; // You failed to create the item, but no materials were lost.
            }

            return 1044154; // You create the item.
        }

        public override void InitCraftList()
        {
            // Placeholder recipe — change amounts freely.
            // Resources: Iron Ingots + Dude Dust → Empty Dude Ball
            int index = AddCraft(
                typeof(DudeBall),
                "Dude Balls",
                "Dude Ball",
                0.0,
                20.0,
                typeof(IronIngot),
                "Iron Ingot",
                5,
                "You need more iron ingots.");

            AddRes(index, typeof(DudeDust), "Dude Dust", 1, "You need Dude Dust (recycle a Dude in the Dude Mixer).");
            SetForceSuccess(index, 100);
            ForceNonExceptional(index);

            // Utility: Job Station (Carpentry 50+, logs); Mixer (Tinkering 50+, 20 iron ingots).
            index = AddCraft(
                typeof(DudeJobStation),
                "Utility",
                "Dude Job Station",
                SkillName.Carpentry,
                50.0,
                75.0,
                typeof(Log),
                "Log",
                50,
                "You need more logs.");
            ForceNonExceptional(index);

            index = AddCraft(
                typeof(DudeMixer),
                "Utility",
                "Dude Mixer",
                SkillName.Tinkering,
                50.0,
                75.0,
                typeof(IronIngot),
                "Iron Ingot",
                20,
                "You need more iron ingots.");
            ForceNonExceptional(index);

            // Healing potions — Weak / Normal / Strong (Strong crafts GreaterDudeHealingPotion for world type stability).
            index = AddCraft(
                typeof(WeakDudeHealingPotion),
                "Potions",
                "Weak Dude Healing Potion",
                SkillName.Alchemy,
                30.0,
                55.0,
                typeof(Bottle),
                "Empty Bottle",
                1,
                "You need an empty bottle.");
            AddRes(index, typeof(Ginseng), "Ginseng", 3, "You need more ginseng.");
            ForceNonExceptional(index);

            index = AddCraft(
                typeof(DudeHealingPotion),
                "Potions",
                "Dude Healing Potion",
                SkillName.Alchemy,
                50.0,
                75.0,
                typeof(Bottle),
                "Empty Bottle",
                1,
                "You need an empty bottle.");
            AddRes(index, typeof(Ginseng), "Ginseng", 5, "You need more ginseng.");
            AddRes(index, typeof(DudeDust), "Dude Dust", 2, "You need more Dude Dust.");
            ForceNonExceptional(index);

            index = AddCraft(
                typeof(GreaterDudeHealingPotion),
                "Potions",
                "Strong Dude Healing Potion",
                SkillName.Alchemy,
                65.0,
                90.0,
                typeof(Bottle),
                "Empty Bottle",
                1,
                "You need an empty bottle.");
            AddRes(index, typeof(Ginseng), "Ginseng", 8, "You need more ginseng.");
            AddRes(index, typeof(DudeDust), "Dude Dust", 5, "You need more Dude Dust.");
            ForceNonExceptional(index);

            // Revival potions — Weak / Normal / Strong by MaxReviveStage.
            index = AddCraft(
                typeof(WeakDudeRevivalPotion),
                "Potions",
                "Weak Dude Revival Potion",
                SkillName.Alchemy,
                30.0,
                55.0,
                typeof(Bottle),
                "Empty Bottle",
                1,
                "You need an empty bottle.");
            AddRes(index, typeof(Ginseng), "Ginseng", 3, "You need more ginseng.");
            ForceNonExceptional(index);

            index = AddCraft(
                typeof(DudeRevivalPotion),
                "Potions",
                "Dude Revival Potion",
                SkillName.Alchemy,
                50.0,
                75.0,
                typeof(Bottle),
                "Empty Bottle",
                1,
                "You need an empty bottle.");
            AddRes(index, typeof(Ginseng), "Ginseng", 5, "You need more ginseng.");
            AddRes(index, typeof(DudeDust), "Dude Dust", 2, "You need more Dude Dust.");
            ForceNonExceptional(index);

            index = AddCraft(
                typeof(StrongDudeRevivalPotion),
                "Potions",
                "Strong Dude Revival Potion",
                SkillName.Alchemy,
                65.0,
                90.0,
                typeof(Bottle),
                "Empty Bottle",
                1,
                "You need an empty bottle.");
            AddRes(index, typeof(Ginseng), "Ginseng", 8, "You need more ginseng.");
            AddRes(index, typeof(DudeDust), "Dude Dust", 5, "You need more Dude Dust.");
            ForceNonExceptional(index);
        }
    }
}
