// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Security.Cryptography;
using System.Text;
using Framework.Database;

namespace Game.Common.Accounts;

public sealed class BNetAccountManager
{
	public AccountOpResult CreateBattlenetAccount(string email, string password, bool withGameAccount, out string gameAccountName)
	{
		gameAccountName = "";

		if (email.IsEmpty() || email.Length > 320)
			return AccountOpResult.NameTooLong;

		if (password.IsEmpty() || password.Length > 16)
			return AccountOpResult.PassTooLong;

		if (GetId(email) != 0)
			return AccountOpResult.NameAlreadyExist;

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ACCOUNT);
		stmt.AddValue(0, email);
		stmt.AddValue(1, CalculateShaPassHash(email.ToUpper(), password.ToUpper()));
		DB.Login.DirectExecute(stmt);

		var newAccountId = GetId(email);

		if (withGameAccount)
		{
			gameAccountName = newAccountId + "#1";
			Global.AccountMgr.CreateAccount(gameAccountName, password, email, newAccountId, 1);
		}

		return AccountOpResult.Ok;
	}

	public AccountOpResult ChangePassword(uint accountId, string newPassword)
	{
		if (!GetName(accountId, out var username))
			return AccountOpResult.NameNotExist;

		if (newPassword.Length > 16)
			return AccountOpResult.PassTooLong;

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_PASSWORD);
		stmt.AddValue(0, CalculateShaPassHash(username.ToUpper(), newPassword.ToUpper()));
		stmt.AddValue(1, accountId);
		DB.Login.DirectExecute(stmt);

		return AccountOpResult.Ok;
	}

	public bool CheckPassword(uint accountId, string password)
	{
		if (!GetName(accountId, out var username))
			return false;

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_CHECK_PASSWORD);
		stmt.AddValue(0, accountId);
		stmt.AddValue(1, CalculateShaPassHash(username.ToUpper(), password.ToUpper()));

		return !DB.Login.Query(stmt).IsEmpty();
	}

	public AccountOpResult LinkWithGameAccount(string email, string gameAccountName)
	{
		var bnetAccountId = GetId(email);

		if (bnetAccountId == 0)
			return AccountOpResult.NameNotExist;

		var gameAccountId = Global.AccountMgr.GetId(gameAccountName);

		if (gameAccountId == 0)
			return AccountOpResult.NameNotExist;

		if (GetIdByGameAccount(gameAccountId) != 0)
			return AccountOpResult.BadLink;

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LINK);
		stmt.AddValue(0, bnetAccountId);
		stmt.AddValue(1, GetMaxIndex(bnetAccountId) + 1);
		stmt.AddValue(2, gameAccountId);
		DB.Login.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public AccountOpResult UnlinkGameAccount(string gameAccountName)
	{
		var gameAccountId = Global.AccountMgr.GetId(gameAccountName);

		if (gameAccountId == 0)
			return AccountOpResult.NameNotExist;

		if (GetIdByGameAccount(gameAccountId) == 0)
			return AccountOpResult.BadLink;

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LINK);
		stmt.AddNull(0);
		stmt.AddNull(1);
		stmt.AddValue(2, gameAccountId);
		DB.Login.Execute(stmt);

		return AccountOpResult.Ok;
	}

	public uint GetId(string username)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_EMAIL);
		stmt.AddValue(0, username);
		var result = DB.Login.Query(stmt);

		if (!result.IsEmpty())
			return result.Read<uint>(0);

		return 0;
	}

	public bool GetName(uint accountId, out string name)
	{
		name = "";
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_EMAIL_BY_ID);
		stmt.AddValue(0, accountId);
		var result = DB.Login.Query(stmt);

		if (!result.IsEmpty())
		{
			name = result.Read<string>(0);

			return true;
		}

		return false;
	}

	public uint GetIdByGameAccount(uint gameAccountId)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_GAME_ACCOUNT);
		stmt.AddValue(0, gameAccountId);
		var result = DB.Login.Query(stmt);

		if (!result.IsEmpty())
			return result.Read<uint>(0);

		return 0;
	}

	public QueryCallback GetIdByGameAccountAsync(uint gameAccountId)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_GAME_ACCOUNT);
		stmt.AddValue(0, gameAccountId);

		return DB.Login.AsyncQuery(stmt);
	}

	public byte GetMaxIndex(uint accountId)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_MAX_ACCOUNT_INDEX);
		stmt.AddValue(0, accountId);
		var result = DB.Login.Query(stmt);

		if (!result.IsEmpty())
			return result.Read<byte>(0);

		return 0;
	}

	public string CalculateShaPassHash(string name, string password)
	{
		var sha256 = SHA256.Create();
		var i = sha256.ComputeHash(Encoding.UTF8.GetBytes(name));

		return sha256.ComputeHash(Encoding.UTF8.GetBytes(i.ToHexString() + ":" + password)).ToHexString(true);
	}
}
