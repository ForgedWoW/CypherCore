﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

internal class MessageCommands
{
    [CommandNonGroup("announce", RBACPermissions.CommandAnnounce, true)]
    private static bool HandleAnnounceCommand(CommandHandler handler, Tail message)
    {
        if (message.IsEmpty())
            return false;

        handler.WorldManager.SendServerMessage(ServerMessageType.String, handler.GetParsedString(CypherStrings.Systemmessage, message));

        return true;
    }

    [CommandNonGroup("gmannounce", RBACPermissions.CommandGmannounce, true)]
    private static bool HandleGMAnnounceCommand(CommandHandler handler, Tail message)
    {
        if (message.IsEmpty())
            return false;

        handler.WorldManager.SendGMText(CypherStrings.GmBroadcast, message);

        return true;
    }

    [CommandNonGroup("gmnameannounce", RBACPermissions.CommandGmnameannounce, true)]
    private static bool HandleGMNameAnnounceCommand(CommandHandler handler, Tail message)
    {
        if (message.IsEmpty())
            return false;

        var name = "Console";
        var session = handler.Session;

        if (session)
            name = session.Player.GetName();

        handler.WorldManager.SendGMText(CypherStrings.AnnounceColor, name, message);

        return true;
    }

    [CommandNonGroup("gmnotify", RBACPermissions.CommandGmnotify, true)]
    private static bool HandleGMNotifyCommand(CommandHandler handler, Tail message)
    {
        if (message.IsEmpty())
            return false;

        var str = handler.GetCypherString(CypherStrings.GmNotify);
        str += message;

        handler.WorldManager.SendGlobalGMMessage(new PrintNotification(str));

        return true;
    }

    [CommandNonGroup("nameannounce", RBACPermissions.CommandNameannounce, true)]
    private static bool HandleNameAnnounceCommand(CommandHandler handler, Tail message)
    {
        if (message.IsEmpty())
            return false;

        var name = "Console";
        var session = handler.Session;

        if (session)
            name = session.Player.GetName();

        handler.WorldManager.SendWorldText(CypherStrings.AnnounceColor, name, message);

        return true;
    }

    [CommandNonGroup("notify", RBACPermissions.CommandNotify, true)]
    private static bool HandleNotifyCommand(CommandHandler handler, Tail message)
    {
        if (message.IsEmpty())
            return false;

        var str = handler.GetCypherString(CypherStrings.GlobalNotify);
        str += message;

        handler.WorldManager.SendGlobalMessage(new PrintNotification(str));

        return true;
    }

    [CommandNonGroup("whispers", RBACPermissions.CommandWhispers)]
    private static bool HandleWhispersCommand(CommandHandler handler, bool? operationArg, [OptionalArg] string playerNameArg)
    {
        if (!operationArg.HasValue)
        {
            handler.SendSysMessage(CypherStrings.CommandWhisperaccepting, handler.Session.Player.IsAcceptWhispers ? handler.GetCypherString(CypherStrings.On) : handler.GetCypherString(CypherStrings.Off));

            return true;
        }

        if (operationArg.HasValue)
        {
            handler.Session.Player.SetAcceptWhispers(true);
            handler.SendSysMessage(CypherStrings.CommandWhisperon);

            return true;
        }
        else
        {
            // Remove all players from the Gamemaster's whisper whitelist
            handler.Session. // Remove all players from the Gamemaster's whisper whitelist
                    Player.ClearWhisperWhiteList();

            handler.Session.Player.SetAcceptWhispers(false);
            handler.SendSysMessage(CypherStrings.CommandWhisperoff);

            return true;
        }

        //todo fix me
        /*if (operationArg->holds_alternative < EXACT_SEQUENCE("remove") > ())
        {
            if (!playerNameArg)
                return false;
      
            if (normalizePlayerName(*playerNameArg))
            {
                if (Player * player = ObjectAccessor::FindPlayerByName(*playerNameArg))
                {
                    handler->GetSession()->GetPlayer()->RemoveFromWhisperWhiteList(player->GetGUID());
                    handler->PSendSysMessage(LANG_COMMAND_WHISPEROFFPLAYER, playerNameArg->c_str());
                    return true;
                }
                else
                {
                    handler->PSendSysMessage(LANG_PLAYER_NOT_FOUND, playerNameArg->c_str());
                    handler->SetSentErrorMessage(true);
                    return false;
                }
            }
        }
        handler.SendSysMessage(CypherStrings.UseBol);
        return false;*/
    }
}