// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Arenas;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("arena")]
internal class ArenaCommands
{
	[Command("create", RBACPermissions.CommandArenaCreate, true)]
    private static bool HandleArenaCreateCommand(CommandHandler handler, PlayerIdentifier captain, string name, ArenaTypes type)
	{
		if (Global.ArenaTeamMgr.GetArenaTeamByName(name) != null)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, name);

			return false;
		}

		if (captain == null)
			captain = PlayerIdentifier.FromTargetOrSelf(handler);

		if (captain == null)
			return false;

		if (Global.CharacterCacheStorage.GetCharacterArenaTeamIdByGuid(captain.GetGUID(), (byte)type) != 0)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorSize, captain.GetName());

			return false;
		}

		ArenaTeam arena = new();

		if (!arena.Create(captain.GetGUID(), (byte)type, name, 4293102085, 101, 4293253939, 4, 4284049911))
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		Global.ArenaTeamMgr.AddArenaTeam(arena);
		handler.SendSysMessage(CypherStrings.ArenaCreate, arena.GetName(), arena.GetId(), arena.GetArenaType(), arena.GetCaptain());

		return true;
	}

	[Command("disband", RBACPermissions.CommandArenaDisband, true)]
    private static bool HandleArenaDisbandCommand(CommandHandler handler, uint teamId)
	{
		var arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

		if (arena == null)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNotFound, teamId);

			return false;
		}

		if (arena.IsFighting())
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorCombat);

			return false;
		}

		var name = arena.GetName();
		arena.Disband();

		handler.SendSysMessage(CypherStrings.ArenaDisband, name, teamId);

		return true;
	}

	[Command("rename", RBACPermissions.CommandArenaRename, true)]
    private static bool HandleArenaRenameCommand(CommandHandler handler, string oldName, string newName)
	{
		var arena = Global.ArenaTeamMgr.GetArenaTeamByName(oldName);

		if (arena == null)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNameNotFound, oldName);

			return false;
		}

		if (Global.ArenaTeamMgr.GetArenaTeamByName(newName) != null)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, oldName);

			return false;
		}

		if (arena.IsFighting())
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorCombat);

			return false;
		}

		if (!arena.SetName(newName))
		{
			handler.SendSysMessage(CypherStrings.ArenaRename, arena.GetId(), oldName, newName);

			return true;
		}

		handler.SendSysMessage(CypherStrings.BadValue);

		return false;
	}

	[Command("captain", RBACPermissions.CommandArenaCaptain)]
    private static bool HandleArenaCaptainCommand(CommandHandler handler, uint teamId, PlayerIdentifier target)
	{
		var arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

		if (arena == null)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNotFound, teamId);

			return false;
		}

		if (arena.IsFighting())
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorCombat);

			return false;
		}

		if (target == null)
			target = PlayerIdentifier.FromTargetOrSelf(handler);

		if (target == null)
			return false;

		if (!arena.IsMember(target.GetGUID()))
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNotMember, target.GetName(), arena.GetName());

			return false;
		}

		if (arena.GetCaptain() == target.GetGUID())
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorCaptain, target.GetName(), arena.GetName());

			return false;
		}

		if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(arena.GetCaptain(), out var oldCaptainName))
			return false;

		arena.SetCaptain(target.GetGUID());
		handler.SendSysMessage(CypherStrings.ArenaCaptain, arena.GetName(), arena.GetId(), oldCaptainName, target.GetName());

		return true;
	}

	[Command("info", RBACPermissions.CommandArenaInfo, true)]
    private static bool HandleArenaInfoCommand(CommandHandler handler, uint teamId)
	{
		var arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

		if (arena == null)
		{
			handler.SendSysMessage(CypherStrings.ArenaErrorNotFound, teamId);

			return false;
		}

		handler.SendSysMessage(CypherStrings.ArenaInfoHeader, arena.GetName(), arena.GetId(), arena.GetRating(), arena.GetArenaType(), arena.GetArenaType());

		foreach (var member in arena.GetMembers())
			handler.SendSysMessage(CypherStrings.ArenaInfoMembers, member.Name, member.Guid, member.PersonalRating, (arena.GetCaptain() == member.Guid ? "- Captain" : ""));

		return true;
	}

	[Command("lookup", RBACPermissions.CommandArenaLookup)]
    private static bool HandleArenaLookupCommand(CommandHandler handler, string needle)
	{
		if (needle.IsEmpty())
			return false;

		var found = false;

		foreach (var (_, team) in Global.ArenaTeamMgr.GetArenaTeamMap())
			if (team.GetName().Equals(needle, StringComparison.OrdinalIgnoreCase))
				if (handler.Session != null)
				{
					handler.SendSysMessage(CypherStrings.ArenaLookup, team.GetName(), team.GetId(), team.GetArenaType(), team.GetArenaType());
					found = true;

					continue;
				}

		if (!found)
			handler.SendSysMessage(CypherStrings.ArenaErrorNameNotFound, needle);

		return true;
	}
}