// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Entities.Items;

public class ItemEnchantmentManager
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<uint, RandomBonusListIds> _storage = new();
    private readonly WorldDatabase _worldDatabase;

    public ItemEnchantmentManager(WorldDatabase worldDatabase, DB2Manager db2Manager, CliDB cliDB, GameObjectManager objectManager)
    {
        _worldDatabase = worldDatabase;
        _db2Manager = db2Manager;
        _cliDB = cliDB;
        _objectManager = objectManager;
    }

    public uint GenerateItemRandomBonusListId(uint itemID)
    {
        var itemProto = _objectManager.GetItemTemplate(itemID);

        if (itemProto == null)
            return 0;

        // item must have one from this field values not null if it can have random enchantments
        if (itemProto.RandomBonusListTemplateId == 0)
            return 0;

        if (_storage.TryGetValue(itemProto.RandomBonusListTemplateId, out var tab))
            return tab.BonusListIDs.SelectRandomElementByWeight(x => (float)tab.Chances[tab.BonusListIDs.IndexOf(x)]);

        Log.Logger.Error($"Item RandomBonusListTemplateId id {itemProto.RandomBonusListTemplateId} used in `item_template_addon` but it does not have records in `item_random_bonus_list_template` table.");

        return 0;

        //todo fix me this is ulgy
    }

    public float GetRandomPropertyPoints(uint itemLevel, ItemQuality quality, InventoryType inventoryType, uint subClass)
    {
        uint propIndex;

        switch (inventoryType)
        {
            case InventoryType.Head:
            case InventoryType.Body:
            case InventoryType.Chest:
            case InventoryType.Legs:
            case InventoryType.Ranged:
            case InventoryType.Weapon2Hand:
            case InventoryType.Robe:
            case InventoryType.Thrown:
                propIndex = 0;

                break;
            case InventoryType.RangedRight:
                if ((ItemSubClassWeapon)subClass == ItemSubClassWeapon.Wand)
                    propIndex = 3;
                else
                    propIndex = 0;

                break;
            case InventoryType.Weapon:
            case InventoryType.WeaponMainhand:
            case InventoryType.WeaponOffhand:
                propIndex = 3;

                break;
            case InventoryType.Shoulders:
            case InventoryType.Waist:
            case InventoryType.Feet:
            case InventoryType.Hands:
            case InventoryType.Trinket:
                propIndex = 1;

                break;
            case InventoryType.Neck:
            case InventoryType.Wrists:
            case InventoryType.Finger:
            case InventoryType.Shield:
            case InventoryType.Cloak:
            case InventoryType.Holdable:
                propIndex = 2;

                break;
            case InventoryType.Relic:
                propIndex = 4;

                break;
            default:
                return 0;
        }

        if (!_cliDB.RandPropPointsStorage.TryGetValue(itemLevel, out var randPropPointsEntry))
            return 0;

        return quality switch
        {
            ItemQuality.Uncommon  => randPropPointsEntry.GoodF[propIndex],
            ItemQuality.Rare      => randPropPointsEntry.SuperiorF[propIndex],
            ItemQuality.Heirloom  => randPropPointsEntry.SuperiorF[propIndex],
            ItemQuality.Epic      => randPropPointsEntry.EpicF[propIndex],
            ItemQuality.Legendary => randPropPointsEntry.EpicF[propIndex],
            ItemQuality.Artifact  => randPropPointsEntry.EpicF[propIndex],
            _                     => 0
        };
    }

    public void LoadItemRandomBonusListTemplates()
    {
        var oldMsTime = Time.MSTime;

        _storage.Clear();

        //                                         0   1            2
        var result = _worldDatabase.Query("SELECT Id, BonusListID, Chance FROM item_random_bonus_list_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Item Enchantment definitions. DB table `item_enchantment_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);
            var bonusListId = result.Read<uint>(1);
            var chance = result.Read<float>(2);

            if (_db2Manager.GetItemBonusList(bonusListId) == null)
            {
                Log.Logger.Error($"Bonus list {bonusListId} used in `item_random_bonus_list_template` by id {id} doesn't have exist in ItemBonus.db2");

                continue;
            }

            if (chance is < 0.000001f or > 100.0f)
            {
                Log.Logger.Error($"Bonus list {bonusListId} used in `item_random_bonus_list_template` by id {id} has invalid chance {chance}");

                continue;
            }

            if (!_storage.ContainsKey(id))
                _storage[id] = new RandomBonusListIds();

            var ids = _storage[id];
            ids.BonusListIDs.Add(bonusListId);
            ids.Chances.Add(chance);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} Random item bonus list definitions in {Time.GetMSTimeDiffToNow(oldMsTime)} ms");
    }
}