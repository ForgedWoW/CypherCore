// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("unban")]
internal class UnBanCommands
{
	[Command("account", RBACPermissions.CommandUnbanAccount, true)]
    private static bool HandleUnBanAccountCommand(CommandHandler handler, string name)
	{
		return HandleUnBanHelper(BanMode.Account, name, handler);
	}

	[Command("character", RBACPermissions.CommandUnbanCharacter, true)]
    private static bool HandleUnBanCharacterCommand(CommandHandler handler, string name)
	{
		if (!GameObjectManager.NormalizePlayerName(ref name))
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		if (!Global.WorldMgr.RemoveBanCharacter(name))
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		handler.SendSysMessage(CypherStrings.UnbanUnbanned, name);

		return true;
	}

	[Command("playeraccount", RBACPermissions.CommandUnbanPlayeraccount, true)]
    private static bool HandleUnBanAccountByCharCommand(CommandHandler handler, string name)
	{
		return HandleUnBanHelper(BanMode.Character, name, handler);
	}

	[Command("ip", RBACPermissions.CommandUnbanIp, true)]
    private static bool HandleUnBanIPCommand(CommandHandler handler, string ip)
	{
		return HandleUnBanHelper(BanMode.IP, ip, handler);
	}

    private static bool HandleUnBanHelper(BanMode mode, string nameOrIp, CommandHandler handler)
	{
		if (nameOrIp.IsEmpty())
			return false;

		switch (mode)
		{
			case BanMode.Character:
				if (!GameObjectManager.NormalizePlayerName(ref nameOrIp))
				{
					handler.SendSysMessage(CypherStrings.PlayerNotFound);

					return false;
				}

				break;
			case BanMode.IP:
				if (!IPAddress.TryParse(nameOrIp, out _))
					return false;

				break;
		}

		if (Global.WorldMgr.RemoveBanAccount(mode, nameOrIp))
			handler.SendSysMessage(CypherStrings.UnbanUnbanned, nameOrIp);
		else
			handler.SendSysMessage(CypherStrings.UnbanError, nameOrIp);

		return true;
	}
}