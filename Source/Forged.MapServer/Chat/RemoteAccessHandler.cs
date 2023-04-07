// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat;

public class RemoteAccessHandler : CommandHandler
{
    private readonly Action<string> _reportToRA;
    private readonly WorldManager _worldManager;

    public RemoteAccessHandler(Action<string> reportToRA, WorldManager worldManager) : base()
    {
        _reportToRA = reportToRA;
        _worldManager = worldManager;
    }

    public override string NameLink => GetCypherString(CypherStrings.ConsoleCommand);

    public override Locale SessionDbcLocale => _worldManager.DefaultDbcLocale;

    public override byte SessionDbLocaleIndex => (byte)_worldManager.DefaultDbcLocale;
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

    public override void SendSysMessage(string str, bool escapeCharacters = false)
    {
        SetSentErrorMessage(true);
        _reportToRA(str);
    }
}