﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Groups;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("lfg")]
internal class LFGCommands
{
    [Command("player", RBACPermissions.CommandLfgPlayer, true)]
    private static bool HandleLfgPlayerInfoCommand(CommandHandler handler, PlayerIdentifier player)
    {
        if (player == null)
            player = PlayerIdentifier.FromTargetOrSelf(handler);

        if (player == null)
            return false;

        var target = player.GetConnectedPlayer();

        if (target != null)
        {
            PrintPlayerInfo(handler, target);

            return true;
        }

        return false;
    }

    [Command("group", RBACPermissions.CommandLfgGroup, true)]
    private static bool HandleLfgGroupInfoCommand(CommandHandler handler, PlayerIdentifier player)
    {
        if (player == null)
            player = PlayerIdentifier.FromTargetOrSelf(handler);

        if (player == null)
            return false;

        PlayerGroup groupTarget = null;
        var target = player.GetConnectedPlayer();

        if (target != null)
        {
            groupTarget = target.Group;
        }
        else
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
            stmt.AddValue(0, player.GetGUID().Counter);
            var resultGroup = DB.Characters.Query(stmt);

            if (!resultGroup.IsEmpty())
                groupTarget = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
        }

        if (!groupTarget)
        {
            handler.SendSysMessage(CypherStrings.LfgNotInGroup, player.GetName());

            return false;
        }

        var guid = groupTarget.GUID;
        handler.SendSysMessage(CypherStrings.LfgGroupInfo, groupTarget.IsLFGGroup, Global.LFGMgr.GetState(guid), Global.LFGMgr.GetDungeon(guid));

        foreach (var slot in groupTarget.MemberSlots)
        {
            var p = Global.ObjAccessor.FindPlayer(slot.Guid);

            if (p)
                PrintPlayerInfo(handler, p);
            else
                handler.SendSysMessage("{0} is offline.", slot.Name);
        }

        return true;
    }

    [Command("options", RBACPermissions.CommandLfgOptions, true)]
    private static bool HandleLfgOptionsCommand(CommandHandler handler, uint? optionsArg)
    {
        if (optionsArg.HasValue)
        {
            Global.LFGMgr.SetOptions((LfgOptions)optionsArg.Value);
            handler.SendSysMessage(CypherStrings.LfgOptionsChanged);
        }

        handler.SendSysMessage(CypherStrings.LfgOptions, Global.LFGMgr.GetOptions());

        return true;
    }

    [Command("queue", RBACPermissions.CommandLfgQueue, true)]
    private static bool HandleLfgQueueInfoCommand(CommandHandler handler, string full)
    {
        handler.SendSysMessage(Global.LFGMgr.DumpQueueInfo(!full.IsEmpty()));

        return true;
    }

    [Command("clean", RBACPermissions.CommandLfgClean, true)]
    private static bool HandleLfgCleanCommand(CommandHandler handler)
    {
        handler.SendSysMessage(CypherStrings.LfgClean);
        Global.LFGMgr.Clean();

        return true;
    }

    private static void PrintPlayerInfo(CommandHandler handler, Player player)
    {
        if (!player)
            return;

        var guid = player.GUID;
        var dungeons = Global.LFGMgr.GetSelectedDungeons(guid);

        handler.SendSysMessage(CypherStrings.LfgPlayerInfo,
                               player.GetName(),
                               Global.LFGMgr.GetState(guid),
                               dungeons.Count,
                               LFGQueue.ConcatenateDungeons(dungeons),
                               LFGQueue.GetRolesString(Global.LFGMgr.GetRoles(guid)));
    }
}