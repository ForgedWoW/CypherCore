// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Chat;

public class RemoteAccessHandler : CommandHandler
{
    private readonly Action<string> _reportToRA;

    public RemoteAccessHandler(Action<string> reportToRA) : base()
    {
        _reportToRA = reportToRA;
    }

    public override string NameLink => GetCypherString(CypherStrings.ConsoleCommand);

    public override Locale SessionDbcLocale => Global.WorldMgr.DefaultDbcLocale;

    public override byte SessionDbLocaleIndex => (byte)Global.WorldMgr.DefaultDbcLocale;
    public override bool HasPermission(RBACPermissions permission)
    {
        return true;
    }

    public override bool IsAvailable(ChatCommandNode cmd)
    {
        return cmd.Permission.AllowConsole;
    }
    public override bool NeedReportToTarget(Player chr)
    {
        return true;
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

    public override void SendSysMessage(string str, bool escapeCharacters)
    {
        SetSentErrorMessage(true);
        _reportToRA(str);
    }
}