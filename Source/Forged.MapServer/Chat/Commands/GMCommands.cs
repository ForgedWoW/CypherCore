// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("gm")]
internal class GMCommands
{
    [Command("chat", RBACPermissions.CommandGmChat)]
    private static bool HandleGMChatCommand(CommandHandler handler, bool? enableArg)
    {
        var session = handler.Session;

        if (session != null)
        {
            if (!enableArg.HasValue)
            {
                if (session.HasPermission(RBACPermissions.ChatUseStaffBadge) && session.Player.IsGMChat)
                    session.SendNotification(CypherStrings.GmChatOn);
                else
                    session.SendNotification(CypherStrings.GmChatOff);

                return true;
            }

            if (enableArg == true)
            {
                session.Player.SetGMChat(true);
                session.SendNotification(CypherStrings.GmChatOn);
            }
            else
            {
                session.Player.SetGMChat(false);
                session.SendNotification(CypherStrings.GmChatOff);
            }

            return true;
        }

        handler.SendSysMessage(CypherStrings.UseBol);

        return false;
    }

    [Command("fly", RBACPermissions.CommandGmFly)]
    private static bool HandleGMFlyCommand(CommandHandler handler, bool enable)
    {
        var target = handler.SelectedPlayer ?? handler.Player;

        if (enable)
        {
            target.SetCanFly(true);
            target.SetCanTransitionBetweenSwimAndFly(true);
        }
        else
        {
            target.SetCanFly(false);
            target.SetCanTransitionBetweenSwimAndFly(false);
        }

        handler.SendSysMessage(CypherStrings.CommandFlymodeStatus, handler.GetNameLink(target), enable ? "on" : "off");

        return true;
    }

    [Command("list", RBACPermissions.CommandGmList, true)]
    private static bool HandleGMListFullCommand(CommandHandler handler)
    {
        // Get the accounts with GM Level >0
        var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
        var stmt = loginDB.GetPreparedStatement(LoginStatements.SEL_GM_ACCOUNTS);
        stmt.AddValue(0, (byte)AccountTypes.Moderator);
        stmt.AddValue(1, WorldManager.Realm.Id.Index);
        var result = loginDB.Query(stmt);

        if (!result.IsEmpty())
        {
            handler.SendSysMessage(CypherStrings.Gmlist);
            handler.SendSysMessage("========================");

            // Cycle through them. Display username and GM level
            do
            {
                var name = result.Read<string>(0);
                var security = result.Read<byte>(1);
                var max = (16 - name.Length) / 2;
                var max2 = max;

                max2 = (max + max2 + name.Length) switch
                {
                    16 => max - 1,
                    _  => max2
                };

                var padding = "";

                if (handler.Session != null)
                    handler.SendSysMessage("|    {0} GMLevel {1}", name, security);
                else
                    handler.SendSysMessage("|{0}{1}{2}|   {3}  |", padding.PadRight(max), name, padding.PadRight(max2), security);
            } while (result.NextRow());

            handler.SendSysMessage("========================");
        }
        else
        {
            handler.SendSysMessage(CypherStrings.GmlistEmpty);
        }

        return true;
    }

    [Command("ingame", RBACPermissions.CommandGmIngame, true)]
    private static bool HandleGMListIngameCommand(CommandHandler handler)
    {
        var first = true;
        var footer = false;

        foreach (var player in handler.ObjectAccessor.GetPlayers())
        {
            var playerSec = player.Session.Security;

            if ((player.IsGameMaster ||
                 (player.Session.HasPermission(RBACPermissions.CommandsAppearInGmList) &&
                  playerSec <= (AccountTypes)handler.Configuration.GetDefaultValue("GM.InGMList.Level", (int)AccountTypes.Administrator))) &&
                (handler.Session == null || player.IsVisibleGloballyFor(handler.Session.Player)))
            {
                if (first)
                {
                    first = false;
                    footer = true;
                    handler.SendSysMessage(CypherStrings.GmsOnSrv);
                    handler.SendSysMessage("========================");
                }

                var size = player.GetName().Length;
                var security = (byte)playerSec;
                var max = ((16 - size) / 2);
                var max2 = max;

                max2 = (max + max2 + size) switch
                {
                    16 => max - 1,
                    _  => max2
                };

                if (handler.Session != null)
                    handler.SendSysMessage("|    {0} GMLevel {1}", player.GetName(), security);
                else
                    handler.SendSysMessage("|{0}{1}{2}|   {3}  |", max, " ", player.GetName(), max2, " ", security);
            }
        }

        if (footer)
            handler.SendSysMessage("========================");

        if (first)
            handler.SendSysMessage(CypherStrings.GmsNotLogged);

        return true;
    }

    [Command("off", RBACPermissions.CommandGm)]
    private static bool HandleGMOffCommand(CommandHandler handler)
    {
        handler.Player.SetGameMaster(false);
        handler.Player.UpdateTriggerVisibility();
        handler.Session.SendNotification(CypherStrings.GmOff);

        return true;
    }

    [Command("on", RBACPermissions.CommandGm)]
    private static bool HandleGMOnCommand(CommandHandler handler)
    {
        handler.Player.SetGameMaster(true);
        handler.Player.UpdateTriggerVisibility();
        handler.Session.SendNotification(CypherStrings.GmOn);

        return true;
    }

    [Command("visible", RBACPermissions.CommandGmVisible)]
    private static bool HandleGMVisibleCommand(CommandHandler handler, bool? visibleArg)
    {
        var player = handler.Session.Player;

        if (!visibleArg.HasValue)
        {
            handler.SendSysMessage(CypherStrings.YouAre, player.IsGMVisible ? handler.ObjectManager.GetCypherString(CypherStrings.Visible) : handler.ObjectManager.GetCypherString(CypherStrings.Invisible));

            return true;
        }

        uint visualAura = 37800;

        if (visibleArg.Value)
        {
            if (player.HasAura(visualAura, ObjectGuid.Empty))
                player.RemoveAura(visualAura);

            player.SetGMVisible(true);
            player.UpdateObjectVisibility();
            handler.Session.SendNotification(CypherStrings.InvisibleVisible);
        }
        else
        {
            player.AddAura(visualAura, player);
            player.SetGMVisible(false);
            player.UpdateObjectVisibility();
            handler.Session.SendNotification(CypherStrings.InvisibleInvisible);
        }

        return true;
    }
}