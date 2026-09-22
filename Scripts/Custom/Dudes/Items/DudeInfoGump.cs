using System;
using System.Collections.Generic;
using System.Text;
using Server.Custom.Dudes;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// Classic UO info sheet for a Dude (ball data, live creature, or boss scout).
    /// </summary>
    public class DudeInfoGump : Gump
    {
        private readonly Serial m_BallSerial;
        private readonly Serial m_CreatureSerial;
        private readonly Serial m_BossSerial;
        private readonly bool m_ShowEvolve;

        public DudeInfoGump(DudeInfoView view)
            : base(50, 50)
        {
            if (view == null)
                view = DudeInfoView.Empty;

            m_BallSerial = view.BallSerial;
            m_CreatureSerial = view.CreatureSerial;
            m_BossSerial = view.BossSerial;
            m_ShowEvolve = view.ShowEvolve;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 720, 520, 9270);
            AddAlphaRegion(10, 10, 700, 500);

            AddHtml(20, 18, 680, 22, "<CENTER><BASEFONT COLOR=#FFFFFF>Trainer's Manual</BASEFONT></CENTER>", false, false);

            int y = 48;
            int labelHue = 0x480;
            int valueHue = 0x34;

            // LEFT column
            AddLabel(24, y, labelHue, "Name:");
            AddLabel(110, y, valueHue, Truncate(view.Name, 28));
            y += 22;

            AddLabel(24, y, labelHue, "Type:");
            AddLabel(110, y, valueHue, view.TypeText);
            y += 22;

            AddLabel(24, y, labelHue, "Status:");
            AddLabel(110, y, valueHue, view.Status);
            y += 22;

            AddLabel(24, y, labelHue, "Level:");
            AddLabel(110, y, valueHue, view.LevelText);
            y += 22;

            if (!string.IsNullOrEmpty(view.EvolutionText))
            {
                AddLabel(24, y, labelHue, "Form:");
                AddLabel(110, y, valueHue, Truncate(view.EvolutionText, 28));
                y += 22;
            }

            AddLabel(24, y, labelHue, "EXP:");
            AddLabel(110, y, valueHue, view.ExpText);
            y += 22;

            AddLabel(24, y, labelHue, "Job skill:");
            AddLabel(110, y, valueHue, view.JobSkillText);
            y += 26;

            AddHtml(24, y, 330, 18, "<BASEFONT COLOR=#FFFFFF>Stats</BASEFONT>", false, false);
            y += 20;

            AddLabel(24, y, labelHue, string.Format("Str: {0}", view.Str));
            AddLabel(130, y, labelHue, string.Format("Dex: {0}", view.Dex));
            AddLabel(236, y, labelHue, string.Format("Int: {0}", view.Int));
            y += 20;

            AddLabel(24, y, labelHue, string.Format("Hits: {0} / {1}", view.Hits, view.HitsMax));
            y += 20;

            AddLabel(24, y, labelHue, string.Format("Damage: {0} - {1}", view.MinDamage, view.MaxDamage));
            y += 20;

            AddLabel(24, y, labelHue, string.Format("Armor: {0}", view.VirtualArmor));
            y += 26;

            AddHtml(24, y, 330, 18, "<BASEFONT COLOR=#FFFFFF>Skills</BASEFONT>", false, false);
            y += 20;

            AddLabel(24, y, labelHue, string.Format("Wrestling: {0:0.0} / {1:0.0}", view.SkillWrestling, DudeCombatSkills.GetCap(view.EvolutionStage)));
            y += 18;
            AddLabel(24, y, labelHue, string.Format("Tactics: {0:0.0} / {1:0.0}", view.SkillTactics, DudeCombatSkills.GetCap(view.EvolutionStage)));
            y += 18;
            AddLabel(24, y, labelHue, string.Format("Anatomy: {0:0.0} / {1:0.0}", view.SkillAnatomy, DudeCombatSkills.GetCap(view.EvolutionStage)));
            y += 18;
            AddLabel(24, y, labelHue, string.Format("Magic Resist: {0:0.0} / {1:0.0}", view.SkillMagicResist, DudeCombatSkills.GetCap(view.EvolutionStage)));
            y += 18;

            double stageCap = DudeCombatSkills.GetCap(view.EvolutionStage);
            if (view.HasMagicalHat || view.SkillMagery > 0.0 || view.SkillEvalInt > 0.0 || view.SkillMeditation > 0.0)
            {
                AddLabel(24, y, labelHue, string.Format("Magery: {0:0.0} / {1:0.0}", view.SkillMagery, stageCap));
                y += 18;
                AddLabel(24, y, labelHue, string.Format("Eval Int: {0:0.0} / {1:0.0}", view.SkillEvalInt, stageCap));
                y += 18;
                AddLabel(24, y, labelHue, string.Format("Meditation: {0:0.0} / {1:0.0}", view.SkillMeditation, stageCap));
                y += 18;
            }

            if (view.HasDudeShield || view.SkillParry > 0.0)
            {
                AddLabel(24, y, labelHue, string.Format("Parrying: {0:0.0} / {1:0.0}", view.SkillParry, stageCap));
                y += 18;
            }

            // RIGHT column — gear slots by evolution stage
            int ry = 48;
            AddHtml(370, ry, 320, 18, "<BASEFONT COLOR=#FFFFFF>Gear</BASEFONT>", false, false);
            ry += 22;

            int unlockedSlots = DudeExperience.GetGearSlots(view.EvolutionStage);
            if (unlockedSlots < 0)
                unlockedSlots = 0;
            if (unlockedSlots > 4)
                unlockedSlots = 4;

            // One row per unique worn DudeGear (names/descs already de-duped by FillEquippedGear).
            int uniqueCount = view.EquippedGearNames != null ? view.EquippedGearNames.Count : 0;
            for (int i = 0; i < uniqueCount; i++)
            {
                string gearName = view.EquippedGearNames[i];
                if (string.IsNullOrEmpty(gearName))
                    gearName = "Dude gear";
                AddHtml(370, ry, 320, 18, string.Format("<BASEFONT COLOR=#66FF66>{0}</BASEFONT>", Truncate(gearName, 42)), false, false);
                ry += 18;

                // That item's desc only — not a second copy of the whole gear list.
                string abilityLine = null;
                if (view.EquippedGearAbilityLines != null && i < view.EquippedGearAbilityLines.Count)
                    abilityLine = view.EquippedGearAbilityLines[i];
                if (!string.IsNullOrEmpty(abilityLine))
                {
                    // Wrap so scaled damage/heal numbers are never truncated.
                    AddHtml(370, ry, 320, 36, string.Format("<BASEFONT COLOR=#CCCCCC>{0}</BASEFONT>", abilityLine), false, false);
                    ry += 36;
                }
            }

            // Empty = unlocked slots minus unique worn gear (never use a doubled list count).
            int emptySlots = unlockedSlots - uniqueCount;
            if (emptySlots < 0)
                emptySlots = 0;

            for (int e = 0; e < emptySlots; e++)
            {
                int slot = uniqueCount + e + 1;
                AddHtml(370, ry, 320, 18, string.Format("<BASEFONT COLOR=#66FF66>Slot {0}: Empty</BASEFONT>", slot), false, false);
                ry += 20;
            }

            for (int slot = uniqueCount + emptySlots + 1; slot <= 4; slot++)
            {
                int unlockStage = slot <= 2 ? 1 : (slot == 3 ? 2 : 3);
                AddHtml(370, ry, 320, 18, string.Format("<BASEFONT COLOR=#808080>Slot {0} — unlocks at stage {1}</BASEFONT>",
                    slot, unlockStage), false, false);
                ry += 20;
            }

            AddButton(24, 478, 4011, 4013, 3, GumpButtonType.Reply, 0);
            AddLabel(59, 480, 0x480, "Refresh");

            if (view.ShowEvolve)
            {
                AddButton(560, 478, 4005, 4006, 2, GumpButtonType.Reply, 0);
                AddLabel(595, 480, 0x35, "Ascend!");
                if (!string.IsNullOrEmpty(view.EvolveHint))
                    AddLabel(560, 460, 0x480, Truncate(view.EvolveHint, 28));
            }

            AddButton(680, 480, 4017, 4019, 0, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info == null || sender == null || sender.Mobile == null)
                return;

            if (info.ButtonID == 3)
            {
                Mobile from = sender.Mobile;
                DudeInfoView view = RebuildView();

                if (view == null || view == DudeInfoView.Empty)
                {
                    from.SendMessage("That Dude is no longer available.");
                    return;
                }

                from.CloseGump(typeof(DudeInfoGump));
                from.SendGump(new DudeInfoGump(view));
                return;
            }

            if (info.ButtonID == 2)
            {
                Mobile from = sender.Mobile;
                DudeBall ball = World.FindItem(m_BallSerial) as DudeBall;

                if (!DudeEvolution.CanPlayerAscend(from, ball))
                {
                    from.SendMessage("That Dude cannot ascend right now (level-gated, unsummoned, ball in pack).");
                    return;
                }

                DudeEvolution.TryAscend(from, ball);
            }
        }

        /// <summary>
        /// Rebuild snapshot from live ball / creature / boss. No OnThink — manual Refresh only.
        /// </summary>
        private DudeInfoView RebuildView()
        {
            DudeBall ball = World.FindItem(m_BallSerial) as DudeBall;
            if (ball != null && !ball.Deleted && ball.HasDude && ball.StoredDude != null)
            {
                if (ball.IsSummoned && ball.SummonedDude != null && !ball.SummonedDude.Deleted)
                {
                    ball.SummonedDude.Refresh();
                    return DudeInfoView.FromDudeCreature(ball.SummonedDude);
                }

                return DudeInfoView.FromDudeBall(ball);
            }

            DudeCreature dude = World.FindMobile(m_CreatureSerial) as DudeCreature;
            if (dude != null && !dude.Deleted)
            {
                dude.Refresh();
                return DudeInfoView.FromDudeCreature(dude);
            }

            DudeBoss boss = World.FindMobile(m_BossSerial) as DudeBoss;
            if (boss != null && !boss.Deleted)
                return DudeInfoView.FromDudeBoss(boss);

            return null;
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
                return "-";

            if (text.Length <= max)
                return text;

            return text.Substring(0, max - 1) + "...";
        }
    }

    /// <summary>
    /// Plain snapshot so the gump does not hold live world references.
    /// </summary>
    public sealed class DudeInfoView
    {
        public static readonly DudeInfoView Empty = new DudeInfoView
        {
            Name = "Unknown",
            TypeText = "N/A",
            Status = "N/A",
            LevelText = "N/A",
            Level = 1,
            ExpText = "N/A",
            JobSkillText = "N/A",
            EvolutionText = null,
            AbilityName = "None",
            AbilityDescription = null,
            SkillWrestling = 0.0,
            SkillTactics = 0.0,
            SkillAnatomy = 0.0,
            SkillMagicResist = 0.0,
            SkillMagery = 0.0,
            SkillEvalInt = 0.0,
            SkillMeditation = 0.0,
            SkillParry = 0.0,
            HasMagicalHat = false,
            HasDudeShield = false,
            EquippedGearNames = null,
            EquippedGearAbilityLines = null,
            EvolutionStage = 0,
            KitType = DudeType.Fire,
            UnlockedAbilityIds = null,
            BallSerial = Serial.MinusOne,
            CreatureSerial = Serial.MinusOne,
            BossSerial = Serial.MinusOne,
            ShowEvolve = false,
            EvolveCost = 0,
            EvolveHint = null
        };

        public string Name { get; set; }
        public string TypeText { get; set; }
        public string Status { get; set; }
        public string LevelText { get; set; }
        public int Level { get; set; }
        public string ExpText { get; set; }
        public string JobSkillText { get; set; }
        public string EvolutionText { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Int { get; set; }
        public int Hits { get; set; }
        public int HitsMax { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int VirtualArmor { get; set; }
        public string AbilityName { get; set; }
        public string AbilityDescription { get; set; }

        public double SkillWrestling { get; set; }
        public double SkillTactics { get; set; }
        public double SkillAnatomy { get; set; }
        public double SkillMagicResist { get; set; }
        public double SkillMagery { get; set; }
        public double SkillEvalInt { get; set; }
        public double SkillMeditation { get; set; }
        public double SkillParry { get; set; }
        public bool HasMagicalHat { get; set; }
        public bool HasDudeShield { get; set; }
        public List<string> EquippedGearNames { get; set; }
        /// <summary>Per-gear ability/skill line (same index as EquippedGearNames); hat/shield show effective skills.</summary>
        public List<string> EquippedGearAbilityLines { get; set; }
        public int EvolutionStage { get; set; }
        public DudeType KitType { get; set; }
        public List<string> UnlockedAbilityIds { get; set; }

        public Serial BallSerial { get; set; }
        public Serial CreatureSerial { get; set; }
        public Serial BossSerial { get; set; }
        public bool ShowEvolve { get; set; }
        public int EvolveCost { get; set; }
        public string EvolveHint { get; set; }

        private static void FillEvolve(DudeInfoView view, DudeBall ball)
        {
            if (view == null)
                return;

            if (ball == null || ball.Deleted || !ball.HasDude || ball.StoredDude == null)
            {
                view.ShowEvolve = false;
                view.BallSerial = Serial.MinusOne;
                view.EvolveCost = 0;
                view.EvolveHint = null;
                return;
            }

            // Keep ball serial for Refresh even when evolve is unavailable.
            view.BallSerial = ball.Serial;

            if (!DudeEvolution.CanAscend(ball.StoredDude))
            {
                view.ShowEvolve = false;
                view.EvolveCost = 0;
                view.EvolveHint = null;
                return;
            }

            view.ShowEvolve = true;
            view.EvolveCost = 0;
            view.EvolveHint = "Ready to ascend";
        }

        private static void FillSkillTexts(DudeInfoView view, DudeData data)
        {
            view.JobSkillText = Server.Custom.Dudes.Jobs.DudeJobHarvest.FormatSkillLabel(data);
        }

        private static void FillCombatSkillsFromData(DudeInfoView view, DudeData data)
        {
            if (data == null)
            {
                view.SkillWrestling = 0.0;
                view.SkillTactics = 0.0;
                view.SkillAnatomy = 0.0;
                view.SkillMagicResist = 0.0;
                view.EvolutionStage = 0;
                view.KitType = DudeType.Fire;
                view.UnlockedAbilityIds = null;
                return;
            }

            DudeCombatSkills.EnsureRolled(data);
            view.SkillWrestling = data.Wrestling;
            view.SkillTactics = data.Tactics;
            view.SkillAnatomy = data.Anatomy;
            view.SkillMagicResist = data.MagicResist;
            view.EvolutionStage = data.EvolutionStage;
            view.KitType = data.Type;
            view.UnlockedAbilityIds = data.GetUnlockedAbilityIds();
        }

        private static void TryOverlayLiveCombatSkills(DudeInfoView view, Mobile m)
        {
            if (view == null || m == null || m.Skills == null)
                return;

            Skill wrestling = m.Skills[SkillName.Wrestling];
            if (wrestling != null)
                view.SkillWrestling = wrestling.Base;

            Skill tactics = m.Skills[SkillName.Tactics];
            if (tactics != null)
                view.SkillTactics = tactics.Base;

            Skill anatomy = m.Skills[SkillName.Anatomy];
            if (anatomy != null)
                view.SkillAnatomy = anatomy.Base;

            Skill magicResist = m.Skills[SkillName.MagicResist];
            if (magicResist != null)
                view.SkillMagicResist = magicResist.Base;

            Skill magery = m.Skills[SkillName.Magery];
            if (magery != null)
                view.SkillMagery = magery.Base;

            Skill evalInt = m.Skills[SkillName.EvalInt];
            if (evalInt != null)
                view.SkillEvalInt = evalInt.Base;

            Skill meditation = m.Skills[SkillName.Meditation];
            if (meditation != null)
                view.SkillMeditation = meditation.Base;

            Skill parry = m.Skills[SkillName.Parry];
            if (parry != null)
                view.SkillParry = parry.Base;
        }

        /// <summary>
        /// Read worn DudeGear only from the live Dude (prefer live mobile over BoundBall copies).
        /// Worn = Parent == dude and FindItemOnLayer(Layer) == gear. Dedupe by Serial.
        /// Backpack / bank / loose Items are skipped (not shown as equipped).
        /// </summary>
        private static void FillEquippedGear(DudeInfoView view, DudeCreature dude)
        {
            if (view == null || dude == null || dude.Deleted)
                return;

            List<string> names = new List<string>();
            List<string> abilityLines = new List<string>();
            HashSet<Serial> seen = new HashSet<Serial>();
            StringBuilder abilityNames = new StringBuilder();
            StringBuilder abilityDescs = new StringBuilder();
            int dudeLevel = view.Level > 0 ? view.Level : 1;
            double stageCap = DudeCombatSkills.GetCap(view.EvolutionStage);

            // Iterate Items once; only truly worn DudeGear (layer winner), unique Serial.
            for (int i = 0; i < dude.Items.Count; i++)
            {
                DudeGear gear = dude.Items[i] as DudeGear;
                if (gear == null || gear.Deleted)
                    continue;
                if (gear.Parent != dude)
                    continue;
                if (dude.FindItemOnLayer(gear.Layer) != gear)
                    continue;
                // Appearance-only / zero-slot cosmetics (e.g. DudeCostume): no manual line, no slot.
                if (gear is DudeCostume)
                    continue;
                if (gear.SlotCost == 0 && string.IsNullOrEmpty(gear.AbilityId))
                    continue;
                if (!seen.Add(gear.Serial))
                    continue;
                string n = gear.Name;
                if (string.IsNullOrEmpty(n))
                    n = gear.GetType().Name;
                int lv = gear.GearLevel;
                int pct = lv * 10;
                // e.g. "Stone Sash  Lv 3  +30%" or "Tide Earrings  Lv 3  +30%  12s"
                string cdText = GetGearCooldownText(gear.AbilityId);
                names.Add(string.IsNullOrEmpty(cdText)
                    ? string.Format("{0}  Lv {1}  +{2}%", n, lv, pct)
                    : string.Format("{0}  Lv {1}  +{2}%  {3}", n, lv, pct, cdText));

                MagicalDudeHat hat = gear as MagicalDudeHat;
                if (hat != null)
                {
                    // Lerp stored → 120 by gear level, then stage cap (100/110/120).
                    double magery = Math.Min(hat.GetSkillLerp(hat.Magery, 120.0), stageCap);
                    double eval = Math.Min(hat.GetSkillLerp(hat.EvalInt, 120.0), stageCap);
                    double med = Math.Min(hat.GetSkillLerp(hat.Meditation, 120.0), stageCap);
                    string hatLine = string.Format("Magery {0:0.0} / Eval {1:0.0} / Med {2:0.0}", magery, eval, med);
                    abilityLines.Add(hatLine);

                    if (abilityNames.Length > 0)
                        abilityNames.Append("<BR>");
                    abilityNames.Append(n);

                    if (abilityDescs.Length > 0)
                        abilityDescs.Append("<BR><BR>");
                    abilityDescs.Append(hatLine);
                    continue;
                }

                DudeShield shield = gear as DudeShield;
                if (shield != null)
                {
                    // Lerp stored → 120 by gear level, then stage cap.
                    double parry = Math.Min(shield.GetSkillLerp(shield.Parrying, 120.0), stageCap);
                    string shieldLine = string.Format("Parrying {0:0.0}", parry);
                    abilityLines.Add(shieldLine);

                    if (abilityNames.Length > 0)
                        abilityNames.Append("<BR>");
                    abilityNames.Append(n);

                    if (abilityDescs.Length > 0)
                        abilityDescs.Append("<BR><BR>");
                    abilityDescs.Append(shieldLine);
                    continue;
                }

                // Other gear without ability id: name only.
                if (string.IsNullOrEmpty(gear.AbilityId))
                {
                    abilityLines.Add(null);
                    continue;
                }

                DudeAbility ability = DudeAbilityRegistry.Get(gear.AbilityId);
                string aName = ability != null ? ability.Name : gear.AbilityId;
                string desc = GetAbilityDescription(gear.AbilityId, dudeLevel, view.HitsMax, gear.GetEffectMultiplier());
                string line = !string.IsNullOrEmpty(desc)
                    ? string.Format("{0}: {1}", aName, desc)
                    : aName;
                abilityLines.Add(line);

                if (abilityNames.Length > 0)
                    abilityNames.Append("<BR>");
                abilityNames.Append(aName);

                if (!string.IsNullOrEmpty(desc))
                {
                    if (abilityDescs.Length > 0)
                        abilityDescs.Append("<BR><BR>");
                    abilityDescs.Append(line);
                }
            }

            view.EquippedGearNames = names;
            view.EquippedGearAbilityLines = abilityLines;
            view.HasMagicalHat = dude.FindItemOnLayer(Layer.Helm) is MagicalDudeHat;
            view.HasDudeShield = dude.FindItemOnLayer(Layer.TwoHanded) is DudeShield;

            if (abilityNames.Length > 0)
            {
                view.AbilityName = abilityNames.ToString();
                view.AbilityDescription = abilityDescs.Length > 0 ? abilityDescs.ToString() : null;
            }
            else
            {
                view.AbilityName = "None";
                view.AbilityDescription = null;
            }
        }

        private static void FillAbilityTexts(DudeInfoView view, DudeData data, string fallbackAbilityId)
        {
            FillAbilityTexts(view, data, fallbackAbilityId, null, 0);
        }

        private static void FillAbilityTexts(DudeInfoView view, DudeData data, string fallbackAbilityId, string definitionId, int evolutionStage)
        {
            List<string> ids = null;
            if (data != null)
                ids = data.GetUnlockedAbilityIds();

            if (ids == null || ids.Count == 0)
            {
                ids = new List<string>();
                if (!string.IsNullOrEmpty(fallbackAbilityId))
                    ids.Add(fallbackAbilityId);
            }

            // Surface Burn for Infernox / stage 3 even if not yet stored in UnlockedAbilities.
            bool showBurn = false;
            if (data != null)
            {
                showBurn = data.EvolutionStage >= 3;
            }
            if (!showBurn)
            {
                showBurn = evolutionStage >= 3;
            }

            if (showBurn)
            {
                bool hasBurn = false;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (string.Equals(ids[i], "burn", StringComparison.OrdinalIgnoreCase))
                    {
                        hasBurn = true;
                        break;
                    }
                }
                if (!hasBurn)
                    ids.Add("burn");
            }

            if (ids.Count == 0)
            {
                view.AbilityName = "None";
                view.AbilityDescription = null;
                return;
            }

            StringBuilder names = new StringBuilder();
            StringBuilder descs = new StringBuilder();

            for (int i = 0; i < ids.Count; i++)
            {
                DudeAbility ability = DudeAbilityRegistry.Get(ids[i]);
                string name = ability != null ? ability.Name : ids[i];
                if (names.Length > 0)
                    names.Append("<BR>");
                names.Append(name);

                int level = view.Level > 0 ? view.Level : 1;
                if (data != null && data.Level > 0)
                    level = data.Level;
                string desc = GetAbilityDescription(ids[i], level, view.HitsMax);
                if (!string.IsNullOrEmpty(desc))
                {
                    if (descs.Length > 0)
                        descs.Append("<BR><BR>");
                    descs.AppendFormat("<B>{0}</B>: {1}", name, desc);
                }
            }

            view.AbilityName = names.ToString();
            view.AbilityDescription = descs.Length > 0 ? descs.ToString() : null;
        }

        private static string BuildEvolutionText(DudeData data)
        {
            if (data == null || data.EvolutionStage <= 1)
                return null;

            DudeDefinition def = DudeRegistry.Get(data.DefinitionId);
            string form = def != null ? def.Name : data.DefinitionId;
            return string.Format("Stage {0} — {1}", data.EvolutionStage, form);
        }

        public static DudeInfoView FromDudeData(DudeData data, string statusOverride)
        {
            if (data == null)
                return Empty;

            DudeInfoView view = new DudeInfoView();
            view.Name = data.DisplayName;
            view.TypeText = data.Type.ToString();
            view.Status = !string.IsNullOrEmpty(statusOverride) ? statusOverride : BuildCapturedStatus(data);
            view.LevelText = string.Format("{0} / {1}", data.Level, DudeExperience.GetMaxLevel(data));
            view.Level = data.Level > 0 ? data.Level : 1;
            view.ExpText = string.Format("{0} / {1}", data.CurrentEXP, data.EXPToNext);
            view.EvolutionText = BuildEvolutionText(data);
            FillSkillTexts(view, data);
            view.Str = data.Str;
            view.Dex = data.Dex;
            view.Int = data.Int;
            view.Hits = data.Hits;
            view.HitsMax = data.HitsMax;
            view.MinDamage = data.MinDamage;
            view.MaxDamage = data.MaxDamage;
            view.VirtualArmor = data.VirtualArmor;
            FillCombatSkillsFromData(view, data);
            view.BallSerial = Serial.MinusOne;
            view.CreatureSerial = Serial.MinusOne;
            view.BossSerial = Serial.MinusOne;
            return view;
        }

        public static DudeInfoView FromDudeCreature(DudeCreature dude)
        {
            if (dude == null || dude.Deleted)
                return Empty;

            // Prefer authoritative ball data when summoned; overlay live HP/stats.
            if (dude.BoundBall != null && !dude.BoundBall.Deleted && dude.BoundBall.StoredDude != null)
            {
                DudeInfoView fromBall = FromDudeData(dude.BoundBall.StoredDude, "Summoned");
                fromBall.Hits = dude.Hits;
                fromBall.HitsMax = dude.HitsMax;
                fromBall.Str = dude.RawStr;
                fromBall.Dex = dude.RawDex;
                fromBall.Int = dude.RawInt;
                fromBall.MinDamage = dude.DamageMin;
                fromBall.MaxDamage = dude.DamageMax;
                fromBall.VirtualArmor = dude.VirtualArmor;
                fromBall.Name = dude.Name;
                fromBall.LevelText = string.Format("{0} / {1}", dude.DudeLevel, DudeExperience.GetMaxLevel(dude.BoundBall.StoredDude));
                fromBall.Level = dude.DudeLevel > 0 ? dude.DudeLevel : 1;
                // Keep KitType / EvolutionStage from data; refresh skill values from live mobile when present.
                TryOverlayLiveCombatSkills(fromBall, dude);
                FillEquippedGear(fromBall, dude);
                fromBall.CreatureSerial = dude.Serial;
                fromBall.BossSerial = Serial.MinusOne;
                FillEvolve(fromBall, dude.BoundBall);
                return fromBall;
            }

            DudeDefinition def = DudeRegistry.Get(dude.DefinitionId);

            DudeInfoView view = new DudeInfoView();
            view.Name = dude.Name;
            view.TypeText = def != null ? def.Type.ToString() : "Unknown";
            view.Status = dude.IsWild ? "Wild" : "Summoned";
            view.LevelText = dude.DudeLevel > 0 ? dude.DudeLevel.ToString() : "1";
            view.Level = dude.DudeLevel > 0 ? dude.DudeLevel : 1;
            view.ExpText = dude.IsWild ? "N/A (wild)" : "N/A";
            view.EvolutionText = dude.EvolutionStage > 1
                ? string.Format("Stage {0}", dude.EvolutionStage)
                : null;
            if (dude.BoundBall != null && dude.BoundBall.StoredDude != null)
                FillSkillTexts(view, dude.BoundBall.StoredDude);
            else
                view.JobSkillText = "N/A";
            view.Str = dude.RawStr;
            view.Dex = dude.RawDex;
            view.Int = dude.RawInt;
            view.Hits = dude.Hits;
            view.HitsMax = dude.HitsMax;
            view.MinDamage = dude.DamageMin;
            view.MaxDamage = dude.DamageMax;
            view.VirtualArmor = dude.VirtualArmor;

            view.KitType = def != null ? def.Type : DudeType.Fire;
            view.EvolutionStage = dude.EvolutionStage;

            if (dude.BoundBall != null && dude.BoundBall.StoredDude != null)
            {
                DudeData ballData = dude.BoundBall.StoredDude;
                DudeCombatSkills.EnsureRolled(ballData);
                view.SkillWrestling = ballData.Wrestling;
                view.SkillTactics = ballData.Tactics;
                view.SkillAnatomy = ballData.Anatomy;
                view.SkillMagicResist = ballData.MagicResist;
                view.UnlockedAbilityIds = ballData.GetUnlockedAbilityIds();
            }
            else
            {
                view.SkillWrestling = 0.0;
                view.SkillTactics = 0.0;
                view.SkillAnatomy = 0.0;
                view.SkillMagicResist = 0.0;
                view.UnlockedAbilityIds = null;
            }

            TryOverlayLiveCombatSkills(view, dude);
            FillEquippedGear(view, dude);
            view.CreatureSerial = dude.Serial;
            view.BossSerial = Serial.MinusOne;
            view.BallSerial = Serial.MinusOne;
            return view;
        }

        public static DudeInfoView FromDudeBoss(DudeBoss boss)
        {
            if (boss == null || boss.Deleted)
                return Empty;

            DudeInfoView view = new DudeInfoView();
            view.Name = boss.Name;
            view.TypeText = boss.DudeAffinity.ToString();
            view.Status = "Boss (uncatchable)";
            view.LevelText = "Boss";
            view.Level = 1;
            view.ExpText = "N/A";
            view.JobSkillText = "N/A";
            view.Str = boss.RawStr;
            view.Dex = boss.RawDex;
            view.Int = boss.RawInt;
            view.Hits = boss.Hits;
            view.HitsMax = boss.HitsMax;
            view.MinDamage = boss.DamageMin;
            view.MaxDamage = boss.DamageMax;
            view.VirtualArmor = boss.VirtualArmor;
            view.SkillWrestling = 0.0;
            view.SkillTactics = 0.0;
            view.SkillAnatomy = 0.0;
            view.SkillMagicResist = 0.0;
            view.KitType = boss.DudeAffinity;
            view.EvolutionStage = 3; // show kit unlocked
            view.UnlockedAbilityIds = null;
            view.ShowEvolve = false;
            view.BallSerial = Serial.MinusOne;
            view.CreatureSerial = Serial.MinusOne;
            view.BossSerial = boss.Serial;
            view.EvolveCost = 0;
            view.EvolveHint = null;
            return view;
        }

        public static DudeInfoView FromDudeBall(DudeBall ball)
        {
            if (ball == null || ball.Deleted || !ball.HasDude || ball.StoredDude == null)
                return Empty;

            string status;
            if (ball.IsAssignedToJob)
                status = "At Job Station";
            else if (ball.IsSummoned)
                status = "Summoned";
            else if (ball.StoredDude.IsFainted)
                status = "Fainted";
            else
                status = "Captured";

            DudeInfoView view = FromDudeData(ball.StoredDude, status);
            view.CreatureSerial = Serial.MinusOne;
            view.BossSerial = Serial.MinusOne;
            FillEvolve(view, ball);

            // Live gear from summoned or parked/internalized Dude on the ball (not old kit list).
            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted)
            {
                TryOverlayLiveCombatSkills(view, live);
                FillEquippedGear(view, live);
            }

            return view;
        }

        private static string BuildCapturedStatus(DudeData data)
        {
            if (data.IsFainted)
                return "Fainted";
            return "Captured";
        }

        public static string GetAbilityDescription(string abilityId, int level, int hitsMax)
        {
            return GetAbilityDescription(abilityId, level, hitsMax, 1.0);
        }

        public static string GetAbilityDescription(string abilityId, int level, int hitsMax, double effectMultiplier)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;

            if (level < 1)
                level = 1;
            if (effectMultiplier < 0.0)
                effectMultiplier = 0.0;

            int blast = ScaleByEffect(DudeExperience.GetBlastDamage(level), effectMultiplier);
            DudeAbilityTune tune = DudeAbilityConfig.Get(abilityId);

            switch (abilityId.ToLowerInvariant())
            {
                case "blast":
                    return string.Format("Deals {0} fire damage to a nearby foe.", blast);

                case "ring_of_fire":
                {
                    double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.5;
                    int damage = ScaleByEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(level) * vs)), effectMultiplier);
                    return string.Format("Deals {0} fire damage in an expanding ring.", damage);
                }

                case "burn":
                {
                    double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.3;
                    int dmg = ScaleByEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(level) * vs)), effectMultiplier);
                    double chance = tune != null && tune.HitChance > 0.0 ? tune.HitChance : 0.5;
                    double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 1.0;
                    return string.Format(
                        "Passive. Every {0:0.#}s in combat, {1}% chance to Burn a nearby foe for {2} damage.",
                        tick, (int)Math.Round(chance * 100.0), dmg);
                }

                case "tide_mend":
                    return string.Format("Heals itself for {0} hit points.", blast);

                case "tide_chorus":
                {
                    // Heal = (10 + 2 * gearLevel)% of each target's HitsMax. gearLevel from the gear
                    // effect multiplier (1.0 + 0.10 * level), so percent = 10 + 20 * (mult - 1).
                    int gearLevel = (int)Math.Round((effectMultiplier - 1.0) / 0.10);
                    if (gearLevel < 0)
                        gearLevel = 0;
                    if (gearLevel > 10)
                        gearLevel = 10;
                    return string.Format("Heals each nearby allied Dude for {0}% of that Dude's hit points.", 10 + (2 * gearLevel));
                }

                case "spring":
                {
                    // Same formula as SpringAbility.Pulse / SpringAbility.PulseLinked.
                    double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 2.0;
                    double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.05;
                    int rawBlast = DudeExperience.GetBlastDamage(level);
                    int selfHeal = Math.Max(1, (int)(rawBlast * 0.15));
                    int pctHeal = Math.Max(1, (int)(hitsMax * healFrac));
                    int heal = ScaleByEffect(Math.Min(selfHeal, pctHeal), effectMultiplier);
                    return string.Format("Passive. Every {0:0.#}s, heals nearby allied Dudes (including itself) for {1}.", tick, heal);
                }

                case "fault_strike":
                {
                    double stun = tune != null && tune.StunSeconds > 0.0 ? tune.StunSeconds : 1.0;
                    return string.Format(
                        "Deals {0} damage and paralyzes a foe for {1:0.#}s. Does not paralyze players.",
                        blast, stun);
                }

                case "aftershock":
                {
                    double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.5;
                    int dmg = ScaleByEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(level) * vs)), effectMultiplier);
                    // Stun comes from StunSeconds (Execute); StunMin/StunMax used by faultline passive.
                    return string.Format(
                        "Deals {0} damage to nearby foes and may stun them briefly. Does not stun players or Dudes.",
                        dmg);
                }

                case "faultline":
                {
                    // Same as FaultlineAbility.Pulse: GapSeconds + DamageVsBlast fraction.
                    double gap = tune != null && tune.GapSeconds > 0.0 ? tune.GapSeconds : 10.0;
                    double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.33;
                    int dmg = ScaleByEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(level) * vs)), effectMultiplier);
                    return string.Format(
                        "Passive. Every {0:0.#}s, deals {1} damage to a nearby foe and may paralyze them.",
                        gap, dmg);
                }

                case "tailwind_self":
                {
                    // Execute defaults: SpeedFactor 0.5 (ActiveSpeed multiplier), DurationSeconds 5.
                    double speed = tune != null && tune.SpeedFactor > 0.0 ? tune.SpeedFactor : 0.5;
                    speed = 1.0 + (speed - 1.0) * effectMultiplier;
                    double dur = tune != null && tune.DurationSeconds > 0.0 ? tune.DurationSeconds : 5.0;
                    int pct = speed > 0.0 && speed < 1.0
                        ? (int)Math.Round((1.0 / speed - 1.0) * 100.0)
                        : (int)Math.Round((speed - 1.0) * 100.0);
                    if (pct < 0)
                        pct = 0;
                    return string.Format("Attack speed +{0}% for {1:0.#}s on itself.", pct, dur);
                }

                case "tailwind":
                {
                    double speed = tune != null && tune.SpeedFactor > 0.0 ? tune.SpeedFactor : 0.5;
                    speed = 1.0 + (speed - 1.0) * effectMultiplier;
                    double dur = tune != null && tune.DurationSeconds > 0.0 ? tune.DurationSeconds : 5.0;
                    int pct = speed > 0.0 && speed < 1.0
                        ? (int)Math.Round((1.0 / speed - 1.0) * 100.0)
                        : (int)Math.Round((speed - 1.0) * 100.0);
                    if (pct < 0)
                        pct = 0;
                    return string.Format("Attack speed +{0}% for {1:0.#}s on nearby allied Dudes.", pct, dur);
                }

                case "slipstream":
                {
                    double reduce = tune != null && tune.ReduceSeconds > 0.0 ? tune.ReduceSeconds : 2.0;
                    reduce *= effectMultiplier;
                    double floor = tune != null && tune.FloorSeconds > 0.0 ? tune.FloorSeconds : 7.0;
                    return string.Format(
                        "Passive. Ability cooldowns are {0:0.#}s faster (minimum {1:0.#}s).",
                        reduce, floor);
                }

                default:
                    return "A special Dude technique.";
            }
        }

        private static int ScaleByEffect(int value, double effectMultiplier)
        {
            int scaled = (int)Math.Round(value * effectMultiplier);
            if (scaled < 0)
                scaled = 0;
            return scaled;
        }

        /// <summary>
        /// Cooldown text for a gear line (e.g. "12s"). Uses tune.GapSeconds when set, else the
        /// ability's own Cooldown. Passives show their pulse gap ("every 2s"); abilities with no
        /// timing (e.g. slipstream) return null so the line stays name/level/bonus only.
        /// </summary>
        private static string GetGearCooldownText(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;

            DudeAbility ability = DudeAbilityRegistry.Get(abilityId);
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get(abilityId);

            bool passive = DudeCreature.IsPassiveAbilityId(abilityId);
            double seconds = 0.0;

            if (passive)
            {
                // Pulse gap: GapSeconds (faultline) or TickSeconds (burn/spring).
                if (tune != null && tune.GapSeconds > 0.0)
                    seconds = tune.GapSeconds;
                else if (tune != null && tune.TickSeconds > 0.0)
                    seconds = tune.TickSeconds;
            }
            else
            {
                if (tune != null && tune.GapSeconds > 0.0)
                    seconds = tune.GapSeconds;
                else if (ability != null)
                    seconds = ability.Cooldown.TotalSeconds;
            }

            int rounded = (int)Math.Round(seconds);
            if (rounded <= 0)
                return null;

            return passive
                ? string.Format("every {0}s", rounded)
                : string.Format("{0}s", rounded);
        }
    }
}
