// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities.Items;

public class AzeriteItemFactory
{
    private readonly CharacterDatabase _characterDatabase;

    public AzeriteItemFactory(CharacterDatabase characterDatabase)
    {
        _characterDatabase = characterDatabase;
    }

    public void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
        stmt.AddValue(0, itemGuid);
        _characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public GameObject FindHeartForge(Player owner)
    {
        var forge = owner.Location.FindNearestGameObjectOfType(GameObjectTypes.ItemForge, 40.0f);

        return forge?.Template.ItemForge.ForgeType == 2 ? forge : null;
    }
}