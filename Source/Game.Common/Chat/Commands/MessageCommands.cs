// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Chat;
using Game.Common.Networking.Packets.Chat;

namespace Game.Common.Chat.Commands;

class MessageCommands
{
	[CommandNonGroup("nameannounce", RBACPermissions.CommandNameannounce, true)]
	static bool HandleNameAnnounceCommand(CommandHandler handler, Tail message)
	{
		if (message.IsEmpty())
			return false;

		var name = "Console";
		var session = handler.Session;

		if (session)
			name = session.Player.GetName();

		Global.WorldMgr.SendWorldText(CypherStrings.AnnounceColor, name, message);

		return true;
	}

	[CommandNonGroup("gmnameannounce", RBACPermissions.CommandGmnameannounce, true)]
	static bool HandleGMNameAnnounceCommand(CommandHandler handler, Tail message)
	{
		if (message.IsEmpty())
			return false;

		var name = "Console";
		var session = handler.Session;

		if (session)
			name = session.Player.GetName();

		Global.WorldMgr.SendGMText(CypherStrings.AnnounceColor, name, message);

		return true;
	}

	[CommandNonGroup("announce", RBACPermissions.CommandAnnounce, true)]
	static bool HandleAnnounceCommand(CommandHandler handler, Tail message)
	{
		if (message.IsEmpty())
			return false;

		Global.WorldMgr.SendServerMessage(ServerMessageType.String, handler.GetParsedString(CypherStrings.Systemmessage, message));

		return true;
	}

	[CommandNonGroup("gmannounce", RBACPermissions.CommandGmannounce, true)]
	static bool HandleGMAnnounceCommand(CommandHandler handler, Tail message)
	{
		if (message.IsEmpty())
			return false;

		Global.WorldMgr.SendGMText(CypherStrings.GmBroadcast, message);

		return true;
	}

	[CommandNonGroup("notify", RBACPermissions.CommandNotify, true)]
	static bool HandleNotifyCommand(CommandHandler handler, Tail message)
	{
		if (message.IsEmpty())
			return false;

		var str = handler.GetCypherString(CypherStrings.GlobalNotify);
		str += message;

		Global.WorldMgr.SendGlobalMessage(new PrintNotification(str));

		return true;
	}

	[CommandNonGroup("gmnotify", RBACPermissions.CommandGmnotify, true)]
	static bool HandleGMNotifyCommand(CommandHandler handler, Tail message)
	{
		if (message.IsEmpty())
			return false;

		var str = handler.GetCypherString(CypherStrings.GmNotify);
		str += message;

		Global.WorldMgr.SendGlobalGMMessage(new PrintNotification(str));

		return true;
	}

	[CommandNonGroup("whispers", RBACPermissions.CommandWhispers)]
	static bool HandleWhispersCommand(CommandHandler handler, bool? operationArg, [OptionalArg] string playerNameArg)
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
