using System;
using Server.Commands;
using Server.Gumps;
using Server.Network;

namespace Server.Custom.Dudes.Commands
{
    /// <summary>
    /// GM command [DudeScale / [DudeAdmin — live-tune global Dude scaling.
    /// </summary>
    public static class DudeScaleCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("DudeScale", AccessLevel.GameMaster, new CommandEventHandler(OnCommand));
            CommandSystem.Register("DudeAdmin", AccessLevel.GameMaster, new CommandEventHandler(OnCommand));
            CommandSystem.Register("DudeTypesReload", AccessLevel.GameMaster, new CommandEventHandler(OnTypesReload));
            DudeScalingConfig.EnsureLoaded();
            DudeTypeProfiles.EnsureInitialized();
        }

        [Usage("DudeTypesReload")]
        [Description("Reloads Data/DudeTypes.cfg (per-type balance table) and re-seeds derived Dude stats.")]
        private static void OnTypesReload(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            DudeTypeConfig.Reload();

            if (from != null)
                from.SendMessage(0x59, "Dude type table reloaded ({0} types).", DudeTypeProfiles.GetAll().Count);
        }

        [Usage("DudeScale")]
        [Aliases("DudeAdmin")]
        [Description("Opens the Dude scaling admin gump (EXP / Hits / damage). Changes apply immediately and save to Data/DudeScaling.cfg.")]
        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            DudeScalingConfig.EnsureLoaded();
            from.CloseGump(typeof(DudeScaleGump));
            from.SendGump(new DudeScaleGump());
        }
    }

    public class DudeScaleGump : Gump
    {
        private const int EntryExpScale = 1;
        private const int EntryHits2to10 = 2;
        private const int EntryHits11to20 = 3;
        private const int EntryHits21to30 = 4;
        private const int EntryMelee = 5;
        private const int EntryAbility = 6;
        private const int EntryStrGain = 7;
        private const int EntryDexGain = 8;
        private const int EntryIntGain = 9;

        private const int BtnSave = 1;
        private const int BtnReload = 2;
        private const int BtnDefaults = 3;

        public DudeScaleGump()
            : base(50, 50)
        {
            DudeScalingConfig.EnsureLoaded();

            AddPage(0);
            AddBackground(0, 0, 420, 450, 9270);
            AddAlphaRegion(10, 10, 400, 430);

            AddHtml(20, 18, 380, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Dude Scaling Admin</BASEFONT></CENTER>", false, false);
            AddHtml(20, 40, 380, 36,
                "<BASEFONT COLOR=#CCCCCC>Applies immediately to new LevelUps / EXP curve. Existing stored Hits are kept.</BASEFONT>",
                false, false);

            int y = 84;
            int labelHue = 0x480;
            int entryHue = 0x481;

            AddLabel(24, y, labelHue, "EXP scale (curve multiplier)");
            AddTextEntry(280, y, 100, 20, entryHue, EntryExpScale,
                DudeScalingConfig.ExpScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            y += 28;

            AddLabel(24, y, labelHue, "Hits gain L2–10");
            AddTextEntry(280, y, 100, 20, entryHue, EntryHits2to10, DudeScalingConfig.HitsGainL2to10.ToString());
            y += 28;

            AddLabel(24, y, labelHue, "Hits gain L11–20");
            AddTextEntry(280, y, 100, 20, entryHue, EntryHits11to20, DudeScalingConfig.HitsGainL11to20.ToString());
            y += 28;

            AddLabel(24, y, labelHue, "Hits gain L21–30");
            AddTextEntry(280, y, 100, 20, entryHue, EntryHits21to30, DudeScalingConfig.HitsGainL21to30.ToString());
            y += 28;

            AddLabel(24, y, labelHue, "Melee +min/+max per level");
            AddTextEntry(280, y, 100, 20, entryHue, EntryMelee, DudeScalingConfig.MeleeDamagePerLevel.ToString());
            y += 28;

            AddLabel(24, y, labelHue, "Ability damage multiplier");
            AddTextEntry(280, y, 100, 20, entryHue, EntryAbility,
                DudeScalingConfig.AbilityDamageMultiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            y += 28;

            AddLabel(24, y, labelHue, "Str gain per level");
            AddTextEntry(280, y, 100, 20, entryHue, EntryStrGain, DudeScalingConfig.StrGainPerLevel.ToString());
            y += 28;

            AddLabel(24, y, labelHue, "Dex gain per level (L30 ~220–240)");
            AddTextEntry(280, y, 100, 20, entryHue, EntryDexGain, DudeScalingConfig.DexGainPerLevel.ToString());
            y += 28;

            AddLabel(24, y, labelHue, "Int gain per level");
            AddTextEntry(280, y, 100, 20, entryHue, EntryIntGain, DudeScalingConfig.IntGainPerLevel.ToString());
            y += 36;

            AddButton(24, y, 4005, 4007, BtnSave, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Save (apply + write file)");
            y += 28;

            AddButton(24, y, 4005, 4007, BtnReload, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Reload from disk");
            y += 28;

            AddButton(24, y, 4005, 4007, BtnDefaults, GumpButtonType.Reply, 0);
            AddLabel(59, y, labelHue, "Reset defaults (does not auto-save)");
            y += 28;

            AddLabel(24, y, 0x34, "File: Data/DudeScaling.cfg");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender != null ? sender.Mobile : null;
            if (from == null || info == null || info.ButtonID == 0)
                return;

            if (from.AccessLevel < AccessLevel.GameMaster)
                return;

            switch (info.ButtonID)
            {
                case BtnSave:
                    if (!ApplyEntries(info, from))
                        break;

                    if (DudeScalingConfig.Save())
                        from.SendMessage(0x59, "Dude scaling saved to {0} and applied live.", DudeScalingConfig.FilePath);
                    else
                        from.SendMessage(0x22, "Dude scaling applied in memory, but file save failed.");

                    from.SendGump(new DudeScaleGump());
                    break;

                case BtnReload:
                    DudeScalingConfig.Load();
                    from.SendMessage(0x59, "Dude scaling reloaded from disk.");
                    from.SendGump(new DudeScaleGump());
                    break;

                case BtnDefaults:
                    DudeScalingConfig.ResetDefaults();
                    from.SendMessage(0x59, "Dude scaling reset to defaults (click Save to persist).");
                    from.SendGump(new DudeScaleGump());
                    break;
            }
        }

        private static bool ApplyEntries(RelayInfo info, Mobile from)
        {
            double expScale;
            if (!TryReadDouble(info, EntryExpScale, out expScale) || expScale <= 0.0)
            {
                from.SendMessage(0x22, "Invalid EXP scale (must be > 0).");
                from.SendGump(new DudeScaleGump());
                return false;
            }

            int h2, h11, h21, melee, strG, dexG, intG;
            if (!TryReadInt(info, EntryHits2to10, out h2) || h2 < 0
                || !TryReadInt(info, EntryHits11to20, out h11) || h11 < 0
                || !TryReadInt(info, EntryHits21to30, out h21) || h21 < 0)
            {
                from.SendMessage(0x22, "Invalid Hits gain values (must be >= 0 integers).");
                from.SendGump(new DudeScaleGump());
                return false;
            }

            if (!TryReadInt(info, EntryMelee, out melee) || melee < 0)
            {
                from.SendMessage(0x22, "Invalid melee damage per level (must be >= 0).");
                from.SendGump(new DudeScaleGump());
                return false;
            }

            double ability;
            if (!TryReadDouble(info, EntryAbility, out ability) || ability <= 0.0)
            {
                from.SendMessage(0x22, "Invalid ability damage multiplier (must be > 0).");
                from.SendGump(new DudeScaleGump());
                return false;
            }

            if (!TryReadInt(info, EntryStrGain, out strG) || strG < 0
                || !TryReadInt(info, EntryDexGain, out dexG) || dexG < 0
                || !TryReadInt(info, EntryIntGain, out intG) || intG < 0)
            {
                from.SendMessage(0x22, "Invalid Str/Dex/Int gain per level (must be >= 0).");
                from.SendGump(new DudeScaleGump());
                return false;
            }

            DudeScalingConfig.ExpScale = expScale;
            DudeScalingConfig.HitsGainL2to10 = h2;
            DudeScalingConfig.HitsGainL11to20 = h11;
            DudeScalingConfig.HitsGainL21to30 = h21;
            DudeScalingConfig.MeleeDamagePerLevel = melee;
            DudeScalingConfig.AbilityDamageMultiplier = ability;
            DudeScalingConfig.StrGainPerLevel = strG;
            DudeScalingConfig.DexGainPerLevel = dexG;
            DudeScalingConfig.IntGainPerLevel = intG;
            return true;
        }

        private static bool TryReadDouble(RelayInfo info, int id, out double value)
        {
            value = 0.0;
            TextRelay tr = info.GetTextEntry(id);
            if (tr == null || string.IsNullOrEmpty(tr.Text))
                return false;

            return double.TryParse(tr.Text.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);
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
