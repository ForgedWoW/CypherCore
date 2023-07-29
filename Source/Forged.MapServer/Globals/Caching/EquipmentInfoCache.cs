// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Creatures;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class EquipmentInfoCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly CreatureTemplateCache _creatureTemplateCache;
    private readonly IConfiguration _configuration;
    private readonly DB6Storage<ItemRecord> _itemRecords;
    private readonly DB2Manager _db2Manager;
    private readonly MultiMap<uint, Tuple<uint, EquipmentInfo>> _equipmentInfoStorage = new();

    public EquipmentInfoCache(WorldDatabase worldDatabase, CreatureTemplateCache creatureTemplateCache, IConfiguration configuration,
                              DB6Storage<ItemRecord> itemRecords, DB2Manager db2Manager)
    {
        _worldDatabase = worldDatabase;
        _creatureTemplateCache = creatureTemplateCache;
        _configuration = configuration;
        _itemRecords = itemRecords;
        _db2Manager = db2Manager;
    }

    public void Load()
    {
        var time = Time.MSTime;

        //                                                0   1        2                 3            4
        var result = _worldDatabase.Query("SELECT CreatureID, ID, ItemID1, AppearanceModID1, ItemVisual1, " +
                                          //5                 6            7       8                 9           10
                                          "ItemID2, AppearanceModID2, ItemVisual2, ItemID3, AppearanceModID3, ItemVisual3 " +
                                          "FROM creature_equip_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature equipment templates. DB table `creature_equip_template` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);

            if (_creatureTemplateCache.GetCreatureTemplate(entry) == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_equip_template WHERE CreatureID = {entry}");
                else
                    Log.Logger.Error("Creature template (CreatureID: {0}) does not exist but has a record in `creature_equip_template`", entry);

                continue;
            }

            var id = result.Read<uint>(1);

            EquipmentInfo equipmentInfo = new();

            for (var i = 0; i < SharedConst.MaxEquipmentItems; ++i)
            {
                equipmentInfo.Items[i].ItemId = result.Read<uint>(2 + i * 3);
                equipmentInfo.Items[i].AppearanceModId = result.Read<ushort>(3 + i * 3);
                equipmentInfo.Items[i].ItemVisual = result.Read<ushort>(4 + i * 3);

                if (equipmentInfo.Items[i].ItemId == 0)
                    continue;

                if (!_itemRecords.TryGetValue(equipmentInfo.Items[i].ItemId, out var dbcItem))
                {
                    Log.Logger.Error("Unknown item (ID: {0}) in creature_equip_template.ItemID{1} for CreatureID  = {2}, forced to 0.",
                                     equipmentInfo.Items[i].ItemId,
                                     i + 1,
                                     entry);

                    equipmentInfo.Items[i].ItemId = 0;

                    continue;
                }

                if (_db2Manager.GetItemModifiedAppearance(equipmentInfo.Items[i].ItemId, equipmentInfo.Items[i].AppearanceModId) == null)
                {
                    Log.Logger.Error("Unknown item appearance for (ID: {0}, AppearanceModID: {1}) pair in creature_equip_template.ItemID{2} creature_equip_template.AppearanceModID{3} " +
                                     "for CreatureID: {4} and ID: {5}, forced to default.",
                                     equipmentInfo.Items[i].ItemId,
                                     equipmentInfo.Items[i].AppearanceModId,
                                     i + 1,
                                     i + 1,
                                     entry,
                                     id);

                    var defaultAppearance = _db2Manager.GetDefaultItemModifiedAppearance(equipmentInfo.Items[i].ItemId);

                    if (defaultAppearance != null)
                        equipmentInfo.Items[i].AppearanceModId = (ushort)defaultAppearance.ItemAppearanceModifierID;
                    else
                        equipmentInfo.Items[i].AppearanceModId = 0;

                    continue;
                }

                if (dbcItem.inventoryType != InventoryType.Weapon &&
                    dbcItem.inventoryType != InventoryType.Shield &&
                    dbcItem.inventoryType != InventoryType.Ranged &&
                    dbcItem.inventoryType != InventoryType.Weapon2Hand &&
                    dbcItem.inventoryType != InventoryType.WeaponMainhand &&
                    dbcItem.inventoryType != InventoryType.WeaponOffhand &&
                    dbcItem.inventoryType != InventoryType.Holdable &&
                    dbcItem.inventoryType != InventoryType.Thrown &&
                    dbcItem.inventoryType != InventoryType.RangedRight)
                {
                    Log.Logger.Error("Item (ID {0}) in creature_equip_template.ItemID{1} for CreatureID  = {2} is not equipable in a hand, forced to 0.",
                                     equipmentInfo.Items[i].ItemId,
                                     i + 1,
                                     entry);

                    equipmentInfo.Items[i].ItemId = 0;
                }
            }

            _equipmentInfoStorage.Add(entry, Tuple.Create(id, equipmentInfo));
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} equipment templates in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }

    public EquipmentInfo GetEquipmentInfo(uint entry, int id)
    {
        if (_equipmentInfoStorage.TryGetValue(entry, out var equip))
            return null;

        if (id == -1)
            return equip[RandomHelper.IRand(0, equip.Count - 1)].Item2;

        return equip.Find(p => p.Item1 == id)?.Item2;
    }
}