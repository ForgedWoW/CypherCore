// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Transmogification;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Entities.Players;

public class CollectionMgr
{
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly DB2Manager _db2Manager;
    private readonly Dictionary<uint, FavoriteAppearanceState> _favoriteAppearances = new();
    private readonly LoginDatabase _loginDatabase;
    private readonly MountCache _mountCache;
    private readonly GameObjectManager _objectManager;
    private readonly WorldSession _owner;

    private readonly uint[] _playerClassByArmorSubclass =
    {
        (int)PlayerClass.ClassMaskAllPlayable,                                                                                                                          //ITEM_SUBCLASS_ARMOR_MISCELLANEOUS
        (1 << ((int)PlayerClass.Priest - 1)) | (1 << ((int)PlayerClass.Mage - 1)) | (1 << ((int)PlayerClass.Warlock - 1)),                                              //ITEM_SUBCLASS_ARMOR_CLOTH
        (1 << ((int)PlayerClass.Rogue - 1)) | (1 << ((int)PlayerClass.Monk - 1)) | (1 << ((int)PlayerClass.Druid - 1)) | (1 << ((int)PlayerClass.DemonHunter - 1)),     //ITEM_SUBCLASS_ARMOR_LEATHER
        (1 << ((int)PlayerClass.Hunter - 1)) | (1 << ((int)PlayerClass.Shaman - 1)),                                                                                    //ITEM_SUBCLASS_ARMOR_MAIL
        (1 << ((int)PlayerClass.Warrior - 1)) | (1 << ((int)PlayerClass.Paladin - 1)) | (1 << ((int)PlayerClass.Deathknight - 1)),                                      //ITEM_SUBCLASS_ARMOR_PLATE
        (int)PlayerClass.ClassMaskAllPlayable,                                                                                                                          //ITEM_SUBCLASS_ARMOR_BUCKLER
        (1 << ((int)PlayerClass.Warrior - 1)) | (1 << ((int)PlayerClass.Paladin - 1)) | (1 << ((int)PlayerClass.Shaman - 1)),                                           //ITEM_SUBCLASS_ARMOR_SHIELD
        1 << ((int)PlayerClass.Paladin - 1),                                                                                                                            //ITEM_SUBCLASS_ARMOR_LIBRAM
        1 << ((int)PlayerClass.Druid - 1),                                                                                                                              //ITEM_SUBCLASS_ARMOR_IDOL
        1 << ((int)PlayerClass.Shaman - 1),                                                                                                                             //ITEM_SUBCLASS_ARMOR_TOTEM
        1 << ((int)PlayerClass.Deathknight - 1),                                                                                                                        //ITEM_SUBCLASS_ARMOR_SIGIL
        (1 << ((int)PlayerClass.Paladin - 1)) | (1 << ((int)PlayerClass.Deathknight - 1)) | (1 << ((int)PlayerClass.Shaman - 1)) | (1 << ((int)PlayerClass.Druid - 1)), //ITEM_SUBCLASS_ARMOR_RELIC
    };

    private readonly MultiMap<uint, ObjectGuid> _temporaryAppearances = new();
    private BitSet _appearances;
    private BitSet _transmogIllusions;

    public CollectionMgr(WorldSession owner, MountCache mountCache, DB2Manager db2Manager, LoginDatabase loginDatabase,
                         ConditionManager conditionManager, CliDB cliDB, GameObjectManager objectManager)
    {
        _owner = owner;
        _mountCache = mountCache;
        _db2Manager = db2Manager;
        _loginDatabase = loginDatabase;
        _conditionManager = conditionManager;
        _cliDB = cliDB;
        _objectManager = objectManager;
        _appearances = new BitSet(0);
        _transmogIllusions = new BitSet(0);
    }

    public Dictionary<uint, HeirloomData> AccountHeirlooms { get; } = new();

    public Dictionary<uint, MountStatusFlags> AccountMounts { get; } = new();

    public Dictionary<uint, ToyFlags> AccountToys { get; } = new();

    public List<uint> AppearanceIds => (from int id in _appearances select (uint)_cliDB.ItemModifiedAppearanceStorage.LookupByKey((uint)id).ItemAppearanceID).ToList();

    public void AddHeirloom(uint itemId, HeirloomPlayerFlags flags)
    {
        if (UpdateAccountHeirlooms(itemId, flags))
            _owner.Player.AddHeirloom(itemId, (uint)flags);
    }

    public void AddItemAppearance(Item item)
    {
        if (!item.IsSoulBound)
            return;

        var itemModifiedAppearance = item.GetItemModifiedAppearance();

        if (!CanAddAppearance(itemModifiedAppearance))
            return;

        if (item.IsBOPTradeable || item.IsRefundable)
        {
            AddTemporaryAppearance(item.GUID, itemModifiedAppearance);

            return;
        }

        AddItemAppearance(itemModifiedAppearance);
    }

    public void AddItemAppearance(uint itemId, uint appearanceModId = 0)
    {
        var itemModifiedAppearance = _db2Manager.GetItemModifiedAppearance(itemId, appearanceModId);

        if (!CanAddAppearance(itemModifiedAppearance))
            return;

        AddItemAppearance(itemModifiedAppearance);
    }

    public bool AddMount(uint spellId, MountStatusFlags flags, bool factionMount = false, bool learned = false)
    {
        var player = _owner.Player;

        if (player == null)
            return false;

        var mount = _db2Manager.GetMount(spellId);

        if (mount == null)
            return false;

        var value = _mountCache.FactionSpecificMounts.LookupByKey(spellId);

        if (value != 0 && !factionMount)
            AddMount(value, flags, true, learned);

        AccountMounts[spellId] = flags;

        // Mount condition only applies to using it, should still learn it.
        if (mount.PlayerConditionID != 0)
        {
            var playerCondition = _cliDB.PlayerConditionStorage.LookupByKey(mount.PlayerConditionID);

            if (playerCondition != null && !_conditionManager.IsPlayerMeetingCondition(player, playerCondition))
                return false;
        }

        if (learned)
            return true;

        if (!factionMount)
            SendSingleMountUpdate(spellId, flags);

        if (!player.HasSpell(spellId))
            player.LearnSpell(spellId, true);

        return true;
    }

    public bool AddToy(uint itemId, bool isFavourite, bool hasFanfare)
    {
        if (!UpdateAccountToys(itemId, isFavourite, hasFanfare))
            return false;

        _owner.Player?.AddToy(itemId, (uint)GetToyFlags(isFavourite, hasFanfare));

        return true;
    }

    public void AddTransmogIllusion(uint transmogIllusionId)
    {
        var owner = _owner.Player;

        if (_transmogIllusions.Count <= transmogIllusionId)
        {
            var numBlocks = (uint)(_transmogIllusions.Count << 2);
            _transmogIllusions.Length = (int)transmogIllusionId + 1;
            numBlocks = (uint)(_transmogIllusions.Count << 2) - numBlocks;

            while (numBlocks-- != 0)
                owner.AddIllusionBlock(0);
        }

        _transmogIllusions.Set((int)transmogIllusionId, true);
        var blockIndex = transmogIllusionId / 32;
        var bitIndex = transmogIllusionId % 32;

        owner.AddIllusionFlag((int)blockIndex, (uint)(1 << (int)bitIndex));
    }

    public void AddTransmogSet(uint transmogSetId)
    {
        var items = _db2Manager.GetTransmogSetItems(transmogSetId);

        if (items.Empty())
            return;

        foreach (var item in items)
        {
            if (!_cliDB.ItemModifiedAppearanceStorage.TryGetValue(item.ItemModifiedAppearanceID, out var itemModifiedAppearance))
                continue;

            AddItemAppearance(itemModifiedAppearance);
        }
    }

    public void CheckHeirloomUpgrades(Item item)
    {
        var player = _owner.Player;

        if (player == null)
            return;

        // Check already owned heirloom for upgrade kits
        var heirloom = _db2Manager.GetHeirloomByItemId(item.Entry);

        if (heirloom == null)
            return;

        if (!AccountHeirlooms.TryGetValue(item.Entry, out var data))
            return;

        // Check for heirloom pairs (normal - heroic, heroic - mythic)
        var heirloomItemId = heirloom.StaticUpgradedItemID;
        uint newItemId = 0;

        while (_db2Manager.GetHeirloomByItemId(heirloomItemId) is { } heirloomDiff)
        {
            if (player.GetItemByEntry(heirloomDiff.ItemID) != null)
                newItemId = heirloomDiff.ItemID;

            var heirloomSub = _db2Manager.GetHeirloomByItemId(heirloomDiff.StaticUpgradedItemID);

            if (heirloomSub != null)
            {
                heirloomItemId = heirloomSub.ItemID;

                continue;
            }

            break;
        }

        if (newItemId != 0)
        {
            List<uint> heirlooms = player.ActivePlayerData.Heirlooms;
            var offset = heirlooms.IndexOf(item.Entry);

            player.SetHeirloom(offset, newItemId);
            player.SetHeirloomFlags(offset, 0);

            AccountHeirlooms.Remove(item.Entry);
            AccountHeirlooms[newItemId] = null;

            return;
        }

        var bonusListIDs = item.GetBonusListIDs();

        if (bonusListIDs.Any(bonusId => bonusId != data.BonusId))
            item.ClearBonuses();

        if (!bonusListIDs.Contains(data.BonusId))
            item.AddBonuses(data.BonusId);
    }

    public uint GetHeirloomBonus(uint itemId)
    {
        return AccountHeirlooms.TryGetValue(itemId, out var data) ? data.BonusId : 0;
    }

    public List<ObjectGuid> GetItemsProvidingTemporaryAppearance(uint itemModifiedAppearanceId)
    {
        return _temporaryAppearances.LookupByKey(itemModifiedAppearanceId);
    }

    public (bool PermAppearance, bool TempAppearance) HasItemAppearance(uint itemModifiedAppearanceId)
    {
        if (itemModifiedAppearanceId < _appearances.Count && _appearances.Get((int)itemModifiedAppearanceId))
            return (true, false);

        return _temporaryAppearances.ContainsKey(itemModifiedAppearanceId) ? (true, true) : (false, false);
    }

    public bool HasToy(uint itemId)
    {
        return AccountToys.ContainsKey(itemId);
    }

    public bool HasTransmogIllusion(uint transmogIllusionId)
    {
        return transmogIllusionId < _transmogIllusions.Count && _transmogIllusions.Get((int)transmogIllusionId);
    }

    public void LoadAccountHeirlooms(SQLResult result)
    {
        if (result.IsEmpty())
            return;

        do
        {
            var itemId = result.Read<uint>(0);
            var flags = (HeirloomPlayerFlags)result.Read<uint>(1);

            var heirloom = _db2Manager.GetHeirloomByItemId(itemId);

            if (heirloom == null)
                continue;

            uint bonusId = 0;

            for (var upgradeLevel = heirloom.UpgradeItemID.Length - 1; upgradeLevel >= 0; --upgradeLevel)
                if (((int)flags & (1 << upgradeLevel)) != 0)
                {
                    bonusId = heirloom.UpgradeItemBonusListID[upgradeLevel];

                    break;
                }

            AccountHeirlooms[itemId] = new HeirloomData(flags, bonusId);
        } while (result.NextRow());
    }

    public void LoadAccountItemAppearances(SQLResult knownAppearances, SQLResult favoriteAppearances)
    {
        if (!knownAppearances.IsEmpty())
        {
            var blocks = new uint[1];

            do
            {
                var blobIndex = knownAppearances.Read<ushort>(0);

                if (blobIndex >= blocks.Length)
                    Array.Resize(ref blocks, blobIndex + 1);

                blocks[blobIndex] = knownAppearances.Read<uint>(1);
            } while (knownAppearances.NextRow());

            _appearances = new BitSet(blocks);
        }

        if (!favoriteAppearances.IsEmpty())
            do
            {
                _favoriteAppearances[favoriteAppearances.Read<uint>(0)] = FavoriteAppearanceState.Unchanged;
            } while (favoriteAppearances.NextRow());

        // Static item appearances known by every player
        uint[] hiddenAppearanceItems =
        {
            134110, // Hidden Helm
            134111, // Hidden Cloak
            134112, // Hidden Shoulder
            168659, // Hidden Chestpiece
            142503, // Hidden Shirt
            142504, // Hidden Tabard
            168665, // Hidden Bracers
            158329, // Hidden Gloves
            143539, // Hidden Belt
            168664  // Hidden Boots
        };

        foreach (var hiddenItem in hiddenAppearanceItems)
        {
            var hiddenAppearance = _db2Manager.GetItemModifiedAppearance(hiddenItem, 0);

            //ASSERT(hiddenAppearance);
            if (_appearances.Length <= hiddenAppearance.Id)
                _appearances.Length = (int)hiddenAppearance.Id + 1;

            _appearances.Set((int)hiddenAppearance.Id, true);
        }
    }

    public void LoadAccountMounts(SQLResult result)
    {
        if (result.IsEmpty())
            return;

        do
        {
            var mountSpellId = result.Read<uint>(0);
            var flags = (MountStatusFlags)result.Read<byte>(1);

            if (_db2Manager.GetMount(mountSpellId) == null)
                continue;

            AccountMounts[mountSpellId] = flags;
        } while (result.NextRow());
    }

    public void LoadAccountToys(SQLResult result)
    {
        if (result.IsEmpty())
            return;

        do
        {
            var itemId = result.Read<uint>(0);
            AccountToys.Add(itemId, GetToyFlags(result.Read<bool>(1), result.Read<bool>(2)));
        } while (result.NextRow());
    }

    public void LoadAccountTransmogIllusions(SQLResult knownTransmogIllusions)
    {
        var blocks = new uint[7];

        if (!knownTransmogIllusions.IsEmpty())
            do
            {
                var blobIndex = knownTransmogIllusions.Read<ushort>(0);

                if (blobIndex >= blocks.Length)
                    Array.Resize(ref blocks, blobIndex + 1);

                blocks[blobIndex] = knownTransmogIllusions.Read<uint>(1);
            } while (knownTransmogIllusions.NextRow());

        _transmogIllusions = new BitSet(blocks);

        // Static illusions known by every player
        ushort[] defaultIllusions =
        {
            3,  // Lifestealing
            13, // Crusader
            22, // Striking
            23, // Agility
            34, // Hide Weapon Enchant
            43, // Beastslayer
            44, // Titanguard
        };

        foreach (var illusionId in defaultIllusions)
            _transmogIllusions.Set(illusionId, true);
    }

    public void LoadHeirlooms()
    {
        foreach (var item in AccountHeirlooms)
            _owner.Player.AddHeirloom(item.Key, (uint)item.Value.Flags);
    }

    public void LoadItemAppearances()
    {
        var owner = _owner.Player;

        foreach (var blockValue in _appearances.ToBlockRange())
            owner.AddTransmogBlock(blockValue);

        foreach (var value in _temporaryAppearances.Keys)
            owner.AddConditionalTransmog(value);
    }

    public void LoadMounts()
    {
        foreach (var m in AccountMounts.ToList())
            AddMount(m.Key, m.Value);
    }

    public void LoadToys()
    {
        foreach (var pair in AccountToys)
            _owner.Player.AddToy(pair.Key, (uint)pair.Value);
    }

    public void LoadTransmogIllusions()
    {
        var owner = _owner.Player;

        foreach (var blockValue in _transmogIllusions.ToBlockRange())
            owner.AddIllusionBlock(blockValue);
    }

    public void MountSetFavorite(uint spellId, bool favorite)
    {
        if (!AccountMounts.ContainsKey(spellId))
            return;

        if (favorite)
            AccountMounts[spellId] |= MountStatusFlags.IsFavorite;
        else
            AccountMounts[spellId] &= ~MountStatusFlags.IsFavorite;

        SendSingleMountUpdate(spellId, AccountMounts[spellId]);
    }

    public void OnItemAdded(Item item)
    {
        if (_db2Manager.GetHeirloomByItemId(item.Entry) != null)
            AddHeirloom(item.Entry, 0);

        AddItemAppearance(item);
    }

    public void RemoveTemporaryAppearance(Item item)
    {
        var itemModifiedAppearance = item.GetItemModifiedAppearance();

        if (itemModifiedAppearance == null)
            return;

        if (_temporaryAppearances.TryGetValue(itemModifiedAppearance.Id, out var guid))
            return;

        guid.Remove(item.GUID);

        if (!guid.Empty())
            return;

        _owner.Player.RemoveConditionalTransmog(itemModifiedAppearance.Id);
        _temporaryAppearances.Remove(itemModifiedAppearance.Id);
    }

    public void SaveAccountHeirlooms(SQLTransaction trans)
    {
        foreach (var heirloom in AccountHeirlooms)
        {
            var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.REP_ACCOUNT_HEIRLOOMS);
            stmt.AddValue(0, _owner.BattlenetAccountId);
            stmt.AddValue(1, heirloom.Key);
            stmt.AddValue(2, (uint)heirloom.Value.Flags);
            trans.Append(stmt);
        }
    }

    public void SaveAccountItemAppearances(SQLTransaction trans)
    {
        PreparedStatement stmt;
        ushort blockIndex = 0;

        foreach (var blockValue in _appearances.ToBlockRange())
        {
            if (blockValue != 0) // this table is only appended/bits are set (never cleared) so don't save empty blocks
            {
                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BNET_ITEM_APPEARANCES);
                stmt.AddValue(0, _owner.BattlenetAccountId);
                stmt.AddValue(1, blockIndex);
                stmt.AddValue(2, blockValue);
                trans.Append(stmt);
            }

            ++blockIndex;
        }

        foreach (var key in _favoriteAppearances.Keys)
        {
            var appearanceState = _favoriteAppearances[key];

            switch (appearanceState)
            {
                case FavoriteAppearanceState.New:
                    stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BNET_ITEM_FAVORITE_APPEARANCE);
                    stmt.AddValue(0, _owner.BattlenetAccountId);
                    stmt.AddValue(1, key);
                    trans.Append(stmt);
                    _favoriteAppearances[key] = FavoriteAppearanceState.Unchanged;

                    break;

                case FavoriteAppearanceState.Removed:
                    stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_BNET_ITEM_FAVORITE_APPEARANCE);
                    stmt.AddValue(0, _owner.BattlenetAccountId);
                    stmt.AddValue(1, key);
                    trans.Append(stmt);
                    _favoriteAppearances.Remove(key);

                    break;

                case FavoriteAppearanceState.Unchanged:
                    break;
            }
        }
    }

    public void SaveAccountMounts(SQLTransaction trans)
    {
        foreach (var mount in AccountMounts)
        {
            var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.REP_ACCOUNT_MOUNTS);
            stmt.AddValue(0, _owner.BattlenetAccountId);
            stmt.AddValue(1, mount.Key);
            stmt.AddValue(2, (byte)mount.Value);
            trans.Append(stmt);
        }
    }

    public void SaveAccountToys(SQLTransaction trans)
    {
        foreach (var pair in AccountToys)
        {
            var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.REP_ACCOUNT_TOYS);
            stmt.AddValue(0, _owner.BattlenetAccountId);
            stmt.AddValue(1, pair.Key);
            stmt.AddValue(2, pair.Value.HasAnyFlag(ToyFlags.Favorite));
            stmt.AddValue(3, pair.Value.HasAnyFlag(ToyFlags.HasFanfare));
            trans.Append(stmt);
        }
    }

    public void SaveAccountTransmogIllusions(SQLTransaction trans)
    {
        ushort blockIndex = 0;

        foreach (var blockValue in _transmogIllusions.ToBlockRange())
        {
            if (blockValue != 0) // this table is only appended/bits are set (never cleared) so don't save empty blocks
            {
                var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_BNET_TRANSMOG_ILLUSIONS);
                stmt.AddValue(0, _owner.BattlenetAccountId);
                stmt.AddValue(1, blockIndex);
                stmt.AddValue(2, blockValue);
                trans.Append(stmt);
            }

            ++blockIndex;
        }
    }

    public void SendFavoriteAppearances()
    {
        AccountTransmogUpdate accountTransmogUpdate = new()
        {
            IsFullUpdate = true
        };

        foreach (var pair in _favoriteAppearances)
            if (pair.Value != FavoriteAppearanceState.Removed)
                accountTransmogUpdate.FavoriteAppearances.Add(pair.Key);

        _owner.SendPacket(accountTransmogUpdate);
    }

    public void SetAppearanceIsFavorite(uint itemModifiedAppearanceId, bool apply)
    {
        var apperanceState = _favoriteAppearances.LookupByKey(itemModifiedAppearanceId);

        if (apply)
        {
            if (!_favoriteAppearances.ContainsKey(itemModifiedAppearanceId))
                _favoriteAppearances[itemModifiedAppearanceId] = FavoriteAppearanceState.New;
            else if (apperanceState == FavoriteAppearanceState.Removed)
                apperanceState = FavoriteAppearanceState.Unchanged;
            else
                return;
        }
        else if (_favoriteAppearances.ContainsKey(itemModifiedAppearanceId))
        {
            if (apperanceState == FavoriteAppearanceState.New)
                _favoriteAppearances.Remove(itemModifiedAppearanceId);
            else
                apperanceState = FavoriteAppearanceState.Removed;
        }
        else
            return;

        _favoriteAppearances[itemModifiedAppearanceId] = apperanceState;

        AccountTransmogUpdate accountTransmogUpdate = new()
        {
            IsFullUpdate = false,
            IsSetFavorite = apply
        };

        accountTransmogUpdate.FavoriteAppearances.Add(itemModifiedAppearanceId);

        _owner.SendPacket(accountTransmogUpdate);
    }

    public void ToyClearFanfare(uint itemId)
    {
        if (!AccountToys.ContainsKey(itemId))
            return;

        AccountToys[itemId] &= ~ToyFlags.HasFanfare;
    }

    public void ToySetFavorite(uint itemId, bool favorite)
    {
        if (!AccountToys.ContainsKey(itemId))
            return;

        if (favorite)
            AccountToys[itemId] |= ToyFlags.Favorite;
        else
            AccountToys[itemId] &= ~ToyFlags.Favorite;
    }

    public void UpgradeHeirloom(uint itemId, uint castItem)
    {
        var player = _owner.Player;

        if (player == null)
            return;

        var heirloom = _db2Manager.GetHeirloomByItemId(itemId);

        if (heirloom == null)
            return;

        if (!AccountHeirlooms.TryGetValue(itemId, out var data))
            return;

        var flags = data.Flags;
        uint bonusId = 0;

        for (var upgradeLevel = 0; upgradeLevel < heirloom.UpgradeItemID.Length; ++upgradeLevel)
            if (heirloom.UpgradeItemID[upgradeLevel] == castItem)
            {
                flags |= (HeirloomPlayerFlags)(1 << upgradeLevel);
                bonusId = heirloom.UpgradeItemBonusListID[upgradeLevel];
            }

        foreach (var item in player.GetItemListByEntry(itemId, true))
            item.AddBonuses(bonusId);

        // Get heirloom offset to update only one part of dynamic field
        List<uint> heirlooms = player.ActivePlayerData.Heirlooms;
        var offset = heirlooms.IndexOf(itemId);

        player.SetHeirloomFlags(offset, (uint)flags);
        data.Flags = flags;
        data.BonusId = bonusId;
    }

    //todo  check this
    private void AddItemAppearance(ItemModifiedAppearanceRecord itemModifiedAppearance)
    {
        var owner = _owner.Player;

        if (_appearances.Count <= itemModifiedAppearance.Id)
        {
            var numBlocks = (uint)(_appearances.Count << 2);
            _appearances.Length = (int)itemModifiedAppearance.Id + 1;
            numBlocks = (uint)(_appearances.Count << 2) - numBlocks;

            while (numBlocks-- != 0)
                owner.AddTransmogBlock(0);
        }

        _appearances.Set((int)itemModifiedAppearance.Id, true);
        var blockIndex = itemModifiedAppearance.Id / 32;
        var bitIndex = itemModifiedAppearance.Id % 32;
        owner.AddTransmogFlag((int)blockIndex, 1u << (int)bitIndex);
        var temporaryAppearance = _temporaryAppearances.LookupByKey(itemModifiedAppearance.Id).ToList(); // make a copy

        if (!temporaryAppearance.Empty())
        {
            owner.RemoveConditionalTransmog(itemModifiedAppearance.Id);
            _temporaryAppearances.Remove(itemModifiedAppearance.Id);
        }

        if (_cliDB.ItemStorage.TryGetValue(itemModifiedAppearance.ItemID, out var item))
        {
            var transmogSlot = Item.ItemTransmogrificationSlots[(int)item.inventoryType];

            if (transmogSlot >= 0)
                _owner.Player.UpdateCriteria(CriteriaType.LearnAnyTransmogInSlot, (ulong)transmogSlot, itemModifiedAppearance.Id);
        }

        var sets = _db2Manager.GetTransmogSetsForItemModifiedAppearance(itemModifiedAppearance.Id);

        foreach (var set in sets)
            if (IsSetCompleted(set.Id))
                _owner.Player.UpdateCriteria(CriteriaType.CollectTransmogSetFromGroup, set.TransmogSetGroupID);
    }

    private void AddTemporaryAppearance(ObjectGuid itemGuid, ItemModifiedAppearanceRecord itemModifiedAppearance)
    {
        var itemsWithAppearance = _temporaryAppearances[itemModifiedAppearance.Id];

        if (itemsWithAppearance.Empty())
            _owner.Player.AddConditionalTransmog(itemModifiedAppearance.Id);

        itemsWithAppearance.Add(itemGuid);
    }

    private bool CanAddAppearance(ItemModifiedAppearanceRecord itemModifiedAppearance)
    {
        if (itemModifiedAppearance == null)
            return false;

        if (itemModifiedAppearance.TransmogSourceTypeEnum is 6 or 9)
            return false;

        if (!_cliDB.ItemSearchNameStorage.ContainsKey(itemModifiedAppearance.ItemID))
            return false;

        var itemTemplate = _objectManager.GetItemTemplate(itemModifiedAppearance.ItemID);

        if (itemTemplate == null)
            return false;

        if (_owner.Player == null)
            return false;

        if (_owner.Player.CanUseItem(itemTemplate) != InventoryResult.Ok)
            return false;

        if (itemTemplate.HasFlag(ItemFlags2.NoSourceForItemVisual) || itemTemplate.Quality == ItemQuality.Artifact)
            return false;

        switch (itemTemplate.Class)
        {
            case ItemClass.Weapon:
            {
                if (!Convert.ToBoolean(_owner.Player.GetWeaponProficiency() & (1 << (int)itemTemplate.SubClass)))
                    return false;

                if ((ItemSubClassWeapon)itemTemplate.SubClass is ItemSubClassWeapon.Exotic or
                                                                 ItemSubClassWeapon.Exotic2 or
                                                                 ItemSubClassWeapon.Miscellaneous or
                                                                 ItemSubClassWeapon.Thrown or
                                                                 ItemSubClassWeapon.Spear or
                                                                 ItemSubClassWeapon.FishingPole)
                    return false;

                break;
            }
            case ItemClass.Armor:
            {
                switch (itemTemplate.InventoryType)
                {
                    case InventoryType.Body:
                    case InventoryType.Shield:
                    case InventoryType.Cloak:
                    case InventoryType.Tabard:
                    case InventoryType.Holdable:
                        break;

                    case InventoryType.Head:
                    case InventoryType.Shoulders:
                    case InventoryType.Chest:
                    case InventoryType.Waist:
                    case InventoryType.Legs:
                    case InventoryType.Feet:
                    case InventoryType.Wrists:
                    case InventoryType.Hands:
                    case InventoryType.Robe:
                        if ((ItemSubClassArmor)itemTemplate.SubClass == ItemSubClassArmor.Miscellaneous)
                            return false;

                        break;

                    default:
                        return false;
                }

                if (itemTemplate.InventoryType != InventoryType.Cloak)
                    if (!Convert.ToBoolean(_playerClassByArmorSubclass[itemTemplate.SubClass] & _owner.Player.ClassMask))
                        return false;

                break;
            }
            default:
                return false;
        }

        if (itemTemplate.Quality >= ItemQuality.Uncommon)
            return itemModifiedAppearance.Id >= _appearances.Count || !_appearances.Get((int)itemModifiedAppearance.Id);

        if (!itemTemplate.HasFlag(ItemFlags2.IgnoreQualityForItemVisualSource) || !itemTemplate.HasFlag(ItemFlags3.ActsAsTransmogHiddenVisualOption))
            return false;

        return itemModifiedAppearance.Id >= _appearances.Count || !_appearances.Get((int)itemModifiedAppearance.Id);
    }

    private ToyFlags GetToyFlags(bool isFavourite, bool hasFanfare)
    {
        var flags = ToyFlags.None;

        if (isFavourite)
            flags |= ToyFlags.Favorite;

        if (hasFanfare)
            flags |= ToyFlags.HasFanfare;

        return flags;
    }

    private bool IsSetCompleted(uint transmogSetId)
    {
        var transmogSetItems = _db2Manager.GetTransmogSetItems(transmogSetId);

        if (transmogSetItems.Empty())
            return false;

        var knownPieces = new int[EquipmentSlot.End];

        for (var i = 0; i < EquipmentSlot.End; ++i)
            knownPieces[i] = -1;

        foreach (var transmogSetItem in transmogSetItems)
        {
            if (!_cliDB.ItemModifiedAppearanceStorage.TryGetValue(transmogSetItem.ItemModifiedAppearanceID, out var itemModifiedAppearance))
                continue;

            if (!_cliDB.ItemStorage.TryGetValue(itemModifiedAppearance.ItemID, out var item))
                continue;

            var transmogSlot = Item.ItemTransmogrificationSlots[(int)item.inventoryType];

            if (transmogSlot < 0 || knownPieces[transmogSlot] == 1)
                continue;

            var (hasAppearance, isTemporary) = HasItemAppearance(transmogSetItem.ItemModifiedAppearanceID);

            knownPieces[transmogSlot] = hasAppearance && !isTemporary ? 1 : 0;
        }

        return !knownPieces.Contains(0);
    }

    private void SendSingleMountUpdate(uint spellId, MountStatusFlags mountStatusFlags)
    {
        var player = _owner.Player;

        if (player == null)
            return;

        AccountMountUpdate mountUpdate = new()
        {
            IsFullUpdate = false
        };

        mountUpdate.Mounts.Add(spellId, mountStatusFlags);
        player.SendPacket(mountUpdate);
    }

    private bool UpdateAccountHeirlooms(uint itemId, HeirloomPlayerFlags flags)
    {
        if (AccountHeirlooms.ContainsKey(itemId))
            return false;

        AccountHeirlooms.Add(itemId, new HeirloomData(flags));

        return true;
    }

    private bool UpdateAccountToys(uint itemId, bool isFavourite, bool hasFanfare)
    {
        if (AccountToys.ContainsKey(itemId))
            return false;

        AccountToys.Add(itemId, GetToyFlags(isFavourite, hasFanfare));

        return true;
    }
}