// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Database;

namespace Forged.MapServer.Entities.Items;

public class AzeriteEmpoweredItemFactory
{
    private readonly CharacterDatabase _characterDatabase;

    public AzeriteEmpoweredItemFactory(CharacterDatabase characterDatabase)
    {
        _characterDatabase = characterDatabase;
    }

    public void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }
}