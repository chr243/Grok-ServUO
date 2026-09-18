using System;
using System.Globalization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.Custom.Dudes.Commands
{
    /// <summary>
    /// GM command [DudeAbilities — live-tune per-ability numerics.
    /// </summary>
    public static class DudeAbilitiesCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("DudeAbilities", AccessLevel.GameMaster, new CommandEventHandler(OnCommand));
            DudeAbilityConfig.EnsureLoaded();
        }

        [Usage("DudeAbilities")]
        [Description("Opens the Dude ability tuning gump. Changes apply immediately and save to Config/DudeAbilities.cfg.")]
        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            DudeAbilityConfig.EnsureLoaded();
            from.CloseGump(typeof(DudeAbilitiesGump));
            from.SendGump(new DudeAbilitiesGump(0));
        }
    }

    /// <summary>
    /// Blessed staff item — double-click opens the same gump (GM+).
    /// </summary>
    public class DudeAbilitiesStaff : Item
    {
        [Constructable]
        public DudeAbilitiesStaff()
            : base(0xE2D) // staff / wand-like graphic
        {
            Name = "Dude Abilities Staff";
            Weight = 1.0;
            Hue = 0x489;
            LootType = LootType.Blessed;
        }

        public DudeAbilitiesStaff(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("GM+: double-click to tune Dude abilities.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage(0x22, "Only GameMasters may use this.");
                return;
            }

            bool inPack = IsChildOf(from.Backpack) || RootParent == from;
            bool inRange = from.InRange(GetWorldLocation(), 2);
            if (!inPack && !inRange)
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            DudeAbilityConfig.EnsureLoaded();
            from.CloseGump(typeof(DudeAbilitiesGump));
            from.SendGump(new DudeAbilitiesGump(0));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    /// <summary>
    /// Page 0 = ability list + shared blast scale.
    /// Page 1..N = detail for AbilityIds[page-1].
    /// </summary>
    public class DudeAbilitiesGump : Gump
    {
        private readonly int m_Page;

        // Shared blast entries (list page)
        private const int EntryBlastBase = 100;
        private const int EntryBlastPerLevel = 101;

        // Detail entry ids
        private const int EntryCooldown = 1;
        private const int EntryStun = 2;
        private const int EntryDuration = 3;
        private const int EntrySpeed = 4;
        private const int EntryVsBlast = 5;
        private const int EntryHealFrac = 6;
        private const int EntryRadius = 7;
        private const int EntryTick = 8;
        private const int EntryHitChance = 9;
        private const int EntryGap = 10;
        private const int EntryStunMin = 11;
        private const int EntryStunMax = 12;
        private const int EntryReduce = 13;
        private const int EntryFloor = 14;
        private const int EntryDetailBlastBase = 15;
        private const int EntryDetailBlastPer = 16;

        private const int BtnSave = 1;
        private const int BtnReload = 2;
        private const int BtnDefaults = 3;
        private const int BtnBack = 4;
        private const int BtnAbilityBase = 100; // + index → open detail

        public DudeAbilitiesGump(int page)
            : base(40, 40)
        {
            m_Page = page;
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityRegistry.EnsureInitialized();

            if (page <= 0)
                BuildListPage();
            else
                BuildDetailPage(page - 1);
        }

        private void BuildListPage()
        {
            AddPage(0);
            AddBackground(0, 0, 520, 520, 9270);
            AddAlphaRegion(10, 10, 500, 500);

            AddHtml(20, 16, 480, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Dude Abilities Admin</BASEFONT></CENTER>", false, false);
            AddHtml(20, 38, 480, 32,
                "<BASEFONT COLOR=#CCCCCC>Live tunables. Save writes Config/DudeAbilities.cfg. Next cast uses new values.</BASEFONT>",
                false, false);

            int y = 74;
            int labelHue = 0x480;
            int entryHue = 0x481;

            AddLabel(24, y, labelHue, "Shared BlastBase");
            AddTextEntry(200, y, 60, 20, entryHue, EntryBlastBase, DudeAbilityConfig.BlastBase.ToString());
            AddLabel(280, y, labelHue, "BlastPerLevel");
            AddTextEntry(390, y, 60, 20, entryHue, EntryBlastPerLevel, DudeAbilityConfig.BlastPerLevel.ToString());
            y += 28;

            AddLabel(24, y, 0x59, "Ability");
            AddLabel(200, y, 0x59, "Kit");
            AddLabel(280, y, 0x59, "Stage");
            AddLabel(340, y, 0x59, "CD / type");
            y += 22;

            string[] ids = DudeAbilityConfig.AbilityIds;
            for (int i = 0; i < ids.Length; i++)
            {
                DudeAbility ability = DudeAbilityRegistry.Get(ids[i]);
                DudeAbilityTune tune = DudeAbilityConfig.Get(ids[i]);

                string name = ability != null ? ability.Name : ids[i];
                string kit = ability != null ? ability.Kit.ToString() : "?";
                string stage = ability != null ? ability.Stage.ToString() : "?";
                string cd;
                if (tune != null && tune.CooldownSeconds > 0.0)
                    cd = DudeAbilityConfig.FormatDouble(tune.CooldownSeconds) + "s";
                else
                    cd = "passive";

                AddButton(24, y, 4005, 4007, BtnAbilityBase + i, GumpButtonType.Reply, 0);
                AddLabel(59, y, labelHue, Truncate(name, 16));
                AddLabel(200, y, entryHue, kit);
                AddLabel(280, y, entryHue, stage);
                AddLabel(340, y, entryHue, cd);
                y += 24;
            }

            y += 8;
            AddButton(24, y, 4005, 4007, BtnSave, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Save (apply + write file)");
            y += 26;
            AddButton(24, y, 4005, 4007, BtnReload, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Reload from disk");
            y += 26;
            AddButton(24, y, 4005, 4007, BtnDefaults, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Reset ALL defaults (click Save to persist)");
            y += 26;
            AddLabel(24, y, 0x34, "File: Config/DudeAbilities.cfg");
        }

        private void BuildDetailPage(int abilityIndex)
        {
            string[] ids = DudeAbilityConfig.AbilityIds;
            if (abilityIndex < 0 || abilityIndex >= ids.Length)
            {
                BuildListPage();
                return;
            }

            string id = ids[abilityIndex];
            DudeAbility ability = DudeAbilityRegistry.Get(id);
            DudeAbilityTune tune = DudeAbilityConfig.Get(id);

            AddPage(0);
            AddBackground(0, 0, 480, 480, 9270);
            AddAlphaRegion(10, 10, 460, 460);

            string title = ability != null ? ability.Name : id;
            string kit = ability != null ? ability.Kit.ToString() : "?";
            string stage = ability != null ? ("S" + ability.Stage) : "?";

            AddHtml(20, 16, 440, 20,
                string.Format("<CENTER><BASEFONT COLOR=#FFFFFF>{0}</BASEFONT></CENTER>", Escape(title)),
                false, false);
            AddHtml(20, 38, 440, 20,
                string.Format("<BASEFONT COLOR=#CCCCCC>id={0}  kit={1}  {2}</BASEFONT>", Escape(id), Escape(kit), Escape(stage)),
                false, false);

            string unlocks = DudeAbilityConfig.FormatUnlockSpecies(id);
            AddHtml(20, 60, 440, 36,
                string.Format("<BASEFONT COLOR=#AAAAAA>Unlocks: {0}</BASEFONT>", Escape(unlocks)),
                false, false);

            int y = 104;
            int labelHue = 0x480;
            int entryHue = 0x481;

            // Shared blast scale when editing blast (source of GetBlastDamage)
            if (string.Equals(id, "blast", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "BlastBase (shared)", EntryDetailBlastBase, DudeAbilityConfig.BlastBase.ToString());
                y = AddEntry(y, labelHue, entryHue, "BlastPerLevel (shared)", EntryDetailBlastPer, DudeAbilityConfig.BlastPerLevel.ToString());
            }

            bool passive = DudeAbilityConfig.IsPassive(id);

            if (!passive)
                y = AddEntry(y, labelHue, entryHue, "Cooldown (seconds)", EntryCooldown, Fmt(tune.CooldownSeconds));

            // Relevant fields per ability
            if (string.Equals(id, "fault_strike", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "aftershock", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "StunSeconds", EntryStun, Fmt(tune.StunSeconds));
            }

            if (string.Equals(id, "tailwind_self", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "DurationSeconds", EntryDuration, Fmt(tune.DurationSeconds));
                y = AddEntry(y, labelHue, entryHue, "SpeedFactor", EntrySpeed, Fmt(tune.SpeedFactor));
            }

            if (string.Equals(id, "ring_of_fire", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "aftershock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "burn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "faultline", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "DamageVsBlast", EntryVsBlast, Fmt(tune.DamageVsBlast));
            }

            if (string.Equals(id, "tide_chorus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "spring", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "HealHitsFraction", EntryHealFrac, Fmt(tune.HealHitsFraction));
            }

            if (string.Equals(id, "aftershock", StringComparison.OrdinalIgnoreCase))
                y = AddEntry(y, labelHue, entryHue, "Radius", EntryRadius, tune.Radius.ToString());

            if (string.Equals(id, "burn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "spring", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "TickSeconds", EntryTick, Fmt(tune.TickSeconds));
            }

            if (string.Equals(id, "burn", StringComparison.OrdinalIgnoreCase))
                y = AddEntry(y, labelHue, entryHue, "HitChance (0-1)", EntryHitChance, Fmt(tune.HitChance));

            if (string.Equals(id, "faultline", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "GapSeconds", EntryGap, Fmt(tune.GapSeconds));
                y = AddEntry(y, labelHue, entryHue, "StunMin", EntryStunMin, Fmt(tune.StunMin));
                y = AddEntry(y, labelHue, entryHue, "StunMax", EntryStunMax, Fmt(tune.StunMax));
            }

            if (string.Equals(id, "slipstream", StringComparison.OrdinalIgnoreCase))
            {
                y = AddEntry(y, labelHue, entryHue, "ReduceSeconds", EntryReduce, Fmt(tune.ReduceSeconds));
                y = AddEntry(y, labelHue, entryHue, "FloorSeconds", EntryFloor, Fmt(tune.FloorSeconds));
            }

            y += 12;
            AddButton(24, y, 4005, 4007, BtnSave, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Save (apply + write file)");
            y += 26;
            AddButton(24, y, 4005, 4007, BtnReload, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Reload from disk");
            y += 26;
            AddButton(24, y, 4005, 4007, BtnDefaults, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Defaults for THIS ability");
            y += 26;
            AddButton(24, y, 4014, 4016, BtnBack, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Back to list");
        }

        private int AddEntry(int y, int labelHue, int entryHue, string label, int entryId, string value)
        {
            AddLabel(24, y, labelHue, label);
            AddTextEntry(280, y, 140, 20, entryHue, entryId, value ?? "");
            return y + 26;
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender != null ? sender.Mobile : null;
            if (from == null || info == null || info.ButtonID == 0)
                return;

            if (from.AccessLevel < AccessLevel.GameMaster)
                return;

            int btn = info.ButtonID;

            if (btn >= BtnAbilityBase && btn < BtnAbilityBase + DudeAbilityConfig.AbilityIds.Length)
            {
                int idx = btn - BtnAbilityBase;
                from.SendGump(new DudeAbilitiesGump(idx + 1));
                return;
            }

            switch (btn)
            {
                case BtnBack:
                    from.SendGump(new DudeAbilitiesGump(0));
                    break;

                case BtnReload:
                    DudeAbilityConfig.Load();
                    from.SendMessage(0x59, "Dude abilities reloaded from disk.");
                    from.SendGump(new DudeAbilitiesGump(m_Page));
                    break;

                case BtnDefaults:
                    if (m_Page <= 0)
                    {
                        DudeAbilityConfig.ResetDefaults();
                        from.SendMessage(0x59, "All ability defaults restored (click Save to persist).");
                    }
                    else
                    {
                        string id = DudeAbilityConfig.AbilityIds[m_Page - 1];
                        DudeAbilityConfig.ResetAbilityDefaults(id);
                        from.SendMessage(0x59, "Defaults restored for {0} (click Save to persist).", id);
                    }
                    from.SendGump(new DudeAbilitiesGump(m_Page));
                    break;

                case BtnSave:
                    if (!ApplyEntries(info, from))
                        break;

                    if (DudeAbilityConfig.Save())
                        from.SendMessage(0x59, "Dude abilities saved to {0} and applied live.", DudeAbilityConfig.FilePath);
                    else
                        from.SendMessage(0x22, "Applied in memory, but file save failed.");

                    from.SendGump(new DudeAbilitiesGump(m_Page));
                    break;
            }
        }

        private bool ApplyEntries(RelayInfo info, Mobile from)
        {
            if (m_Page <= 0)
            {
                int blastBase, blastPer;
                if (!TryReadInt(info, EntryBlastBase, out blastBase) || blastBase < 0
                    || !TryReadInt(info, EntryBlastPerLevel, out blastPer) || blastPer < 0)
                {
                    from.SendMessage(0x22, "Invalid BlastBase / BlastPerLevel.");
                    from.SendGump(new DudeAbilitiesGump(m_Page));
                    return false;
                }

                DudeAbilityConfig.BlastBase = blastBase;
                DudeAbilityConfig.BlastPerLevel = blastPer;
                return true;
            }

            string id = DudeAbilityConfig.AbilityIds[m_Page - 1];
            DudeAbilityTune tune = DudeAbilityConfig.Get(id);
            if (tune == null)
            {
                from.SendMessage(0x22, "Unknown ability.");
                from.SendGump(new DudeAbilitiesGump(0));
                return false;
            }

            if (string.Equals(id, "blast", StringComparison.OrdinalIgnoreCase))
            {
                int bb, bp;
                if (TryReadInt(info, EntryDetailBlastBase, out bb) && bb >= 0)
                    DudeAbilityConfig.BlastBase = bb;
                if (TryReadInt(info, EntryDetailBlastPer, out bp) && bp >= 0)
                    DudeAbilityConfig.BlastPerLevel = bp;
            }

            double d;
            int n;

            if (!DudeAbilityConfig.IsPassive(id))
            {
                if (TryReadDouble(info, EntryCooldown, out d) && d > 0.0)
                    tune.CooldownSeconds = d;
                else if (HasText(info, EntryCooldown))
                {
                    from.SendMessage(0x22, "Invalid Cooldown (must be > 0).");
                    from.SendGump(new DudeAbilitiesGump(m_Page));
                    return false;
                }
            }

            if (HasText(info, EntryStun) && TryReadDouble(info, EntryStun, out d) && d >= 0.0)
                tune.StunSeconds = d;
            if (HasText(info, EntryDuration) && TryReadDouble(info, EntryDuration, out d) && d >= 0.0)
                tune.DurationSeconds = d;
            if (HasText(info, EntrySpeed) && TryReadDouble(info, EntrySpeed, out d) && d > 0.0)
                tune.SpeedFactor = d;
            if (HasText(info, EntryVsBlast) && TryReadDouble(info, EntryVsBlast, out d) && d >= 0.0)
                tune.DamageVsBlast = d;
            if (HasText(info, EntryHealFrac) && TryReadDouble(info, EntryHealFrac, out d) && d >= 0.0)
                tune.HealHitsFraction = d;
            if (HasText(info, EntryRadius) && TryReadInt(info, EntryRadius, out n) && n >= 0)
                tune.Radius = n;
            if (HasText(info, EntryTick) && TryReadDouble(info, EntryTick, out d) && d >= 0.0)
                tune.TickSeconds = d;
            if (HasText(info, EntryHitChance) && TryReadDouble(info, EntryHitChance, out d) && d >= 0.0)
                tune.HitChance = d;
            if (HasText(info, EntryGap) && TryReadDouble(info, EntryGap, out d) && d >= 0.0)
                tune.GapSeconds = d;
            if (HasText(info, EntryStunMin) && TryReadDouble(info, EntryStunMin, out d) && d >= 0.0)
                tune.StunMin = d;
            if (HasText(info, EntryStunMax) && TryReadDouble(info, EntryStunMax, out d) && d >= 0.0)
                tune.StunMax = d;
            if (HasText(info, EntryReduce) && TryReadDouble(info, EntryReduce, out d) && d >= 0.0)
                tune.ReduceSeconds = d;
            if (HasText(info, EntryFloor) && TryReadDouble(info, EntryFloor, out d) && d >= 0.0)
                tune.FloorSeconds = d;

            return true;
        }

        private static string Fmt(double d)
        {
            return DudeAbilityConfig.FormatDouble(d);
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max)
                return s ?? "";
            return s.Substring(0, max);
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("<", "(").Replace(">", ")");
        }

        private static bool HasText(RelayInfo info, int id)
        {
            TextRelay tr = info.GetTextEntry(id);
            return tr != null && !string.IsNullOrEmpty(tr.Text);
        }

        private static bool TryReadDouble(RelayInfo info, int id, out double value)
        {
            value = 0.0;
            TextRelay tr = info.GetTextEntry(id);
            if (tr == null || string.IsNullOrEmpty(tr.Text))
                return false;

            return double.TryParse(tr.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadInt(RelayInfo info, int id, out int value)
        {
            value = 0;
            TextRelay tr = info.GetTextEntry(id);
            if (tr == null || string.IsNullOrEmpty(tr.Text))
                return false;

            return int.TryParse(tr.Text.Trim(), out value);
        }
    }
}
