// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Reputation;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("character")]
class CharacterCommands
{
	[Command("titles", RBACPermissions.CommandCharacterTitles, true)]
	static bool HandleCharacterTitlesCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null || !player.IsConnected())
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var target = player.GetConnectedPlayer();

		var loc = handler.SessionDbcLocale;
		var targetName = player.GetName();
		var knownStr = handler.GetCypherString(CypherStrings.Known);

		// Search in CharTitles.dbc
		foreach (var titleInfo in CliDB.CharTitlesStorage.Values)
			if (target.HasTitle(titleInfo))
			{
				var name = (target.NativeGender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[loc];

				if (name.IsEmpty())
					name = (target.NativeGender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[Global.WorldMgr.DefaultDbcLocale];

				if (name.IsEmpty())
					continue;

				var activeStr = "";

				if (target.PlayerData.PlayerTitle == titleInfo.MaskID)
					activeStr = handler.GetCypherString(CypherStrings.Active);

				var titleName = string.Format(name.ConvertFormatSyntax(), targetName);

				// send title in "id (idx:idx) - [namedlink locale]" format
				if (handler.Session != null)
					handler.SendSysMessage(CypherStrings.TitleListChat, titleInfo.Id, titleInfo.MaskID, titleInfo.Id, titleName, loc, knownStr, activeStr);
				else
					handler.SendSysMessage(CypherStrings.TitleListConsole, titleInfo.Id, titleInfo.MaskID, name, loc, knownStr, activeStr);
			}

		return true;
	}

	//rename characters
	[Command("rename", RBACPermissions.CommandCharacterRename, true)]
	static bool HandleCharacterRenameCommand(CommandHandler handler, PlayerIdentifier player, [OptionalArg] string newName)
	{
		if (player == null && !newName.IsEmpty())
			return false;

		if (player == null)
			player = PlayerIdentifier.FromTarget(handler);

		if (player == null)
			return false;

		// check online security
		if (handler.HasLowerSecurity(null, player.GetGUID()))
			return false;

		if (!newName.IsEmpty())
		{
			if (!GameObjectManager.NormalizePlayerName(ref newName))
			{
				handler.SendSysMessage(CypherStrings.BadValue);

				return false;
			}

			if (GameObjectManager.CheckPlayerName(newName, player.IsConnected() ? player.GetConnectedPlayer().Session.SessionDbcLocale : Global.WorldMgr.DefaultDbcLocale, true) != ResponseCodes.CharNameSuccess)
			{
				handler.SendSysMessage(CypherStrings.BadValue);

				return false;
			}

			var session = handler.Session;

			if (session != null)
				if (!session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(newName))
				{
					handler.SendSysMessage(CypherStrings.ReservedName);

					return false;
				}

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
			stmt.AddValue(0, newName);
			var result = DB.Characters.Query(stmt);

			if (!result.IsEmpty())
			{
				handler.SendSysMessage(CypherStrings.RenamePlayerAlreadyExists, newName);

				return false;
			}

			// Remove declined name from db
			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
			stmt.AddValue(0, player.GetGUID().Counter);
			DB.Characters.Execute(stmt);

			var target = player.GetConnectedPlayer();

			if (target != null)
			{
				target.SetName(newName);
				session = target.Session;

				if (session != null)
					session.KickPlayer("HandleCharacterRenameCommand GM Command renaming character");
			}
			else
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_NAME_BY_GUID);
				stmt.AddValue(0, newName);
				stmt.AddValue(1, player.GetGUID().Counter);
				DB.Characters.Execute(stmt);
			}

			Global.CharacterCacheStorage.UpdateCharacterData(player.GetGUID(), newName);

			handler.SendSysMessage(CypherStrings.RenamePlayerWithNewName, player.GetName(), newName);

			if (session != null)
			{
				var sessionPlayer = session.Player;

				if (sessionPlayer)
					Log.outCommand(session.AccountId, "GM {0} (Account: {1}) forced rename {2} to player {3} (Account: {4})", sessionPlayer.GetName(), session.AccountId, newName, sessionPlayer.GetName(), Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(sessionPlayer.GUID));
			}
			else
			{
				Log.outCommand(0, "CONSOLE forced rename '{0}' to '{1}' ({2})", player.GetName(), newName, player.GetGUID().ToString());
			}
		}
		else
		{
			var target = player.GetConnectedPlayer();

			if (target != null)
			{
				handler.SendSysMessage(CypherStrings.RenamePlayer, handler.GetNameLink(target));
				target.SetAtLoginFlag(AtLoginFlags.Rename);
			}
			else
			{
				// check offline security
				if (handler.HasLowerSecurity(null, player.GetGUID()))
					return false;

				handler.SendSysMessage(CypherStrings.RenamePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().ToString());

				var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
				stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
				stmt.AddValue(1, player.GetGUID().Counter);
				DB.Characters.Execute(stmt);
			}
		}

		return true;
	}

	[Command("level", RBACPermissions.CommandCharacterLevel, true)]
	static bool HandleCharacterLevelCommand(CommandHandler handler, PlayerIdentifier player, short newlevel)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		var oldlevel = player.IsConnected() ? player.GetConnectedPlayer().Level : Global.CharacterCacheStorage.GetCharacterLevelByGuid(player.GetGUID());

		if (newlevel < 1)
			newlevel = 1;

		if (newlevel > SharedConst.StrongMaxLevel)
			newlevel = SharedConst.StrongMaxLevel;

		var target = player.GetConnectedPlayer();

		if (target != null)
		{
			target.GiveLevel((uint)newlevel);
			target.InitTalentForLevel();
			target.XP = 0;

			if (handler.NeedReportToTarget(target))
			{
				if (oldlevel == newlevel)
					target.SendSysMessage(CypherStrings.YoursLevelProgressReset, handler.NameLink);
				else if (oldlevel < newlevel)
					target.SendSysMessage(CypherStrings.YoursLevelUp, handler.NameLink, newlevel);
				else // if (oldlevel > newlevel)
					target.SendSysMessage(CypherStrings.YoursLevelDown, handler.NameLink, newlevel);
			}
		}
		else
		{
			// Update level and reset XP, everything else will be updated at login
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_LEVEL);
			stmt.AddValue(0, (byte)newlevel);
			stmt.AddValue(1, player.GetGUID().Counter);
			DB.Characters.Execute(stmt);
		}

		if (!handler.Session || (handler.Session.Player != player.GetConnectedPlayer())) // including chr == NULL
			handler.SendSysMessage(CypherStrings.YouChangeLvl, handler.PlayerLink(player.GetName()), newlevel);

		return true;
	}

	[Command("customize", RBACPermissions.CommandCharacterCustomize, true)]
	static bool HandleCharacterCustomizeCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTarget(handler);

		if (player == null)
			return false;

		var target = player.GetConnectedPlayer();

		if (target != null)
		{
			handler.SendSysMessage(CypherStrings.CustomizePlayer, handler.GetNameLink(target));
			target.SetAtLoginFlag(AtLoginFlags.Customize);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().Counter);
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
			stmt.AddValue(0, (ushort)AtLoginFlags.Customize);
			stmt.AddValue(1, player.GetGUID().Counter);
			DB.Characters.Execute(stmt);
		}

		return true;
	}

	[Command("changeaccount", RBACPermissions.CommandCharacterChangeaccount, true)]
	static bool HandleCharacterChangeAccountCommand(CommandHandler handler, PlayerIdentifier player, AccountIdentifier newAccount)
	{
		if (player == null)
			player = PlayerIdentifier.FromTarget(handler);

		if (player == null)
			return false;

		var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(player.GetGUID());

		if (characterInfo == null)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var oldAccountId = characterInfo.AccountId;

		// nothing to do :)
		if (newAccount.GetID() == oldAccountId)
			return true;

		var charCount = Global.AccountMgr.GetCharactersCount(newAccount.GetID());

		if (charCount != 0)
			if (charCount >= GetDefaultValue("CharactersPerRealm", 60))
			{
				handler.SendSysMessage(CypherStrings.AccountCharacterListFull, newAccount.GetName(), newAccount.GetID());

				return false;
			}

		var onlinePlayer = player.GetConnectedPlayer();

		if (onlinePlayer != null)
			onlinePlayer.Session.KickPlayer("HandleCharacterChangeAccountCommand GM Command transferring character to another account");

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ACCOUNT_BY_GUID);
		stmt.AddValue(0, newAccount.GetID());
		stmt.AddValue(1, player.GetGUID().Counter);
		DB.Characters.DirectExecute(stmt);

		Global.WorldMgr.UpdateRealmCharCount(oldAccountId);
		Global.WorldMgr.UpdateRealmCharCount(newAccount.GetID());

		Global.CharacterCacheStorage.UpdateCharacterAccountId(player.GetGUID(), newAccount.GetID());

		handler.SendSysMessage(CypherStrings.ChangeAccountSuccess, player.GetName(), newAccount.GetName());

		var logString = $"changed ownership of player {player.GetName()} ({player.GetGUID()}) from account {oldAccountId} to account {newAccount.GetID()}";
		var session = handler.Session;

		if (session != null)
		{
			var sessionPlayer = session.Player;

			if (sessionPlayer != null)
				Log.outCommand(session.AccountId, $"GM {sessionPlayer.GetName()} (Account: {session.AccountId}) {logString}");
		}
		else
		{
			Log.outCommand(0, $"{handler.GetCypherString(CypherStrings.Console)} {logString}");
		}

		return true;
	}

	[Command("changefaction", RBACPermissions.CommandCharacterChangefaction, true)]
	static bool HandleCharacterChangeFactionCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTarget(handler);

		if (player == null)
			return false;

		var target = player.GetConnectedPlayer();

		if (target != null)
		{
			handler.SendSysMessage(CypherStrings.CustomizePlayer, handler.GetNameLink(target));
			target.SetAtLoginFlag(AtLoginFlags.ChangeFaction);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().Counter);
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
			stmt.AddValue(0, (ushort)AtLoginFlags.ChangeFaction);
			stmt.AddValue(1, player.GetGUID().Counter);
			DB.Characters.Execute(stmt);
		}

		return true;
	}

	[Command("changerace", RBACPermissions.CommandCharacterChangerace, true)]
	static bool HandleCharacterChangeRaceCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTarget(handler);

		if (player == null)
			return false;

		var target = player.GetConnectedPlayer();

		if (target != null)
		{
			handler.SendSysMessage(CypherStrings.CustomizePlayer, handler.GetNameLink(target));
			target.SetAtLoginFlag(AtLoginFlags.ChangeRace);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().Counter);
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
			stmt.AddValue(0, (ushort)AtLoginFlags.ChangeRace);
			stmt.AddValue(1, player.GetGUID().Counter);
			DB.Characters.Execute(stmt);
		}

		return true;
	}

	[Command("reputation", RBACPermissions.CommandCharacterReputation, true)]
	static bool HandleCharacterReputationCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null || !player.IsConnected())
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var target = player.GetConnectedPlayer();
		var loc = handler.SessionDbcLocale;

		var targetFSL = target.ReputationMgr.StateList;

		foreach (var pair in targetFSL)
		{
			var faction = pair.Value;
			var factionEntry = CliDB.FactionStorage.LookupByKey(faction.Id);
			var factionName = factionEntry != null ? factionEntry.Name[loc] : "#Not found#";
			var rank = target.ReputationMgr.GetRank(factionEntry);
			var rankName = handler.GetCypherString(ReputationMgr.ReputationRankStrIndex[(int)rank]);
			StringBuilder ss = new();

			if (handler.Session != null)
				ss.AppendFormat("{0} - |cffffffff|Hfaction:{0}|h[{1} {2}]|h|r", faction.Id, factionName, loc);
			else
				ss.AppendFormat("{0} - {1} {2}", faction.Id, factionName, loc);

			ss.AppendFormat(" {0} ({1})", rankName, target.ReputationMgr.GetReputation(factionEntry));

			if (faction.Flags.HasFlag(ReputationFlags.Visible))
				ss.Append(handler.GetCypherString(CypherStrings.FactionVisible));

			if (faction.Flags.HasFlag(ReputationFlags.AtWar))
				ss.Append(handler.GetCypherString(CypherStrings.FactionAtwar));

			if (faction.Flags.HasFlag(ReputationFlags.Peaceful))
				ss.Append(handler.GetCypherString(CypherStrings.FactionPeaceForced));

			if (faction.Flags.HasFlag(ReputationFlags.Hidden))
				ss.Append(handler.GetCypherString(CypherStrings.FactionHidden));

			if (faction.Flags.HasFlag(ReputationFlags.Header))
				ss.Append(handler.GetCypherString(CypherStrings.FactionInvisibleForced));

			if (faction.Flags.HasFlag(ReputationFlags.Inactive))
				ss.Append(handler.GetCypherString(CypherStrings.FactionInactive));

			handler.SendSysMessage(ss.ToString());
		}

		return true;
	}

	[Command("erase", RBACPermissions.CommandCharacterErase, true)]
	static bool HandleCharacterEraseCommand(CommandHandler handler, PlayerIdentifier player)
	{
		uint accountId;

		var target = player?.GetConnectedPlayer();

		if (target != null)
		{
			accountId = target.Session.AccountId;
			target.Session.KickPlayer("HandleCharacterEraseCommand GM Command deleting character");
		}
		else
		{
			accountId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(player.GetGUID());
		}

		Global.AccountMgr.GetName(accountId, out var accountName);

		Player.DeleteFromDB(player.GetGUID(), accountId, true, true);
		handler.SendSysMessage(CypherStrings.CharacterDeleted, player.GetName(), player.GetGUID().ToString(), accountName, accountId);

		return true;
	}

	[CommandNonGroup("levelup", RBACPermissions.CommandLevelup)]
	static bool HandleLevelUpCommand(CommandHandler handler, [OptionalArg] PlayerIdentifier player, short level)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		var oldlevel = (int)(player.IsConnected() ? player.GetConnectedPlayer().Level : Global.CharacterCacheStorage.GetCharacterLevelByGuid(player.GetGUID()));
		var newlevel = oldlevel + level;

		if (newlevel < 1)
			newlevel = 1;

		if (newlevel > SharedConst.StrongMaxLevel) // hardcoded maximum level
			newlevel = SharedConst.StrongMaxLevel;

		var target = player.GetConnectedPlayer();

		if (target != null)
		{
			target.GiveLevel((uint)newlevel);
			target.InitTalentForLevel();
			target.XP = 0;

			if (handler.NeedReportToTarget(player.GetConnectedPlayer()))
			{
				if (oldlevel == newlevel)
					player.GetConnectedPlayer().SendSysMessage(CypherStrings.YoursLevelProgressReset, handler.NameLink);
				else if (oldlevel < newlevel)
					player.GetConnectedPlayer().SendSysMessage(CypherStrings.YoursLevelUp, handler.NameLink, newlevel);
				else // if (oldlevel > newlevel)
					player.GetConnectedPlayer().SendSysMessage(CypherStrings.YoursLevelDown, handler.NameLink, newlevel);
			}
		}
		else
		{
			// Update level and reset XP, everything else will be updated at login
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_LEVEL);
			stmt.AddValue(0, newlevel);
			stmt.AddValue(1, player.GetGUID().Counter);
			DB.Characters.Execute(stmt);
		}

		if (handler.Session == null || handler.Session.Player != target) // including chr == NULL
			handler.SendSysMessage(CypherStrings.YouChangeLvl, handler.PlayerLink(player.GetName()), newlevel);

		return true;
	}

	[CommandGroup("deleted")]
	class DeletedCommands
	{
		[Command("delete", RBACPermissions.CommandCharacterDeletedDelete, true)]
		static bool HandleCharacterDeletedDeleteCommand(CommandHandler handler, string needle)
		{
			List<DeletedInfo> foundList = new();

			if (!GetDeletedCharacterInfoList(foundList, needle))
				return false;

			if (foundList.Empty())
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedListEmpty);

				return false;
			}

			handler.SendSysMessage(CypherStrings.CharacterDeletedDelete);
			HandleCharacterDeletedListHelper(foundList, handler);

			// Call the appropriate function to delete them (current account for deleted characters is 0)
			foreach (var info in foundList)
				Player.DeleteFromDB(info.guid, 0, false, true);

			return true;
		}

		[Command("list", RBACPermissions.CommandCharacterDeletedList, true)]
		static bool HandleCharacterDeletedListCommand(CommandHandler handler, [OptionalArg] string needle)
		{
			List<DeletedInfo> foundList = new();

			if (!GetDeletedCharacterInfoList(foundList, needle))
				return false;

			// if no characters have been found, output a warning
			if (foundList.Empty())
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedListEmpty);

				return false;
			}

			HandleCharacterDeletedListHelper(foundList, handler);

			return true;
		}

		[Command("restore", RBACPermissions.CommandCharacterDeletedRestore, true)]
		static bool HandleCharacterDeletedRestoreCommand(CommandHandler handler, string needle, [OptionalArg] string newCharName, AccountIdentifier newAccount)
		{
			List<DeletedInfo> foundList = new();

			if (!GetDeletedCharacterInfoList(foundList, needle))
				return false;

			if (foundList.Empty())
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedListEmpty);

				return false;
			}

			handler.SendSysMessage(CypherStrings.CharacterDeletedRestore);
			HandleCharacterDeletedListHelper(foundList, handler);

			if (newCharName.IsEmpty())
			{
				// Drop not existed account cases
				foreach (var info in foundList)
					HandleCharacterDeletedRestoreHelper(info, handler);

				return true;
			}

			if (foundList.Count == 1)
			{
				var delInfo = foundList[0];

				// update name
				delInfo.name = newCharName;

				// if new account provided update deleted info
				if (newAccount != null)
				{
					delInfo.accountId = newAccount.GetID();
					delInfo.accountName = newAccount.GetName();
				}

				HandleCharacterDeletedRestoreHelper(delInfo, handler);

				return true;
			}

			handler.SendSysMessage(CypherStrings.CharacterDeletedErrRename);

			return false;
		}

		[Command("old", RBACPermissions.CommandCharacterDeletedOld, true)]
		static bool HandleCharacterDeletedOldCommand(CommandHandler handler, ushort? days)
		{
			var keepDays = GetDefaultValue("CharDelete.KeepDays", 30);

			if (days.HasValue)
				keepDays = days.Value;
			else if (keepDays <= 0) // config option value 0 -> disabled and can't be used
				return false;

			Player.DeleteOldCharacters(keepDays);

			return true;
		}

		static bool GetDeletedCharacterInfoList(List<DeletedInfo> foundList, string searchString)
		{
			SQLResult result;
			PreparedStatement stmt;

			if (!searchString.IsEmpty())
			{
				// search by GUID
				if (searchString.IsNumber())
				{
					if (!ulong.TryParse(searchString, out var guid))
						return false;

					stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
					stmt.AddValue(0, guid);
					result = DB.Characters.Query(stmt);
				}
				// search by name
				else
				{
					if (!GameObjectManager.NormalizePlayerName(ref searchString))
						return false;

					stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_NAME);
					stmt.AddValue(0, searchString);
					result = DB.Characters.Query(stmt);
				}
			}
			else
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO);
				result = DB.Characters.Query(stmt);
			}

			if (!result.IsEmpty())
				do
				{
					DeletedInfo info;

					info.guid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
					info.name = result.Read<string>(1);
					info.accountId = result.Read<uint>(2);

					// account name will be empty for not existed account
					Global.AccountMgr.GetName(info.accountId, out info.accountName);
					info.deleteDate = result.Read<long>(3);
					foundList.Add(info);
				} while (result.NextRow());

			return true;
		}

		static void HandleCharacterDeletedListHelper(List<DeletedInfo> foundList, CommandHandler handler)
		{
			if (handler.Session == null)
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
				handler.SendSysMessage(CypherStrings.CharacterDeletedListHeader);
				handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
			}

			foreach (var info in foundList)
			{
				var dateStr = Time.UnixTimeToDateTime(info.deleteDate).ToShortDateString();

				if (!handler.Session)
					handler.SendSysMessage(CypherStrings.CharacterDeletedListLineConsole,
											info.guid.ToString(),
											info.name,
											info.accountName.IsEmpty() ? "<Not existed>" : info.accountName,
											info.accountId,
											dateStr);
				else
					handler.SendSysMessage(CypherStrings.CharacterDeletedListLineChat,
											info.guid.ToString(),
											info.name,
											info.accountName.IsEmpty() ? "<Not existed>" : info.accountName,
											info.accountId,
											dateStr);
			}

			if (!handler.Session)
				handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
		}

		static void HandleCharacterDeletedRestoreHelper(DeletedInfo delInfo, CommandHandler handler)
		{
			if (delInfo.accountName.IsEmpty()) // account not exist
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedSkipAccount, delInfo.name, delInfo.guid.ToString(), delInfo.accountId);

				return;
			}

			// check character count
			var charcount = Global.AccountMgr.GetCharactersCount(delInfo.accountId);

			if (charcount >= GetDefaultValue("CharactersPerRealm", 60))
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedSkipFull, delInfo.name, delInfo.guid.ToString(), delInfo.accountId);

				return;
			}

			if (!Global.CharacterCacheStorage.GetCharacterGuidByName(delInfo.name).IsEmpty)
			{
				handler.SendSysMessage(CypherStrings.CharacterDeletedSkipName, delInfo.name, delInfo.guid.ToString(), delInfo.accountId);

				return;
			}

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
			stmt.AddValue(0, delInfo.name);
			stmt.AddValue(1, delInfo.accountId);
			stmt.AddValue(2, delInfo.guid.Counter);
			DB.Characters.Execute(stmt);

			Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(delInfo.guid, false, delInfo.name);
		}

		struct DeletedInfo
		{
			public ObjectGuid guid;    // the GUID from the character
			public string name;        // the character name
			public uint accountId;     // the account id
			public string accountName; // the account name
			public long deleteDate;    // the date at which the character has been deleted
		}
	}
}