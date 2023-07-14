// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Bank;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class BankHandler : IWorldSessionHandler
{
    private readonly DB6Storage<BankBagSlotPricesRecord> _bagSlotPricesRecords;
    private readonly PlayerComputators _playerComputators;
    private readonly WorldSession _session;

    public BankHandler(WorldSession session, PlayerComputators playerComputators, DB6Storage<BankBagSlotPricesRecord> bagSlotPricesRecords)
    {
        _session = session;
        _playerComputators = playerComputators;
        _bagSlotPricesRecords = bagSlotPricesRecords;
    }

    public void SendShowBank(ObjectGuid guid)
    {
        _session.Player.PlayerTalkClass.InteractionData.Reset();
        _session.Player.PlayerTalkClass.InteractionData.SourceGuid = guid;
        NPCInteractionOpenResult npcInteraction = new()
        {
            Npc = guid,
            InteractionType = PlayerInteractionType.Banker,
            Success = true
        };

        _session.SendPacket(npcInteraction);
    }

    [WorldPacketHandler(ClientOpcodes.AutobankItem, Processing = PacketProcessing.Inplace)]
    private void HandleAutoBankItem(AutoBankItem packet)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleAutoBankItemOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        var item = _session.Player.GetItemByPos(packet.Bag, packet.Slot);

        if (item == null)
            return;

        List<ItemPosCount> dest = new();
        var msg = _session.Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);

        if (msg != InventoryResult.Ok)
        {
            _session.Player.SendEquipError(msg, item);

            return;
        }

        if (dest.Count == 1 && dest[0].Pos == item.Pos)
        {
            _session.Player.SendEquipError(InventoryResult.CantSwap, item);

            return;
        }

        _session.Player.RemoveItem(packet.Bag, packet.Slot, true);
        _session.Player.ItemRemovedQuestCheck(item.Entry, item.Count);
        _session.Player.BankItem(dest, item, true);
    }

    [WorldPacketHandler(ClientOpcodes.AutobankReagent)]
    private void HandleAutoBankReagent(AutoBankReagent autoBankReagent)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleAutoBankReagentOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        if (!_session.Player.IsReagentBankUnlocked)
        {
            _session.Player.SendEquipError(InventoryResult.ReagentBankLocked);

            return;
        }

        var item = _session.Player.GetItemByPos(autoBankReagent.PackSlot, autoBankReagent.Slot);

        if (item == null)
            return;

        List<ItemPosCount> dest = new();
        var msg = _session.Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false, true, true);

        if (msg != InventoryResult.Ok)
        {
            _session.Player.SendEquipError(msg, item);

            return;
        }

        if (dest.Count == 1 && dest[0].Pos == item.Pos)
        {
            _session.Player.SendEquipError(InventoryResult.CantSwap, item);

            return;
        }

        _session.Player.RemoveItem(autoBankReagent.PackSlot, autoBankReagent.Slot, true);
        _session.Player.BankItem(dest, item, true);
    }

    [WorldPacketHandler(ClientOpcodes.AutostoreBankItem, Processing = PacketProcessing.Inplace)]
    private void HandleAutoStoreBankItem(AutoStoreBankItem packet)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleAutoBankItemOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        var item = _session.Player.GetItemByPos(packet.Bag, packet.Slot);

        if (item == null)
            return;

        if (_playerComputators.IsBankPos(packet.Bag, packet.Slot)) // moving from bank to inventory
        {
            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item);

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, item);

                return;
            }

            _session.Player.RemoveItem(packet.Bag, packet.Slot, true);
            var storedItem = _session.Player.StoreItem(dest, item, true);

            if (storedItem != null)
                _session.Player.ItemAddedQuestCheck(storedItem.Entry, storedItem.Count);
        }
        else // moving from inventory to bank
        {
            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, item);

                return;
            }

            _session.Player.RemoveItem(packet.Bag, packet.Slot, true);
            _session.Player.BankItem(dest, item, true);
        }
    }

    [WorldPacketHandler(ClientOpcodes.AutostoreBankReagent)]
    private void HandleAutoStoreBankReagent(AutoStoreBankReagent autoStoreBankReagent)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank())
        {
            Log.Logger.Debug($"WORLD: HandleAutoBankReagentOpcode - {_session.Player.PlayerTalkClass.InteractionData.SourceGuid} not found or you can't interact with him.");

            return;
        }

        if (!_session.Player.IsReagentBankUnlocked)
        {
            _session.Player.SendEquipError(InventoryResult.ReagentBankLocked);

            return;
        }

        var pItem = _session.Player.GetItemByPos(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot);

        if (pItem == null)
            return;

        if (_playerComputators.IsReagentBankPos(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot))
        {
            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem);

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, pItem);

                return;
            }

            _session.Player.RemoveItem(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot, true);
            _session.Player.StoreItem(dest, pItem, true);
        }
        else
        {
            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem, false, true, true);

            if (msg != InventoryResult.Ok)
            {
                _session.Player.SendEquipError(msg, pItem);

                return;
            }

            _session.Player.RemoveItem(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot, true);
            _session.Player.BankItem(dest, pItem, true);
        }
    }

    [WorldPacketHandler(ClientOpcodes.BankerActivate, Processing = PacketProcessing.Inplace)]
    private void HandleBankerActivate(Hello packet)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Banker, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Error("HandleBankerActivate: {0} not found or you can not interact with him.", packet.Unit.ToString());

            return;
        }

        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        SendShowBank(packet.Unit);
    }

    [WorldPacketHandler(ClientOpcodes.BuyBankSlot, Processing = PacketProcessing.Inplace)]
    private void HandleBuyBankSlot(BuyBankSlot packet)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank(packet.Guid))
        {
            Log.Logger.Debug("WORLD: HandleBuyBankSlot - {0} not found or you can't interact with him.", packet.Guid.ToString());

            return;
        }

        uint slot = _session.Player.GetBankBagSlotCount();
        // next slot
        ++slot;

        var slotEntry = _bagSlotPricesRecords.LookupByKey(slot);

        if (slotEntry == null)
            return;

        var price = slotEntry.Cost;

        if (!_session.Player.HasEnoughMoney(price))
            return;

        _session.Player.SetBankBagSlotCount((byte)slot);
        _session.Player.ModifyMoney(-price);
        _session.Player.UpdateCriteria(CriteriaType.BankSlotsPurchased);
    }

    [WorldPacketHandler(ClientOpcodes.BuyReagentBank)]
    private void HandleBuyReagentBank(ReagentBank reagentBank)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank(reagentBank.Banker))
        {
            Log.Logger.Debug($"WORLD: HandleBuyReagentBankOpcode - {reagentBank.Banker} not found or you can't interact with him.");

            return;
        }

        if (_session.Player.IsReagentBankUnlocked)
        {
            Log.Logger.Debug($"WORLD: HandleBuyReagentBankOpcode - _session.Player ({_session.Player.GUID}, name: {_session.Player.GetName()}) tried to unlock reagent bank a 2nd time.");

            return;
        }

        long price = 100 * MoneyConstants.Gold;

        if (!_session.Player.HasEnoughMoney(price))
        {
            Log.Logger.Debug($"WORLD: HandleBuyReagentBankOpcode - _session.Player ({_session.Player.GUID}, name: {_session.Player.GetName()}) without enough gold.");

            return;
        }

        _session.Player.ModifyMoney(-price);
        _session.Player.UnlockReagentBank();
    }

    [WorldPacketHandler(ClientOpcodes.DepositReagentBank)]
    private void HandleReagentBankDeposit(ReagentBank reagentBank)
    {
        if (_session.PacketRouter.TryGetOpCodeHandler(out ItemHandler itemHandler) && !itemHandler.CanUseBank(reagentBank.Banker))
        {
            Log.Logger.Debug($"WORLD: HandleReagentBankDepositOpcode - {reagentBank.Banker} not found or you can't interact with him.");

            return;
        }

        if (!_session.Player.IsReagentBankUnlocked)
        {
            _session.Player.SendEquipError(InventoryResult.ReagentBankLocked);

            return;
        }

        // query all reagents from _session.Player's inventory
        var anyDeposited = false;

        foreach (var item in _session.Player.GetCraftingReagentItemsToDeposit())
        {
            List<ItemPosCount> dest = new();
            var msg = _session.Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false, true, true);

            if (msg != InventoryResult.Ok)
            {
                if (msg != InventoryResult.ReagentBankFull || !anyDeposited)
                    _session.Player.SendEquipError(msg, item);

                break;
            }

            if (dest.Count == 1 && dest[0].Pos == item.Pos)
            {
                _session.Player.SendEquipError(InventoryResult.CantSwap, item);

                continue;
            }

            // store reagent
            _session.Player.RemoveItem(item.BagSlot, item.Slot, true);
            _session.Player.BankItem(dest, item, true);
            anyDeposited = true;
        }
    }
}