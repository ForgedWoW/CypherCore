// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;

namespace Game;

class AccountInfoQueryHolderPerRealm : SQLQueryHolder<AccountInfoQueryLoad>
{
	public void Initialize(uint accountId, uint battlenetAccountId)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_DATA);
		stmt.AddValue(0, accountId);
		SetQuery(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm, stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_TUTORIALS);
		stmt.AddValue(0, accountId);
		SetQuery(AccountInfoQueryLoad.TutorialsIndexPerRealm, stmt);
	}
}