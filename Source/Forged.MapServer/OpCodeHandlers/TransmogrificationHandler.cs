// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Networking.Packets.Transmogification;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.OpCodeHandlers;

public class TransmogrificationHandler : IWorldSessionHandler
{
    public void SendOpenTransmogrifier(ObjectGuid guid)
    {
        NPCInteractionOpenResult npcInteraction = new();
        npcInteraction.Npc = guid;
        npcInteraction.InteractionType = PlayerInteractionType.Transmogrifier;
        npcInteraction.Success = true;
        SendPacket(npcInteraction);
    }

    [WorldPacketHandler(ClientOpcodes.TransmogrifyItems)]
    private void HandleTransmogrifyItems(TransmogrifyItems transmogrifyItems)
    {
        var player = Player;

        // Validate
        if (!player.GetNPCIfCanInteractWith(transmogrifyItems.Npc, NPCFlags.Transmogrifier, NPCFlags2.None))
        {
            Log.Logger.Debug("WORLD: HandleTransmogrifyItems - Unit (GUID: {0}) not found or player can't interact with it.", transmogrifyItems.ToString());

            return;
        }

        long cost = 0;
        Dictionary<Item, uint[]> transmogItems = new(); // new Dictionary<Item, Tuple<uint, uint>>();
        Dictionary<Item, uint> illusionItems = new();

        List<Item> resetAppearanceItems = new();
        List<Item> resetIllusionItems = new();
        List<uint> bindAppearances = new();

        bool validateAndStoreTransmogItem(Item itemTransmogrified, uint itemModifiedAppearanceId, bool isSecondary)
        {
            var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);

            if (itemModifiedAppearance == null)
            {
                Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {player.GUID}, Name: {player.GetName()} tried to transmogrify using invalid appearance ({itemModifiedAppearanceId}).");

                return false;
            }

            if (isSecondary && itemTransmogrified.Template.InventoryType != InventoryType.Shoulders)
            {
                Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {player.GUID}, Name: {player.GetName()} tried to transmogrify secondary appearance to non-shoulder item.");

                return false;
            }

            bool hasAppearance, isTemporary;
            (hasAppearance, isTemporary) = CollectionMgr.HasItemAppearance(itemModifiedAppearanceId);

            if (!hasAppearance)
            {
                Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {player.GUID}, Name: {player.GetName()} tried to transmogrify using appearance he has not collected ({itemModifiedAppearanceId}).");

                return false;
            }

            var itemTemplate = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID);

            if (player.CanUseItem(itemTemplate) != InventoryResult.Ok)
            {
                Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {player.GUID}, Name: {player.GetName()} tried to transmogrify using appearance he can never use ({itemModifiedAppearanceId}).");

                return false;
            }

            // validity of the transmogrification items
            if (!Item.CanTransmogrifyItemWithItem(itemTransmogrified, itemModifiedAppearance))
            {
                Log.Logger.Debug($"WORLD: HandleTransmogrifyItems - {player.GUID}, Name: {player.GetName()} failed CanTransmogrifyItemWithItem ({itemTransmogrified.Entry} with appearance {itemModifiedAppearanceId}).");

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

        ;

        foreach (var transmogItem in transmogrifyItems.Items)
        {
            // slot of the transmogrified item
            if (transmogItem.Slot >= EquipmentSlot.End)
            {
                Log.Logger.Debug("WORLD: HandleTransmogrifyItems - Player ({0}, name: {1}) tried to transmogrify wrong slot {2} when transmogrifying items.", player.GUID.ToString(), player.GetName(), transmogItem.Slot);

                return;
            }

            // transmogrified item
            var itemTransmogrified = player.GetItemByPos(InventorySlots.Bag0, (byte)transmogItem.Slot);

            if (!itemTransmogrified)
            {
                Log.Logger.Debug("WORLD: HandleTransmogrifyItems - Player (GUID: {0}, name: {1}) tried to transmogrify an invalid item in a valid slot (slot: {2}).", player.GUID.ToString(), player.GetName(), transmogItem.Slot);

                return;
            }

            if (transmogItem.ItemModifiedAppearanceID != 0 || transmogItem.SecondaryItemModifiedAppearanceID > 0)
            {
                if (transmogItem.ItemModifiedAppearanceID != 0 && !validateAndStoreTransmogItem(itemTransmogrified, (uint)transmogItem.ItemModifiedAppearanceID, false))
                    return;

                if (transmogItem.SecondaryItemModifiedAppearanceID > 0 && !validateAndStoreTransmogItem(itemTransmogrified, (uint)transmogItem.SecondaryItemModifiedAppearanceID, true))
                    return;

                // add cost
                cost += itemTransmogrified.GetSellPrice(_player);
            }
            else
            {
                resetAppearanceItems.Add(itemTransmogrified);
            }

            if (transmogItem.SpellItemEnchantmentID != 0)
            {
                if (transmogItem.Slot != EquipmentSlot.MainHand && transmogItem.Slot != EquipmentSlot.OffHand)
                {
                    Log.Logger.Debug("WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion into non-weapon slot ({2}).", player.GUID.ToString(), player.GetName(), transmogItem.Slot);

                    return;
                }

                var illusion = Global.DB2Mgr.GetTransmogIllusionForEnchantment((uint)transmogItem.SpellItemEnchantmentID);

                if (illusion == null)
                {
                    Log.Logger.Debug("WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using invalid enchant ({2}).", player.GUID.ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);

                    return;
                }

                var condition = CliDB.PlayerConditionStorage.LookupByKey(illusion.UnlockConditionID);

                if (condition != null)
                    if (!ConditionManager.IsPlayerMeetingCondition(player, condition))
                    {
                        Log.Logger.Debug("WORLD: HandleTransmogrifyItems - {0}, Name: {1} tried to transmogrify illusion using not allowed enchant ({2}).", player.GUID.ToString(), player.GetName(), transmogItem.SpellItemEnchantmentID);

                        return;
                    }

                illusionItems[itemTransmogrified] = (uint)transmogItem.SpellItemEnchantmentID;
                cost += illusion.TransmogCost;
            }
            else
            {
                resetIllusionItems.Add(itemTransmogrified);
            }
        }

        if (!player.HasAuraType(AuraType.RemoveTransmogCost) && cost != 0) // 0 cost if reverting look
        {
            if (!player.HasEnoughMoney(cost))
                return;

            player.ModifyMoney(-cost);
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

                transmogrified.SetModifier(ItemConst.AppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], transmogPair.Value[0]);
                transmogrified.SetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], transmogPair.Value[1]);
            }

            player.SetVisibleItemSlot(transmogrified.Slot, transmogrified);

            transmogrified.SetNotRefundable(player);
            transmogrified.ClearSoulboundTradeable(player);
            transmogrified.SetState(ItemUpdateState.Changed, player);
        }

        foreach (var illusionPair in illusionItems)
        {
            var transmogrified = illusionPair.Key;

            if (!transmogrifyItems.CurrentSpecOnly)
            {
                transmogrified.SetModifier(ItemModifier.EnchantIllusionAllSpecs, illusionPair.Value);
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

                transmogrified.SetModifier(ItemConst.IllusionModifierSlotBySpec[player.GetActiveTalentGroup()], illusionPair.Value);
            }

            player.SetVisibleItemSlot(transmogrified.Slot, transmogrified);

            transmogrified.SetNotRefundable(player);
            transmogrified.ClearSoulboundTradeable(player);
            transmogrified.SetState(ItemUpdateState.Changed, player);
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

                item.SetModifier(ItemConst.AppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
                item.SetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
                item.SetModifier(ItemModifier.EnchantIllusionAllSpecs, 0);
            }

            item.SetState(ItemUpdateState.Changed, player);
            player.SetVisibleItemSlot(item.Slot, item);
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

                item.SetModifier(ItemConst.IllusionModifierSlotBySpec[player.GetActiveTalentGroup()], 0);
                item.SetModifier(ItemModifier.TransmogAppearanceAllSpecs, 0);
            }

            item.SetState(ItemUpdateState.Changed, player);
            player.SetVisibleItemSlot(item.Slot, item);
        }

        foreach (var itemModifedAppearanceId in bindAppearances)
        {
            var itemsProvidingAppearance = CollectionMgr.GetItemsProvidingTemporaryAppearance(itemModifedAppearanceId);

            foreach (var itemGuid in itemsProvidingAppearance)
            {
                var item = player.GetItemByGuid(itemGuid);

                if (item)
                {
                    item.SetNotRefundable(player);
                    item.ClearSoulboundTradeable(player);
                    CollectionMgr.AddItemAppearance(item);
                }
            }
        }
    }
}