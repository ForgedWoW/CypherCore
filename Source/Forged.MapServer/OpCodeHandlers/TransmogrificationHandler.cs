// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Networking.Packets.Transmogification;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class TransmogrificationHandler : IWorldSessionHandler
{
    private readonly CollectionMgr _collectionMgr;
    private readonly DB2Manager _db2Manager;
    private readonly DB6Storage<ItemModifiedAppearanceRecord> _itemModifiedAppearanceRecords;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly DB6Storage<PlayerConditionRecord> _playerConditionRecords;
    private readonly WorldSession _session;

    public TransmogrificationHandler(WorldSession session, CollectionMgr collectionMgr, DB2Manager db2Manager, ItemTemplateCache itemTemplateCache,
                                     DB6Storage<PlayerConditionRecord> playerConditionRecords, DB6Storage<ItemModifiedAppearanceRecord> itemModifiedAppearanceRecords)
    {
        _session = session;
        _collectionMgr = collectionMgr;
        _db2Manager = db2Manager;
        _itemTemplateCache = itemTemplateCache;
        _playerConditionRecords = playerConditionRecords;
        _itemModifiedAppearanceRecords = itemModifiedAppearanceRecords;
    }

    public void SendOpenTransmogrifier(ObjectGuid guid)
    {
        _session.SendPacket(new NPCInteractionOpenResult
        {
            Npc = guid,
            InteractionType = PlayerInteractionType.Transmogrifier,
            Success = true
        });
    }

    [WorldPacketHandler(ClientOpcodes.TransmogrifyItems)]
    private void HandleTransmogrifyItems(TransmogrifyItems transmogrifyItems)
    {
        // Validate
        if (_session.Player.GetNPCIfCanInteractWith(transmogrifyItems.Npc, NPCFlags.Transmogrifier, NPCFlags2.None) == null)
        {
            Log.Logger.Debug("WORLD: HandleTransmogrifyItems - Unit (GUID: {0}) not found or _session.Player can't interact with it.", transmogrifyItems.ToString());

            return;
        }

        long cost = 0;
        Dictionary<Item, uint[]> transmogItems = new(); // new Dictionary<Item, Tuple<uint, uint>>();
        Dictionary<Item, uint> illusionItems = new();

        List<Item> resetAppearanceItems = new();
        List<Item> resetIllusionItems = new();
        List<uint> bindAppearances = new();

        foreach (var transmogItem in transmogrifyItems.Items)
        {
            // slot of the transmogrified item
            if (transmogItem.Slot >= EquipmentSlot.End)
            {
                Log.Logger.Debug("WORLD: HandleTransmogrifyItems - _session.Player ({0}, name: {1}) tried to transmogrify wrong slot {2} when transmogrifying items.", _session.Player.GUID.ToString(), _session.Player.GetName(), transmogItem.Slot);

                return;
            }

            // transmogrified item
            var itemTransmogrified = _session.Player.GetItemByPos(InventorySlots.Bag0, (byte)transmogItem.Slot);

            if (itemTransmogrified == null)
            {
                Log.Logger.Debug("WORLD: HandleTransmogrifyItems - _session.Player (GUID: {0}, name: {1}) tried to transmogrify an invalid item in a valid slot (slot: {2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), transmogItem.Slot);

                return;
            }

            if (transmogItem.ItemModifiedAppearanceID != 0 || transmogItem.SecondaryItemModifiedAppearanceID > 0)
            {
                if (transmogItem.ItemModifiedAppearanceID != 0 && !ValidateAndStoreTransmogItem(itemTransmogrified, (uint)transmogItem.ItemModifiedAppearanceID, false, transmogItems, bindAppearances))
                    return;

                if (transmogItem.SecondaryItemModifiedAppearanceID > 0 && !ValidateAndStoreTransmogItem(itemTransmogrified, (uint)transmogItem.SecondaryItemModifiedAppearanceID, true, transmogItems, bindAppearances))
                    return;

                // add cost
                cost += itemTransmogrified.GetSellPrice(_session.Player);
            }
            else
                resetAppearanceItems.Add(itemTransmogrified);

            if (transmogItem.SpellItemEnchantmentID != 0)
            {
                if (transmogItem.Slot != EquipmentSlot.MainHand && transmogItem.Slot != EquipmentSlot.OffHand)
                {
                    Log.Logger.Debug("WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion into non-weapon slot ({2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), transmogItem.Slot);

                    return;
                }

                var illusion = _db2Manager.GetTransmogIllusionForEnchantment((uint)transmogItem.SpellItemEnchantmentID);

                if (illusion == null)
                {
                    Log.Logger.Debug("WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using invalid enchant ({2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), transmogItem.SpellItemEnchantmentID);

                    return;
                }

                if (_playerConditionRecords.TryGetValue((uint)illusion.UnlockConditionID, out var condition))
                    if (!_session.Player.ConditionManager.IsPlayerMeetingCondition(_session.Player, condition))
                    {
                        Log.Logger.Debug("WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using not allowed enchant ({2}).", _session.Player.GUID.ToString(), _session.Player.GetName(), transmogItem.SpellItemEnchantmentID);

                        return;
                    }

                illusionItems[itemTransmogrified] = (uint)transmogItem.SpellItemEnchantmentID;
                cost += illusion.TransmogCost;
            }
            else
                resetIllusionItems.Add(itemTransmogrified);
        }

        if (!_session.Player.HasAuraType(AuraType.RemoveTransmogCost) && cost != 0) // 0 cost if reverting look
        {
            if (!_session.Player.HasEnoughMoney(cost))
                return;

            _session.Player.ModifyMoney(-cost);
        }

        // Everything is fine, proceed
        foreach (var transmogPair in transmogItems)
        {
            var transmogrified = transmogPair.Key;

            if (!transmogrifyItems.CurrentSpecOnly)
            {
                transmogrified.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, transmogPair.Value[0]);
                transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec1, 0);
                transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec2, 0);
                transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec3, 0);
                transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec4, 0);

                transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs, transmogPair.Value[1]);
                transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, 0);
                transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, 0);
                transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, 0);
                transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, 0);
            }
            else
            {
                if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec1) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec1, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec2) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec2, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec3) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec3, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogAppearanceSpec4) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogAppearanceSpec4, transmogrified.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4) == 0)
                    transmogrified.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, transmogrified.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                transmogrified.SetModifier(ItemConst.AppearanceModifierSlotBySpec[_session.Player.GetActiveTalentGroup()], transmogPair.Value[0]);
                transmogrified.SetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[_session.Player.GetActiveTalentGroup()], transmogPair.Value[1]);
            }

            _session.Player.SetVisibleItemSlot(transmogrified.Slot, transmogrified);

            transmogrified.SetNotRefundable(_session.Player);
            transmogrified.ClearSoulboundTradeable(_session.Player);
            transmogrified.SetState(ItemUpdateState.Changed, _session.Player);
        }

        foreach (var (transmogrified, group) in illusionItems)
        {
            if (!transmogrifyItems.CurrentSpecOnly)
            {
                transmogrified.SetModifier(ItemModifier.EnchantIllusionAllSpecs, group);
                transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec1, 0);
                transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec2, 0);
                transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec3, 0);
                transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec4, 0);
            }
            else
            {
                if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec1) == 0)
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec1, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec2) == 0)
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec2, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec3) == 0)
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec3, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                if (transmogrified.GetModifier(ItemModifier.EnchantIllusionSpec4) == 0)
                    transmogrified.SetModifier(ItemModifier.EnchantIllusionSpec4, transmogrified.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                transmogrified.SetModifier(ItemConst.IllusionModifierSlotBySpec[_session.Player.GetActiveTalentGroup()], group);
            }

            _session.Player.SetVisibleItemSlot(transmogrified.Slot, transmogrified);

            transmogrified.SetNotRefundable(_session.Player);
            transmogrified.ClearSoulboundTradeable(_session.Player);
            transmogrified.SetState(ItemUpdateState.Changed, _session.Player);
        }

        foreach (var item in resetAppearanceItems)
        {
            if (!transmogrifyItems.CurrentSpecOnly)
            {
                item.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, 0);
                item.SetModifier(ItemModifier.TransmogAppearanceSpec1, 0);
                item.SetModifier(ItemModifier.TransmogAppearanceSpec2, 0);
                item.SetModifier(ItemModifier.TransmogAppearanceSpec3, 0);
                item.SetModifier(ItemModifier.TransmogAppearanceSpec4, 0);

                item.SetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs, 0);
                item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, 0);
                item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, 0);
                item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, 0);
                item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, 0);
            }
            else
            {
                if (item.GetModifier(ItemModifier.TransmogAppearanceSpec1) == 0)
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec1, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogAppearanceSpec2) == 0)
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec2, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogAppearanceSpec2) == 0)
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec3, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogAppearanceSpec4) == 0)
                    item.SetModifier(ItemModifier.TransmogAppearanceSpec4, item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1) == 0)
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2) == 0)
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3) == 0)
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                if (item.GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4) == 0)
                    item.SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, item.GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));

                item.SetModifier(ItemConst.AppearanceModifierSlotBySpec[_session.Player.GetActiveTalentGroup()], 0);
                item.SetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[_session.Player.GetActiveTalentGroup()], 0);
                item.SetModifier(ItemModifier.EnchantIllusionAllSpecs, 0);
            }

            item.SetState(ItemUpdateState.Changed, _session.Player);
            _session.Player.SetVisibleItemSlot(item.Slot, item);
        }

        foreach (var item in resetIllusionItems)
        {
            if (!transmogrifyItems.CurrentSpecOnly)
            {
                item.SetModifier(ItemModifier.EnchantIllusionAllSpecs, 0);
                item.SetModifier(ItemModifier.EnchantIllusionSpec1, 0);
                item.SetModifier(ItemModifier.EnchantIllusionSpec2, 0);
                item.SetModifier(ItemModifier.EnchantIllusionSpec3, 0);
                item.SetModifier(ItemModifier.EnchantIllusionSpec4, 0);
            }
            else
            {
                if (item.GetModifier(ItemModifier.EnchantIllusionSpec1) == 0)
                    item.SetModifier(ItemModifier.EnchantIllusionSpec1, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                if (item.GetModifier(ItemModifier.EnchantIllusionSpec2) == 0)
                    item.SetModifier(ItemModifier.EnchantIllusionSpec2, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                if (item.GetModifier(ItemModifier.EnchantIllusionSpec3) == 0)
                    item.SetModifier(ItemModifier.EnchantIllusionSpec3, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                if (item.GetModifier(ItemModifier.EnchantIllusionSpec4) == 0)
                    item.SetModifier(ItemModifier.EnchantIllusionSpec4, item.GetModifier(ItemModifier.EnchantIllusionAllSpecs));

                item.SetModifier(ItemConst.IllusionModifierSlotBySpec[_session.Player.GetActiveTalentGroup()], 0);
                item.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, 0);
            }

            item.SetState(ItemUpdateState.Changed, _session.Player);
            _session.Player.SetVisibleItemSlot(item.Slot, item);
        }

        foreach (var itemModifedAppearanceId in bindAppearances)
        {
            var itemsProvidingAppearance = _collectionMgr.GetItemsProvidingTemporaryAppearance(itemModifedAppearanceId);

            foreach (var itemGuid in itemsProvidingAppearance)
            {
                var item = _session.Player.GetItemByGuid(itemGuid);

                if (item == null)
                    continue;

                item.SetNotRefundable(_session.Player);
                item.ClearSoulboundTradeable(_session.Player);
                _collectionMgr.AddItemAppearance(item);
            }
        }
    }

    private bool ValidateAndStoreTransmogItem(Item itemTransmogrified, uint itemModifiedAppearanceId, bool isSecondary, Dictionary<Item, uint[]> transmogItems, List<uint> bindAppearances)
    {
        if (!_itemModifiedAppearanceRecords.TryGetValue(itemModifiedAppearanceId, out var itemModifiedAppearance))
        {
            Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {_session.Player.GUID}, Name: {_session.Player.GetName()} tried to transmogrify using invalid appearance ({itemModifiedAppearanceId}).");

            return false;
        }

        if (isSecondary && itemTransmogrified.Template.InventoryType != InventoryType.Shoulders)
        {
            Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {_session.Player.GUID}, Name: {_session.Player.GetName()} tried to transmogrify secondary appearance to non-shoulder item.");

            return false;
        }

        var (hasAppearance, isTemporary) = _collectionMgr.HasItemAppearance(itemModifiedAppearanceId);

        if (!hasAppearance)
        {
            Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {_session.Player.GUID}, Name: {_session.Player.GetName()} tried to transmogrify using appearance he has not collected ({itemModifiedAppearanceId}).");

            return false;
        }

        var itemTemplate = _itemTemplateCache.GetItemTemplate(itemModifiedAppearance.ItemID);

        if (_session.Player.CanUseItem(itemTemplate) != InventoryResult.Ok)
        {
            Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {_session.Player.GUID}, Name: {_session.Player.GetName()} tried to transmogrify using appearance he can never use ({itemModifiedAppearanceId}).");

            return false;
        }

        // validity of the transmogrification items
        if (!_session.Player.ItemFactory.CanTransmogrifyItemWithItem(itemTransmogrified, itemModifiedAppearance))
        {
            Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {_session.Player.GUID}, Name: {_session.Player.GetName()} failed CanTransmogrifyItemWithItem ({itemTransmogrified.Entry} with appearance {itemModifiedAppearanceId}).");

            return false;
        }

        if (!transmogItems.ContainsKey(itemTransmogrified))
            transmogItems[itemTransmogrified] = new uint[2];

        if (!isSecondary)
            transmogItems[itemTransmogrified][0] = itemModifiedAppearanceId;
        else
            transmogItems[itemTransmogrified][1] = itemModifiedAppearanceId;

        if (isTemporary)
            bindAppearances.Add(itemModifiedAppearanceId);

        return true;
    }
}