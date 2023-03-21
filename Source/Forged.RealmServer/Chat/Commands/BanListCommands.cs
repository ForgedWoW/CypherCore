// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Database;

namespace Forged.RealmServer.Chat.Commands;

[CommandGroup("banlist")]
class BanListCommands
{
	[Command("account", RBACPermissions.CommandBanlistAccount, true)]
	static bool HandleBanListAccountCommand(CommandHandler handler, [OptionalArg] string filter)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.DelExpiredIpBans);
		DB.Login.Execute(stmt);

		SQLResult result;

		if (filter.IsEmpty())
		{
			stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_ALL);
			result = DB.Login.Query(stmt);
		}
		else
		{
			stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_FILTER);
			stmt.AddValue(0, filter);
			result = DB.Login.Query(stmt);
		}

		if (result.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.BanlistNoaccount);

			return true;
		}

		return HandleBanListHelper(result, handler);
	}

	[Command("character", RBACPermissions.CommandBanlistCharacter, true)]
	static bool HandleBanListCharacterCommand(CommandHandler handler, string filter)
	{
		if (filter.IsEmpty())
			return false;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUID_BY_NAME_FILTER);
		stmt.AddValue(0, filter);
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.BanlistNocharacter);

			return true;
		}

		handler.SendSysMessage(CypherStrings.BanlistMatchingcharacter);

		// Chat short output
		if (handler.Session)
		{
			do
			{
				var stmt2 = DB.Characters.GetPreparedStatement(CharStatements.SEL_BANNED_NAME);
				stmt2.AddValue(0, result.Read<ulong>(0));
				var banResult = DB.Characters.Query(stmt2);

				if (!banResult.IsEmpty())
					handler.SendSysMessage(banResult.Read<string>(0));
			} while (result.NextRow());
		}
		// Console wide output
		else
		{
			handler.SendSysMessage(CypherStrings.BanlistCharacters);
			handler.SendSysMessage(" =============================================================================== ");
			handler.SendSysMessage(CypherStrings.BanlistCharactersHeader);

			do
			{
				handler.SendSysMessage("-------------------------------------------------------------------------------");

				var char_name = result.Read<string>(1);

				var stmt2 = DB.Characters.GetPreparedStatement(CharStatements.SEL_BANINFO_LIST);
				stmt2.AddValue(0, result.Read<ulong>(0));
				var banInfo = DB.Characters.Query(stmt2);

				if (!banInfo.IsEmpty())
					do
					{
						var timeBan = banInfo.Read<long>(0);
						var tmBan = Time.UnixTimeToDateTime(timeBan);
						var bannedby = banInfo.Read<string>(2).Substring(0, 15);
						var banreason = banInfo.Read<string>(3).Substring(0, 15);

						if (banInfo.Read<long>(0) == banInfo.Read<long>(1))
						{
							handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
													char_name,
													tmBan.Year % 100,
													tmBan.Month + 1,
													tmBan.Day,
													tmBan.Hour,
													tmBan.Minute,
													bannedby,
													banreason);
						}
						else
						{
							var timeUnban = banInfo.Read<long>(1);
							var tmUnban = Time.UnixTimeToDateTime(timeUnban);

							handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|{6:D2}-{7:D2}-{8:D2} {9:D2}:{10:D2}|{11}|{12}|",
													char_name,
													tmBan.Year % 100,
													tmBan.Month + 1,
													tmBan.Day,
													tmBan.Hour,
													tmBan.Minute,
													tmUnban.Year % 100,
													tmUnban.Month + 1,
													tmUnban.Day,
													tmUnban.Hour,
													tmUnban.Minute,
													bannedby,
													banreason);
						}
					} while (banInfo.NextRow());
			} while (result.NextRow());

			handler.SendSysMessage(" =============================================================================== ");
		}

		return true;
	}

	[Command("ip", RBACPermissions.CommandBanlistIp, true)]
	static bool HandleBanListIPCommand(CommandHandler handler, [OptionalArg] string filter)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.DelExpiredIpBans);
		DB.Login.Execute(stmt);

		SQLResult result;

		if (filter.IsEmpty())
		{
			stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_IP_BANNED_ALL);
			result = DB.Login.Query(stmt);
		}
		else
		{
			stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_IP_BANNED_BY_IP);
			stmt.AddValue(0, filter);
			result = DB.Login.Query(stmt);
		}

		if (result.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.BanlistNoip);

			return true;
		}

		handler.SendSysMessage(CypherStrings.BanlistMatchingip);

		// Chat short output
		if (handler.Session)
		{
			do
			{
				handler.SendSysMessage("{0}", result.Read<string>(0));
			} while (result.NextRow());
		}
		// Console wide output
		else
		{
			handler.SendSysMessage(CypherStrings.BanlistIps);
			handler.SendSysMessage(" ===============================================================================");
			handler.SendSysMessage(CypherStrings.BanlistIpsHeader);

			do
			{
				handler.SendSysMessage("-------------------------------------------------------------------------------");

				long timeBan = result.Read<uint>(1);
				var tmBan = Time.UnixTimeToDateTime(timeBan);
				var bannedby = result.Read<string>(3).Substring(0, 15);
				var banreason = result.Read<string>(4).Substring(0, 15);

				if (result.Read<uint>(1) == result.Read<uint>(2))
				{
					handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
											result.Read<string>(0),
											tmBan.Year % 100,
											tmBan.Month + 1,
											tmBan.Day,
											tmBan.Hour,
											tmBan.Minute,
											bannedby,
											banreason);
				}
				else
				{
					long timeUnban = result.Read<uint>(2);
					DateTime tmUnban;
					tmUnban = Time.UnixTimeToDateTime(timeUnban);

					handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|{6:D2}-{7:D2}-{8:D2} {9:D2}:{10:D2}|{11}|{12}|",
											result.Read<string>(0),
											tmBan.Year % 100,
											tmBan.Month + 1,
											tmBan.Day,
											tmBan.Hour,
											tmBan.Minute,
											tmUnban.Year % 100,
											tmUnban.Month + 1,
											tmUnban.Day,
											tmUnban.Hour,
											tmUnban.Minute,
											bannedby,
											banreason);
				}
			} while (result.NextRow());

			handler.SendSysMessage(" ===============================================================================");
		}

		return true;
	}

	static bool HandleBanListHelper(SQLResult result, CommandHandler handler)
	{
		handler.SendSysMessage(CypherStrings.BanlistMatchingaccount);

		// Chat short output
		if (handler.Session)
		{
			do
			{
				var accountid = result.Read<uint>(0);

				var banResult = DB.Login.Query("SELECT account.username FROM account, account_banned WHERE account_banned.id='{0}' AND account_banned.id=account.id", accountid);

				if (!banResult.IsEmpty())
					handler.SendSysMessage(banResult.Read<string>(0));
			} while (result.NextRow());
		}
		// Console wide output
		else
		{
			handler.SendSysMessage(CypherStrings.BanlistAccounts);
			handler.SendSysMessage(" ===============================================================================");
			handler.SendSysMessage(CypherStrings.BanlistAccountsHeader);

			do
			{
				handler.SendSysMessage("-------------------------------------------------------------------------------");

				var accountId = result.Read<uint>(0);

				string accountName;

				// "account" case, name can be get in same query
				if (result.GetFieldCount() > 1)
					accountName = result.Read<string>(1);
				// "character" case, name need extract from another DB
				else
					Global.AccountMgr.GetName(accountId, out accountName);

				// No SQL injection. id is uint32.
				var banInfo = DB.Login.Query("SELECT bandate, unbandate, bannedby, banreason FROM account_banned WHERE id = {0} ORDER BY unbandate", accountId);

				if (!banInfo.IsEmpty())
					do
					{
						long timeBan = banInfo.Read<uint>(0);
						DateTime tmBan;
						tmBan = Time.UnixTimeToDateTime(timeBan);
						var bannedby = banInfo.Read<string>(2).Substring(0, 15);
						var banreason = banInfo.Read<string>(3).Substring(0, 15);

						if (banInfo.Read<uint>(0) == banInfo.Read<uint>(1))
						{
							handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
													accountName.Substring(0, 15),
													tmBan.Year % 100,
													tmBan.Month + 1,
													tmBan.Day,
													tmBan.Hour,
													tmBan.Minute,
													bannedby,
													banreason);
						}
						else
						{
							long timeUnban = banInfo.Read<uint>(1);
							DateTime tmUnban;
							tmUnban = Time.UnixTimeToDateTime(timeUnban);

							handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|{6:D2}-{7:D2}-{8:D2} {9:D2}:{10:D2}|{11}|{12}|",
													accountName.Substring(0, 15),
													tmBan.Year % 100,
													tmBan.Month + 1,
													tmBan.Day,
													tmBan.Hour,
													tmBan.Minute,
													tmUnban.Year % 100,
													tmUnban.Month + 1,
													tmUnban.Day,
													tmUnban.Hour,
													tmUnban.Minute,
													bannedby,
													banreason);
						}
					} while (banInfo.NextRow());
			} while (result.NextRow());

			handler.SendSysMessage(" ===============================================================================");
		}

		return true;
	}
}