﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.BattlePets;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.G;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Item;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Handlers;

public class ItemHandler : IWorldSessionHandler
{
    public void SendEnchantmentLog(ObjectGuid owner, ObjectGuid caster, ObjectGuid itemGuid, uint itemId, uint enchantId, uint enchantSlot)
    {
        EnchantmentLog packet = new()
        {
            Owner = owner,
            Caster = caster,
            ItemGUID = itemGuid,
            ItemID = itemId,
            Enchantment = enchantId,
            EnchantSlot = enchantSlot
        };

        Player.SendMessageToSet(packet, true);
    }

    public void SendItemEnchantTimeUpdate(ObjectGuid Playerguid, ObjectGuid Itemguid, uint slot, uint Duration)
    {
        ItemEnchantTimeUpdate data = new()
        {
            ItemGuid = Itemguid,
            DurationLeft = Duration,
            Slot = slot,
            OwnerGuid = Playerguid
        };

        SendPacket(data);
    }

    private bool CanUseBank(ObjectGuid bankerGUID = default)
    {
        // bankerGUID parameter is optional, set to 0 by default.
        if (bankerGUID.IsEmpty)
            bankerGUID = _player.PlayerTalkClass.GetInteractionData().SourceGuid;

        var isUsingBankCommand = bankerGUID == Player.GUID && bankerGUID == _player.PlayerTalkClass.GetInteractionData().SourceGuid;

        if (!isUsingBankCommand)
        {
            var creature = Player.GetNPCIfCanInteractWith(bankerGUID, NPCFlags.Banker, NPCFlags2.None);

            if (!creature)
                return false;
        }

        return true;
    }

    [WorldPacketHandler(ClientOpcodes.AutoEquipItem, Processing = PacketProcessing.Inplace)]
    private void HandleAutoEquipItem(AutoEquipItem autoEquipItem)
    {
        if (autoEquipItem.Inv.Items.Count != 1)
        {
            Log.Logger.Error("WORLD: HandleAutoEquipItem - Invalid itemCount ({0})", autoEquipItem.Inv.Items.Count);

            return;
        }

        var pl = Player;
        var srcItem = pl.GetItemByPos(autoEquipItem.PackSlot, autoEquipItem.Slot);

        if (srcItem == null)
            return; // only at cheat

        var msg = pl.CanEquipItem(ItemConst.NullSlot, out var dest, srcItem, !srcItem.IsBag);

        if (msg != InventoryResult.Ok)
        {
            pl.SendEquipError(msg, srcItem);

            return;
        }

        var src = srcItem.Pos;

        if (dest == src) // prevent equip in same slot, only at cheat
            return;

        var dstItem = pl.GetItemByPos(dest);

        if (dstItem == null) // empty slot, simple case
        {
            if (!srcItem.ChildItem.IsEmpty)
            {
                var childEquipResult = _player.CanEquipChildItem(srcItem);

                if (childEquipResult != InventoryResult.Ok)
                {
                    _player.SendEquipError(msg, srcItem);

                    return;
                }
            }

            pl.RemoveItem(autoEquipItem.PackSlot, autoEquipItem.Slot, true);
            pl.EquipItem(dest, srcItem, true);

            if (!srcItem.ChildItem.IsEmpty)
                _player.EquipChildItem(autoEquipItem.PackSlot, autoEquipItem.Slot, srcItem);

            pl.AutoUnequipOffhandIfNeed();
        }
        else // have currently equipped item, not simple case
        {
            var dstbag = dstItem.BagSlot;
            var dstslot = dstItem.Slot;

            msg = pl.CanUnequipItem(dest, !srcItem.IsBag);

            if (msg != InventoryResult.Ok)
            {
                pl.SendEquipError(msg, dstItem);

                return;
            }

            if (!dstItem.HasItemFlag(ItemFieldFlags.Child))
            {
                // check dest.src move possibility
                List<ItemPosCount> sSrc = new();
                ushort eSrc = 0;

                if (pl.IsInventoryPos(src))
                {
                    msg = pl.CanStoreItem(autoEquipItem.PackSlot, autoEquipItem.Slot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = pl.CanStoreItem(autoEquipItem.PackSlot, ItemConst.NullSlot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = pl.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
                }
                else if (PlayerComputators.IsBankPos(src))
                {
                    msg = pl.CanBankItem(autoEquipItem.PackSlot, autoEquipItem.Slot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = pl.CanBankItem(autoEquipItem.PackSlot, ItemConst.NullSlot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = pl.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
                }
                else if (PlayerComputators.IsEquipmentPos(src))
                {
                    msg = pl.CanEquipItem(autoEquipItem.Slot, out eSrc, dstItem, true);

                    if (msg == InventoryResult.Ok)
                        msg = pl.CanUnequipItem(eSrc, true);
                }

                if (msg == InventoryResult.Ok && PlayerComputators.IsEquipmentPos(dest) && !srcItem.ChildItem.IsEmpty)
                    msg = _player.CanEquipChildItem(srcItem);

                if (msg != InventoryResult.Ok)
                {
                    pl.SendEquipError(msg, dstItem, srcItem);

                    return;
                }

                // now do moves, remove...
                pl.RemoveItem(dstbag, dstslot, false);
                pl.RemoveItem(autoEquipItem.PackSlot, autoEquipItem.Slot, false);

                // add to dest
                pl.EquipItem(dest, srcItem, true);

                // add to src
                if (pl.IsInventoryPos(src))
                    pl.StoreItem(sSrc, dstItem, true);
                else if (PlayerComputators.IsBankPos(src))
                    pl.BankItem(sSrc, dstItem, true);
                else if (PlayerComputators.IsEquipmentPos(src))
                    pl.EquipItem(eSrc, dstItem, true);

                if (PlayerComputators.IsEquipmentPos(dest) && !srcItem.ChildItem.IsEmpty)
                    _player.EquipChildItem(autoEquipItem.PackSlot, autoEquipItem.Slot, srcItem);
            }
            else
            {
                var parentItem = _player.GetItemByGuid(dstItem.Creator);

                if (parentItem)
                    if (PlayerComputators.IsEquipmentPos(dest))
                    {
                        _player.AutoUnequipChildItem(parentItem);
                        // dest is now empty
                        _player.SwapItem(src, dest);
                        // src is now empty
                        _player.SwapItem(parentItem.Pos, src);
                    }
            }

            pl.AutoUnequipOffhandIfNeed();

            // if inventory item was moved, check if we can remove dependent auras, because they were not removed in Player::RemoveItem (update was set to false)
            // do this after swaps are done, we pass nullptr because both weapons could be swapped and none of them should be ignored
            if ((autoEquipItem.PackSlot == InventorySlots.Bag0 && autoEquipItem.Slot < InventorySlots.BagEnd) || (dstbag == InventorySlots.Bag0 && dstslot < InventorySlots.BagEnd))
                pl.ApplyItemDependentAuras(null, false);
        }
    }

    [WorldPacketHandler(ClientOpcodes.AutoEquipItemSlot, Processing = PacketProcessing.Inplace)]
    private void HandleAutoEquipItemSlot(AutoEquipItemSlot packet)
    {
        // cheating attempt, client should never send opcode in that case
        if (packet.Inv.Items.Count != 1 || !PlayerComputators.IsEquipmentPos(InventorySlots.Bag0, packet.ItemDstSlot))
            return;

        var item = Player.GetItemByGuid(packet.Item);
        var dstPos = (ushort)(packet.ItemDstSlot | (InventorySlots.Bag0 << 8));
        var srcPos = (ushort)(packet.Inv.Items[0].Slot | (packet.Inv.Items[0].ContainerSlot << 8));

        if (item == null || item.Pos != srcPos || srcPos == dstPos)
            return;

        Player.SwapItem(srcPos, dstPos);
    }

    [WorldPacketHandler(ClientOpcodes.AutoStoreBagItem, Processing = PacketProcessing.Inplace)]
    private void HandleAutoStoreBagItem(AutoStoreBagItem packet)
    {
        if (!packet.Inv.Items.Empty())
        {
            Log.Logger.Error("HandleAutoStoreBagItemOpcode - Invalid itemCount ({0})", packet.Inv.Items.Count);

            return;
        }

        var item = Player.GetItemByPos(packet.ContainerSlotA, packet.SlotA);

        if (!item)
            return;

        if (!Player.IsValidPos(packet.ContainerSlotB, ItemConst.NullSlot, false)) // can be autostore pos
        {
            Player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        var src = item.Pos;
        InventoryResult msg;

        // check unequip potability for equipped items and bank bags
        if (PlayerComputators.IsEquipmentPos(src) || PlayerComputators.IsBagPos(src))
        {
            msg = Player.CanUnequipItem(src, !PlayerComputators.IsBagPos(src));

            if (msg != InventoryResult.Ok)
            {
                Player.SendEquipError(msg, item);

                return;
            }
        }

        List<ItemPosCount> dest = new();
        msg = Player.CanStoreItem(packet.ContainerSlotB, ItemConst.NullSlot, dest, item);

        if (msg != InventoryResult.Ok)
        {
            Player.SendEquipError(msg, item);

            return;
        }

        // no-op: placed in same slot
        if (dest.Count == 1 && dest[0].Pos == src)
        {
            // just remove grey item state
            Player.SendEquipError(InventoryResult.InternalBagError, item);

            return;
        }

        Player.RemoveItem(packet.ContainerSlotA, packet.SlotA, true);
        Player.StoreItem(dest, item, true);
    }

    [WorldPacketHandler(ClientOpcodes.BuyBackItem, Processing = PacketProcessing.Inplace)]
    private void HandleBuybackItem(BuyBackItem packet)
    {
        var creature = _player.GetNPCIfCanInteractWith(packet.VendorGUID, NPCFlags.Vendor, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug("WORLD: HandleBuybackItem - {0} not found or you can not interact with him.", packet.VendorGUID.ToString());
            _player.SendSellError(SellResult.CantFindVendor, null, ObjectGuid.Empty);

            return;
        }

        // remove fake death
        if (_player.HasUnitState(UnitState.Died))
            _player.RemoveAurasByType(AuraType.FeignDeath);

        var pItem = _player.GetItemFromBuyBackSlot(packet.Slot);

        if (pItem != null)
        {
            var price = _player.ActivePlayerData.BuybackPrice[(int)(packet.Slot - InventorySlots.BuyBackStart)];

            if (!_player.HasEnoughMoney(price))
            {
                _player.SendBuyError(BuyResult.NotEnoughtMoney, creature, pItem.Entry);

                return;
            }

            List<ItemPosCount> dest = new();
            var msg = _player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem, false);

            if (msg == InventoryResult.Ok)
            {
                _player.ModifyMoney(-price);
                _player.RemoveItemFromBuyBackSlot(packet.Slot, false);
                _player.ItemAddedQuestCheck(pItem.Entry, pItem.Count);
                _player.StoreItem(dest, pItem, true);
            }
            else
            {
                _player.SendEquipError(msg, pItem);
            }

            return;
        }
        else
        {
            _player.SendBuyError(BuyResult.CantFindItem, creature, 0);
        }
    }

    [WorldPacketHandler(ClientOpcodes.BuyItem, Processing = PacketProcessing.Inplace)]
    private void HandleBuyItem(BuyItem packet)
    {
        // client expects count starting at 1, and we send vendorslot+1 to client already
        if (packet.Muid > 0)
            --packet.Muid;
        else
            return; // cheating

        switch (packet.ItemType)
        {
            case ItemVendorType.Item:
                var bagItem = Player.GetItemByGuid(packet.ContainerGUID);

                var bag = ItemConst.NullBag;

                if (bagItem is { IsBag: true })
                    bag = bagItem.Slot;
                else if (packet.ContainerGUID == Player.GUID) // The client sends the player guid when trying to store an item in the default backpack
                    bag = InventorySlots.Bag0;

                Player.BuyItemFromVendorSlot(packet.VendorGUID, packet.Muid, packet.Item.ItemID, (byte)packet.Quantity, bag, (byte)packet.Slot);

                break;
            case ItemVendorType.Currency:
                Player.BuyCurrencyFromVendorSlot(packet.VendorGUID, packet.Muid, packet.Item.ItemID, (byte)packet.Quantity);

                break;
            default:
                Log.Logger.Debug("WORLD: received wrong itemType {0} in HandleBuyItem", packet.ItemType);

                break;
        }
    }

    [WorldPacketHandler(ClientOpcodes.CancelTempEnchantment, Processing = PacketProcessing.Inplace)]
    private void HandleCancelTempEnchantment(CancelTempEnchantment packet)
    {
        // apply only to equipped item
        if (!PlayerComputators.IsEquipmentPos(InventorySlots.Bag0, (byte)packet.Slot))
            return;

        var item = Player.GetItemByPos(InventorySlots.Bag0, (byte)packet.Slot);

        if (!item)
            return;

        if (item.GetEnchantmentId(EnchantmentSlot.Temp) == 0)
            return;

        Player.ApplyEnchantment(item, EnchantmentSlot.Temp, false);
        item.ClearEnchantment(EnchantmentSlot.Temp);
    }

    [WorldPacketHandler(ClientOpcodes.DestroyItem, Processing = PacketProcessing.Inplace)]
    private void HandleDestroyItem(DestroyItem destroyItem)
    {
        var pos = (ushort)((destroyItem.ContainerId << 8) | destroyItem.SlotNum);

        // prevent drop unequipable items (in combat, for example) and non-empty bags
        if (PlayerComputators.IsEquipmentPos(pos) || PlayerComputators.IsBagPos(pos))
        {
            var msg = _player.CanUnequipItem(pos, false);

            if (msg != InventoryResult.Ok)
            {
                _player.SendEquipError(msg, _player.GetItemByPos(pos));

                return;
            }
        }

        var pItem = _player.GetItemByPos(destroyItem.ContainerId, destroyItem.SlotNum);

        if (pItem == null)
        {
            _player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (pItem.Template.HasFlag(ItemFlags.NoUserDestroy))
        {
            _player.SendEquipError(InventoryResult.DropBoundItem);

            return;
        }

        if (destroyItem.Count != 0)
        {
            var i_count = destroyItem.Count;
            _player.DestroyItemCount(pItem, ref i_count, true);
        }
        else
        {
            _player.DestroyItem(destroyItem.ContainerId, destroyItem.SlotNum, true);
        }
    }

    [WorldPacketHandler(ClientOpcodes.GetItemPurchaseData, Processing = PacketProcessing.Inplace)]
    private void HandleGetItemPurchaseData(GetItemPurchaseData packet)
    {
        var item = Player.GetItemByGuid(packet.ItemGUID);

        if (!item)
        {
            Log.Logger.Debug("HandleGetItemPurchaseData: Item {0} not found!", packet.ItemGUID.ToString());

            return;
        }

        Player.SendRefundInfo(item);
    }

    [WorldPacketHandler(ClientOpcodes.ItemPurchaseRefund, Processing = PacketProcessing.Inplace)]
    private void HandleItemRefund(ItemPurchaseRefund packet)
    {
        var item = Player.GetItemByGuid(packet.ItemGUID);

        if (!item)
        {
            Log.Logger.Debug("WorldSession.HandleItemRefund: Item {0} not found!", packet.ItemGUID.ToString());

            return;
        }

        // Don't try to refund item currently being disenchanted
        if (Player.GetLootGUID() == packet.ItemGUID)
            return;

        Player.RefundItem(item);
    }

    [WorldPacketHandler(ClientOpcodes.ReadItem, Processing = PacketProcessing.Inplace)]
    private void HandleReadItem(ReadItem readItem)
    {
        var item = _player.GetItemByPos(readItem.PackSlot, readItem.Slot);

        if (item != null && item.Template.PageText != 0)
        {
            var msg = _player.CanUseItem(item);

            if (msg == InventoryResult.Ok)
            {
                ReadItemResultOK packet = new()
                {
                    Item = item.GUID
                };

                SendPacket(packet);
            }
            else
            {
                // @todo: 6.x research new values
                /*WorldPackets.Item.ReadItemResultFailed packet;
                packet.Item = item.GetGUID();
                packet.Subcode = ??;
                packet.Delay = ??;
                SendPacket(packet);*/

                Log.Logger.Information("STORAGE: Unable to read item");
                _player.SendEquipError(msg, item);
            }
        }
        else
        {
            _player.SendEquipError(InventoryResult.ItemNotFound);
        }
    }

    [WorldPacketHandler(ClientOpcodes.RemoveNewItem, Processing = PacketProcessing.Inplace)]
    private void HandleRemoveNewItem(RemoveNewItem removeNewItem)
    {
        var item = _player.GetItemByGuid(removeNewItem.ItemGuid);

        if (!item)
        {
            Log.Logger.Debug($"WorldSession.HandleRemoveNewItem: Item ({removeNewItem.ItemGuid}) not found for {GetPlayerInfo()}!");

            return;
        }

        if (item.HasItemFlag(ItemFieldFlags.NewItem))
        {
            item.RemoveItemFlag(ItemFieldFlags.NewItem);
            item.SetState(ItemUpdateState.Changed, _player);
        }
    }

    [WorldPacketHandler(ClientOpcodes.SellItem, Processing = PacketProcessing.Inplace)]
    private void HandleSellItem(SellItem packet)
    {
        if (packet.ItemGUID.IsEmpty)
            return;

        var pl = Player;

        var creature = pl.GetNPCIfCanInteractWith(packet.VendorGUID, NPCFlags.Vendor, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug("WORLD: HandleSellItemOpcode - {0} not found or you can not interact with him.", packet.VendorGUID.ToString());
            pl.SendSellError(SellResult.CantFindVendor, null, packet.ItemGUID);

            return;
        }

        if (creature.Template.FlagsExtra.HasFlag(CreatureFlagsExtra.NoSellVendor))
        {
            _player.SendSellError(SellResult.CantSellToThisMerchant, creature, packet.ItemGUID);

            return;
        }

        // remove fake death
        if (pl.HasUnitState(UnitState.Died))
            pl.RemoveAurasByType(AuraType.FeignDeath);

        var pItem = pl.GetItemByGuid(packet.ItemGUID);

        if (pItem != null)
        {
            // prevent sell not owner item
            if (pl.GUID != pItem.OwnerGUID)
            {
                pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }

            // prevent sell non empty bag by drag-and-drop at vendor's item list
            if (pItem.IsNotEmptyBag)
            {
                pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }

            // prevent sell currently looted item
            if (pl.GetLootGUID() == pItem.GUID)
            {
                pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }

            // prevent selling item for sellprice when the item is still refundable
            // this probably happens when right clicking a refundable item, the client sends both
            // CMSG_SELL_ITEM and CMSG_REFUND_ITEM (unverified)
            if (pItem.IsRefundable)
                return; // Therefore, no feedback to client

            // special case at auto sell (sell all)
            if (packet.Amount == 0)
            {
                packet.Amount = pItem.Count;
            }
            else
            {
                // prevent sell more items that exist in stack (possible only not from client)
                if (packet.Amount > pItem.Count)
                {
                    pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                    return;
                }
            }

            var pProto = pItem.Template;

            if (pProto != null)
            {
                if (pProto.SellPrice > 0)
                {
                    ulong money = pProto.SellPrice * packet.Amount;

                    if (!_player.ModifyMoney((long)money)) // ensure player doesn't exceed gold limit
                    {
                        _player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                        return;
                    }

                    _player.UpdateCriteria(CriteriaType.MoneyEarnedFromSales, money);
                    _player.UpdateCriteria(CriteriaType.SellItemsToVendors, 1);

                    if (packet.Amount < pItem.Count) // need split items
                    {
                        var pNewItem = pItem.CloneItem(packet.Amount, pl);

                        if (pNewItem == null)
                        {
                            Log.Logger.Error<uint, uint>("WORLD: HandleSellItemOpcode - could not create clone of item {0}; count = {1}", pItem.Entry, packet.Amount);
                            pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                            return;
                        }

                        pItem.SetCount(pItem.Count - packet.Amount);
                        pl.ItemRemovedQuestCheck(pItem.Entry, packet.Amount);

                        if (pl.IsInWorld)
                            pItem.SendUpdateToPlayer(pl);

                        pItem.SetState(ItemUpdateState.Changed, pl);

                        pl.AddItemToBuyBackSlot(pNewItem);

                        if (pl.IsInWorld)
                            pNewItem.SendUpdateToPlayer(pl);
                    }
                    else
                    {
                        pl.RemoveItem(pItem.BagSlot, pItem.Slot, true);
                        pl.ItemRemovedQuestCheck(pItem.Entry, pItem.Count);
                        Item.RemoveItemFromUpdateQueueOf(pItem, pl);
                        pl.AddItemToBuyBackSlot(pItem);
                    }
                }
                else
                {
                    pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);
                }

                return;
            }
        }

        pl.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

        return;
    }

    [WorldPacketHandler(ClientOpcodes.SocketGems, Processing = PacketProcessing.Inplace)]
    private void HandleSocketGems(SocketGems socketGems)
    {
        if (socketGems.ItemGuid.IsEmpty)
            return;

        //cheat . tried to socket same gem multiple times
        if ((!socketGems.GemItem[0].IsEmpty && (socketGems.GemItem[0] == socketGems.GemItem[1] || socketGems.GemItem[0] == socketGems.GemItem[2])) ||
            (!socketGems.GemItem[1].IsEmpty && (socketGems.GemItem[1] == socketGems.GemItem[2])))
            return;

        var itemTarget = Player.GetItemByGuid(socketGems.ItemGuid);

        if (!itemTarget) //missing item to socket
            return;

        var itemProto = itemTarget.Template;

        if (itemProto == null)
            return;

        //this slot is excepted when applying / removing meta gem bonus
        var slot = itemTarget.IsEquipped ? itemTarget.Slot : ItemConst.NullSlot;

        var gems = new Item[ItemConst.MaxGemSockets];
        var gemData = new ItemDynamicFieldGems[ItemConst.MaxGemSockets];
        var gemProperties = new GemPropertiesRecord[ItemConst.MaxGemSockets];
        var oldGemData = new SocketedGem[ItemConst.MaxGemSockets];


        for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
        {
            var gem = _player.GetItemByGuid(socketGems.GemItem[i]);

            if (gem)
            {
                gems[i] = gem;
                gemData[i].ItemId = gem.Entry;
                gemData[i].Context = (byte)gem.ItemData.Context;

                for (var b = 0; b < gem.GetBonusListIDs().Count && b < 16; ++b)
                    gemData[i].BonusListIDs[b] = (ushort)gem.GetBonusListIDs()[b];

                gemProperties[i] = CliDB.GemPropertiesStorage.LookupByKey(gem.Template.GemProperties);
            }

            oldGemData[i] = itemTarget.GetGem((ushort)i);
        }

        // Find first prismatic socket
        uint firstPrismatic = 0;

        while (firstPrismatic < ItemConst.MaxGemSockets && itemTarget.GetSocketColor(firstPrismatic) != 0)
            ++firstPrismatic;

        for (uint i = 0; i < ItemConst.MaxGemSockets; ++i) //check for hack maybe
        {
            if (gemProperties[i] == null)
                continue;

            // tried to put gem in socket where no socket exists (take care about prismatic sockets)
            if (itemTarget.GetSocketColor(i) == 0)
            {
                // no prismatic socket
                if (itemTarget.GetEnchantmentId(EnchantmentSlot.Prismatic) == 0)
                    return;

                if (i != firstPrismatic)
                    return;
            }

            // Gem must match socket color
            if (ItemConst.SocketColorToGemTypeMask[(int)itemTarget.GetSocketColor(i)] != gemProperties[i].Type)
                // unless its red, blue, yellow or prismatic
                if (!ItemConst.SocketColorToGemTypeMask[(int)itemTarget.GetSocketColor(i)].HasAnyFlag(SocketColor.Prismatic) || !gemProperties[i].Type.HasAnyFlag(SocketColor.Prismatic))
                    return;
        }

        // check unique-equipped conditions
        for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
        {
            if (!gems[i])
                continue;

            // continue check for case when attempt add 2 similar unique equipped gems in one item.
            var iGemProto = gems[i].Template;

            // unique item (for new and already placed bit removed enchantments
            if (iGemProto.HasFlag(ItemFlags.UniqueEquippable))
                for (var j = 0; j < ItemConst.MaxGemSockets; ++j)
                {
                    if (i == j) // skip self
                        continue;

                    if (gems[j])
                    {
                        if (iGemProto.Id == gems[j].Entry)
                        {
                            Player.SendEquipError(InventoryResult.ItemUniqueEquippableSocketed, itemTarget);

                            return;
                        }
                    }
                    else if (oldGemData[j] != null)
                    {
                        if (iGemProto.Id == oldGemData[j].ItemId)
                        {
                            Player.SendEquipError(InventoryResult.ItemUniqueEquippableSocketed, itemTarget);

                            return;
                        }
                    }
                }

            // unique limit type item
            var limit_newcount = 0;

            if (iGemProto.ItemLimitCategory != 0)
            {
                var limitEntry = CliDB.ItemLimitCategoryStorage.LookupByKey(iGemProto.ItemLimitCategory);

                if (limitEntry != null)
                {
                    // NOTE: limitEntry.mode is not checked because if item has limit then it is applied in equip case
                    for (var j = 0; j < ItemConst.MaxGemSockets; ++j)
                        if (gems[j])
                        {
                            // new gem
                            if (iGemProto.ItemLimitCategory == gems[j].Template.ItemLimitCategory)
                                ++limit_newcount;
                        }
                        else if (oldGemData[j] != null)
                        {
                            // existing gem
                            var jProto = Global.ObjectMgr.GetItemTemplate(oldGemData[j].ItemId);

                            if (jProto != null)
                                if (iGemProto.ItemLimitCategory == jProto.ItemLimitCategory)
                                    ++limit_newcount;
                        }

                    if (limit_newcount > 0 && limit_newcount > _player.GetItemLimitCategoryQuantity(limitEntry))
                    {
                        Player.SendEquipError(InventoryResult.ItemUniqueEquippableSocketed, itemTarget);

                        return;
                    }
                }
            }

            // for equipped item check all equipment for duplicate equipped gems
            if (itemTarget.IsEquipped)
            {
                var res = Player.CanEquipUniqueItem(gems[i], slot, (uint)Math.Max(limit_newcount, 0));

                if (res != 0)
                {
                    Player.SendEquipError(res, itemTarget);

                    return;
                }
            }
        }

        var SocketBonusActivated = itemTarget.GemsFitSockets(); //save state of socketbonus
        Player.ToggleMetaGemsActive(slot, false);               //turn off all metagems (except for the target item)

        //if a meta gem is being equipped, all information has to be written to the item before testing if the conditions for the gem are met

        //remove ALL mods - gem can change item level
        if (itemTarget.IsEquipped)
            _player._ApplyItemMods(itemTarget, itemTarget.Slot, false);

        for (ushort i = 0; i < ItemConst.MaxGemSockets; ++i)
            if (gems[i])
            {
                var gemScalingLevel = _player.Level;
                var fixedLevel = gems[i].GetModifier(ItemModifier.TimewalkerLevel);

                if (fixedLevel != 0)
                    gemScalingLevel = fixedLevel;

                itemTarget.SetGem(i, gemData[i], gemScalingLevel);

                if (gemProperties[i] != null && gemProperties[i].EnchantId != 0)
                    itemTarget.SetEnchantment(EnchantmentSlot.Sock1 + i, gemProperties[i].EnchantId, 0, 0, Player.GUID);

                uint gemCount = 1;
                Player.DestroyItemCount(gems[i], ref gemCount, true);
            }

        if (itemTarget.IsEquipped)
            _player._ApplyItemMods(itemTarget, itemTarget.Slot, true);

        var childItem = _player.GetChildItemByGuid(itemTarget.ChildItem);

        if (childItem)
        {
            if (childItem.IsEquipped)
                _player._ApplyItemMods(childItem, childItem.Slot, false);

            childItem.CopyArtifactDataFromParent(itemTarget);

            if (childItem.IsEquipped)
                _player._ApplyItemMods(childItem, childItem.Slot, true);
        }

        var SocketBonusToBeActivated = itemTarget.GemsFitSockets(); //current socketbonus state

        if (SocketBonusActivated ^ SocketBonusToBeActivated) //if there was a change...
        {
            Player.ApplyEnchantment(itemTarget, EnchantmentSlot.Bonus, false);
            itemTarget.SetEnchantment(EnchantmentSlot.Bonus, SocketBonusToBeActivated ? itemTarget.Template.SocketBonus : 0, 0, 0, Player.GUID);
            Player.ApplyEnchantment(itemTarget, EnchantmentSlot.Bonus, true);
            //it is not displayed, client has an inbuilt system to determine if the bonus is activated
        }

        Player.ToggleMetaGemsActive(slot, true); //turn on all metagems (except for target item)

        Player.RemoveTradeableItem(itemTarget);
        itemTarget.ClearSoulboundTradeable(Player); // clear tradeable Id

        itemTarget.SendUpdateSockets();
    }

    [WorldPacketHandler(ClientOpcodes.SortBags, Processing = PacketProcessing.Inplace)]
    private void HandleSortBags(SortBags sortBags)
    {
        // TODO: Implement sorting
        // Placeholder to prevent completely locking out bags clientside
        SendPacket(new BagCleanupFinished());
    }

    [WorldPacketHandler(ClientOpcodes.SortBankBags, Processing = PacketProcessing.Inplace)]
    private void HandleSortBankBags(SortBankBags sortBankBags)
    {
        // TODO: Implement sorting
        // Placeholder to prevent completely locking out bags clientside
        SendPacket(new BagCleanupFinished());
    }

    [WorldPacketHandler(ClientOpcodes.SortReagentBankBags, Processing = PacketProcessing.Inplace)]
    private void HandleSortReagentBankBags(SortReagentBankBags sortReagentBankBags)
    {
        // TODO: Implement sorting
        // Placeholder to prevent completely locking out bags clientside
        SendPacket(new BagCleanupFinished());
    }

    [WorldPacketHandler(ClientOpcodes.SplitItem, Processing = PacketProcessing.Inplace)]
    private void HandleSplitItem(SplitItem splitItem)
    {
        if (splitItem.Inv.Items.Count != 0)
        {
            Log.Logger.Error("WORLD: HandleSplitItemOpcode - Invalid itemCount ({0})", splitItem.Inv.Items.Count);

            return;
        }

        var src = (ushort)((splitItem.FromPackSlot << 8) | splitItem.FromSlot);
        var dst = (ushort)((splitItem.ToPackSlot << 8) | splitItem.ToSlot);

        if (src == dst)
            return;

        if (splitItem.Quantity == 0)
            return; //check count - if zero it's fake packet

        if (!_player.IsValidPos(splitItem.FromPackSlot, splitItem.FromSlot, true))
        {
            _player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!_player.IsValidPos(splitItem.ToPackSlot, splitItem.ToSlot, false)) // can be autostore pos
        {
            _player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        _player.SplitItem(src, dst, (uint)splitItem.Quantity);
    }

    [WorldPacketHandler(ClientOpcodes.SwapInvItem, Processing = PacketProcessing.Inplace)]
    private void HandleSwapInvenotryItem(SwapInvItem swapInvItem)
    {
        if (swapInvItem.Inv.Items.Count != 2)
        {
            Log.Logger.Error("WORLD: HandleSwapInvItemOpcode - Invalid itemCount ({0})", swapInvItem.Inv.Items.Count);

            return;
        }

        // prevent attempt swap same item to current position generated by client at special checting sequence
        if (swapInvItem.Slot1 == swapInvItem.Slot2)
            return;

        if (!Player.IsValidPos(InventorySlots.Bag0, swapInvItem.Slot1, true))
        {
            Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!Player.IsValidPos(InventorySlots.Bag0, swapInvItem.Slot2, true))
        {
            Player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        if (PlayerComputators.IsBankPos(InventorySlots.Bag0, swapInvItem.Slot1) && !CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");

            return;
        }

        if (PlayerComputators.IsBankPos(InventorySlots.Bag0, swapInvItem.Slot2) && !CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");

            return;
        }

        var src = (ushort)((InventorySlots.Bag0 << 8) | swapInvItem.Slot1);
        var dst = (ushort)((InventorySlots.Bag0 << 8) | swapInvItem.Slot2);

        Player.SwapItem(src, dst);
    }
    [WorldPacketHandler(ClientOpcodes.SwapItem, Processing = PacketProcessing.Inplace)]
    private void HandleSwapItem(SwapItem swapItem)
    {
        if (swapItem.Inv.Items.Count != 2)
        {
            Log.Logger.Error("WORLD: HandleSwapItem - Invalid itemCount ({0})", swapItem.Inv.Items.Count);

            return;
        }

        var src = (ushort)((swapItem.ContainerSlotA << 8) | swapItem.SlotA);
        var dst = (ushort)((swapItem.ContainerSlotB << 8) | swapItem.SlotB);

        var pl = Player;

        // prevent attempt swap same item to current position generated by client at special checting sequence
        if (src == dst)
            return;

        if (!pl.IsValidPos(swapItem.ContainerSlotA, swapItem.SlotA, true))
        {
            pl.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!pl.IsValidPos(swapItem.ContainerSlotB, swapItem.SlotB, true))
        {
            pl.SendEquipError(InventoryResult.WrongSlot);

            return;
        }


        if (PlayerComputators.IsBankPos(swapItem.ContainerSlotA, swapItem.SlotA) && !CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");

            return;
        }

        if (PlayerComputators.IsBankPos(swapItem.ContainerSlotB, swapItem.SlotB) && !CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");

            return;
        }

        pl.SwapItem(src, dst);
    }
    [WorldPacketHandler(ClientOpcodes.UseCritterItem)]
    private void HandleUseCritterItem(UseCritterItem useCritterItem)
    {
        var item = Player.GetItemByGuid(useCritterItem.ItemGuid);

        if (!item)
            return;

        foreach (var itemEffect in item.Effects)
        {
            if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                continue;

            var speciesEntry = BattlePetMgr.GetBattlePetSpeciesBySpell((uint)itemEffect.SpellID);

            if (speciesEntry != null)
                BattlePetMgr.AddPet(speciesEntry.Id, BattlePetMgr.SelectPetDisplay(speciesEntry), BattlePetMgr.RollPetBreed(speciesEntry.Id), BattlePetMgr.GetDefaultPetQuality(speciesEntry.Id));
        }

        Player.DestroyItem(item.BagSlot, item.Slot, true);
    }

    [WorldPacketHandler(ClientOpcodes.WrapItem)]
    private void HandleWrapItem(WrapItem packet)
    {
        if (packet.Inv.Items.Count != 2)
        {
            Log.Logger.Error("HandleWrapItem - Invalid itemCount ({0})", packet.Inv.Items.Count);

            return;
        }

        // Gift
        var giftContainerSlot = packet.Inv.Items[0].ContainerSlot;
        var giftSlot = packet.Inv.Items[0].Slot;
        // Item
        var itemContainerSlot = packet.Inv.Items[1].ContainerSlot;
        var itemSlot = packet.Inv.Items[1].Slot;

        var gift = Player.GetItemByPos(giftContainerSlot, giftSlot);

        if (!gift)
        {
            Player.SendEquipError(InventoryResult.ItemNotFound, gift);

            return;
        }

        if (!gift.Template.HasFlag(ItemFlags.IsWrapper)) // cheating: non-wrapper wrapper
        {
            Player.SendEquipError(InventoryResult.ItemNotFound, gift);

            return;
        }

        var item = Player.GetItemByPos(itemContainerSlot, itemSlot);

        if (!item)
        {
            Player.SendEquipError(InventoryResult.ItemNotFound, item);

            return;
        }

        if (item == gift) // not possable with pacjket from real client
        {
            Player.SendEquipError(InventoryResult.CantWrapWrapped, item);

            return;
        }

        if (item.IsEquipped)
        {
            Player.SendEquipError(InventoryResult.CantWrapEquipped, item);

            return;
        }

        if (!item.GiftCreator.IsEmpty) // HasFlag(ITEM_FIELD_FLAGS, ITEM_FLAGS_WRAPPED);
        {
            Player.SendEquipError(InventoryResult.CantWrapWrapped, item);

            return;
        }

        if (item.IsBag)
        {
            Player.SendEquipError(InventoryResult.CantWrapBags, item);

            return;
        }

        if (item.IsSoulBound)
        {
            Player.SendEquipError(InventoryResult.CantWrapBound, item);

            return;
        }

        if (item.MaxStackCount != 1)
        {
            Player.SendEquipError(InventoryResult.CantWrapStackable, item);

            return;
        }

        // maybe not correct check  (it is better than nothing)
        if (item.Template.MaxCount > 0)
        {
            Player.SendEquipError(InventoryResult.CantWrapUnique, item);

            return;
        }

        SQLTransaction trans = new();

        var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_GIFT);
        stmt.AddValue(0, item.OwnerGUID.Counter);
        stmt.AddValue(1, item.GUID.Counter);
        stmt.AddValue(2, item.Entry);
        stmt.AddValue(3, (uint)item.ItemData.DynamicFlags);
        trans.Append(stmt);

        item.Entry = gift.Entry;

        switch (item.Entry)
        {
            case 5042:
                item.Entry = 5043;

                break;
            case 5048:
                item.Entry = 5044;

                break;
            case 17303:
                item.Entry = 17302;

                break;
            case 17304:
                item.Entry = 17305;

                break;
            case 17307:
                item.Entry = 17308;

                break;
            case 21830:
                item.Entry = 21831;

                break;
        }

        item.SetGiftCreator(Player.GUID);
        item.ReplaceAllItemFlags(ItemFieldFlags.Wrapped);
        item.SetState(ItemUpdateState.Changed, Player);

        if (item.State == ItemUpdateState.New) // save new item, to have alway for `character_gifts` record in `item_instance`
        {
            // after save it will be impossible to remove the item from the queue
            Item.RemoveItemFromUpdateQueueOf(item, Player);
            item.SaveToDB(trans); // item gave inventory record unchanged and can be save standalone
        }

        DB.Characters.CommitTransaction(trans);

        uint count = 1;
        Player.DestroyItemCount(gift, ref count, true);
    }
}