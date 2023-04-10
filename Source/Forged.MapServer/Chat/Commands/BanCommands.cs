// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using Forged.MapServer.Globals;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("ban")]
internal class BanCommands
{
    [Command("playeraccount", RBACPermissions.CommandBanPlayeraccount, true)]
    private static bool HandleBanAccountByCharCommand(CommandHandler handler, string playerName, uint duration, string reason)
    {
        return HandleBanHelper(BanMode.Character, playerName, duration, reason, handler);
    }

    [Command("account", RBACPermissions.CommandBanAccount, true)]
    private static bool HandleBanAccountCommand(CommandHandler handler, string playerName, uint duration, string reason)
    {
        return HandleBanHelper(BanMode.Account, playerName, duration, reason, handler);
    }

    [Command("character", RBACPermissions.CommandBanCharacter, true)]
    private static bool HandleBanCharacterCommand(CommandHandler handler, string playerName, uint duration, string reason)
    {
        if (playerName.IsEmpty())
            return false;

        if (duration == 0)
            return false;

        if (reason.IsEmpty())
            return false;

        if (!GameObjectManager.NormalizePlayerName(ref playerName))
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var author = handler.Session != null ? handler.Session.PlayerName : "Server";
        var worldManager = handler.ClassFactory.Resolve<WorldManager>();
        var cfg = handler.ClassFactory.Resolve<IConfiguration>();

        switch (worldManager.BanCharacter(playerName, duration, reason, author))
        {
            case BanReturn.Success:
            {
                if (duration > 0)
                {
                    if (cfg.GetDefaultValue("ShowBanInWorld", false))
                        worldManager.SendWorldText(CypherStrings.BanCharacterYoubannedmessageWorld, author, playerName, Time.SecsToTimeString(duration, TimeFormat.ShortText), reason);
                    else
                        handler.SendSysMessage(CypherStrings.BanYoubanned, playerName, Time.SecsToTimeString(duration, TimeFormat.ShortText), reason);
                }
                else
                {
                    if (cfg.GetDefaultValue("ShowBanInWorld", false))
                        worldManager.SendWorldText(CypherStrings.BanCharacterYoupermbannedmessageWorld, author, playerName, reason);
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
        }

        return true;
    }

    private static bool HandleBanHelper(BanMode mode, string nameOrIP, uint duration, string reason, CommandHandler handler)
    {
        if (nameOrIP.IsEmpty())
            return false;

        if (reason.IsEmpty())
            return false;

        switch (mode)
        {
            case BanMode.Character:
                if (!GameObjectManager.NormalizePlayerName(ref nameOrIP))
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
        var worldManager = handler.ClassFactory.Resolve<WorldManager>();
        var cfg = handler.ClassFactory.Resolve<IConfiguration>();

        switch (worldManager.BanAccount(mode, nameOrIP, duration, reason, author))
        {
            case BanReturn.Success:
                if (duration > 0)
                {
                    if (cfg.GetDefaultValue("ShowBanInWorld", false))
                        worldManager.SendWorldText(CypherStrings.BanAccountYoubannedmessageWorld, author, nameOrIP, Time.SecsToTimeString(duration), reason);
                    else
                        handler.SendSysMessage(CypherStrings.BanYoubanned, nameOrIP, Time.SecsToTimeString(duration, TimeFormat.ShortText), reason);
                }
                else
                {
                    if (cfg.GetDefaultValue("ShowBanInWorld", false))
                        worldManager.SendWorldText(CypherStrings.BanAccountYoupermbannedmessageWorld, author, nameOrIP, reason);
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

    [Command("ip", RBACPermissions.CommandBanIp, true)]
    private static bool HandleBanIPCommand(CommandHandler handler, string ipAddress, uint duration, string reason)
    {
        return HandleBanHelper(BanMode.IP, ipAddress, duration, reason, handler);
    }
}