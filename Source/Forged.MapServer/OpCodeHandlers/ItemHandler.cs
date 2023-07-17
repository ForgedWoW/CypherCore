// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.BattlePets;
using Forged.MapServer.Chat;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.G;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class ItemHandler : IWorldSessionHandler
{
    private readonly BattlePetData _battlePetData;
    private readonly BattlePetMgr _battlePetMgr;
    private readonly CharacterDatabase _characterDatabase;
    private readonly DB6Storage<GemPropertiesRecord> _gemPropertiesRecords;
    private readonly ItemFactory _itemFactory;
    private readonly DB6Storage<ItemLimitCategoryRecord> _itemLimitCategoryRecords;
    private readonly GameObjectManager _objectManager;
    private readonly PlayerComputators _playerComputators;
    private readonly WorldSession _session;

    public ItemHandler(WorldSession session, PlayerComputators playerComputators, ItemFactory itemFactory, DB6Storage<GemPropertiesRecord> gemPropertiesRecords,
                       DB6Storage<ItemLimitCategoryRecord> itemLimitCategoryRecords, GameObjectManager objectManager, BattlePetData battlePetData, BattlePetMgr battlePetMgr, CharacterDatabase characterDatabase)
    {
        _session = session;
        _playerComputators = playerComputators;
        _itemFactory = itemFactory;
        _gemPropertiesRecords = gemPropertiesRecords;
        _itemLimitCategoryRecords = itemLimitCategoryRecords;
        _objectManager = objectManager;
        _battlePetData = battlePetData;
        _battlePetMgr = battlePetMgr;
        _characterDatabase = characterDatabase;
    }

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

        _session.Player.SendMessageToSet(packet, true);
    }

    public void SendItemEnchantTimeUpdate(ObjectGuid playerguid, ObjectGuid itemguid, uint slot, uint duration)
    {
        ItemEnchantTimeUpdate data = new()
        {
            ItemGuid = itemguid,
            DurationLeft = duration,
            Slot = slot,
            OwnerGuid = playerguid
        };

        _session.SendPacket(data);
    }

    public bool CanUseBank(ObjectGuid bankerGUID = default)
    {
        // bankerGUID parameter is optional, set to 0 by default.
        if (bankerGUID.IsEmpty)
            bankerGUID = _session.Player.PlayerTalkClass.InteractionData.SourceGuid;

        var isUsingBankCommand = bankerGUID == _session.Player.GUID && bankerGUID == _session.Player.PlayerTalkClass.InteractionData.SourceGuid;

        if (isUsingBankCommand)
            return true;

        return _session.Player.GetNPCIfCanInteractWith(bankerGUID, NPCFlags.Banker, NPCFlags2.None) != null;
    }

    [WorldPacketHandler(ClientOpcodes.AutoEquipItem, Processing = PacketProcessing.Inplace)]
    private void HandleAutoEquipItem(AutoEquipItem autoEquipItem)
    {
        if (autoEquipItem.Inv.Items.Count != 1)
        {
            Log.Logger.Error("WORLD: HandleAutoEquipItem - Invalid itemCount ({0})", autoEquipItem.Inv.Items.Count);

            return;
        }

        var srcItem = _session.Player.GetItemByPos(autoEquipItem.PackSlot, autoEquipItem.Slot);

        if (srcItem == null)
            return; // only at cheat

        var msg = _session.Player.CanEquipItem(ItemConst.NullSlot, out var dest, srcItem, !srcItem.IsBag);

        if (msg != InventoryResult.Ok)
        {
            _session.Player.SendEquipError(msg, srcItem);

            return;
        }

        var src = srcItem.Pos;

        if (dest == src) // prevent equip in same slot, only at cheat
            return;

        var dstItem = _session.Player.GetItemByPos(dest);

        if (dstItem == null) // empty slot, simple case
        {
            if (!srcItem.ChildItem.IsEmpty)
            {
                var childEquipResult = _session.Player.CanEquipChildItem(srcItem);

                if (childEquipResult != InventoryResult.Ok)
                {
                    _session.Player.SendEquipError(msg, srcItem);

                    return;
                }
            }

            _session.Player.RemoveItem(autoEquipItem.PackSlot, autoEquipItem.Slot, true);
            _session.Player.EquipItem(dest, srcItem, true);

            if (!srcItem.ChildItem.IsEmpty)
                _session.Player.EquipChildItem(autoEquipItem.PackSlot, autoEquipItem.Slot, srcItem);

            _session.Player.AutoUnequipOffhandIfNeed();
        }
        else // have currently equipped item, not simple case
        {
            var dstbag = dstItem.BagSlot;
            var dstslot = dstItem.Slot;

            msg = _session.Player.CanUnequipItem(dest, !srcItem.IsBag);

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, dstItem);

                return;
            }

            if (!dstItem.HasItemFlag(ItemFieldFlags.Child))
            {
                // check dest.src move possibility
                List<ItemPosCount> sSrc = new();
                ushort eSrc = 0;

                if (_session.Player.IsInventoryPos(src))
                {
                    msg = _session.Player.CanStoreItem(autoEquipItem.PackSlot, autoEquipItem.Slot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = _session.Player.CanStoreItem(autoEquipItem.PackSlot, ItemConst.NullSlot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
                }
                else if (_playerComputators.IsBankPos(src))
                {
                    msg = _session.Player.CanBankItem(autoEquipItem.PackSlot, autoEquipItem.Slot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = _session.Player.CanBankItem(autoEquipItem.PackSlot, ItemConst.NullSlot, sSrc, dstItem, true);

                    if (msg != InventoryResult.Ok)
                        msg = _session.Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, sSrc, dstItem, true);
                }
                else if (_playerComputators.IsEquipmentPos(src))
                {
                    msg = _session.Player.CanEquipItem(autoEquipItem.Slot, out eSrc, dstItem, true);

                    if (msg == InventoryResult.Ok)
                        msg = _session.Player.CanUnequipItem(eSrc, true);
                }

                if (msg == InventoryResult.Ok && _playerComputators.IsEquipmentPos(dest) && !srcItem.ChildItem.IsEmpty)
                    msg = _session.Player.CanEquipChildItem(srcItem);

                if (msg != InventoryResult.Ok)
                {
                    _session.Player.SendEquipError(msg, dstItem, srcItem);

                    return;
                }

                // now do moves, remove...
                _session.Player.RemoveItem(dstbag, dstslot, false);
                _session.Player.RemoveItem(autoEquipItem.PackSlot, autoEquipItem.Slot, false);

                // add to dest
                _session.Player.EquipItem(dest, srcItem, true);

                // add to src
                if (_session.Player.IsInventoryPos(src))
                    _session.Player.StoreItem(sSrc, dstItem, true);
                else if (_playerComputators.IsBankPos(src))
                    _session.Player.BankItem(sSrc, dstItem, true);
                else if (_playerComputators.IsEquipmentPos(src))
                    _session.Player.EquipItem(eSrc, dstItem, true);

                if (_playerComputators.IsEquipmentPos(dest) && !srcItem.ChildItem.IsEmpty)
                    _session.Player.EquipChildItem(autoEquipItem.PackSlot, autoEquipItem.Slot, srcItem);
            }
            else
            {
                var parentItem = _session.Player.GetItemByGuid(dstItem.Creator);

                if (parentItem != null)
                    if (_playerComputators.IsEquipmentPos(dest))
                    {
                        _session.Player.AutoUnequipChildItem(parentItem);
                        // dest is now empty
                        _session.Player.SwapItem(src, dest);
                        // src is now empty
                        _session.Player.SwapItem(parentItem.Pos, src);
                    }
            }

            _session.Player.AutoUnequipOffhandIfNeed();

            // if inventory item was moved, check if we can remove dependent auras, because they were not removed in _session.Player::RemoveItem (update was set to false)
            // do this after swaps are done, we pass nullptr because both weapons could be swapped and none of them should be ignored
            if ((autoEquipItem.PackSlot == InventorySlots.Bag0 && autoEquipItem.Slot < InventorySlots.BagEnd) || (dstbag == InventorySlots.Bag0 && dstslot < InventorySlots.BagEnd))
                _session.Player.ApplyItemDependentAuras(null, false);
        }
    }

    [WorldPacketHandler(ClientOpcodes.AutoEquipItemSlot, Processing = PacketProcessing.Inplace)]
    private void HandleAutoEquipItemSlot(AutoEquipItemSlot packet)
    {
        // cheating attempt, client should never send opcode in that case
        if (packet.Inv.Items.Count != 1 || !_playerComputators.IsEquipmentPos(InventorySlots.Bag0, packet.ItemDstSlot))
            return;

        var item = _session.Player.GetItemByGuid(packet.Item);
        var dstPos = (ushort)(packet.ItemDstSlot | (InventorySlots.Bag0 << 8));
        var srcPos = (ushort)(packet.Inv.Items[0].Slot | (packet.Inv.Items[0].ContainerSlot << 8));

        if (item == null || item.Pos != srcPos || srcPos == dstPos)
            return;

        _session.Player.SwapItem(srcPos, dstPos);
    }

    [WorldPacketHandler(ClientOpcodes.AutoStoreBagItem, Processing = PacketProcessing.Inplace)]
    private void HandleAutoStoreBagItem(AutoStoreBagItem packet)
    {
        if (!packet.Inv.Items.Empty())
        {
            Log.Logger.Error("HandleAutoStoreBagItemOpcode - Invalid itemCount ({0})", packet.Inv.Items.Count);

            return;
        }

        var item = _session.Player.GetItemByPos(packet.ContainerSlotA, packet.SlotA);

        if (item == null)
            return;

        if (!_session.Player.IsValidPos(packet.ContainerSlotB, ItemConst.NullSlot, false)) // can be autostore pos
        {
            _session.Player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        var src = item.Pos;
        InventoryResult msg;

        // check unequip potability for equipped items and bank bags
        if (_playerComputators.IsEquipmentPos(src) || _playerComputators.IsBagPos(src))
        {
            msg = _session.Player.CanUnequipItem(src, !_playerComputators.IsBagPos(src));

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, item);

                return;
            }
        }

        List<ItemPosCount> dest = new();
        msg = _session.Player.CanStoreItem(packet.ContainerSlotB, ItemConst.NullSlot, dest, item);

        if (msg != InventoryResult.Ok)
        {
            _session.Player.SendEquipError(msg, item);

            return;
        }

        // no-op: placed in same slot
        if (dest.Count == 1 && dest[0].Pos == src)
        {
            // just remove grey item state
            _session.Player.SendEquipError(InventoryResult.InternalBagError, item);

            return;
        }

        _session.Player.RemoveItem(packet.ContainerSlotA, packet.SlotA, true);
        _session.Player.StoreItem(dest, item, true);
    }

    [WorldPacketHandler(ClientOpcodes.BuyBackItem, Processing = PacketProcessing.Inplace)]
    private void HandleBuybackItem(BuyBackItem packet)
    {
        var creature = _session.Player.GetNPCIfCanInteractWith(packet.VendorGUID, NPCFlags.Vendor, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug("WORLD: HandleBuybackItem - {0} not found or you can not interact with him.", packet.VendorGUID.ToString());
            _session.Player.SendSellError(SellResult.CantFindVendor, null, ObjectGuid.Empty);

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var pItem = _session.Player.GetItemFromBuyBackSlot(packet.Slot);

        if (pItem != null)
        {
            var price = _session.Player.ActivePlayerData.BuybackPrice[(int)(packet.Slot - InventorySlots.BuyBackStart)];

            if (!_session.Player.HasEnoughMoney(price))
            {
                _session.Player.SendBuyError(BuyResult.NotEnoughtMoney, creature, pItem.Entry);

                return;
            }

            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem);

            if (msg == InventoryResult.Ok)
            {
                _session.Player.ModifyMoney(-price);
                _session.Player.RemoveItemFromBuyBackSlot(packet.Slot, false);
                _session.Player.ItemAddedQuestCheck(pItem.Entry, pItem.Count);
                _session.Player.StoreItem(dest, pItem, true);
            }
            else
                _session.Player.SendEquipError(msg, pItem);

            return;
        }

        _session.Player.SendBuyError(BuyResult.CantFindItem, creature, 0);
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
                var bagItem = _session.Player.GetItemByGuid(packet.ContainerGUID);

                var bag = ItemConst.NullBag;

                if (bagItem is { IsBag: true })
                    bag = bagItem.Slot;
                else if (packet.ContainerGUID == _session.Player.GUID) // The client sends the player guid when trying to store an item in the default backpack
                    bag = InventorySlots.Bag0;

                _session.Player.BuyItemFromVendorSlot(packet.VendorGUID, packet.Muid, packet.Item.ItemID, (byte)packet.Quantity, bag, (byte)packet.Slot);

                break;

            case ItemVendorType.Currency:
                _session.Player.BuyCurrencyFromVendorSlot(packet.VendorGUID, packet.Muid, packet.Item.ItemID, (byte)packet.Quantity);

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
        if (!_playerComputators.IsEquipmentPos(InventorySlots.Bag0, (byte)packet.Slot))
            return;

        var item = _session.Player.GetItemByPos(InventorySlots.Bag0, (byte)packet.Slot);

        if (item == null)
            return;

        if (item.GetEnchantmentId(EnchantmentSlot.Temp) == 0)
            return;

        _session.Player.ApplyEnchantment(item, EnchantmentSlot.Temp, false);
        item.ClearEnchantment(EnchantmentSlot.Temp);
    }

    [WorldPacketHandler(ClientOpcodes.DestroyItem, Processing = PacketProcessing.Inplace)]
    private void HandleDestroyItem(DestroyItem destroyItem)
    {
        var pos = (ushort)((destroyItem.ContainerId << 8) | destroyItem.SlotNum);

        // prevent drop unequipable items (in combat, for example) and non-empty bags
        if (_playerComputators.IsEquipmentPos(pos) || _playerComputators.IsBagPos(pos))
        {
            var msg = _session.Player.CanUnequipItem(pos, false);

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, _session.Player.GetItemByPos(pos));

                return;
            }
        }

        var pItem = _session.Player.GetItemByPos(destroyItem.ContainerId, destroyItem.SlotNum);

        if (pItem == null)
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (pItem.Template.HasFlag(ItemFlags.NoUserDestroy))
        {
            _session.Player.SendEquipError(InventoryResult.DropBoundItem);

            return;
        }

        if (destroyItem.Count != 0)
        {
            var iCount = destroyItem.Count;
            _session.Player.DestroyItemCount(pItem, ref iCount, true);
        }
        else
            _session.Player.DestroyItem(destroyItem.ContainerId, destroyItem.SlotNum, true);
    }

    [WorldPacketHandler(ClientOpcodes.GetItemPurchaseData, Processing = PacketProcessing.Inplace)]
    private void HandleGetItemPurchaseData(GetItemPurchaseData packet)
    {
        var item = _session.Player.GetItemByGuid(packet.ItemGUID);

        if (item == null)
        {
            Log.Logger.Debug("HandleGetItemPurchaseData: Item {0} not found!", packet.ItemGUID.ToString());

            return;
        }

        _session.Player.SendRefundInfo(item);
    }

    [WorldPacketHandler(ClientOpcodes.ItemPurchaseRefund, Processing = PacketProcessing.Inplace)]
    private void HandleItemRefund(ItemPurchaseRefund packet)
    {
        var item = _session.Player.GetItemByGuid(packet.ItemGUID);

        if (item == null)
        {
            Log.Logger.Debug("WorldSession.HandleItemRefund: Item {0} not found!", packet.ItemGUID.ToString());

            return;
        }

        // Don't try to refund item currently being disenchanted
        if (_session.Player.GetLootGUID() == packet.ItemGUID)
            return;

        _session.Player.RefundItem(item);
    }

    [WorldPacketHandler(ClientOpcodes.ReadItem, Processing = PacketProcessing.Inplace)]
    private void HandleReadItem(ReadItem readItem)
    {
        var item = _session.Player.GetItemByPos(readItem.PackSlot, readItem.Slot);

        if (item != null && item.Template.PageText != 0)
        {
            var msg = _session.Player.CanUseItem(item);

            if (msg == InventoryResult.Ok)
            {
                ReadItemResultOK packet = new()
                {
                    Item = item.GUID
                };

                _session.SendPacket(packet);
            }
            else
            {
                // @todo: 6.x research new values
                /*WorldPackets.Item.ReadItemResultFailed packet;
                packet.Item = item.GetGUID();
                packet.Subcode = ??;
                packet.Delay = ??;
                _session.SendPacket(packet);*/

                Log.Logger.Information("STORAGE: Unable to read item");
                _session.Player.SendEquipError(msg, item);
            }
        }
        else
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);
    }

    [WorldPacketHandler(ClientOpcodes.RemoveNewItem, Processing = PacketProcessing.Inplace)]
    private void HandleRemoveNewItem(RemoveNewItem removeNewItem)
    {
        var item = _session.Player.GetItemByGuid(removeNewItem.ItemGuid);

        if (item == null)
        {
            Log.Logger.Debug($"WorldSession.HandleRemoveNewItem: Item ({removeNewItem.ItemGuid}) not found for {_session.GetPlayerInfo()}!");

            return;
        }

        if (!item.HasItemFlag(ItemFieldFlags.NewItem))
            return;

        item.RemoveItemFlag(ItemFieldFlags.NewItem);
        item.SetState(ItemUpdateState.Changed, _session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.SellItem, Processing = PacketProcessing.Inplace)]
    private void HandleSellItem(SellItem packet)
    {
        if (packet.ItemGUID.IsEmpty)
            return;

        var creature = _session.Player.GetNPCIfCanInteractWith(packet.VendorGUID, NPCFlags.Vendor, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug("WORLD: HandleSellItemOpcode - {0} not found or you can not interact with him.", packet.VendorGUID.ToString());
            _session.Player.SendSellError(SellResult.CantFindVendor, null, packet.ItemGUID);

            return;
        }

        if (creature.Template.FlagsExtra.HasFlag(CreatureFlagsExtra.NoSellVendor))
        {
            _session.Player.SendSellError(SellResult.CantSellToThisMerchant, creature, packet.ItemGUID);

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        var pItem = _session.Player.GetItemByGuid(packet.ItemGUID);

        if (pItem != null)
        {
            // prevent sell not owner item
            if (_session.Player.GUID != pItem.OwnerGUID)
            {
                _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }

            // prevent sell non empty bag by drag-and-drop at vendor's item list
            if (pItem.IsNotEmptyBag)
            {
                _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }

            // prevent sell currently looted item
            if (_session.Player.GetLootGUID() == pItem.GUID)
            {
                _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }

            // prevent selling item for sellprice when the item is still refundable
            // this probably happens when right clicking a refundable item, the client sends both
            // CMSG_SELL_ITEM and CMSG_REFUND_ITEM (unverified)
            if (pItem.IsRefundable)
                return; // Therefore, no feedback to client

            // special case at auto sell (sell all)
            if (packet.Amount == 0)
                packet.Amount = pItem.Count;
            else
            {
                // prevent sell more items that exist in stack (possible only not from client)
                if (packet.Amount > pItem.Count)
                {
                    _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                    return;
                }
            }

            var pProto = pItem.Template;

            if (pProto != null)
            {
                if (pProto.SellPrice > 0)
                {
                    ulong money = pProto.SellPrice * packet.Amount;

                    if (!_session.Player.ModifyMoney((long)money)) // ensure player doesn't exceed gold limit
                    {
                        _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                        return;
                    }

                    _session.Player.UpdateCriteria(CriteriaType.MoneyEarnedFromSales, money);
                    _session.Player.UpdateCriteria(CriteriaType.SellItemsToVendors, 1);

                    if (packet.Amount < pItem.Count) // need split items
                    {
                        var pNewItem = pItem.CloneItem(packet.Amount, _session.Player);

                        if (pNewItem == null)
                        {
                            Log.Logger.Error("WORLD: HandleSellItemOpcode - could not create clone of item {0}; count = {1}", pItem.Entry, packet.Amount);
                            _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                            return;
                        }

                        pItem.SetCount(pItem.Count - packet.Amount);
                        _session.Player.ItemRemovedQuestCheck(pItem.Entry, packet.Amount);

                        if (_session.Player.Location.IsInWorld)
                            pItem.SendUpdateToPlayer(_session.Player);

                        pItem.SetState(ItemUpdateState.Changed, _session.Player);

                        _session.Player.AddItemToBuyBackSlot(pNewItem);

                        if (_session.Player.Location.IsInWorld)
                            pNewItem.SendUpdateToPlayer(_session.Player);
                    }
                    else
                    {
                        _session.Player.RemoveItem(pItem.BagSlot, pItem.Slot, true);
                        _session.Player.ItemRemovedQuestCheck(pItem.Entry, pItem.Count);
                        _itemFactory.RemoveItemFromUpdateQueueOf(pItem, _session.Player);
                        _session.Player.AddItemToBuyBackSlot(pItem);
                    }
                }
                else
                    _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);

                return;
            }
        }

        _session.Player.SendSellError(SellResult.CantSellItem, creature, packet.ItemGUID);
    }

    [WorldPacketHandler(ClientOpcodes.SocketGems, Processing = PacketProcessing.Inplace)]
    private void HandleSocketGems(SocketGems socketGems)
    {
        if (socketGems.ItemGuid.IsEmpty)
            return;

        //cheat . tried to socket same gem multiple times
        if ((!socketGems.GemItem[0].IsEmpty && (socketGems.GemItem[0] == socketGems.GemItem[1] || socketGems.GemItem[0] == socketGems.GemItem[2])) ||
            (!socketGems.GemItem[1].IsEmpty && socketGems.GemItem[1] == socketGems.GemItem[2]))
            return;

        var itemTarget = _session.Player.GetItemByGuid(socketGems.ItemGuid);

        if (itemTarget == null) //missing item to socket
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
            var gem = _session.Player.GetItemByGuid(socketGems.GemItem[i]);

            if (gem != null)
            {
                gems[i] = gem;
                gemData[i].ItemId = gem.Entry;
                gemData[i].Context = (byte)gem.ItemData.Context;

                for (var b = 0; b < gem.BonusListIDs.Count && b < 16; ++b)
                    gemData[i].BonusListIDs[b] = (ushort)gem.BonusListIDs[b];

                gemProperties[i] = _gemPropertiesRecords.LookupByKey(gem.Template.GemProperties);
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
            if (ItemConst.SocketColorToGemTypeMask[(int)itemTarget.GetSocketColor(i)] == gemProperties[i].Type)
                continue;

            // unless its red, blue, yellow or prismatic
            if (!ItemConst.SocketColorToGemTypeMask[(int)itemTarget.GetSocketColor(i)].HasAnyFlag(SocketColor.Prismatic) || !gemProperties[i].Type.HasAnyFlag(SocketColor.Prismatic))
                return;
        }

        // check unique-equipped conditions
        for (var i = 0; i < ItemConst.MaxGemSockets; ++i)
        {
            if (gems[i] == null)
                continue;

            // continue check for case when attempt add 2 similar unique equipped gems in one item.
            var iGemProto = gems[i].Template;

            // unique item (for new and already placed bit removed enchantments
            if (iGemProto.HasFlag(ItemFlags.UniqueEquippable))
                for (var j = 0; j < ItemConst.MaxGemSockets; ++j)
                {
                    if (i == j) // skip self
                        continue;

                    if (gems[j] != null)
                    {
                        if (iGemProto.Id != gems[j].Entry)
                            continue;

                        _session.Player.SendEquipError(InventoryResult.ItemUniqueEquippableSocketed, itemTarget);

                        return;
                    }
                    else if (oldGemData[j] != null)
                        if (iGemProto.Id == oldGemData[j].ItemId)
                        {
                            _session.Player.SendEquipError(InventoryResult.ItemUniqueEquippableSocketed, itemTarget);

                            return;
                        }
                }

            // unique limit type item
            var limitNewcount = 0;

            if (iGemProto.ItemLimitCategory != 0)
                if (_itemLimitCategoryRecords.TryGetValue(iGemProto.ItemLimitCategory, out var limitEntry))
                {
                    // NOTE: limitEntry.mode is not checked because if item has limit then it is applied in equip case
                    for (var j = 0; j < ItemConst.MaxGemSockets; ++j)
                        if (gems[j] != null)
                        {
                            // new gem
                            if (iGemProto.ItemLimitCategory == gems[j].Template.ItemLimitCategory)
                                ++limitNewcount;
                        }
                        else if (oldGemData[j] != null)
                        {
                            // existing gem
                            var jProto = _objectManager.GetItemTemplate(oldGemData[j].ItemId);

                            if (jProto == null)
                                continue;

                            if (iGemProto.ItemLimitCategory == jProto.ItemLimitCategory)
                                ++limitNewcount;
                        }

                    if (limitNewcount > 0 && limitNewcount > _session.Player.GetItemLimitCategoryQuantity(limitEntry))
                    {
                        _session.Player.SendEquipError(InventoryResult.ItemUniqueEquippableSocketed, itemTarget);

                        return;
                    }
                }

            // for equipped item check all equipment for duplicate equipped gems
            if (!itemTarget.IsEquipped)
                continue;

            var res = _session.Player.CanEquipUniqueItem(gems[i], slot, (uint)Math.Max(limitNewcount, 0));

            if (res == 0)
                continue;

            _session.Player.SendEquipError(res, itemTarget);

            return;
        }

        var socketBonusActivated = itemTarget.GemsFitSockets(); //save state of socketbonus
        _session.Player.ToggleMetaGemsActive(slot, false);       //turn off all metagems (except for the target item)

        //if a meta gem is being equipped, all information has to be written to the item before testing if the conditions for the gem are met

        //remove ALL mods - gem can change item level
        if (itemTarget.IsEquipped)
            _session.Player._ApplyItemMods(itemTarget, itemTarget.Slot, false);

        for (ushort i = 0; i < ItemConst.MaxGemSockets; ++i)
            if (gems[i] != null)
            {
                var gemScalingLevel = _session.Player.Level;
                var fixedLevel = gems[i].GetModifier(ItemModifier.TimewalkerLevel);

                if (fixedLevel != 0)
                    gemScalingLevel = fixedLevel;

                itemTarget.SetGem(i, gemData[i], gemScalingLevel);

                if (gemProperties[i] != null && gemProperties[i].EnchantId != 0)
                    itemTarget.SetEnchantment(EnchantmentSlot.Sock1 + i, gemProperties[i].EnchantId, 0, 0, _session.Player.GUID);

                uint gemCount = 1;
                _session.Player.DestroyItemCount(gems[i], ref gemCount, true);
            }

        if (itemTarget.IsEquipped)
            _session.Player._ApplyItemMods(itemTarget, itemTarget.Slot, true);

        var childItem = _session.Player.GetChildItemByGuid(itemTarget.ChildItem);

        if (childItem != null)
        {
            if (childItem.IsEquipped)
                _session.Player._ApplyItemMods(childItem, childItem.Slot, false);

            childItem.CopyArtifactDataFromParent(itemTarget);

            if (childItem.IsEquipped)
                _session.Player._ApplyItemMods(childItem, childItem.Slot, true);
        }

        var socketBonusToBeActivated = itemTarget.GemsFitSockets(); //current socketbonus state

        if (socketBonusActivated ^ socketBonusToBeActivated) //if there was a change...
        {
            _session.Player.ApplyEnchantment(itemTarget, EnchantmentSlot.Bonus, false);
            itemTarget.SetEnchantment(EnchantmentSlot.Bonus, socketBonusToBeActivated ? itemTarget.Template.SocketBonus : 0, 0, 0, _session.Player.GUID);
            _session.Player.ApplyEnchantment(itemTarget, EnchantmentSlot.Bonus, true);
            //it is not displayed, client has an inbuilt system to determine if the bonus is activated
        }

        _session.Player.ToggleMetaGemsActive(slot, true); //turn on all metagems (except for target item)

        _session.Player.RemoveTradeableItem(itemTarget);
        itemTarget.ClearSoulboundTradeable(_session.Player); // clear tradeable Id

        itemTarget.SendUpdateSockets();
    }

    [WorldPacketHandler(ClientOpcodes.SortBags, Processing = PacketProcessing.Inplace)]
    private void HandleSortBags(SortBags sortBags)
    {
        // TODO: Implement sorting
        // Placeholder to prevent completely locking out bags clientside

        if (sortBags == null) return;
        _session.SendPacket(new BagCleanupFinished());
    }

    [WorldPacketHandler(ClientOpcodes.SortBankBags, Processing = PacketProcessing.Inplace)]
    private void HandleSortBankBags(SortBankBags sortBankBags)
    {
        // TODO: Implement sorting
        // Placeholder to prevent completely locking out bags clientside

        if (sortBankBags == null) return;
        _session.SendPacket(new BagCleanupFinished());
    }

    [WorldPacketHandler(ClientOpcodes.SortReagentBankBags, Processing = PacketProcessing.Inplace)]
    private void HandleSortReagentBankBags(SortReagentBankBags sortReagentBankBags)
    {
        // TODO: Implement sorting
        // Placeholder to prevent completely locking out bags clientside

        if (sortReagentBankBags == null) return;
        _session.SendPacket(new BagCleanupFinished());
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

        if (!_session.Player.IsValidPos(splitItem.FromPackSlot, splitItem.FromSlot, true))
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!_session.Player.IsValidPos(splitItem.ToPackSlot, splitItem.ToSlot, false)) // can be autostore pos
        {
            _session.Player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        _session.Player.SplitItem(src, dst, (uint)splitItem.Quantity);
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

        if (!_session.Player.IsValidPos(InventorySlots.Bag0, swapInvItem.Slot1, true))
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!_session.Player.IsValidPos(InventorySlots.Bag0, swapInvItem.Slot2, true))
        {
            _session.Player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        if (_playerComputators.IsBankPos(InventorySlots.Bag0, swapInvItem.Slot1) && _session.PacketRouter.TryGetOpCodeHandler(out ItemHandler bankHandler) && !bankHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        if (_playerComputators.IsBankPos(InventorySlots.Bag0, swapInvItem.Slot2) && _session.PacketRouter.TryGetOpCodeHandler(out bankHandler) && !bankHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        var src = (ushort)((InventorySlots.Bag0 << 8) | swapInvItem.Slot1);
        var dst = (ushort)((InventorySlots.Bag0 << 8) | swapInvItem.Slot2);

        _session.Player.SwapItem(src, dst);
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

        // prevent attempt swap same item to current position generated by client at special checting sequence
        if (src == dst)
            return;

        if (!_session.Player.IsValidPos(swapItem.ContainerSlotA, swapItem.SlotA, true))
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!_session.Player.IsValidPos(swapItem.ContainerSlotB, swapItem.SlotB, true))
        {
            _session.Player.SendEquipError(InventoryResult.WrongSlot);

            return;
        }

        if (_playerComputators.IsBankPos(swapItem.ContainerSlotA, swapItem.SlotA) && _session.PacketRouter.TryGetOpCodeHandler(out ItemHandler bankHandler) && !bankHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        if (_playerComputators.IsBankPos(swapItem.ContainerSlotB, swapItem.SlotB) && _session.PacketRouter.TryGetOpCodeHandler(out bankHandler) && !bankHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleSwapInvItemOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        _session.Player.SwapItem(src, dst);
    }

    [WorldPacketHandler(ClientOpcodes.UseCritterItem)]
    private void HandleUseCritterItem(UseCritterItem useCritterItem)
    {
        var item = _session.Player.GetItemByGuid(useCritterItem.ItemGuid);

        if (item == null)
            return;

        foreach (var itemEffect in item.Effects)
        {
            if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                continue;

            var speciesEntry = _battlePetData.GetBattlePetSpeciesBySpell((uint)itemEffect.SpellID);

            if (speciesEntry != null)
                _battlePetMgr.AddPet(speciesEntry.Id, _battlePetData.SelectPetDisplay(speciesEntry), _battlePetData.RollPetBreed(speciesEntry.Id), _battlePetData.GetDefaultPetQuality(speciesEntry.Id));
        }

        _session.Player.DestroyItem(item.BagSlot, item.Slot, true);
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

        var gift = _session.Player.GetItemByPos(giftContainerSlot, giftSlot);

        if (gift == null)
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!gift.Template.HasFlag(ItemFlags.IsWrapper)) // cheating: non-wrapper wrapper
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound, gift);

            return;
        }

        var item = _session.Player.GetItemByPos(itemContainerSlot, itemSlot);

        if (item == null)
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (item == gift) // not possable with pacjket from real client
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapWrapped, item);

            return;
        }

        if (item.IsEquipped)
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapEquipped, item);

            return;
        }

        if (!item.GiftCreator.IsEmpty) // HasFlag(ITEM_FIELD_FLAGS, ITEM_FLAGS_WRAPPED);
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapWrapped, item);

            return;
        }

        if (item.IsBag)
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapBags, item);

            return;
        }

        if (item.IsSoulBound)
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapBound, item);

            return;
        }

        if (item.MaxStackCount != 1)
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapStackable, item);

            return;
        }

        // maybe not correct check  (it is better than nothing)
        if (item.Template.MaxCount > 0)
        {
            _session.Player.SendEquipError(InventoryResult.CantWrapUnique, item);

            return;
        }

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_GIFT);
        stmt.AddValue(0, item.OwnerGUID.Counter);
        stmt.AddValue(1, item.GUID.Counter);
        stmt.AddValue(2, item.Entry);
        stmt.AddValue(3, (uint)item.ItemData.DynamicFlags);
        trans.Append(stmt);

        item.Entry = item.Entry switch
        {
            5042 => 5043,
            5048 => 5044,
            17303 => 17302,
            17304 => 17305,
            17307 => 17308,
            21830 => 21831,
            _ => gift.Entry
        };

        item.SetGiftCreator(_session.Player.GUID);
        item.ReplaceAllItemFlags(ItemFieldFlags.Wrapped);
        item.SetState(ItemUpdateState.Changed, _session.Player);

        if (item.State == ItemUpdateState.New) // save new item, to have alway for `character_gifts` record in `item_instance`
        {
            // after save it will be impossible to remove the item from the queue
            _itemFactory.RemoveItemFromUpdateQueueOf(item, _session.Player);
            item.SaveToDB(trans); // item gave inventory record unchanged and can be save standalone
        }

        _characterDatabase.CommitTransaction(trans);

        uint count = 1;
        _session.Player.DestroyItemCount(gift, ref count, true);
    }
}