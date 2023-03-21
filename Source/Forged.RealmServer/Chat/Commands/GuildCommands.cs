// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Forged.RealmServer.Guilds;

namespace Forged.RealmServer.Chat;

[CommandGroup("guild")]
class GuildCommands
{
	[Command("create", RBACPermissions.CommandGuildCreate, true)]
	static bool HandleGuildCreateCommand(CommandHandler handler, StringArguments args)
	{
		if (args.Empty())
			return false;

		if (!handler.ExtractPlayerTarget(args[0] != '"' ? args : null, out var target))
			return false;

		var guildName = handler.ExtractQuotedArg(args.NextString());

		if (string.IsNullOrEmpty(guildName))
			return false;

		if (target.GuildId != 0)
		{
			handler.SendSysMessage(CypherStrings.PlayerInGuild);

			return false;
		}

		if (Global.GuildMgr.GetGuildByName(guildName))
		{
			handler.SendSysMessage(CypherStrings.GuildRenameAlreadyExists);

			return false;
		}

		if (Global.ObjectMgr.IsReservedName(guildName) || !ObjectManager.IsValidCharterName(guildName))
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		Guild guild = new();

		if (!guild.Create(target, guildName))
		{
			handler.SendSysMessage(CypherStrings.GuildNotCreated);

			return false;
		}

		Global.GuildMgr.AddGuild(guild);

		return true;
	}

	[Command("delete", RBACPermissions.CommandGuildDelete, true)]
	static bool HandleGuildDeleteCommand(CommandHandler handler, QuotedString guildName)
	{
		if (guildName.IsEmpty())
			return false;

		var guild = Global.GuildMgr.GetGuildByName(guildName);

		if (guild == null)
			return false;

		guild.Disband();

		return true;
	}

	[Command("invite", RBACPermissions.CommandGuildInvite, true)]
	static bool HandleGuildInviteCommand(CommandHandler handler, PlayerIdentifier targetIdentifier, QuotedString guildName)
	{
		if (targetIdentifier == null)
			targetIdentifier = PlayerIdentifier.FromTargetOrSelf(handler);

		if (targetIdentifier == null)
			return false;

		if (guildName.IsEmpty())
			return false;

		var targetGuild = Global.GuildMgr.GetGuildByName(guildName);

		if (targetGuild == null)
			return false;

		targetGuild.AddMember(null, targetIdentifier.GetGUID());

		return true;
	}

	[Command("uninvite", RBACPermissions.CommandGuildUninvite, true)]
	static bool HandleGuildUninviteCommand(CommandHandler handler, PlayerIdentifier targetIdentifier, QuotedString guildName)
	{
		if (targetIdentifier == null)
			targetIdentifier = PlayerIdentifier.FromTargetOrSelf(handler);

		if (targetIdentifier == null)
			return false;

		var guildId = targetIdentifier.IsConnected() ? targetIdentifier.GetConnectedPlayer().GuildId : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(targetIdentifier.GetGUID());

		if (guildId == 0)
			return false;

		var targetGuild = Global.GuildMgr.GetGuildById(guildId);

		if (targetGuild == null)
			return false;

		targetGuild.DeleteMember(null, targetIdentifier.GetGUID(), false, true, true);

		return true;
	}

	[Command("rank", RBACPermissions.CommandGuildRank, true)]
	static bool HandleGuildRankCommand(CommandHandler handler, PlayerIdentifier player, byte rank)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		var guildId = player.IsConnected() ? player.GetConnectedPlayer().GuildId : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(player.GetGUID());

		if (guildId == 0)
			return false;

		var targetGuild = Global.GuildMgr.GetGuildById(guildId);

		if (!targetGuild)
			return false;

		return targetGuild.ChangeMemberRank(null, player.GetGUID(), (GuildRankId)rank);
	}

	[Command("rename", RBACPermissions.CommandGuildRename, true)]
	static bool HandleGuildRenameCommand(CommandHandler handler, QuotedString oldGuildName, QuotedString newGuildName)
	{
		if (oldGuildName.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		if (newGuildName.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.InsertGuildName);

			return false;
		}

		var guild = Global.GuildMgr.GetGuildByName(oldGuildName);

		if (!guild)
		{
			handler.SendSysMessage(CypherStrings.CommandCouldnotfind, oldGuildName);

			return false;
		}

		if (Global.GuildMgr.GetGuildByName(newGuildName))
		{
			handler.SendSysMessage(CypherStrings.GuildRenameAlreadyExists, newGuildName);

			return false;
		}

		if (!guild.SetName(newGuildName))
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		handler.SendSysMessage(CypherStrings.GuildRenameDone, oldGuildName, newGuildName);

		return true;
	}

	[Command("info", RBACPermissions.CommandGuildInfo, true)]
	static bool HandleGuildInfoCommand(CommandHandler handler, StringArguments args)
	{
		Guild guild = null;
		var target = handler.SelectedPlayerOrSelf;

		if (!args.Empty() && args[0] != '\0')
		{
			if (char.IsDigit(args[0]))
				guild = Global.GuildMgr.GetGuildById(args.NextUInt64());
			else
				guild = Global.GuildMgr.GetGuildByName(args.NextString());
		}
		else if (target)
		{
			guild = target.Guild;
		}

		if (!guild)
			return false;

		// Display Guild Information
		handler.SendSysMessage(CypherStrings.GuildInfoName, guild.GetName(), guild.GetId()); // Guild Id + Name

		if (Global.CharacterCacheStorage.GetCharacterNameByGuid(guild.GetLeaderGUID(), out var guildMasterName))
			handler.SendSysMessage(CypherStrings.GuildInfoGuildMaster, guildMasterName, guild.GetLeaderGUID().ToString()); // Guild Master

		// Format creation date

		var createdDateTime = Time.UnixTimeToDateTime(guild.GetCreatedDate());
		handler.SendSysMessage(CypherStrings.GuildInfoCreationDate, createdDateTime.ToLongDateString()); // Creation Date
		handler.SendSysMessage(CypherStrings.GuildInfoMemberCount, guild.GetMembersCount());             // Number of Members
		handler.SendSysMessage(CypherStrings.GuildInfoBankGold, guild.GetBankMoney() / 100 / 100);       // Bank Gold (in gold coins)
		handler.SendSysMessage(CypherStrings.GuildInfoLevel, guild.GetLevel());                          // Level
		handler.SendSysMessage(CypherStrings.GuildInfoMotd, guild.GetMOTD());                            // Message of the Day
		handler.SendSysMessage(CypherStrings.GuildInfoExtraInfo, guild.GetInfo());                       // Extra Information

		return true;
	}
}