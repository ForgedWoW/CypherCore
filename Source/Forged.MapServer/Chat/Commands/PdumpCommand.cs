// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("pdump")]
internal class PdumpCommand
{
    [Command("copy", RBACPermissions.CommandPdumpCopy, true)]
    private static bool HandlePDumpCopyCommand(CommandHandler handler, PlayerIdentifier player, AccountIdentifier account, [OptionalArg] string characterName, ulong? characterGUID)
    {
        /*
            std::string name;
            if (!ValidatePDumpTarget(handler, name, characterName, characterGUID))
                return false;
      
            std::string dump;
            switch (PlayerDumpWriter().WriteDumpToString(dump, player.GetGUID().GetCounter()))
            {
                case DUMP_SUCCESS:
                    break;
                case DUMP_CHARACTER_DELETED:
                    handler->PSendSysMessage(LANG_COMMAND_EXPORT_DELETED_CHAR);
                    handler->SetSentErrorMessage(true);
                    return false;
                case DUMP_FILE_OPEN_ERROR: // this error code should not happen
                default:
                    handler->PSendSysMessage(LANG_COMMAND_EXPORT_FAILED);
                    handler->SetSentErrorMessage(true);
                    return false;
            }
      
            switch (PlayerDumpReader().LoadDumpFromString(dump, account, name, characterGUID.value_or(0)))
            {
                case DUMP_SUCCESS:
                    break;
                case DUMP_TOO_MANY_CHARS:
                    handler->PSendSysMessage(LANG_ACCOUNT_CHARACTER_LIST_FULL, account.GetName().c_str(), account.GetID());
                    handler->SetSentErrorMessage(true);
                    return false;
                case DUMP_FILE_OPEN_ERROR: // this error code should not happen
                case DUMP_FILE_BROKEN: // this error code should not happen
                default:
                    handler->PSendSysMessage(LANG_COMMAND_IMPORT_FAILED);
                    handler->SetSentErrorMessage(true);
                    return false;
            }
      
            // ToDo: use a new trinity_string for this commands
            handler->PSendSysMessage(LANG_COMMAND_IMPORT_SUCCESS);
            */
        return true;
    }

    [Command("load", RBACPermissions.CommandPdumpLoad, true)]
    private static bool HandlePDumpLoadCommand(CommandHandler handler, string fileName, AccountIdentifier account, [OptionalArg] string characterName, ulong? characterGuid)
    {
        /*
            if (!AccountMgr.normalizeString(accountName))
            {
                handler.SendSysMessage(LANG_ACCOUNT_NOT_EXIST, accountName);
                handler.SetSentErrorMessage(true);
                return false;
            }
      
            public uint accountId = AccountMgr.GetId(accountName);
            if (!accountId)
            {
                accountId = atoi(accountStr);                             // use original string
                if (!accountId)
                {
                    handler.SendSysMessage(LANG_ACCOUNT_NOT_EXIST, accountName);
      
                    return false;
                }
            }
      
            if (!AccountMgr.GetName(accountId, accountName))
            {
                handler.SendSysMessage(LANG_ACCOUNT_NOT_EXIST, accountName);
                handler.SetSentErrorMessage(true);
                return false;
            }
      
            string name;
            if (nameStr)
            {
                name = nameStr;
                // normalize the name if specified and check if it exists
                if (!GameObjectManager.NormalizePlayerName(name))
                {
                    handler.SendSysMessage(LANG_INVALID_CHARACTER_NAME);
      
                    return false;
                }
      
                if (ObjectMgr.CheckPlayerName(name, true) != CHAR_NAME_SUCCESS)
                {
                    handler.SendSysMessage(LANG_INVALID_CHARACTER_NAME);
      
                    return false;
                }
      
                guidStr = strtok(NULL, " ");
            }
      
            public uint guid = 0;
      
            if (guidStr)
            {
                guid = uint32(atoi(guidStr));
                if (!guid)
                {
                    handler.SendSysMessage(LANG_INVALID_CHARACTER_GUID);
      
                    return false;
                }
      
                if (Global.ObjectMgr.GetPlayerAccountIdByGUID(guid))
                {
                    handler.SendSysMessage(LANG_CHARACTER_GUID_IN_USE, guid);
      
                    return false;
                }
            }
      
            switch (PlayerDumpReader().LoadDump(fileStr, accountId, name, guid))
            {
                case DUMP_SUCCESS:
                    handler.SendSysMessage(LANG_COMMAND_IMPORT_SUCCESS);
                    break;
                case DUMP_FILE_OPEN_ERROR:
                    handler.SendSysMessage(LANG_FILE_OPEN_FAIL, fileStr);
      
                    return false;
                case DUMP_FILE_BROKEN:
                    handler.SendSysMessage(LANG_DUMP_BROKEN, fileStr);
      
                    return false;
                case DUMP_TOO_MANY_CHARS:
                    handler.SendSysMessage(LANG_ACCOUNT_CHARACTER_LIST_FULL, accountName, accountId);
      
                    return false;
                default:
                    handler.SendSysMessage(LANG_COMMAND_IMPORT_FAILED);
      
                    return false;
            }
            */
        return true;
    }

    [Command("write", RBACPermissions.CommandPdumpWrite, true)]
    private static bool HandlePDumpWriteCommand(CommandHandler handler, string fileName, string playerName)
    {
        /*
            switch (PlayerDumpWriter().WriteDump(fileName, player.GetGUID().GetCounter()))
            {
                case DUMP_SUCCESS:
                    handler.SendSysMessage(LANG_COMMAND_EXPORT_SUCCESS);
                    break;
                case DUMP_FILE_OPEN_ERROR:
                    handler.SendSysMessage(LANG_FILE_OPEN_FAIL, fileName);
      
                    return false;
                case DUMP_CHARACTER_DELETED:
                    handler.SendSysMessage(LANG_COMMAND_EXPORT_DELETED_CHAR);
      
                    return false;
                default:
                    handler.SendSysMessage(LANG_COMMAND_EXPORT_FAILED);
      
                    return false;
            }
            */
        return true;
    }
}