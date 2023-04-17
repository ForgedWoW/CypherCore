// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.World;
using Framework.Database;

namespace Forged.MapServer.Server;

internal class AccountInfoQueryHolder : SQLQueryHolder<AccountInfoQueryLoad>
{
    private readonly LoginDatabase _loginDatabase;

    public AccountInfoQueryHolder(LoginDatabase loginDatabase)
    {
        _loginDatabase = loginDatabase;
    }

    public void Initialize(uint accountId, uint battlenetAccountId)
    {
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOYS);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.GlobalAccountToys, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_BATTLE_PETS);
        stmt.AddValue(0, battlenetAccountId);
        stmt.AddValue(1, WorldManager.Realm.Id.Index);
        SetQuery(AccountInfoQueryLoad.BattlePets, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_BATTLE_PET_SLOTS);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.BattlePetSlot, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_HEIRLOOMS);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.GlobalAccountHeirlooms, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_MOUNTS);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.Mounts, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SelBnetCharacterCountsByAccountId);
        stmt.AddValue(0, accountId);
        SetQuery(AccountInfoQueryLoad.GlobalRealmCharacterCounts, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_APPEARANCES);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.ItemAppearances, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_FAVORITE_APPEARANCES);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.ItemFavoriteAppearances, stmt);

        stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_TRANSMOG_ILLUSIONS);
        stmt.AddValue(0, battlenetAccountId);
        SetQuery(AccountInfoQueryLoad.TransmogIllusions, stmt);
    }
}