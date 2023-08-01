// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.VoidStorage;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class VoidStorageHandler : IWorldSessionHandler
{
    private readonly CollectionMgr _collectionMgr;
    private readonly GameObjectManager _objectManager;
    private readonly WorldSession _session;

    public VoidStorageHandler(WorldSession session, GameObjectManager objectManager, CollectionMgr collectionMgr)
    {
        _session = session;
        _objectManager = objectManager;
        _collectionMgr = collectionMgr;
    }

    public void SendVoidStorageTransferResult(VoidTransferError result)
    {
        _session.SendPacket(new VoidTransferResult(result));
    }

    [WorldPacketHandler(ClientOpcodes.QueryVoidStorage, Processing = PacketProcessing.Inplace)]
    private void HandleVoidStorageQuery(QueryVoidStorage queryVoidStorage)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(queryVoidStorage.Npc, NPCFlags.Transmogrifier | NPCFlags.VaultKeeper, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleVoidStorageQuery - {0} not found or _session.Player can't interact with it.", queryVoidStorage.Npc.ToString());
            _session.SendPacket(new VoidStorageFailed());

            return;
        }

        if (!_session.Player.IsVoidStorageUnlocked())
        {
            Log.Logger.Debug("WORLD: HandleVoidStorageQuery - {0} name: {1} queried void storage without unlocking it.", _session.Player.GUID.ToString(), _session.Player.GetName());
            _session.SendPacket(new VoidStorageFailed());

            return;
        }

        VoidStorageContents voidStorageContents = new();

        for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
        {
            var item = _session.Player.GetVoidStorageItem(i);

            if (item == null)
                continue;

            voidStorageContents.Items.Add(new VoidItem
            {
                Guid = ObjectGuid.Create(HighGuid.Item, item.ItemId),
                Creator = item.CreatorGuid,
                Slot = i,
                Item = new ItemInstance(item)
            });
        }

        _session.SendPacket(voidStorageContents);
    }

    [WorldPacketHandler(ClientOpcodes.VoidStorageTransfer)]
    private void HandleVoidStorageTransfer(VoidStorageTransfer voidStorageTransfer)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(voidStorageTransfer.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} not found or _session.Player can't interact with it.", voidStorageTransfer.Npc.ToString());

            return;
        }

        if (!_session.Player.IsVoidStorageUnlocked())
        {
            Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - Player ({0}, name: {1}) queried void storage without unlocking it.", _session.Player.GUID.ToString(), _session.Player.GetName());

            return;
        }

        if (voidStorageTransfer.Deposits.Length > _session.Player.GetNumOfVoidStorageFreeSlots())
        {
            SendVoidStorageTransferResult(VoidTransferError.Full);

            return;
        }

        uint freeBagSlots = 0;

        if (!voidStorageTransfer.Withdrawals.Empty())
        {
            // make this a Player function
            for (var i = InventorySlots.BagStart; i < InventorySlots.BagEnd; i++)
            {
                var bag = _session.Player.GetBagByPos(i);

                if (bag != null)
                    freeBagSlots += bag.GetFreeSlots();
            }

            var inventoryEnd = InventorySlots.ItemStart + _session.Player.GetInventorySlotCount();

            for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
                if (_session.Player.GetItemByPos(InventorySlots.Bag0, i) == null)
                    ++freeBagSlots;
        }

        if (voidStorageTransfer.Withdrawals.Length > freeBagSlots)
        {
            SendVoidStorageTransferResult(VoidTransferError.InventoryFull);

            return;
        }

        if (!_session.Player.HasEnoughMoney(voidStorageTransfer.Deposits.Length * SharedConst.VoidStorageStoreItemCost))
        {
            SendVoidStorageTransferResult(VoidTransferError.NotEnoughMoney);

            return;
        }

        VoidStorageTransferChanges voidStorageTransferChanges = new();

        byte depositCount = 0;

        foreach (var deposit in voidStorageTransfer.Deposits)
        {
            var item = _session.Player.GetItemByGuid(deposit);

            if (item == null)
            {
                Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} {1} wants to deposit an invalid item ({2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), deposit.ToString());

                continue;
            }

            VoidStorageItem itemVs = new(_objectManager.IDGeneratorCache.GenerateVoidStorageItemId(),
                                         item.Entry,
                                         item.Creator,
                                         item.ItemRandomBonusListId,
                                         item.GetModifier(ItemModifier.TimewalkerLevel),
                                         item.GetModifier(ItemModifier.ArtifactKnowledgeLevel),
                                         item.Context,
                                         item.BonusListIDs);

            VoidItem voidItem;
            voidItem.Guid = ObjectGuid.Create(HighGuid.Item, itemVs.ItemId);
            voidItem.Creator = item.Creator;
            voidItem.Item = new ItemInstance(itemVs);
            voidItem.Slot = _session.Player.AddVoidStorageItem(itemVs);

            voidStorageTransferChanges.AddedItems.Add(voidItem);

            _session.Player.DestroyItem(item.BagSlot, item.Slot, true);
            ++depositCount;
        }

        long cost = depositCount * SharedConst.VoidStorageStoreItemCost;

        _session.Player.ModifyMoney(-cost);

        foreach (var withdrawl in voidStorageTransfer.Withdrawals)
        {
            var itemVs = _session.Player.GetVoidStorageItem(withdrawl.Counter, out var slot);

            if (itemVs == null)
            {
                Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} {1} tried to withdraw an invalid item ({2})", _session.Player.GUID.ToString(), _session.Player.GetName(), withdrawl.ToString());

                continue;
            }

            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemVs.ItemEntry, 1);

            if (msg != InventoryResult.Ok)
            {
                SendVoidStorageTransferResult(VoidTransferError.InventoryFull);
                Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} {1} couldn't withdraw {2} because inventory was full.", _session.Player.GUID.ToString(), _session.Player.GetName(), withdrawl.ToString());

                return;
            }

            var item = _session.Player.StoreNewItem(dest, itemVs.ItemEntry, true, itemVs.RandomBonusListId, null, itemVs.Context, itemVs.BonusListIDs);
            item.SetCreator(itemVs.CreatorGuid);
            item.SetBinding(true);
            _collectionMgr.AddItemAppearance(item);

            voidStorageTransferChanges.RemovedItems.Add(ObjectGuid.Create(HighGuid.Item, itemVs.ItemId));

            _session.Player.DeleteVoidStorageItem(slot);
        }

        _session.SendPacket(voidStorageTransferChanges);
        SendVoidStorageTransferResult(VoidTransferError.Ok);
    }

    [WorldPacketHandler(ClientOpcodes.UnlockVoidStorage, Processing = PacketProcessing.Inplace)]
    private void HandleVoidStorageUnlock(UnlockVoidStorage unlockVoidStorage)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(unlockVoidStorage.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleVoidStorageUnlock - {0} not found or _session.Player can't interact with it.", unlockVoidStorage.Npc.ToString());

            return;
        }

        if (_session.Player.IsVoidStorageUnlocked())
        {
            Log.Logger.Debug("WORLD: HandleVoidStorageUnlock - Player({0}, name: {1}) tried to unlock void storage a 2nd time.", _session.Player.GUID.ToString(), _session.Player.GetName());

            return;
        }

        _session.Player.ModifyMoney(-SharedConst.VoidStorageUnlockCost);
        _session.Player.UnlockVoidStorage();
    }

    [WorldPacketHandler(ClientOpcodes.SwapVoidItem, Processing = PacketProcessing.Inplace)]
    private void HandleVoidSwapItem(SwapVoidItem swapVoidItem)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(swapVoidItem.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleVoidSwapItem - {0} not found or _session.Player can't interact with it.", swapVoidItem.Npc.ToString());

            return;
        }

        if (!_session.Player.IsVoidStorageUnlocked())
        {
            Log.Logger.Debug("WORLD: HandleVoidSwapItem - Player ({0}, name: {1}) queried void storage without unlocking it.", _session.Player.GUID.ToString(), _session.Player.GetName());

            return;
        }

        if (_session.Player.GetVoidStorageItem(swapVoidItem.VoidItemGuid.Counter, out var oldSlot) == null)
        {
            Log.Logger.Debug("WORLD: HandleVoidSwapItem - Player (GUID: {0}, name: {1}) requested swapping an invalid item (slot: {2}, itemid: {3}).", _session.Player.GUID.ToString(), _session.Player.GetName(), swapVoidItem.DstSlot, swapVoidItem.VoidItemGuid.ToString());

            return;
        }

        var usedDestSlot = _session.Player.GetVoidStorageItem((byte)swapVoidItem.DstSlot) != null;
        var itemIdDest = ObjectGuid.Empty;

        if (usedDestSlot)
            itemIdDest = ObjectGuid.Create(HighGuid.Item, _session.Player.GetVoidStorageItem((byte)swapVoidItem.DstSlot).ItemId);

        if (!_session.Player.SwapVoidStorageItem(oldSlot, (byte)swapVoidItem.DstSlot))
        {
            SendVoidStorageTransferResult(VoidTransferError.InternalError1);

            return;
        }

        VoidItemSwapResponse voidItemSwapResponse = new()
        {
            VoidItemA = swapVoidItem.VoidItemGuid,
            VoidItemSlotA = swapVoidItem.DstSlot
        };

        if (usedDestSlot)
        {
            voidItemSwapResponse.VoidItemB = itemIdDest;
            voidItemSwapResponse.VoidItemSlotB = oldSlot;
        }

        _session.SendPacket(voidItemSwapResponse);
    }
}