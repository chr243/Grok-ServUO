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
        public DudeInfoGump(DudeInfoView view)
            : base(50, 50)
        {
            if (view == null)
                view = DudeInfoView.Empty;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 360, 520, 9270);
            AddAlphaRegion(10, 10, 340, 500);

            AddHtml(20, 18, 320, 22, "<CENTER><BASEFONT COLOR=#FFFFFF>Trainer's Manual</BASEFONT></CENTER>", false, false);

            int y = 48;
            int labelHue = 0x480;
            int valueHue = 0x34;

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

            AddHtml(24, y, 312, 18, "<BASEFONT COLOR=#FFFFFF>Stats</BASEFONT>", false, false);
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

            AddHtml(24, y, 312, 18, "<BASEFONT COLOR=#FFFFFF>Abilities</BASEFONT>", false, false);
            y += 20;

            AddHtml(24, y, 312, 40, string.Format("<BASEFONT COLOR=#66FF66>{0}</BASEFONT>",
                string.IsNullOrEmpty(view.AbilityName) ? "None" : view.AbilityName), false, false);
            y += 44;

            if (!string.IsNullOrEmpty(view.AbilityDescription))
            {
                AddHtml(24, y, 312, 90, string.Format("<BASEFONT COLOR=#C0C0C0>{0}</BASEFONT>", view.AbilityDescription), false, true);
                y += 96;
            }

            AddButton(300, 480, 4017, 4019, 0, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            // Read-only sheet — close only.
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
            ExpText = "N/A",
            JobSkillText = "N/A",
            EvolutionText = null,
            AbilityName = "None",
            AbilityDescription = null
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

        private static void FillSkillTexts(DudeInfoView view, DudeData data)
        {
            view.JobSkillText = Server.Custom.Dudes.Jobs.DudeJobHarvest.FormatSkillLabel(data);
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
                    || string.Equals(data.DefinitionId, "infernox", StringComparison.OrdinalIgnoreCase);
            }
            if (!showBurn)
            {
                showBurn = evolutionStage >= 3
                    || string.Equals(definitionId, "infernox", StringComparison.OrdinalIgnoreCase);
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
            FillAbilityTexts(view, data, data.AbilityId);
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

            string abilityId = !string.IsNullOrEmpty(dude.AbilityId)
                ? dude.AbilityId
                : (def != null ? def.AbilityId : null);

            FillAbilityTexts(view, null, abilityId, dude.DefinitionId, dude.EvolutionStage);
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
            view.AbilityName = boss.AbilityDisplayName;
            view.AbilityDescription = boss.AbilityDescription;
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

            return FromDudeData(ball.StoredDude, status);
        }

        private static string BuildCapturedStatus(DudeData data)
        {
            if (data.IsFainted)
                return "Fainted";
            return "Captured";
        }

        private static string GetAbilityDescription(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;

            switch (abilityId.ToLowerInvariant())
            {
                case "ember_burst":
                    return "Fire burst that scorches a nearby foe.";
                case "tide_crash":
                    return "Water crash that batters a nearby foe.";
                case "stone_slam":
                    return "Heavy earth slam against a nearby foe.";
                case "gust_slash":
                    return "Cutting wind slash against a nearby foe.";
                case "cinder_bite":
                    return "Searing fangs that bite with cinder heat.";
                case "riptide_crash":
                    return "A crushing surge of cold water.";
                case "boulder_crush":
                    return "A crushing boulder smash against a foe.";
                case "pyre_blast":
                    return "Elite fire blast that sears a nearby foe.";
                case "blast":
                    return "Fire strike that blasts a nearby foe.";
                case "ring_of_fire":
                    return "AOE ring of flames scorching enemies nearby.";
                case "burn":
                    return "Passive: 5% chance per combat tick to Burn nearby foes (range 8) for 30% of Blast damage.";
                default:
                    return "A special Dude technique.";
            }
        }
    }
}
