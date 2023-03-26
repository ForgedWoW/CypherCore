// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("bf")]
internal class BattleFieldCommands
{
	[Command("enable", RBACPermissions.CommandBfEnable)]
    private static bool HandleBattlefieldEnable(CommandHandler handler, uint battleId)
	{
		var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.Player.Map, battleId);

		if (bf == null)
			return false;

		if (bf.IsEnabled())
		{
			bf.ToggleBattlefield(false);

			if (battleId == 1)
				handler.SendGlobalGMSysMessage("Wintergrasp is disabled");
		}
		else
		{
			bf.ToggleBattlefield(true);

			if (battleId == 1)
				handler.SendGlobalGMSysMessage("Wintergrasp is enabled");
		}

		return true;
	}

	[Command("start", RBACPermissions.CommandBfStart)]
    private static bool HandleBattlefieldStart(CommandHandler handler, uint battleId)
	{
		var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.Player.Map, battleId);

		if (bf == null)
			return false;

		bf.StartBattle();

		if (battleId == 1)
			handler.SendGlobalGMSysMessage("Wintergrasp (Command start used)");

		return true;
	}

	[Command("stop", RBACPermissions.CommandBfStop)]
    private static bool HandleBattlefieldEnd(CommandHandler handler, uint battleId)
	{
		var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.Player.Map, battleId);

		if (bf == null)
			return false;

		bf.EndBattle(true);

		if (battleId == 1)
			handler.SendGlobalGMSysMessage("Wintergrasp (Command stop used)");

		return true;
	}

	[Command("switch", RBACPermissions.CommandBfSwitch)]
    private static bool HandleBattlefieldSwitch(CommandHandler handler, uint battleId)
	{
		var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.Player.Map, battleId);

		if (bf == null)
			return false;

		bf.EndBattle(false);

		if (battleId == 1)
			handler.SendGlobalGMSysMessage("Wintergrasp (Command switch used)");

		return true;
	}

	[Command("timer", RBACPermissions.CommandBfTimer)]
    private static bool HandleBattlefieldTimer(CommandHandler handler, uint battleId, uint time)
	{
		var bf = Global.BattleFieldMgr.GetBattlefieldByBattleId(handler.Player.Map, battleId);

		if (bf == null)
			return false;

		bf.SetTimer(time * Time.InMilliseconds);

		if (battleId == 1)
			handler.SendGlobalGMSysMessage("Wintergrasp (Command timer used)");

		return true;
	}
}