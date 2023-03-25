// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("cheat")]
class CheatCommands
{
	[Command("casttime", RBACPermissions.CommandCheatCasttime)]
	static bool HandleCasttimeCheatCommand(CommandHandler handler, bool? enableArg)
	{
		var enable = !handler.Session.Player.GetCommandStatus(PlayerCommandStates.Casttime);

		if (enableArg.HasValue)
			enable = enableArg.Value;

		if (enable)
		{
			handler.Session.Player.SetCommandStatusOn(PlayerCommandStates.Casttime);
			handler.SendSysMessage("CastTime Cheat is ON. Your spells won't have a casttime.");
		}
		else
		{
			handler.Session.Player.SetCommandStatusOff(PlayerCommandStates.Casttime);
			handler.SendSysMessage("CastTime Cheat is OFF. Your spells will have a casttime.");
		}

		return true;
	}

	[Command("cooldown", RBACPermissions.CommandCheatCooldown)]
	static bool HandleCoolDownCheatCommand(CommandHandler handler, bool? enableArg)
	{
		var enable = !handler.Session.Player.GetCommandStatus(PlayerCommandStates.Cooldown);

		if (enableArg.HasValue)
			enable = enableArg.Value;

		if (enable)
		{
			handler.Session.Player.SetCommandStatusOn(PlayerCommandStates.Cooldown);
			handler.SendSysMessage("Cooldown Cheat is ON. You are not on the global cooldown.");
		}
		else
		{
			handler.Session.Player.SetCommandStatusOff(PlayerCommandStates.Cooldown);
			handler.SendSysMessage("Cooldown Cheat is OFF. You are on the global cooldown.");
		}

		return true;
	}

	[Command("explore", RBACPermissions.CommandCheatExplore)]
	static bool HandleExploreCheatCommand(CommandHandler handler, bool reveal)
	{
		var chr = handler.SelectedPlayer;

		if (!chr)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		if (reveal)
		{
			handler.SendSysMessage(CypherStrings.YouSetExploreAll, handler.GetNameLink(chr));

			if (handler.NeedReportToTarget(chr))
				chr.SendSysMessage(CypherStrings.YoursExploreSetAll, handler.NameLink);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.YouSetExploreNothing, handler.GetNameLink(chr));

			if (handler.NeedReportToTarget(chr))
				chr.SendSysMessage(CypherStrings.YoursExploreSetNothing, handler.NameLink);
		}

		for (ushort i = 0; i < PlayerConst.ExploredZonesSize; ++i)
			if (reveal)
				handler.Session.Player.AddExploredZones(i, 0xFFFFFFFFFFFFFFFF);
			else
				handler.Session.Player.RemoveExploredZones(i, 0xFFFFFFFFFFFFFFFF);

		return true;
	}

	[Command("god", RBACPermissions.CommandCheatGod)]
	static bool HandleGodModeCheatCommand(CommandHandler handler, bool? enableArg)
	{
		var enable = !handler.Session.Player.GetCommandStatus(PlayerCommandStates.God);

		if (enableArg.HasValue)
			enable = enableArg.Value;

		if (enable)
		{
			handler.Session.Player.SetCommandStatusOn(PlayerCommandStates.God);
			handler.SendSysMessage("Godmode is ON. You won't take damage.");
		}
		else
		{
			handler.Session.Player.SetCommandStatusOff(PlayerCommandStates.God);
			handler.SendSysMessage("Godmode is OFF. You can take damage.");
		}

		return true;
	}

	[Command("power", RBACPermissions.CommandCheatPower)]
	static bool HandlePowerCheatCommand(CommandHandler handler, bool? enableArg)
	{
		var enable = !handler.Session.Player.GetCommandStatus(PlayerCommandStates.Power);

		if (enableArg.HasValue)
			enable = enableArg.Value;

		if (enable)
		{
			var player = handler.Session.Player;

			// Set max power to all powers
			for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
				player.SetPower(powerType, player.GetMaxPower(powerType));

			player.SetCommandStatusOn(PlayerCommandStates.Power);
			handler.SendSysMessage("Power Cheat is ON. You don't need mana/rage/energy to use spells.");
		}
		else
		{
			handler.Session.Player.SetCommandStatusOff(PlayerCommandStates.Power);
			handler.SendSysMessage("Power Cheat is OFF. You need mana/rage/energy to use spells.");
		}

		return true;
	}

	[Command("status", RBACPermissions.CommandCheatStatus)]
	static bool HandleCheatStatusCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;

		var enabled = "ON";
		var disabled = "OFF";

		handler.SendSysMessage(CypherStrings.CommandCheatStatus);
		handler.SendSysMessage(CypherStrings.CommandCheatGod, player.GetCommandStatus(PlayerCommandStates.God) ? enabled : disabled);
		handler.SendSysMessage(CypherStrings.CommandCheatCd, player.GetCommandStatus(PlayerCommandStates.Cooldown) ? enabled : disabled);
		handler.SendSysMessage(CypherStrings.CommandCheatCt, player.GetCommandStatus(PlayerCommandStates.Casttime) ? enabled : disabled);
		handler.SendSysMessage(CypherStrings.CommandCheatPower, player.GetCommandStatus(PlayerCommandStates.Power) ? enabled : disabled);
		handler.SendSysMessage(CypherStrings.CommandCheatWw, player.GetCommandStatus(PlayerCommandStates.Waterwalk) ? enabled : disabled);
		handler.SendSysMessage(CypherStrings.CommandCheatTaxinodes, player.IsTaxiCheater ? enabled : disabled);

		return true;
	}

	[Command("taxi", RBACPermissions.CommandCheatTaxi)]
	static bool HandleTaxiCheatCommand(CommandHandler handler, bool? enableArg)
	{
		var chr = handler.SelectedPlayer;

		if (!chr)
			chr = handler.Session.Player;
		else if (handler.HasLowerSecurity(chr, ObjectGuid.Empty)) // check online security
			return false;

		var enable = !chr.IsTaxiCheater;

		if (enableArg.HasValue)
			enable = enableArg.Value;

		if (enable)
		{
			chr.SetTaxiCheater(true);
			handler.SendSysMessage(CypherStrings.YouGiveTaxis, handler.GetNameLink(chr));

			if (handler.NeedReportToTarget(chr))
				chr.SendSysMessage(CypherStrings.YoursTaxisAdded, handler.NameLink);
		}
		else
		{
			chr.SetTaxiCheater(false);
			handler.SendSysMessage(CypherStrings.YouRemoveTaxis, handler.GetNameLink(chr));

			if (handler.NeedReportToTarget(chr))
				chr.SendSysMessage(CypherStrings.YoursTaxisRemoved, handler.NameLink);
		}

		return true;
	}

	[Command("waterwalk", RBACPermissions.CommandCheatWaterwalk)]
	static bool HandleWaterWalkCheatCommand(CommandHandler handler, bool? enableArg)
	{
		var enable = !handler.Session.Player.GetCommandStatus(PlayerCommandStates.Waterwalk);

		if (enableArg.HasValue)
			enable = enableArg.Value;

		if (enable)
		{
			handler.Session.Player.SetCommandStatusOn(PlayerCommandStates.Waterwalk);
			handler.Session.Player.SetWaterWalking(true);
			handler.SendSysMessage("Waterwalking is ON. You can walk on water.");
		}
		else
		{
			handler.Session.Player.SetCommandStatusOff(PlayerCommandStates.Waterwalk);
			handler.Session.Player.SetWaterWalking(false);
			handler.SendSysMessage("Waterwalking is OFF. You can't walk on water.");
		}

		return true;
	}
}