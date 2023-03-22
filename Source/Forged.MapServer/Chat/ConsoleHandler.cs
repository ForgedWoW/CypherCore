﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Chat;

public class ConsoleHandler : CommandHandler
{
	public override string NameLink => GetCypherString(CypherStrings.ConsoleCommand);

	public override Locale SessionDbcLocale => Global.WorldMgr.DefaultDbcLocale;

	public override byte SessionDbLocaleIndex => (byte)Global.WorldMgr.DefaultDbcLocale;

	public override bool IsAvailable(ChatCommandNode cmd)
	{
		return cmd.Permission.AllowConsole;
	}

	public override bool HasPermission(RBACPermissions permission)
	{
		return true;
	}

	public override void SendSysMessage(string str, bool escapeCharacters)
	{
		SetSentErrorMessage(true);
		Log.outInfo(LogFilter.Server, str);
	}

	public override bool ParseCommands(string str)
	{
		if (str.IsEmpty())
			return false;

		// Console allows using commands both with and without leading indicator
		if (str[0] == '.' || str[0] == '!')
			str = str.Substring(1);

		return _ParseCommands(str);
	}

	public override bool NeedReportToTarget(Player chr)
	{
		return true;
	}
}