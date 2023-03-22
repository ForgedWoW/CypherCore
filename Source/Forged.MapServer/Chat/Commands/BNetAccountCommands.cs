﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Database;

namespace Game.Chat.Commands;

[CommandGroup("bnetaccount")]
class BNetAccountCommands
{
	[Command("create", RBACPermissions.CommandBnetAccountCreate, true)]
	static bool HandleAccountCreateCommand(CommandHandler handler, string accountName, string password, bool? createGameAccount)
	{
		if (accountName.IsEmpty() || !accountName.Contains('@'))
		{
			handler.SendSysMessage(CypherStrings.AccountInvalidBnetName);

			return false;
		}

		switch (Global.BNetAccountMgr.CreateBattlenetAccount(accountName, password, createGameAccount.GetValueOrDefault(true), out var gameAccountName))
		{
			case AccountOpResult.Ok:
				if (createGameAccount.HasValue && createGameAccount.Value)
					handler.SendSysMessage(CypherStrings.AccountCreatedBnetWithGame, accountName, gameAccountName);
				else
					handler.SendSysMessage(CypherStrings.AccountCreated, accountName);

				if (handler.Session != null)
					Log.outInfo(LogFilter.Player,
								"Account: {0} (IP: {1}) Character:[{2}] ({3}) created Battle.net account {4}{5}{6}",
								handler.Session.AccountId,
								handler.Session.RemoteAddress,
								handler.Session.Player.GetName(),
								handler.Session.Player.GUID.ToString(),
								accountName,
								createGameAccount.Value ? " with game account " : "",
								createGameAccount.Value ? gameAccountName : "");

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
			default:
				break;
		}

		return true;
	}

	[Command("gameaccountcreate", RBACPermissions.CommandBnetAccountCreateGame, true)]
	static bool HandleGameAccountCreateCommand(CommandHandler handler, string bnetAccountName)
	{
		var accountId = Global.BNetAccountMgr.GetId(bnetAccountName);

		if (accountId == 0)
		{
			handler.SendSysMessage(CypherStrings.AccountNotExist, bnetAccountName);

			return false;
		}

		var index = (byte)(Global.BNetAccountMgr.GetMaxIndex(accountId) + 1);
		var accountName = accountId.ToString() + '#' + index;

		// Generate random hex string for password, these accounts must not be logged on with GRUNT
		var randPassword = Array.Empty<byte>().GenerateRandomKey(8);

		switch (Global.AccountMgr.CreateAccount(accountName, randPassword.ToHexString(), bnetAccountName, accountId, index))
		{
			case AccountOpResult.Ok:
				handler.SendSysMessage(CypherStrings.AccountCreated, accountName);

				if (handler.Session != null)
					Log.outInfo(LogFilter.Player,
								"Account: {0} (IP: {1}) Character:[{2}] ({3}) created Account {4} (Email: '{5}')",
								handler.Session.AccountId,
								handler.Session.RemoteAddress,
								handler.Session.Player.GetName(),
								handler.Session.Player.GUID.ToString(),
								accountName,
								bnetAccountName);

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

	[Command("link", RBACPermissions.CommandBnetAccountLink, true)]
	static bool HandleAccountLinkCommand(CommandHandler handler, string bnetAccountName, string gameAccountName)
	{
		switch (Global.BNetAccountMgr.LinkWithGameAccount(bnetAccountName, gameAccountName))
		{
			case AccountOpResult.Ok:
				handler.SendSysMessage(CypherStrings.AccountBnetLinked, bnetAccountName, gameAccountName);

				break;
			case AccountOpResult.NameNotExist:
				handler.SendSysMessage(CypherStrings.AccountOrBnetDoesNotExist, bnetAccountName, gameAccountName);

				break;
			case AccountOpResult.BadLink:
				handler.SendSysMessage(CypherStrings.AccountAlreadyLinked, gameAccountName);

				break;
			default:
				break;
		}

		return true;
	}

	[Command("listgameaccounts", RBACPermissions.CommandBnetAccountListGameAccounts, true)]
	static bool HandleListGameAccountsCommand(CommandHandler handler, string battlenetAccountName)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST);
		stmt.AddValue(0, battlenetAccountName);

		var accountList = DB.Login.Query(stmt);

		if (!accountList.IsEmpty())
		{
			var formatDisplayName = new Func<string, string>(name =>
			{
				var index = name.IndexOf('#');

				if (index > 0)
					return "WoW" + name[++index..];
				else
					return name;
			});

			handler.SendSysMessage("----------------------------------------------------");
			handler.SendSysMessage(CypherStrings.AccountBnetListHeader);
			handler.SendSysMessage("----------------------------------------------------");

			do
			{
				handler.SendSysMessage("| {0,10} | {1,16} | {2,16} |", accountList.Read<uint>(0), accountList.Read<string>(1), formatDisplayName(accountList.Read<string>(1)));
			} while (accountList.NextRow());

			handler.SendSysMessage("----------------------------------------------------");
		}
		else
		{
			handler.SendSysMessage(CypherStrings.AccountBnetListNoAccounts, battlenetAccountName);
		}

		return true;
	}

	[Command("password", RBACPermissions.CommandBnetAccountPassword, true)]
	static bool HandleAccountPasswordCommand(CommandHandler handler, string oldPassword, string newPassword, string passwordConfirmation)
	{
		// We compare the old, saved password to the entered old password - no chance for the unauthorized.
		if (!Global.BNetAccountMgr.CheckPassword(handler.Session.BattlenetAccountId, oldPassword))
		{
			handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);

			Log.outInfo(LogFilter.Player,
						"Battle.net account: {0} (IP: {1}) Character:[{2}] ({3}) Tried to change password, but the provided old password is wrong.",
						handler.Session.BattlenetAccountId,
						handler.Session.RemoteAddress,
						handler.Session.Player.GetName(),
						handler.Session.Player.GUID.ToString());

			return false;
		}

		// Making sure that newly entered password is correctly entered.
		if (newPassword != passwordConfirmation)
		{
			handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);

			return false;
		}

		// Changes password and prints result.
		var result = Global.BNetAccountMgr.ChangePassword(handler.Session.BattlenetAccountId, newPassword);

		switch (result)
		{
			case AccountOpResult.Ok:
				handler.SendSysMessage(CypherStrings.CommandPassword);

				Log.outInfo(LogFilter.Player,
							"Battle.net account: {0} (IP: {1}) Character:[{2}] ({3}) Changed Password.",
							handler.Session.BattlenetAccountId,
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

	[Command("unlink", RBACPermissions.CommandBnetAccountUnlink, true)]
	static bool HandleAccountUnlinkCommand(CommandHandler handler, string gameAccountName)
	{
		switch (Global.BNetAccountMgr.UnlinkGameAccount(gameAccountName))
		{
			case AccountOpResult.Ok:
				handler.SendSysMessage(CypherStrings.AccountBnetUnlinked, gameAccountName);

				break;
			case AccountOpResult.NameNotExist:
				handler.SendSysMessage(CypherStrings.AccountNotExist, gameAccountName);

				break;
			case AccountOpResult.BadLink:
				handler.SendSysMessage(CypherStrings.AccountBnetNotLinked, gameAccountName);

				break;
			default:
				break;
		}

		return true;
	}

	[CommandGroup("lock")]
	class AccountLockCommands
	{
		[Command("country", RBACPermissions.CommandBnetAccountLockCountry, true)]
		static bool HandleAccountLockCountryCommand(CommandHandler handler, bool state)
		{
			/*if (state)
			{
				if (IpLocationRecord const* location = sIPLocation->GetLocationRecord(handler->GetSession()->GetRemoteAddress()))
		{
					LoginDatabasePreparedStatement* stmt = DB.Login.GetPreparedStatement(LOGIN_UPD_BNET_ACCOUNT_LOCK_CONTRY);
					stmt->setString(0, location->CountryCode);
					stmt->setUInt32(1, handler->GetSession()->GetBattlenetAccountId());
					LoginDatabase.Execute(stmt);
					handler->PSendSysMessage(LANG_COMMAND_ACCLOCKLOCKED);
				}
		else
				{
					handler->PSendSysMessage("IP2Location] No information");
					TC_LOG_DEBUG("server.bnetserver", "IP2Location] No information");
				}
			}
			else
			{
				LoginDatabasePreparedStatement* stmt = DB.Login.GetPreparedStatement(LOGIN_UPD_BNET_ACCOUNT_LOCK_CONTRY);
				stmt->setString(0, "00");
				stmt->setUInt32(1, handler->GetSession()->GetBattlenetAccountId());
				LoginDatabase.Execute(stmt);
				handler->PSendSysMessage(LANG_COMMAND_ACCLOCKUNLOCKED);
			}
			*/
			return true;
		}

		[Command("ip", RBACPermissions.CommandBnetAccountLockIp, true)]
		static bool HandleAccountLockIpCommand(CommandHandler handler, bool state)
		{
			var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK);

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

			stmt.AddValue(1, handler.Session.BattlenetAccountId);

			DB.Login.Execute(stmt);

			return true;
		}
	}

	[CommandGroup("set")]
	class AccountSetCommands
	{
		[Command("password", RBACPermissions.CommandBnetAccountSetPassword, true)]
		static bool HandleAccountSetPasswordCommand(CommandHandler handler, string accountName, string password, string passwordConfirmation)
		{
			var targetAccountId = Global.BNetAccountMgr.GetId(accountName);

			if (targetAccountId == 0)
			{
				handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

				return false;
			}

			if (password != passwordConfirmation)
			{
				handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);

				return false;
			}

			var result = Global.BNetAccountMgr.ChangePassword(targetAccountId, password);

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
					break;
			}

			return true;
		}
	}
}