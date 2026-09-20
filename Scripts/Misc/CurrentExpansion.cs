#region References
using System;

using Server.Accounting;
using Server.Network;
using Server.Services.TownCryer;
#endregion

namespace Server
{
	public class CurrentExpansion
	{
		public static readonly Expansion Expansion = Config.GetEnum("Expansion.CurrentExpansion", Expansion.EJ);

		/// <summary>
		/// When true, enables ObjectPropertyList tooltips even if CurrentExpansion is pre-AOS (e.g. UOR hybrid).
		/// </summary>
		public static readonly bool ForceTooltips = Config.Get("Expansion.ForceTooltips", false);

		[CallPriority(Int32.MinValue)]
		public static void Configure()
		{
			Core.Expansion = Expansion;

			AccountGold.Enabled = Core.TOL;
			AccountGold.ConvertOnBank = true;
			AccountGold.ConvertOnTrade = false;
			VirtualCheck.UseEditGump = true;

			TownCryerSystem.Enabled = Core.TOL;

			// UOR (or earlier) + ForceTooltips = hybrid: classic rules, AOS-style property tooltips.
			ObjectPropertyList.Enabled = Core.AOS || ForceTooltips;

			Mobile.InsuranceEnabled = Core.AOS && !Siege.SiegeShard;
			Mobile.VisibleDamageType = VisibleDamageType.Related;

			if (ObjectPropertyList.Enabled)
			{
				// Tooltips own single-click; disable classic ascii/guild click spam.
				Mobile.GuildClickMessage = false;
				Mobile.AsciiClickMessage = false;
				PacketHandlers.SingleClickProps = true;

				// Classic client only draws OPL when the AOS feature bit is advertised.
				// OR it onto UOR (etc.) without enabling Core.AOS combat/loot systems.
				if (ForceTooltips && !Core.AOS)
				{
					ExpansionInfo info = ExpansionInfo.CoreExpansion;
					if (info != null)
					{
						info.SupportedFeatures |= FeatureFlags.AOS;
						info.CharacterListFlags |= CharacterListFlags.AOS;
					}
				}
			}
			else
			{
				Mobile.GuildClickMessage = !Core.AOS;
				Mobile.AsciiClickMessage = !Core.AOS;
			}

			if (!Core.AOS)
			{
				Mobile.ActionDelay = 500;
				return;
			}

			AOS.DisableStatInfluences();

			Mobile.ActionDelay = Core.TOL ? 500 : 1000;
			Mobile.AOSStatusHandler = AOS.GetStatus;
		}
	}
}
