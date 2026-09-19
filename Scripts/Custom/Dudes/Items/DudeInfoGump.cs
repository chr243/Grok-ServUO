using System;
using System.Collections.Generic;
using System.Text;
using Server.Custom.Dudes;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Classic UO info sheet for a Dude (ball data, live creature, or boss scout).
    /// </summary>
    public class DudeInfoGump : Gump
    {
        private readonly Serial m_BallSerial;
        private readonly bool m_ShowEvolve;

        public DudeInfoGump(DudeInfoView view)
            : base(50, 50)
        {
            if (view == null)
                view = DudeInfoView.Empty;

            m_BallSerial = view.BallSerial;
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

            // RIGHT column — kit abilities by stage
            int ry = 48;
            AddHtml(370, ry, 320, 18, "<BASEFONT COLOR=#FFFFFF>Abilities</BASEFONT>", false, false);
            ry += 22;

            DudeType kit = view.KitType;
            bool any = false;

            for (int stage = 1; stage <= 3; stage++)
            {
                DudeAbility ability = DudeAbilityRegistry.GetByKitStage(kit, stage);
                if (ability == null)
                    continue;

                any = true;

                bool unlocked = view.EvolutionStage >= ability.Stage;
                if (!unlocked && view.UnlockedAbilityIds != null)
                {
                    for (int i = 0; i < view.UnlockedAbilityIds.Count; i++)
                    {
                        if (string.Equals(view.UnlockedAbilityIds[i], ability.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            unlocked = true;
                            break;
                        }
                    }
                }

                string nameColor = unlocked ? "#66FF66" : "#808080";
                string descColor = unlocked ? "#C0C0C0" : "#808080";

                AddHtml(370, ry, 320, 18, string.Format("<BASEFONT COLOR={0}>{1} — unlocks at stage {2}</BASEFONT>",
                    nameColor, ability.Name, ability.Stage), false, false);
                ry += 18;

                string desc = DudeInfoView.GetAbilityDescription(ability.Id);
                if (!string.IsNullOrEmpty(desc))
                {
                    AddHtml(370, ry, 320, 40, string.Format("<BASEFONT COLOR={0}>{1}</BASEFONT>", descColor, desc), false, false);
                    ry += 44;
                }
                else
                {
                    ry += 8;
                }
            }

            if (!any)
                AddHtml(370, ry, 320, 18, "<BASEFONT COLOR=#808080>None</BASEFONT>", false, false);

            if (view.ShowEvolve)
            {
                AddButton(560, 478, 4005, 4006, 2, GumpButtonType.Reply, 0);
                AddLabel(595, 480, 0x35, "Evolve!");
                if (!string.IsNullOrEmpty(view.EvolveHint))
                    AddLabel(560, 460, 0x480, Truncate(view.EvolveHint, 28));
            }

            AddButton(680, 480, 4017, 4019, 0, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info == null || sender == null || sender.Mobile == null)
                return;

            if (info.ButtonID == 2)
            {
                Mobile from = sender.Mobile;
                DudeBall ball = World.FindItem(m_BallSerial) as DudeBall;

                if (!DudeEvolution.CanPlayerEvolveBall(from, ball))
                {
                    from.SendMessage("That Dude cannot evolve right now.");
                    return;
                }

                DudeData data = ball.StoredDude;
                int cost = DudeEvolution.GetCoreCost(data.EvolutionStage);
                Type coreType = DudeEvolution.GetRequiredCoreType(data.Type);
                string name = DudeEvolution.GetCoreDisplayName(data.Type);

                if (coreType == null || cost < 1)
                {
                    from.SendMessage("That Dude cannot evolve right now.");
                    return;
                }

                from.SendMessage("Target {0} {1} in your backpack.", cost, name);
                from.Target = new EvolveCoreTarget(ball.Serial, cost, coreType, data.Type);
            }
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

    public sealed class EvolveCoreTarget : Target
    {
        private readonly Serial m_BallSerial;
        private readonly int m_Cost;
        private readonly Type m_RequiredType;
        private readonly DudeType m_DudeType;

        public EvolveCoreTarget(Serial ballSerial, int cost, Type requiredType, DudeType dudeType)
            : base(2, false, TargetFlags.None)
        {
            m_BallSerial = ballSerial;
            m_Cost = cost;
            m_RequiredType = requiredType;
            m_DudeType = dudeType;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from == null)
                return;

            Item item = targeted as Item;
            if (item == null || item.Deleted || !item.IsChildOf(from.Backpack))
            {
                from.SendMessage("That must be in your backpack.");
                return;
            }

            if (m_RequiredType == null
                || (item.GetType() != m_RequiredType && !m_RequiredType.IsAssignableFrom(item.GetType())))
            {
                from.SendMessage("That is not the correct essence.");
                return;
            }

            DudeBall ball = World.FindItem(m_BallSerial) as DudeBall;
            if (!DudeEvolution.CanPlayerEvolveBall(from, ball))
            {
                from.SendMessage("That Dude cannot evolve right now.");
                return;
            }

            switch (m_DudeType)
            {
                case DudeType.Fire:
                    DudeEvolution.TryEvolve(from, ball, item, DudeType.Fire, "ember", "flame", "blaze");
                    break;
                case DudeType.Water:
                    DudeEvolution.TryEvolve(from, ball, item, DudeType.Water, "droplet", "ripple", "torrent");
                    break;
                case DudeType.Earth:
                    DudeEvolution.TryEvolve(from, ball, item, DudeType.Earth, "pebble", "boulder", "quake");
                    break;
                case DudeType.Air:
                    DudeEvolution.TryEvolve(from, ball, item, DudeType.Air, "breeze", "gale", "hurricane");
                    break;
                default:
                    from.SendMessage("That Dude cannot evolve right now.");
                    break;
            }
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
            ExpText = "N/A",
            JobSkillText = "N/A",
            EvolutionText = null,
            AbilityName = "None",
            AbilityDescription = null,
            SkillWrestling = 0.0,
            SkillTactics = 0.0,
            SkillAnatomy = 0.0,
            SkillMagicResist = 0.0,
            EvolutionStage = 0,
            KitType = DudeType.Fire,
            UnlockedAbilityIds = null,
            BallSerial = Serial.MinusOne,
            ShowEvolve = false,
            EvolveCost = 0,
            EvolveHint = null
        };

        public string Name { get; set; }
        public string TypeText { get; set; }
        public string Status { get; set; }
        public string LevelText { get; set; }
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
        public int EvolutionStage { get; set; }
        public DudeType KitType { get; set; }
        public List<string> UnlockedAbilityIds { get; set; }

        public Serial BallSerial { get; set; }
        public bool ShowEvolve { get; set; }
        public int EvolveCost { get; set; }
        public string EvolveHint { get; set; }

        private static void FillEvolve(DudeInfoView view, DudeBall ball)
        {
            if (view == null)
                return;

            if (ball == null || ball.Deleted || !ball.HasDude || ball.StoredDude == null
                || !DudeEvolution.CanEvolve(ball.StoredDude))
            {
                view.ShowEvolve = false;
                view.BallSerial = Serial.MinusOne;
                view.EvolveCost = 0;
                view.EvolveHint = null;
                return;
            }

            int cost = DudeEvolution.GetCoreCost(ball.StoredDude.EvolutionStage);
            view.ShowEvolve = true;
            view.BallSerial = ball.Serial;
            view.EvolveCost = cost;
            view.EvolveHint = string.Format("Needs {0} essences", cost);
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
                showBurn = data.EvolutionStage >= 3
                    || string.Equals(data.DefinitionId, "blaze", StringComparison.OrdinalIgnoreCase);
            }
            if (!showBurn)
            {
                showBurn = evolutionStage >= 3
                    || string.Equals(definitionId, "blaze", StringComparison.OrdinalIgnoreCase);
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

                string desc = GetAbilityDescription(ids[i]);
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
                // Keep KitType / EvolutionStage from data; refresh skill values from live mobile when present.
                TryOverlayLiveCombatSkills(fromBall, dude);
                FillEvolve(fromBall, dude.BoundBall);
                return fromBall;
            }

            DudeDefinition def = DudeRegistry.Get(dude.DefinitionId);

            DudeInfoView view = new DudeInfoView();
            view.Name = dude.Name;
            view.TypeText = def != null ? def.Type.ToString() : "Unknown";
            view.Status = dude.IsWild ? "Wild" : "Summoned";
            view.LevelText = dude.DudeLevel > 0 ? dude.DudeLevel.ToString() : "1";
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
            FillEvolve(view, ball);
            return view;
        }

        private static string BuildCapturedStatus(DudeData data)
        {
            if (data.IsFainted)
                return "Fainted";
            return "Captured";
        }

        public static string GetAbilityDescription(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;

            switch (abilityId.ToLowerInvariant())
            {
                case "blast":
                    return "Instant fire strike on a nearby foe.";
                case "ring_of_fire":
                    return "Expanding ring of flames that scorches nearby enemies.";
                case "burn":
                    return "Passive. In combat, may Burn nearby foes.";
                case "tide_mend":
                    return "Heals itself.";
                case "tide_chorus":
                    return "Heals nearby allied Dudes.";
                case "spring":
                    return "Passive. Slowly heals itself and nearby allied Dudes.";
                case "fault_strike":
                    return "Earth strike that can paralyze a foe. Does not paralyze players.";
                case "aftershock":
                    return "Damages nearby foes and may briefly stun them. Does not stun players or Dudes.";
                case "faultline":
                    return "Passive. Periodically strikes a nearby foe and may paralyze them.";
                case "tailwind_self":
                    return "Brief attack-speed boost on itself.";
                case "tailwind":
                    return "Brief attack-speed boost on nearby allied Dudes.";
                case "slipstream":
                    return "Passive. Ability cooldowns are shorter.";
                default:
                    return "A special Dude technique.";
            }
        }
    }
}
