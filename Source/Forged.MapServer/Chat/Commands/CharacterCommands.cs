// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Accounts;
using Forged.MapServer.Cache;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Reputation;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Serilog;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("character")]
internal class CharacterCommands
{
    [Command("changeaccount", RBACPermissions.CommandCharacterChangeaccount, true)]
    private static bool HandleCharacterChangeAccountCommand(CommandHandler handler, PlayerIdentifier player, AccountIdentifier newAccount)
    {
        player ??= PlayerIdentifier.FromTarget(handler);

        if (player == null)
            return false;

        var charChache = handler.ClassFactory.Resolve<CharacterCache>();
        var characterInfo = charChache.GetCharacterCacheByGuid(player.GetGUID());

        if (characterInfo == null)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var oldAccountId = characterInfo.AccountId;

        // nothing to do :)
        if (newAccount.GetID() == oldAccountId)
            return true;

        var charCount = handler.AccountManager.GetCharactersCount(newAccount.GetID());

        if (charCount != 0)
            if (charCount >= handler.Configuration.GetDefaultValue("CharactersPerRealm", 60))
            {
                handler.SendSysMessage(CypherStrings.AccountCharacterListFull, newAccount.GetName(), newAccount.GetID());

                return false;
            }

        var onlinePlayer = player.GetConnectedPlayer();

        onlinePlayer?.Session.KickPlayer("HandleCharacterChangeAccountCommand GM Command transferring character to another account");
        var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
        var stmt = charDB.GetPreparedStatement(CharStatements.UPD_ACCOUNT_BY_GUID);
        stmt.AddValue(0, newAccount.GetID());
        stmt.AddValue(1, player.GetGUID().Counter);
        charDB.DirectExecute(stmt);

        handler.WorldManager.UpdateRealmCharCount(oldAccountId);
        handler.WorldManager.UpdateRealmCharCount(newAccount.GetID());

        charChache.UpdateCharacterAccountId(player.GetGUID(), newAccount.GetID());

        handler.SendSysMessage(CypherStrings.ChangeAccountSuccess, player.GetName(), newAccount.GetName());

        var logString = $"changed ownership of player {player.GetName()} ({player.GetGUID()}) from account {oldAccountId} to account {newAccount.GetID()}";
        var session = handler.Session;

        if (session != null)
        {
            var sessionPlayer = session.Player;

            if (sessionPlayer != null)
                Log.Logger.ForContext<GMCommands>().Information($"GM {sessionPlayer.GetName()} (Account: {session.AccountId}) {logString}");
        }
        else
            Log.Logger.ForContext<GMCommands>().Information($"{handler.GetCypherString(CypherStrings.Console)} {logString}");

        return true;
    }

    [Command("changefaction", RBACPermissions.CommandCharacterChangefaction, true)]
    private static bool HandleCharacterChangeFactionCommand(CommandHandler handler, PlayerIdentifier player)
    {
        player ??= PlayerIdentifier.FromTarget(handler);

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
            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().Counter);
            var stmt = charDB.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, (ushort)AtLoginFlags.ChangeFaction);
            stmt.AddValue(1, player.GetGUID().Counter);
            charDB.Execute(stmt);
        }

        return true;
    }

    [Command("changerace", RBACPermissions.CommandCharacterChangerace, true)]
    private static bool HandleCharacterChangeRaceCommand(CommandHandler handler, PlayerIdentifier player)
    {
        player ??= PlayerIdentifier.FromTarget(handler);

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
            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().Counter);
            var stmt = charDB.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, (ushort)AtLoginFlags.ChangeRace);
            stmt.AddValue(1, player.GetGUID().Counter);
            charDB.Execute(stmt);
        }

        return true;
    }

    [Command("customize", RBACPermissions.CommandCharacterCustomize, true)]
    private static bool HandleCharacterCustomizeCommand(CommandHandler handler, PlayerIdentifier player)
    {
        player ??= PlayerIdentifier.FromTarget(handler);

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
            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, handler.PlayerLink(player.GetName()), player.GetGUID().Counter);
            var stmt = charDB.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, (ushort)AtLoginFlags.Customize);
            stmt.AddValue(1, player.GetGUID().Counter);
            charDB.Execute(stmt);
        }

        return true;
    }

    [Command("erase", RBACPermissions.CommandCharacterErase, true)]
    private static bool HandleCharacterEraseCommand(CommandHandler handler, PlayerIdentifier player)
    {
        if (player == null) return false;

        uint accountId;

        var target = player.GetConnectedPlayer();

        if (target != null)
        {
            accountId = target.Session.AccountId;
            target.Session.KickPlayer("HandleCharacterEraseCommand GM Command deleting character");
        }
        else
            accountId = handler.ClassFactory.Resolve<CharacterCache>().GetCharacterAccountIdByGuid(player.GetGUID());

        handler.AccountManager.GetName(accountId, out var accountName);

        handler.ClassFactory.Resolve<PlayerComputators>().DeleteFromDB(player.GetGUID(), accountId, true, true);
        handler.SendSysMessage(CypherStrings.CharacterDeleted, player.GetName(), player.GetGUID().ToString(), accountName, accountId);

        return true;
    }

    [Command("level", RBACPermissions.CommandCharacterLevel, true)]
    private static bool HandleCharacterLevelCommand(CommandHandler handler, PlayerIdentifier player, short newlevel)
    {
        player ??= PlayerIdentifier.FromTargetOrSelf(handler);

        if (player == null)
            return false;

        var oldlevel = player.IsConnected() ? player.GetConnectedPlayer().Level : handler.ClassFactory.Resolve<CharacterCache>().GetCharacterLevelByGuid(player.GetGUID());

        newlevel = newlevel switch
        {
            > SharedConst.StrongMaxLevel => SharedConst.StrongMaxLevel,
            _ => newlevel switch
            {
                < 1 => 1,
                _   => newlevel
            }
        };

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
            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            var stmt = charDB.GetPreparedStatement(CharStatements.UPD_LEVEL);
            stmt.AddValue(0, (byte)newlevel);
            stmt.AddValue(1, player.GetGUID().Counter);
            charDB.Execute(stmt);
        }

        if (handler.Session == null || handler.Session.Player != player.GetConnectedPlayer()) // including chr == NULL
            handler.SendSysMessage(CypherStrings.YouChangeLvl, handler.PlayerLink(player.GetName()), newlevel);

        return true;
    }

    //rename characters
    [Command("rename", RBACPermissions.CommandCharacterRename, true)]
    private static bool HandleCharacterRenameCommand(CommandHandler handler, PlayerIdentifier player, [OptionalArg] string newName)
    {
        switch (player)
        {
            case null when !newName.IsEmpty():
                return false;
            case null:
                player = PlayerIdentifier.FromTarget(handler);

                break;
        }

        if (player == null)
            return false;

        // check online security
        if (handler.HasLowerSecurity(null, player.GetGUID()))
            return false;

        if (!newName.IsEmpty())
        {
            if (!handler.ObjectManager.NormalizePlayerName(ref newName))
            {
                handler.SendSysMessage(CypherStrings.BadValue);

                return false;
            }

            if (handler.ObjectManager.CheckPlayerName(newName, player.IsConnected() ? player.GetConnectedPlayer().Session.SessionDbcLocale : handler.WorldManager.DefaultDbcLocale, true) != ResponseCodes.CharNameSuccess)
            {
                handler.SendSysMessage(CypherStrings.BadValue);

                return false;
            }

            var session = handler.Session;

            if (session != null)
                if (!session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && handler.ObjectManager.IsReservedName(newName))
                {
                    handler.SendSysMessage(CypherStrings.ReservedName);

                    return false;
                }

            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            var stmt = charDB.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
            stmt.AddValue(0, newName);
            var result = charDB.Query(stmt);

            if (!result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.RenamePlayerAlreadyExists, newName);

                return false;
            }

            // Remove declined name from db
            stmt = charDB.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, player.GetGUID().Counter);
            charDB.Execute(stmt);

            var target = player.GetConnectedPlayer();

            if (target != null)
            {
                target.SetName(newName);
                session = target.Session;

                session?.KickPlayer("HandleCharacterRenameCommand GM Command renaming character");
            }
            else
            {
                stmt = charDB.GetPreparedStatement(CharStatements.UPD_NAME_BY_GUID);
                stmt.AddValue(0, newName);
                stmt.AddValue(1, player.GetGUID().Counter);
                charDB.Execute(stmt);
            }

            handler.ClassFactory.Resolve<CharacterCache>().UpdateCharacterData(player.GetGUID(), newName);

            handler.SendSysMessage(CypherStrings.RenamePlayerWithNewName, player.GetName(), newName);

            if (session != null)
            {
                var sessionPlayer = session.Player;

                if (sessionPlayer != null)
                    Log.Logger.ForContext<GMCommands>().Information("GM {0} (Account: {1}) forced rename {2} to player {3} (Account: {4})", sessionPlayer.GetName(), session.AccountId, newName, sessionPlayer.GetName(), handler.ClassFactory.Resolve<CharacterCache>().GetCharacterAccountIdByGuid(sessionPlayer.GUID));
            }
            else
                Log.Logger.ForContext<GMCommands>().Information("CONSOLE forced rename '{0}' to '{1}' ({2})", player.GetName(), newName, player.GetGUID().ToString());
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
                var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
                var stmt = charDB.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
                stmt.AddValue(1, player.GetGUID().Counter);
                charDB.Execute(stmt);
            }
        }

        return true;
    }

    [Command("reputation", RBACPermissions.CommandCharacterReputation, true)]
    private static bool HandleCharacterReputationCommand(CommandHandler handler, PlayerIdentifier player)
    {
        player = player switch
        {
            null => PlayerIdentifier.FromTargetOrSelf(handler),
            _    => player
        };

        if (player == null || !player.IsConnected())
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var target = player.GetConnectedPlayer();
        var loc = handler.SessionDbcLocale;

        var targetFsl = target.ReputationMgr.StateList;

        foreach (var pair in targetFsl)
        {
            var faction = pair.Value;
            var factionEntry = handler.CliDB.FactionStorage.LookupByKey(faction.Id);
            var factionName = factionEntry != null ? factionEntry.Name[loc] : "#Not found#";
            var rank = target.ReputationMgr.GetRank(factionEntry);
            var rankName = handler.GetCypherString(ReputationMgr.ReputationRankStrIndex[(int)rank]);
            StringBuilder ss = new();

            if (handler.Session != null)
                ss.AppendFormat("{0} - |cffffffff|Hfaction:{0}|h[{1} {2}]|h|r", faction.Id, factionName, loc);
            else
                ss.Append($"{faction.Id} - {factionName} {loc}");

            ss.Append($" {rankName} ({target.ReputationMgr.GetReputation(factionEntry)})");

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

    [Command("titles", RBACPermissions.CommandCharacterTitles, true)]
    private static bool HandleCharacterTitlesCommand(CommandHandler handler, PlayerIdentifier player)
    {
        player = player switch
        {
            null => PlayerIdentifier.FromTargetOrSelf(handler),
            _    => player
        };

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
        foreach (var titleInfo in handler.CliDB.CharTitlesStorage.Values)
            if (target.HasTitle(titleInfo))
            {
                var name = (target.NativeGender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[loc];

                if (name.IsEmpty())
                    name = (target.NativeGender == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.WorldManager.DefaultDbcLocale];

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

    [CommandNonGroup("levelup", RBACPermissions.CommandLevelup)]
    private static bool HandleLevelUpCommand(CommandHandler handler, [OptionalArg] PlayerIdentifier player, short level)
    {
        player = player switch
        {
            null => PlayerIdentifier.FromTargetOrSelf(handler),
            _    => player
        };

        if (player == null)
            return false;

        var oldlevel = (int)(player.IsConnected() ? player.GetConnectedPlayer().Level : handler.ClassFactory.Resolve<CharacterCache>().GetCharacterLevelByGuid(player.GetGUID()));
        var newlevel = oldlevel + level;

        newlevel = newlevel switch
        {
            // hardcoded maximum level
            > SharedConst.StrongMaxLevel => SharedConst.StrongMaxLevel,
            _ => newlevel switch
            {
                < 1 => 1,
                _   => newlevel
            }
        };

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
            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            var stmt = charDB.GetPreparedStatement(CharStatements.UPD_LEVEL);
            stmt.AddValue(0, newlevel);
            stmt.AddValue(1, player.GetGUID().Counter);
            charDB.Execute(stmt);
        }

        if (handler.Session == null || handler.Session.Player != target) // including chr == NULL
            handler.SendSysMessage(CypherStrings.YouChangeLvl, handler.PlayerLink(player.GetName()), newlevel);

        return true;
    }

    [CommandGroup("deleted")]
    private class DeletedCommands
    {
        private static bool GetDeletedCharacterInfoList(List<DeletedInfo> foundList, string searchString, CharacterDatabase charDB, AccountManager accountManager, GameObjectManager gameObjectManager)
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

                    stmt = charDB.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
                    stmt.AddValue(0, guid);
                    result = charDB.Query(stmt);
                }
                // search by name
                else
                {
                    if (!gameObjectManager.NormalizePlayerName(ref searchString))
                        return false;

                    stmt = charDB.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_NAME);
                    stmt.AddValue(0, searchString);
                    result = charDB.Query(stmt);
                }
            }
            else
            {
                stmt = charDB.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO);
                result = charDB.Query(stmt);
            }

            if (result.IsEmpty())
                return true;

            do
            {
                DeletedInfo info;

                info.GUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                info.Name = result.Read<string>(1);
                info.AccountId = result.Read<uint>(2);

                // account name will be empty for not existed account
                accountManager.GetName(info.AccountId, out info.AccountName);
                info.DeleteDate = result.Read<long>(3);
                foundList.Add(info);
            } while (result.NextRow());

            return true;
        }

        [Command("delete", RBACPermissions.CommandCharacterDeletedDelete, true)]
        private static bool HandleCharacterDeletedDeleteCommand(CommandHandler handler, string needle)
        {
            List<DeletedInfo> foundList = new();

            if (!GetDeletedCharacterInfoList(foundList, needle, handler.ClassFactory.Resolve<CharacterDatabase>(), handler.AccountManager, handler.ObjectManager))
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
                handler.ClassFactory.Resolve<PlayerComputators>().DeleteFromDB(info.GUID, 0, false, true);

            return true;
        }

        [Command("list", RBACPermissions.CommandCharacterDeletedList, true)]
        private static bool HandleCharacterDeletedListCommand(CommandHandler handler, [OptionalArg] string needle)
        {
            List<DeletedInfo> foundList = new();

            if (!GetDeletedCharacterInfoList(foundList, needle, handler.ClassFactory.Resolve<CharacterDatabase>(), handler.AccountManager, handler.ObjectManager))
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

        private static void HandleCharacterDeletedListHelper(List<DeletedInfo> foundList, CommandHandler handler)
        {
            if (handler.Session == null)
            {
                handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
                handler.SendSysMessage(CypherStrings.CharacterDeletedListHeader);
                handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
            }

            foreach (var info in foundList)
            {
                var dateStr = Time.UnixTimeToDateTime(info.DeleteDate).ToShortDateString();

                handler.SendSysMessage(handler.Session == null ? CypherStrings.CharacterDeletedListLineConsole : CypherStrings.CharacterDeletedListLineChat,
                                       info.GUID.ToString(),
                                       info.Name,
                                       info.AccountName.IsEmpty() ? "<Not existed>" : info.AccountName,
                                       info.AccountId,
                                       dateStr);
            }

            if (handler.Session == null)
                handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
        }

        [Command("old", RBACPermissions.CommandCharacterDeletedOld, true)]
        private static bool HandleCharacterDeletedOldCommand(CommandHandler handler, ushort? days)
        {
            var keepDays = handler.Configuration.GetDefaultValue("CharDelete:KeepDays", 30);

            if (days.HasValue)
                keepDays = days.Value;
            else if (keepDays <= 0) // config option value 0 -> disabled and can't be used
                return false;

            handler.ClassFactory.Resolve<PlayerComputators>().DeleteOldCharacters(keepDays);

            return true;
        }

        [Command("restore", RBACPermissions.CommandCharacterDeletedRestore, true)]
        private static bool HandleCharacterDeletedRestoreCommand(CommandHandler handler, string needle, [OptionalArg] string newCharName, AccountIdentifier newAccount)
        {
            List<DeletedInfo> foundList = new();

            if (!GetDeletedCharacterInfoList(foundList, needle, handler.ClassFactory.Resolve<CharacterDatabase>(), handler.AccountManager, handler.ObjectManager))
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
                delInfo.Name = newCharName;

                // if new account provided update deleted info
                if (newAccount != null)
                {
                    delInfo.AccountId = newAccount.GetID();
                    delInfo.AccountName = newAccount.GetName();
                }

                HandleCharacterDeletedRestoreHelper(delInfo, handler);

                return true;
            }

            handler.SendSysMessage(CypherStrings.CharacterDeletedErrRename);

            return false;
        }

        private static void HandleCharacterDeletedRestoreHelper(DeletedInfo delInfo, CommandHandler handler)
        {
            if (delInfo.AccountName.IsEmpty()) // account not exist
            {
                handler.SendSysMessage(CypherStrings.CharacterDeletedSkipAccount, delInfo.Name, delInfo.GUID.ToString(), delInfo.AccountId);

                return;
            }

            // check character count
            var charcount = handler.AccountManager.GetCharactersCount(delInfo.AccountId);

            if (charcount >= handler.Configuration.GetDefaultValue("CharactersPerRealm", 60))
            {
                handler.SendSysMessage(CypherStrings.CharacterDeletedSkipFull, delInfo.Name, delInfo.GUID.ToString(), delInfo.AccountId);

                return;
            }

            if (!handler.ClassFactory.Resolve<CharacterCache>().GetCharacterGuidByName(delInfo.Name).IsEmpty)
            {
                handler.SendSysMessage(CypherStrings.CharacterDeletedSkipName, delInfo.Name, delInfo.GUID.ToString(), delInfo.AccountId);

                return;
            }

            var charDB = handler.ClassFactory.Resolve<CharacterDatabase>();
            var stmt = charDB.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
            stmt.AddValue(0, delInfo.Name);
            stmt.AddValue(1, delInfo.AccountId);
            stmt.AddValue(2, delInfo.GUID.Counter);
            charDB.Execute(stmt);

            handler.ClassFactory.Resolve<CharacterCache>().UpdateCharacterInfoDeleted(delInfo.GUID, false, delInfo.Name);
        }

        private struct DeletedInfo
        {
            public uint AccountId;

            // the account id
            public string AccountName;

            // the account name
            public long DeleteDate;

            public ObjectGuid GUID; // the GUID from the character

            public string Name; // the character name
            // the date at which the character has been deleted
        }
    }
}