// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Framework.Constants;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.LootManagement;

public class LootFactory
{
    private readonly ConditionManager _conditionManager;
    private readonly GameObjectManager _objectManager;
    private readonly DB2Manager _db2Manager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly LootStorage _lootStorage;
    private readonly IConfiguration _configuration;

    public LootFactory(ConditionManager conditionManager, GameObjectManager objectManager,
                       DB2Manager db2Manager, ObjectAccessor objectAccessor, LootStorage lootStorage, IConfiguration configuration)
    {
        _conditionManager = conditionManager;
        _objectManager = objectManager;
        _db2Manager = db2Manager;
        _objectAccessor = objectAccessor;
        _lootStorage = lootStorage;
        _configuration = configuration;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, uint lootId, LootStorageType store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var loot = new Loot(map, owner, type, null, _conditionManager, _objectManager, _db2Manager, _objectAccessor, _lootStorage, _configuration);
        loot.FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);

        return loot;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, PlayerGroup group, uint dugeonEncounterId, uint lootId, LootStorageType store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var loot = new Loot(map, owner, type, group, _conditionManager, _objectManager, _db2Manager, _objectAccessor, _lootStorage, _configuration);
        loot.SetDungeonEncounterId(dugeonEncounterId);
        loot.FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);

        return loot;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, PlayerGroup group, uint lootId, LootStorageType store, Player lootOwner, bool personal, bool noEmptyError = false, LootModes lootMode = LootModes.Default, ItemContext context = 0)
    {
        var loot = new Loot(map, owner, type, group, _conditionManager, _objectManager, _db2Manager, _objectAccessor, _lootStorage, _configuration);
        loot.FillLoot(lootId, store, lootOwner, personal, noEmptyError, lootMode, context);

        return loot;
    }

    public Loot GenerateLoot(Map map, ObjectGuid owner, LootType type, PlayerGroup group = null)
    {
        return new Loot(map, owner, type, group, _conditionManager, _objectManager, _db2Manager, _objectAccessor, _lootStorage, _configuration);
    }
}