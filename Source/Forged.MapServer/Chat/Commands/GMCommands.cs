// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("gm")]
class GMCommands
{
	[Command("chat", RBACPermissions.CommandGmChat)]
	static bool HandleGMChatCommand(CommandHandler handler, bool? enableArg)
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

			if (enableArg.HasValue)
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
	static bool HandleGMFlyCommand(CommandHandler handler, bool enable)
	{
		var target = handler.SelectedPlayer;

		if (target == null)
			target = handler.Player;

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

	[Command("ingame", RBACPermissions.CommandGmIngame, true)]
	static bool HandleGMListIngameCommand(CommandHandler handler)
	{
		var first = true;
		var footer = false;

		foreach (var player in Global.ObjAccessor.GetPlayers())
		{
			var playerSec = player.Session.Security;

			if ((player.IsGameMaster ||
				(player.Session.HasPermission(RBACPermissions.CommandsAppearInGmList) &&
				playerSec <= (AccountTypes)GetDefaultValue("GM.InGMList.Level", (int)AccountTypes.Administrator))) &&
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

				if ((max + max2 + size) == 16)
					max2 = max - 1;

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

	[Command("list", RBACPermissions.CommandGmList, true)]
	static bool HandleGMListFullCommand(CommandHandler handler)
	{
		// Get the accounts with GM Level >0
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_GM_ACCOUNTS);
		stmt.AddValue(0, (byte)AccountTypes.Moderator);
		stmt.AddValue(1, Global.WorldMgr.Realm.Id.Index);
		var result = DB.Login.Query(stmt);

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

				if ((max + max2 + name.Length) == 16)
					max2 = max - 1;

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

	[Command("off", RBACPermissions.CommandGm)]
	static bool HandleGMOffCommand(CommandHandler handler)
	{
		handler.Player.SetGameMaster(false);
		handler.Player.UpdateTriggerVisibility();
		handler.Session.SendNotification(CypherStrings.GmOff);

		return true;
	}

	[Command("on", RBACPermissions.CommandGm)]
	static bool HandleGMOnCommand(CommandHandler handler)
	{
		handler.Player.SetGameMaster(true);
		handler.Player.UpdateTriggerVisibility();
		handler.Session.SendNotification(CypherStrings.GmOn);

		return true;
	}

	[Command("visible", RBACPermissions.CommandGmVisible)]
	static bool HandleGMVisibleCommand(CommandHandler handler, bool? visibleArg)
	{
		var _player = handler.Session.Player;

		if (!visibleArg.HasValue)
		{
			handler.SendSysMessage(CypherStrings.YouAre, _player.IsGMVisible ? Global.ObjectMgr.GetCypherString(CypherStrings.Visible) : Global.ObjectMgr.GetCypherString(CypherStrings.Invisible));

			return true;
		}

		uint VISUAL_AURA = 37800;

		if (visibleArg.Value)
		{
			if (_player.HasAura(VISUAL_AURA, ObjectGuid.Empty))
				_player.RemoveAura(VISUAL_AURA);

			_player.SetGMVisible(true);
			_player.UpdateObjectVisibility();
			handler.Session.SendNotification(CypherStrings.InvisibleVisible);
		}
		else
		{
			_player.AddAura(VISUAL_AURA, _player);
			_player.SetGMVisible(false);
			_player.UpdateObjectVisibility();
			handler.Session.SendNotification(CypherStrings.InvisibleInvisible);
		}

		return true;
	}
}