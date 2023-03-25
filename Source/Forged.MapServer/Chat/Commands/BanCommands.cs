// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using Forged.MapServer.Globals;
using Forged.MapServer.Server;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("ban")]
class BanCommands
{
	[Command("account", RBACPermissions.CommandBanAccount, true)]
	static bool HandleBanAccountCommand(CommandHandler handler, string playerName, uint duration, string reason)
	{
		return HandleBanHelper(BanMode.Account, playerName, duration, reason, handler);
	}

	[Command("character", RBACPermissions.CommandBanCharacter, true)]
	static bool HandleBanCharacterCommand(CommandHandler handler, string playerName, uint duration, string reason)
	{
		if (playerName.IsEmpty())
			return false;

		if (duration == 0)
			return false;

		if (reason.IsEmpty())
			return false;

		if (!ObjectManager.NormalizePlayerName(ref playerName))
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var author = handler.Session != null ? handler.Session.PlayerName : "Server";

		switch (Global.WorldMgr.BanCharacter(playerName, duration, reason, author))
		{
			case BanReturn.Success:
			{
				if (duration > 0)
				{
					if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
						Global.WorldMgr.SendWorldText(CypherStrings.BanCharacterYoubannedmessageWorld, author, playerName, global::Time.secsToTimeString(duration, TimeFormat.ShortText), reason);
					else
						handler.SendSysMessage(CypherStrings.BanYoubanned, playerName, global::Time.secsToTimeString(duration, TimeFormat.ShortText), reason);
				}
				else
				{
					if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
						Global.WorldMgr.SendWorldText(CypherStrings.BanCharacterYoupermbannedmessageWorld, author, playerName, reason);
					else
						handler.SendSysMessage(CypherStrings.BanYoupermbanned, playerName, reason);
				}

				break;
			}
			case BanReturn.Notfound:
			{
				handler.SendSysMessage(CypherStrings.BanNotfound, "character", playerName);

				return false;
			}
			default:
				break;
		}

		return true;
	}

	[Command("playeraccount", RBACPermissions.CommandBanPlayeraccount, true)]
	static bool HandleBanAccountByCharCommand(CommandHandler handler, string playerName, uint duration, string reason)
	{
		return HandleBanHelper(BanMode.Character, playerName, duration, reason, handler);
	}

	[Command("ip", RBACPermissions.CommandBanIp, true)]
	static bool HandleBanIPCommand(CommandHandler handler, string ipAddress, uint duration, string reason)
	{
		return HandleBanHelper(BanMode.IP, ipAddress, duration, reason, handler);
	}

	static bool HandleBanHelper(BanMode mode, string nameOrIP, uint duration, string reason, CommandHandler handler)
	{
		if (nameOrIP.IsEmpty())
			return false;

		if (reason.IsEmpty())
			return false;

		switch (mode)
		{
			case BanMode.Character:
				if (!ObjectManager.NormalizePlayerName(ref nameOrIP))
				{
					handler.SendSysMessage(CypherStrings.PlayerNotFound);

					return false;
				}

				break;
			case BanMode.IP:
				if (!IPAddress.TryParse(nameOrIP, out _))
					return false;

				break;
		}

		var author = handler.Session ? handler.Session.PlayerName : "Server";

		switch (Global.WorldMgr.BanAccount(mode, nameOrIP, duration, reason, author))
		{
			case BanReturn.Success:
				if (duration > 0)
				{
					if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
						Global.WorldMgr.SendWorldText(CypherStrings.BanAccountYoubannedmessageWorld, author, nameOrIP, global::Time.secsToTimeString(duration), reason);
					else
						handler.SendSysMessage(CypherStrings.BanYoubanned, nameOrIP, global::Time.secsToTimeString(duration, TimeFormat.ShortText), reason);
				}
				else
				{
					if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
						Global.WorldMgr.SendWorldText(CypherStrings.BanAccountYoupermbannedmessageWorld, author, nameOrIP, reason);
					else
						handler.SendSysMessage(CypherStrings.BanYoupermbanned, nameOrIP, reason);
				}

				break;
			case BanReturn.SyntaxError:
				return false;
			case BanReturn.Notfound:
				switch (mode)
				{
					default:
						handler.SendSysMessage(CypherStrings.BanNotfound, "account", nameOrIP);

						break;
					case BanMode.Character:
						handler.SendSysMessage(CypherStrings.BanNotfound, "character", nameOrIP);

						break;
					case BanMode.IP:
						handler.SendSysMessage(CypherStrings.BanNotfound, "ip", nameOrIP);

						break;
				}

				return false;
			case BanReturn.Exists:
				handler.SendSysMessage(CypherStrings.BanExists);

				break;
		}

		return true;
	}
}