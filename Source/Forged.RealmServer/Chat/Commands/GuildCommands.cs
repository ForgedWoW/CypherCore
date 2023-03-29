// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;
using Forged.RealmServer.Guilds;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Cache;

namespace Forged.RealmServer.Chat;

[CommandGroup("guild")]
public class GuildCommands
{
    private readonly GuildManager _guildManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ClassFactory _classFactory;
    private readonly CharacterCache _characterCache;

    public GuildCommands(ClassFactory classFactory)
    {
        _classFactory = classFactory;
        _characterCache = _classFactory.Resolve<CharacterCache>();
        _guildManager = _classFactory.Resolve<GuildManager>();
        _gameObjectManager = _classFactory.Resolve<GameObjectManager>();
    }

    [Command("create", RBACPermissions.CommandGuildCreate, true)]
	bool HandleGuildCreateCommand(CommandHandler handler, StringArguments args)
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

		if (_guildManager.GetGuildByName(guildName))
		{
			handler.SendSysMessage(CypherStrings.GuildRenameAlreadyExists);

			return false;
		}

		if (_gameObjectManager.IsReservedName(guildName) || !GameObjectManager.IsValidCharterName(guildName))
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		Guild guild = new(_classFactory);

		if (!guild.Create(target, guildName))
		{
			handler.SendSysMessage(CypherStrings.GuildNotCreated);

			return false;
		}

		_guildManager.AddGuild(guild);

		return true;
	}

	[Command("delete", RBACPermissions.CommandGuildDelete, true)]
	bool HandleGuildDeleteCommand(CommandHandler handler, QuotedString guildName)
	{
		if (guildName.IsEmpty())
			return false;

		var guild = _guildManager.GetGuildByName(guildName);

		if (guild == null)
			return false;

		guild.Disband();

		return true;
	}

	[Command("invite", RBACPermissions.CommandGuildInvite, true)]
	bool HandleGuildInviteCommand(CommandHandler handler, PlayerIdentifier targetIdentifier, QuotedString guildName)
	{
		if (targetIdentifier == null)
			targetIdentifier = PlayerIdentifier.FromTargetOrSelf(handler);

		if (targetIdentifier == null)
			return false;

		if (guildName.IsEmpty())
			return false;

		var targetGuild = _guildManager.GetGuildByName(guildName);

		if (targetGuild == null)
			return false;

		targetGuild.AddMember(null, targetIdentifier.GetGUID());

		return true;
	}

	[Command("uninvite", RBACPermissions.CommandGuildUninvite, true)]
	bool HandleGuildUninviteCommand(CommandHandler handler, PlayerIdentifier targetIdentifier, QuotedString guildName)
	{
		if (targetIdentifier == null)
			targetIdentifier = PlayerIdentifier.FromTargetOrSelf(handler);

		if (targetIdentifier == null)
			return false;

		var guildId = targetIdentifier.IsConnected() ? targetIdentifier.GetConnectedPlayer().GuildId : _characterCache.GetCharacterGuildIdByGuid(targetIdentifier.GetGUID());

		if (guildId == 0)
			return false;

		var targetGuild = _guildManager.GetGuildById(guildId);

		if (targetGuild == null)
			return false;

		targetGuild.DeleteMember(null, targetIdentifier.GetGUID(), false, true, true);

		return true;
	}

	[Command("rank", RBACPermissions.CommandGuildRank, true)]
	bool HandleGuildRankCommand(CommandHandler handler, PlayerIdentifier player, byte rank)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		var guildId = player.IsConnected() ? player.GetConnectedPlayer().GuildId : _characterCache.GetCharacterGuildIdByGuid(player.GetGUID());

		if (guildId == 0)
			return false;

		var targetGuild = _guildManager.GetGuildById(guildId);

		if (!targetGuild)
			return false;

		return targetGuild.ChangeMemberRank(null, player.GetGUID(), (GuildRankId)rank);
	}

	[Command("rename", RBACPermissions.CommandGuildRename, true)]
	bool HandleGuildRenameCommand(CommandHandler handler, QuotedString oldGuildName, QuotedString newGuildName)
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

		var guild = _guildManager.GetGuildByName(oldGuildName);

		if (!guild)
		{
			handler.SendSysMessage(CypherStrings.CommandCouldnotfind, oldGuildName);

			return false;
		}

		if (_guildManager.GetGuildByName(newGuildName))
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
	bool HandleGuildInfoCommand(CommandHandler handler, StringArguments args)
	{
		Guild guild = null;
		var target = handler.SelectedPlayerOrSelf;

		if (!args.Empty() && args[0] != '\0')
		{
			if (char.IsDigit(args[0]))
				guild = _guildManager.GetGuildById(args.NextUInt64());
			else
				guild = _guildManager.GetGuildByName(args.NextString());
		}
		else if (target)
		{
			guild = target.Guild;
		}

		if (!guild)
			return false;

		// Display Guild Information
		handler.SendSysMessage(CypherStrings.GuildInfoName, guild.GetName(), guild.GetId()); // Guild Id + Name

		if (_characterCache.GetCharacterNameByGuid(guild.GetLeaderGUID(), out var guildMasterName))
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