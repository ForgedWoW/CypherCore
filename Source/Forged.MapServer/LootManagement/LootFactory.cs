// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Autofac;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Framework.Constants;
using Game.Common;

namespace Forged.MapServer.LootManagement;

public class LootFactory
{
    private readonly ClassFactory _classFactory;

    public LootFactory(ClassFactory classFactory)
    {
        _classFactory = classFactory;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, uint lootId, LootStorageType store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var loot = _classFactory.Resolve<Loot>(new PositionalParameter(0, map), new PositionalParameter(1, owner), new PositionalParameter(2, type));
        loot.FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);

        return loot;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, PlayerGroup group, uint dugeonEncounterId, uint lootId, LootStorageType store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var loot = _classFactory.Resolve<Loot>(new PositionalParameter(0, map), new PositionalParameter(1, owner), new PositionalParameter(2, type), new PositionalParameter(3, group));
        loot.SetDungeonEncounterId(dugeonEncounterId);
        loot.FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);

        return loot;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, PlayerGroup group, uint lootId, LootStorageType store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var loot = _classFactory.Resolve<Loot>(new PositionalParameter(0, map), new PositionalParameter(1, owner), new PositionalParameter(2, type), new PositionalParameter(3, group));
        loot.FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);

        return loot;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, PlayerGroup group = null)
    {
        return _classFactory.Resolve<Loot>(new PositionalParameter(0, map), new PositionalParameter(1, owner), new PositionalParameter(2, type), new PositionalParameter(3, group));
    }
}