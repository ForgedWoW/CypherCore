﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Accounts;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("account")]
internal class AccountCommands
{
    [Command("2fa remove", CypherStrings.CommandAcc2faRemoveHelp, RBACPermissions.CommandAccount2FaRemove)]
    private static bool HandleAccount2FaRemoveCommand(CommandHandler handler, uint? token)
    {
        /*var masterKey = Global.SecretMgr.GetSecret(Secrets.TOTPMasterKey);
        if (!masterKey.IsAvailable())
        {
            handler.SendSysMessage(CypherStrings.TwoFACommandsNotSetup);
            return false;
        }
      
        uint accountId = handler.GetSession().GetAccountId();
        byte[] secret;
        { // get current TOTP secret
            PreparedStatement stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOTP_SECRET);
            stmt.AddValue(0, accountId);
            SQLResult result = handler.ClassFactory.Resolve<LoginDatabase>().Query(stmt);
      
            if (result.IsEmpty())
            {
                Log.Logger.Error($"Account {accountId} not found in login database when processing .account 2fa setup command.");
                handler.SendSysMessage(CypherStrings.UnknownError);
                return false;
            }
      
            if (result.IsNull(0))
            { // 2FA not enabled
                handler.SendSysMessage(CypherStrings.TwoFANotSetup);
                return false;
            }
      
            secret = result.Read<byte[]>(0);
        }
      
        if (token.HasValue)
        {
            if (masterKey.IsValid())
            {
                bool success = AES.Decrypt(secret, masterKey.GetValue());
                if (!success)
                {
                    Log.Logger.Error($"Account {accountId} has invalid ciphertext in TOTP token.");
                    handler.SendSysMessage(CypherStrings.UnknownError);
                    return false;
                }
            }
      
            if (TOTP.ValidateToken(secret, token.Value))
            {
                PreparedStatement stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                stmt.AddNull(0);
                stmt.AddValue(1, accountId);
                handler.ClassFactory.Resolve<LoginDatabase>().Execute(stmt);
                handler.SendSysMessage(CypherStrings.TwoFARemoveComplete);
                return true;
            }
            else
                handler.SendSysMessage(CypherStrings.TwoFAInvalidToken);
        }
      
        handler.SendSysMessage(CypherStrings.TwoFARemoveNeedToken);*/
        return false;
    }

    [Command("2fa setup", CypherStrings.CommandAcc2faSetupHelp, RBACPermissions.CommandAccount2FaSetup)]
    private static bool HandleAccount2FaSetupCommand(CommandHandler handler, uint? token)
    {
        /*var masterKey = Global.SecretMgr.GetSecret(Secrets.TOTPMasterKey);
        if (!masterKey.IsAvailable())
        {
            handler.SendSysMessage(CypherStrings.TwoFACommandsNotSetup);
            return false;
        }
      
        uint accountId = handler.GetSession().GetAccountId();
      
        { // check if 2FA already enabled
            PreparedStatement stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOTP_SECRET);
            stmt.AddValue(0, accountId);
            SQLResult result = handler.ClassFactory.Resolve<LoginDatabase>().Query(stmt);
      
            if (result.IsEmpty())
            {
                Log.Logger.Error($"Account {accountId} not found in login database when processing .account 2fa setup command.");
                handler.SendSysMessage(CypherStrings.UnknownError);
                return false;
            }
      
            if (!result.IsNull(0))
            {
                handler.SendSysMessage(CypherStrings.TwoFAAlreadySetup);
                return false;
            }
        }
      
        // store random suggested secrets
        Dictionary<uint, byte[]> suggestions = new();
        var pair = suggestions.TryAdd(accountId, new byte[20]); // std::vector 1-argument size_t constructor invokes resize
        if (pair) // no suggestion yet, generate random secret
            suggestions[accountId] = new byte[0].GenerateRandomKey(20);
      
        if (!pair && token.HasValue) // suggestion already existed and token specified - validate
        {
            if (TOTP.ValidateToken(suggestions[accountId], token.Value))
            {
                if (masterKey.IsValid())
                    AES.Encrypt(suggestions[accountId], masterKey.GetValue());
      
                PreparedStatement stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                stmt.AddValue(0, suggestions[accountId]);
                stmt.AddValue(1, accountId);
                handler.ClassFactory.Resolve<LoginDatabase>().Execute(stmt);
                suggestions.Remove(accountId);
                handler.SendSysMessage(CypherStrings.TwoFASetupComplete);
                return true;
            }
            else
                handler.SendSysMessage(CypherStrings.TwoFAInvalidToken);
        }
      
        // new suggestion, or no token specified, output TOTP parameters
        handler.SendSysMessage(CypherStrings.TwoFASecretSuggestion, suggestions[accountId].ToBase32());*/
        return false;
    }

    [Command("addon", CypherStrings.CommandAccAddonHelp, RBACPermissions.CommandAccountAddon)]
    private static bool HandleAccountAddonCommand(CommandHandler handler, byte expansion)
    {
        if (expansion > handler.Configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight))
        {
            handler.SendSysMessage(CypherStrings.ImproperValue);

            return false;
        }

        var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
        var stmt = loginDB.GetPreparedStatement(LoginStatements.UPD_EXPANSION);
        stmt.AddValue(0, expansion);
        stmt.AddValue(1, handler.Session.AccountId);
        loginDB.Execute(stmt);

        handler.SendSysMessage(CypherStrings.AccountAddon, expansion);

        return true;
    }

    [Command("", CypherStrings.CommandAccountHelp, RBACPermissions.CommandAccount)]
    private static bool HandleAccountCommand(CommandHandler handler)
    {
        if (handler.Session == null)
            return false;

        // GM Level
        var securityLevel = handler.Session.Security;
        handler.SendSysMessage(CypherStrings.AccountLevel, securityLevel);

        // Security level required
        var session = handler.Session;
        var hasRBAC = session.HasPermission(RBACPermissions.EmailConfirmForPassChange);
        uint pwConfig = 0; // 0 - PW_NONE, 1 - PW_EMAIL, 2 - PW_RBAC

        handler.SendSysMessage(CypherStrings.AccountSecType,
                               pwConfig == 0 ? "Lowest level: No Email input required." :
                               pwConfig == 1 ? "Highest level: Email input required." :
                               pwConfig == 2 ? "Special level: Your account may require email input depending on settings. That is the case if another lien is printed." :
                                               "Unknown security level: Notify technician for details.");

        // RBAC required display - is not displayed for console
        if (pwConfig == 2 && hasRBAC)
            handler.SendSysMessage(CypherStrings.RbacEmailRequired);

        // Email display if sufficient rights
        if (session.HasPermission(RBACPermissions.MayCheckOwnEmail))
        {
            string emailoutput;
            var accountId = session.AccountId;
            var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
            var stmt = loginDB.GetPreparedStatement(LoginStatements.GET_EMAIL_BY_ID);
            stmt.AddValue(0, accountId);
            var result = loginDB.Query(stmt);

            if (!result.IsEmpty())
            {
                emailoutput = result.Read<string>(0);
                handler.SendSysMessage(CypherStrings.CommandEmailOutput, emailoutput);
            }
        }

        return true;
    }

    [Command("create", CypherStrings.CommandAccCreateHelp, RBACPermissions.CommandAccountCreate, true)]
    private static bool HandleAccountCreateCommand(CommandHandler handler, string accountName, string password, [OptionalArg] string email)
    {
        if (accountName.Contains("@"))
        {
            handler.SendSysMessage(CypherStrings.AccountUseBnetCommands);

            return false;
        }

        var result = handler.AccountManager.CreateAccount(accountName, password, email ?? "");

        switch (result)
        {
            case AccountOpResult.Ok:
                handler.SendSysMessage(CypherStrings.AccountCreated, accountName);

                if (handler.Session != null)
                    Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) created Account {4} (Email: '{5}')",
                                           handler.Session.AccountId,
                                           handler.Session.RemoteAddress,
                                           handler.Session.Player.GetName(),
                                           handler.Session.Player.GUID.ToString(),
                                           accountName,
                                           email ?? "");

                break;
            case AccountOpResult.NameTooLong:
                handler.SendSysMessage(CypherStrings.AccountNameTooLong);

                return false;
            case AccountOpResult.PassTooLong:
                handler.SendSysMessage(CypherStrings.AccountPassTooLong);

                return false;
            case AccountOpResult.NameAlreadyExist:
                handler.SendSysMessage(CypherStrings.AccountAlreadyExist);

                return false;
            case AccountOpResult.DBInternalError:
                handler.SendSysMessage(CypherStrings.AccountNotCreatedSqlError, accountName);

                return false;
            default:
                handler.SendSysMessage(CypherStrings.AccountNotCreated, accountName);

                return false;
        }

        return true;
    }

    [Command("delete", CypherStrings.CommandAccDeleteHelp, RBACPermissions.CommandAccountDelete, true)]
    private static bool HandleAccountDeleteCommand(CommandHandler handler, string accountName)
    {
        var accountId = handler.AccountManager.GetId(accountName);

        if (accountId == 0)
        {
            handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

            return false;
        }

        if (handler.HasLowerSecurityAccount(null, accountId, true))
            return false;

        var result = handler.AccountManager.DeleteAccount(accountId);

        switch (result)
        {
            case AccountOpResult.Ok:
                handler.SendSysMessage(CypherStrings.AccountDeleted, accountName);

                break;
            case AccountOpResult.NameNotExist:
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                return false;
            case AccountOpResult.DBInternalError:
                handler.SendSysMessage(CypherStrings.AccountNotDeletedSqlError, accountName);

                return false;
            default:
                handler.SendSysMessage(CypherStrings.AccountNotDeleted, accountName);

                return false;
        }

        return true;
    }

    [Command("email", CypherStrings.CommandAccEmailHelp, RBACPermissions.CommandAccountEmail)]
    private static bool HandleAccountEmailCommand(CommandHandler handler, string oldEmail, string password, string email, string emailConfirm)
    {
        if (!handler.AccountManager.CheckEmail(handler.Session.AccountId, oldEmail))
        {
            handler.SendSysMessage(CypherStrings.CommandWrongemail);

            Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided email [{4}] is not equal to registration email [{5}].",
                                   handler.Session.AccountId,
                                   handler.Session.RemoteAddress,
                                   handler.Session.Player.GetName(),
                                   handler.Session.Player.GUID.ToString(),
                                   email,
                                   oldEmail);

            return false;
        }

        if (!handler.AccountManager.CheckPassword(handler.Session.AccountId, password))
        {
            handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);

            Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided password is wrong.",
                                   handler.Session.AccountId,
                                   handler.Session.RemoteAddress,
                                   handler.Session.Player.GetName(),
                                   handler.Session.Player.GUID.ToString());

            return false;
        }

        if (email == oldEmail)
        {
            handler.SendSysMessage(CypherStrings.OldEmailIsNewEmail);

            return false;
        }

        if (email != emailConfirm)
        {
            handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);

            Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided password is wrong.",
                                   handler.Session.AccountId,
                                   handler.Session.RemoteAddress,
                                   handler.Session.Player.GetName(),
                                   handler.Session.Player.GUID.ToString());

            return false;
        }


        var result = handler.AccountManager.ChangeEmail(handler.Session.AccountId, email);

        switch (result)
        {
            case AccountOpResult.Ok:
                handler.SendSysMessage(CypherStrings.CommandEmail);

                Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Changed Email from [{4}] to [{5}].",
                                       handler.Session.AccountId,
                                       handler.Session.RemoteAddress,
                                       handler.Session.Player.GetName(),
                                       handler.Session.Player.GUID.ToString(),
                                       oldEmail,
                                       email);

                break;
            case AccountOpResult.EmailTooLong:
                handler.SendSysMessage(CypherStrings.EmailTooLong);

                return false;
            default:
                handler.SendSysMessage(CypherStrings.CommandNotchangeemail);

                return false;
        }

        return true;
    }

    [Command("password", CypherStrings.CommandAccPasswordHelp, RBACPermissions.CommandAccountPassword)]
    private static bool HandleAccountPasswordCommand(CommandHandler handler, string oldPassword, string newPassword, string confirmPassword, [OptionalArg] string confirmEmail)
    {
        // First, we check config. What security type (sec type) is it ? Depending on it, the command branches out
        var pwConfig = handler.Configuration.GetDefaultValue("Account.PasswordChangeSecurity", 0); // 0 - PW_NONE, 1 - PW_EMAIL, 2 - PW_RBAC

        // We compare the old, saved password to the entered old password - no chance for the unauthorized.
        if (!handler.AccountManager.CheckPassword(handler.Session.AccountId, oldPassword))
        {
            handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);

            Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change password, but the provided old password is wrong.",
                                   handler.Session.AccountId,
                                   handler.Session.RemoteAddress,
                                   handler.Session.Player.GetName(),
                                   handler.Session.Player.GUID.ToString());

            return false;
        }

        // This compares the old, current email to the entered email - however, only...
        if ((pwConfig == 1 || (pwConfig == 2 && handler.Session.HasPermission(RBACPermissions.EmailConfirmForPassChange))) // ...if either PW_EMAIL or PW_RBAC with the Permission is active...
            &&
            !handler.AccountManager.CheckEmail(handler.Session.AccountId, confirmEmail)) // ... and returns false if the comparison fails.
        {
            handler.SendSysMessage(CypherStrings.CommandWrongemail);

            Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change password, but the entered email [{4}] is wrong.",
                                   handler.Session.AccountId,
                                   handler.Session.RemoteAddress,
                                   handler.Session.Player.GetName(),
                                   handler.Session.Player.GUID.ToString(),
                                   confirmEmail);

            return false;
        }

        // Making sure that newly entered password is correctly entered.
        if (newPassword != confirmPassword)
        {
            handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);

            return false;
        }

        // Changes password and prints result.
        var result = handler.AccountManager.ChangePassword(handler.Session.AccountId, newPassword);

        switch (result)
        {
            case AccountOpResult.Ok:
                handler.SendSysMessage(CypherStrings.CommandPassword);

                Log.Logger.Information("Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Changed Password.",
                                       handler.Session.AccountId,
                                       handler.Session.RemoteAddress,
                                       handler.Session.Player.GetName(),
                                       handler.Session.Player.GUID.ToString());

                break;
            case AccountOpResult.PassTooLong:
                handler.SendSysMessage(CypherStrings.PasswordTooLong);

                return false;
            default:
                handler.SendSysMessage(CypherStrings.CommandNotchangepassword);

                return false;
        }

        return true;
    }

    [CommandGroup("lock")]
    private class AccountLockCommands
    {
        [Command("country", CypherStrings.CommandAccLockCountryHelp, RBACPermissions.CommandAccountLockCountry)]
        private static bool HandleAccountLockCountryCommand(CommandHandler handler, bool state)
        {
            if (state)
            {
                /*var ipBytes = System.Net.IPAddress.Parse(handler.GetSession().GetRemoteAddress()).GetAddressBytes();
                Array.Reverse(ipBytes);
            
                PreparedStatement stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.SEL_LOGON_COUNTRY);
                stmt.AddValue(0, BitConverter.ToUInt32(ipBytes, 0));
            
                SQLResult result = handler.ClassFactory.Resolve<LoginDatabase>().Query(stmt);
                if (!result.IsEmpty())
                {
                    string country = result.Read<string>(0);
                    stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
                    stmt.AddValue(0, country);
                    stmt.AddValue(1, handler.GetSession().GetAccountId());
                    handler.ClassFactory.Resolve<LoginDatabase>().Execute(stmt);
                    handler.SendSysMessage(CypherStrings.CommandAcclocklocked);
                }
                else
                {
                    handler.SendSysMessage("[IP2NATION] Table empty");
                    Log.Logger.Debug("[IP2NATION] Table empty");
                }*/
            }
            else
            {
                var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
                var stmt = loginDB.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
                stmt.AddValue(0, "00");
                stmt.AddValue(1, handler.Session.AccountId);
                loginDB.Execute(stmt);
                handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
            }

            return true;
        }

        [Command("ip", CypherStrings.CommandAccLockIpHelp, RBACPermissions.CommandAccountLockIp)]
        private static bool HandleAccountLockIpCommand(CommandHandler handler, bool state)
        {
            var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
            var stmt = loginDB.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK);

            if (state)
            {
                stmt.AddValue(0, true); // locked
                handler.SendSysMessage(CypherStrings.CommandAcclocklocked);
            }
            else
            {
                stmt.AddValue(0, false); // unlocked
                handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
            }

            stmt.AddValue(1, handler.Session.AccountId);

            loginDB.Execute(stmt);

            return true;
        }
    }

    [CommandGroup("onlinelist")]
    private class AccountOnlineListCommands
    {
        [Command("", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
        private static bool HandleAccountOnlineListCommand(CommandHandler handler)
        {
            return HandleAccountOnlineListCommandWithParameters(handler, null, null, null, null);
        }

        private static bool HandleAccountOnlineListCommandWithParameters(CommandHandler handler, string ipAddress, uint? limit, uint? mapId, uint? zoneId)
        {
            var sessionsMatchCount = 0;

            foreach (var session in handler.WorldManager.AllSessions)
            {
                var player = session.Player;

                // Ignore sessions on character selection screen
                if (player == null)
                    continue;

                var playerMapId = player.Location.MapId;
                var playerZoneId = player.Location.Zone;

                // Apply optional ipAddress filter
                if (!ipAddress.IsEmpty() && ipAddress != session.RemoteAddress)
                    continue;

                // Apply optional mapId filter
                if (mapId.HasValue && mapId != playerMapId)
                    continue;

                // Apply optional zoneId filter
                if (zoneId.HasValue && zoneId != playerZoneId)
                    continue;

                if (sessionsMatchCount == 0)
                {
                    //- Display the list of account/characters online on the first matched sessions
                    handler.SendSysMessage(CypherStrings.AccountListBarHeader);
                    handler.SendSysMessage(CypherStrings.AccountListHeader);
                    handler.SendSysMessage(CypherStrings.AccountListBar);
                }

                handler.SendSysMessage(CypherStrings.AccountListLine,
                                       session.AccountName,
                                       session.PlayerName,
                                       session.RemoteAddress,
                                       playerMapId,
                                       playerZoneId,
                                       session.AccountExpansion,
                                       session.Security);

                ++sessionsMatchCount;

                // Apply optional count limit
                if (sessionsMatchCount >= limit)
                    break;
            }

            // Header is printed on first matched session. If it wasn't printed then no sessions matched the criteria
            if (sessionsMatchCount == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountListEmpty);

                return true;
            }

            handler.SendSysMessage(CypherStrings.AccountListBar);

            return true;
        }

        [Command("ip", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
        private static bool HandleAccountOnlineListWithIpFilterCommand(CommandHandler handler, string ipAddress)
        {
            return HandleAccountOnlineListCommandWithParameters(handler, ipAddress, null, null, null);
        }

        [Command("limit", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
        private static bool HandleAccountOnlineListWithLimitCommand(CommandHandler handler, uint limit)
        {
            return HandleAccountOnlineListCommandWithParameters(handler, null, limit, null, null);
        }

        [Command("map", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
        private static bool HandleAccountOnlineListWithMapFilterCommand(CommandHandler handler, uint mapId)
        {
            return HandleAccountOnlineListCommandWithParameters(handler, null, null, mapId, null);
        }

        [Command("zone", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
        private static bool HandleAccountOnlineListWithZoneFilterCommand(CommandHandler handler, uint zoneId)
        {
            return HandleAccountOnlineListCommandWithParameters(handler, null, null, null, zoneId);
        }
    }

    [CommandGroup("set")]
    private class AccountSetCommands
    {
        [Command("2fa", CypherStrings.CommandAccSet2faHelp, RBACPermissions.CommandAccountSet2Fa, true)]
        private static bool HandleAccountSet2FaCommand(CommandHandler handler, string accountName, string secret)
        {
            /*uint targetAccountId = handler.AccountManager.GetId(accountName);
            if (targetAccountId == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                return false;
            }
         
            if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                return false;
         
            PreparedStatement stmt;
            if (secret == "off")
            {
                stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                stmt.AddNull(0);
                stmt.AddValue(1, targetAccountId);
                handler.ClassFactory.Resolve<LoginDatabase>().Execute(stmt);
                handler.SendSysMessage(CypherStrings.TwoFARemoveComplete);
                return true;
            }
         
            var masterKey = Global.SecretMgr.GetSecret(Secrets.TOTPMasterKey);
            if (!masterKey.IsAvailable())
            {
                handler.SendSysMessage(CypherStrings.TwoFACommandsNotSetup);
                return false;
            }
         
            var decoded = secret.FromBase32();
            if (decoded == null)
            {
                handler.SendSysMessage(CypherStrings.TwoFASecretInvalid);
                return false;
            }
            if (128 < (decoded.Length + 12 + 12))
            {
                handler.SendSysMessage(CypherStrings.TwoFASecretTooLong);
                return false;
            }
         
            if (masterKey.IsValid())
                AES.Encrypt(decoded, masterKey.GetValue());
         
            stmt = handler.ClassFactory.Resolve<LoginDatabase>().GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
            stmt.AddValue(0, decoded);
            stmt.AddValue(1, targetAccountId);
            handler.ClassFactory.Resolve<LoginDatabase>().Execute(stmt);
            handler.SendSysMessage(CypherStrings.TwoFASecretSetComplete, accountName);*/
            return true;
        }

        [Command("addon", CypherStrings.CommandAccSetAddonHelp, RBACPermissions.CommandAccountSetAddon, true)]
        private static bool HandleAccountSetAddonCommand(CommandHandler handler, [OptionalArg] string accountName, byte expansion)
        {
            uint accountId;

            if (!accountName.IsEmpty())
            {
                // Convert Account name to Upper Format
                accountName = accountName.ToUpper();

                accountId = handler.AccountManager.GetId(accountName);

                if (accountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                    return false;
                }
            }
            else
            {
                var player = handler.SelectedPlayer;

                if (!player)
                    return false;

                accountId = player.Session.AccountId;
                handler.AccountManager.GetName(accountId, out accountName);
            }

            // Let set addon state only for lesser (strong) security level
            // or to self account
            if (handler.Session != null &&
                handler.Session.AccountId != accountId &&
                handler.HasLowerSecurityAccount(null, accountId, true))
                return false;

            if (expansion > handler.Configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight))
                return false;

            var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
            var stmt = loginDB.GetPreparedStatement(LoginStatements.UPD_EXPANSION);

            stmt.AddValue(0, expansion);
            stmt.AddValue(1, accountId);

            loginDB.Execute(stmt);

            handler.SendSysMessage(CypherStrings.AccountSetaddon, accountName, accountId, expansion);

            return true;
        }

        [Command("password", CypherStrings.CommandAccSetPasswordHelp, RBACPermissions.CommandAccountSetPassword, true)]
        private static bool HandleAccountSetPasswordCommand(CommandHandler handler, string accountName, string password, string confirmPassword)
        {
            var targetAccountId = handler.AccountManager.GetId(accountName);

            if (targetAccountId == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                return false;
            }

            // can set password only for target with less security
            // This also restricts setting handler's own password
            if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                return false;

            if (!password.Equals(confirmPassword))
            {
                handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);

                return false;
            }

            var result = handler.AccountManager.ChangePassword(targetAccountId, password);

            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.CommandPassword);

                    break;
                case AccountOpResult.NameNotExist:
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                    return false;
                case AccountOpResult.PassTooLong:
                    handler.SendSysMessage(CypherStrings.PasswordTooLong);

                    return false;
                default:
                    handler.SendSysMessage(CypherStrings.CommandNotchangepassword);

                    return false;
            }

            return true;
        }

        [Command("seclevel", CypherStrings.CommandAccSetSeclevelHelp, RBACPermissions.CommandAccountSetSecLevel, true)]
        [Command("gmlevel", CypherStrings.CommandAccSetSeclevelHelp, RBACPermissions.CommandAccountSetSecLevel, true)]
        private static bool HandleAccountSetSecLevelCommand(CommandHandler handler, [OptionalArg] string accountName, byte securityLevel, int? realmId)
        {
            uint accountId;

            if (!accountName.IsEmpty())
            {
                accountId = handler.AccountManager.GetId(accountName);

                if (accountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                    return false;
                }
            }
            else
            {
                var player = handler.SelectedPlayer;

                if (!player)
                    return false;

                accountId = player.Session.AccountId;
                handler.AccountManager.GetName(accountId, out accountName);
            }

            if (securityLevel > (uint)AccountTypes.Console)
            {
                handler.SendSysMessage(CypherStrings.BadValue);

                return false;
            }

            var realmID = -1;

            if (realmId.HasValue)
                realmID = realmId.Value;

            AccountTypes playerSecurity;

            playerSecurity = handler.IsConsole ? AccountTypes.Console : handler.AccountManager.GetSecurity(handler.Session.AccountId, realmID);

            // can set security level only for target with less security and to less security that we have
            // This is also reject self apply in fact
            var targetSecurity = handler.AccountManager.GetSecurity(accountId, realmID);

            if (targetSecurity >= playerSecurity || (AccountTypes)securityLevel >= playerSecurity)
            {
                handler.SendSysMessage(CypherStrings.YoursSecurityIsLow);

                return false;
            }

            switch (realmID)
            {
                // Check and abort if the target gm has a higher rank on one of the realms and the new realm is -1
                case -1 when !handler.AccountManager.IsConsoleAccount(playerSecurity):
                {
                    var loginDB = handler.ClassFactory.Resolve<LoginDatabase>();
                    var stmt = loginDB.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ACCESS_SECLEVEL_TEST);
                    stmt.AddValue(0, accountId);
                    stmt.AddValue(1, securityLevel);

                    var result = loginDB.Query(stmt);

                    if (!result.IsEmpty())
                    {
                        handler.SendSysMessage(CypherStrings.YoursSecurityIsLow);

                        return false;
                    }

                    break;
                }
                // Check if provided realmID has a negative value other than -1
                case < -1:
                    handler.SendSysMessage(CypherStrings.InvalidRealmid);

                    return false;
            }

            handler.AccountManager.UpdateAccountAccess(null, accountId, securityLevel, realmID);

            handler.SendSysMessage(CypherStrings.YouChangeSecurity, accountName, securityLevel);

            return true;
        }

        [CommandGroup("sec")]
        private class SetSecCommands
        {
            [Command("email", CypherStrings.CommandAccSetSecEmailHelp, RBACPermissions.CommandAccountSetSecEmail, true)]
            private static bool HandleAccountSetEmailCommand(CommandHandler handler, string accountName, string email, string confirmEmail)
            {
                var targetAccountId = handler.AccountManager.GetId(accountName);

                if (targetAccountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                    return false;
                }

                // can set email only for target with less security
                // This also restricts setting handler's own email.
                if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                    return false;

                if (!email.Equals(confirmEmail))
                {
                    handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);

                    return false;
                }

                var result = handler.AccountManager.ChangeEmail(targetAccountId, email);

                switch (result)
                {
                    case AccountOpResult.Ok:
                        handler.SendSysMessage(CypherStrings.CommandEmail);
                        Log.Logger.Information("ChangeEmail: Account {0} [Id: {1}] had it's email changed to {2}.", accountName, targetAccountId, email);

                        break;
                    case AccountOpResult.NameNotExist:
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                        return false;
                    case AccountOpResult.EmailTooLong:
                        handler.SendSysMessage(CypherStrings.EmailTooLong);

                        return false;
                    default:
                        handler.SendSysMessage(CypherStrings.CommandNotchangeemail);

                        return false;
                }

                return true;
            }

            [Command("regmail", CypherStrings.CommandAccSetSecRegmailHelp, RBACPermissions.CommandAccountSetSecRegmail, true)]
            private static bool HandleAccountSetRegEmailCommand(CommandHandler handler, string accountName, string email, string confirmEmail)
            {
                var targetAccountId = handler.AccountManager.GetId(accountName);

                if (targetAccountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                    return false;
                }

                // can set email only for target with less security
                // This also restricts setting handler's own email.
                if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                    return false;

                if (!email.Equals(confirmEmail))
                {
                    handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);

                    return false;
                }

                var result = handler.AccountManager.ChangeRegEmail(targetAccountId, email);

                switch (result)
                {
                    case AccountOpResult.Ok:
                        handler.SendSysMessage(CypherStrings.CommandEmail);
                        Log.Logger.Information("ChangeRegEmail: Account {0} [Id: {1}] had it's Registration Email changed to {2}.", accountName, targetAccountId, email);

                        break;
                    case AccountOpResult.NameNotExist:
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

                        return false;
                    case AccountOpResult.EmailTooLong:
                        handler.SendSysMessage(CypherStrings.EmailTooLong);

                        return false;
                    default:
                        handler.SendSysMessage(CypherStrings.CommandNotchangeemail);

                        return false;
                }

                return true;
            }
        }
    }
}