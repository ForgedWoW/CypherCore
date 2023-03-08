// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AutobankItem, Processing = PacketProcessing.Inplace)]
        void HandleAutoBankItem(AutoBankItem packet)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            Item item = Player.GetItemByPos(packet.Bag, packet.Slot);
            if (!item)
                return;

            List<ItemPosCount> dest = new();
            InventoryResult msg = Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);
            if (msg != InventoryResult.Ok)
            {
                Player.SendEquipError(msg, item);
                return;
            }

            if (dest.Count == 1 && dest[0].Pos == item.GetPos())
            {
                Player.SendEquipError(InventoryResult.CantSwap, item);
                return;
            }

            Player.RemoveItem(packet.Bag, packet.Slot, true);
            Player.ItemRemovedQuestCheck(item.Entry, item.GetCount());
            Player.BankItem(dest, item, true);
        }

        [WorldPacketHandler(ClientOpcodes.BankerActivate, Processing = PacketProcessing.Inplace)]
        void HandleBankerActivate(Hello packet)
        {
            Creature unit = Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Banker, NPCFlags2.None);
            if (!unit)
            {
                Log.outError(LogFilter.Network, "HandleBankerActivate: {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            if (Player.HasUnitState(UnitState.Died))
                Player.RemoveAurasByType(AuraType.FeignDeath);

            SendShowBank(packet.Unit);
        }

        [WorldPacketHandler(ClientOpcodes.AutostoreBankItem, Processing = PacketProcessing.Inplace)]
        void HandleAutoStoreBankItem(AutoStoreBankItem packet)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankItemOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            Item item = Player.GetItemByPos(packet.Bag, packet.Slot);
            if (!item)
                return;

            if (Player.IsBankPos(packet.Bag, packet.Slot))                 // moving from bank to inventory
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = Player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);
                if (msg != InventoryResult.Ok)
                {
                    Player.SendEquipError(msg, item);
                    return;
                }

                Player.RemoveItem(packet.Bag, packet.Slot, true);
                Item storedItem = Player.StoreItem(dest, item, true);
                if (storedItem)
                    Player.ItemAddedQuestCheck(storedItem.Entry, storedItem.GetCount());
            }
            else                                                    // moving from inventory to bank
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = Player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false);
                if (msg != InventoryResult.Ok)
                {
                    Player.SendEquipError(msg, item);
                    return;
                }

                Player.RemoveItem(packet.Bag, packet.Slot, true);
                Player.BankItem(dest, item, true);
            }
        }

        [WorldPacketHandler(ClientOpcodes.BuyBankSlot, Processing = PacketProcessing.Inplace)]
        void HandleBuyBankSlot(BuyBankSlot packet)
        {
            if (!CanUseBank(packet.Guid))
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBuyBankSlot - {0} not found or you can't interact with him.", packet.Guid.ToString());
                return;
            }

            uint slot = Player.GetBankBagSlotCount();
            // next slot
            ++slot;

            BankBagSlotPricesRecord slotEntry = CliDB.BankBagSlotPricesStorage.LookupByKey(slot);
            if (slotEntry == null)
                return;

            uint price = slotEntry.Cost;
            if (!Player.HasEnoughMoney(price))
                return;

            Player.SetBankBagSlotCount((byte)slot);
            Player.ModifyMoney(-price);
            Player.UpdateCriteria(CriteriaType.BankSlotsPurchased);
        }

        [WorldPacketHandler(ClientOpcodes.BuyReagentBank)]
        void HandleBuyReagentBank(ReagentBank reagentBank)
        {
            if (!CanUseBank(reagentBank.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleBuyReagentBankOpcode - {reagentBank.Banker} not found or you can't interact with him.");
                return;
            }

            if (_player.IsReagentBankUnlocked)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleBuyReagentBankOpcode - Player ({_player.GUID}, name: {_player.GetName()}) tried to unlock reagent bank a 2nd time.");
                return;
            }

            long price = 100 * MoneyConstants.Gold;

            if (!_player.HasEnoughMoney(price))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleBuyReagentBankOpcode - Player ({_player.GUID}, name: {_player.GetName()}) without enough gold.");
                return;
            }

            _player.ModifyMoney(-price);
            _player.UnlockReagentBank();
        }

        [WorldPacketHandler(ClientOpcodes.DepositReagentBank)]
        void HandleReagentBankDeposit(ReagentBank reagentBank)
        {
            if (!CanUseBank(reagentBank.Banker))
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleReagentBankDepositOpcode - {reagentBank.Banker} not found or you can't interact with him.");
                return;
            }

            if (!_player.IsReagentBankUnlocked)
            {
                _player.SendEquipError(InventoryResult.ReagentBankLocked);
                return;
            }

            // query all reagents from player's inventory
            bool anyDeposited = false;
            foreach (Item item in _player.GetCraftingReagentItemsToDeposit())
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = _player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false, true, true);
                if (msg != InventoryResult.Ok)
                {
                    if (msg != InventoryResult.ReagentBankFull || !anyDeposited)
                        _player.SendEquipError(msg, item);
                    break;
                }

                if (dest.Count == 1 && dest[0].Pos == item.GetPos())
                {
                    _player.SendEquipError(InventoryResult.CantSwap, item);
                    continue;
                }

                // store reagent
                _player.RemoveItem(item.GetBagSlot(), item.GetSlot(), true);
                _player.BankItem(dest, item, true);
                anyDeposited = true;
            }
        }

        [WorldPacketHandler(ClientOpcodes.AutobankReagent)]
        void HandleAutoBankReagent(AutoBankReagent autoBankReagent)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankReagentOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            if (!_player.IsReagentBankUnlocked)
            {
                _player.SendEquipError(InventoryResult.ReagentBankLocked);
                return;
            }

            Item item = _player.GetItemByPos(autoBankReagent.PackSlot, autoBankReagent.Slot);
            if (!item)
                return;

            List<ItemPosCount> dest = new();
            InventoryResult msg = _player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item, false, true, true);
            if (msg != InventoryResult.Ok)
            {
                _player.SendEquipError(msg, item);
                return;
            }

            if (dest.Count == 1 && dest[0].Pos == item.GetPos())
            {
                _player.SendEquipError(InventoryResult.CantSwap, item);
                return;
            }

            _player.RemoveItem(autoBankReagent.PackSlot, autoBankReagent.Slot, true);
            _player.BankItem(dest, item, true);
        }

        [WorldPacketHandler(ClientOpcodes.AutostoreBankReagent)]
        void HandleAutoStoreBankReagent(AutoStoreBankReagent autoStoreBankReagent)
        {
            if (!CanUseBank())
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleAutoBankReagentOpcode - {_player.PlayerTalkClass.GetInteractionData().SourceGuid} not found or you can't interact with him.");
                return;
            }

            if (!_player.IsReagentBankUnlocked)
            {
                _player.SendEquipError(InventoryResult.ReagentBankLocked);
                return;
            }

            Item pItem = _player.GetItemByPos(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot);
            if (!pItem)
                return;

            if (Player.IsReagentBankPos(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot))
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = _player.CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem, false);
                if (msg != InventoryResult.Ok)
                {
                    _player.SendEquipError(msg, pItem);
                    return;
                }

                _player.RemoveItem(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot, true);
                _player.StoreItem(dest, pItem, true);
            }
            else
            {
                List<ItemPosCount> dest = new();
                InventoryResult msg = _player.CanBankItem(ItemConst.NullBag, ItemConst.NullSlot, dest, pItem, false, true, true);
                if (msg != InventoryResult.Ok)
                {
                    _player.SendEquipError(msg, pItem);
                    return;
                }

                _player.RemoveItem(autoStoreBankReagent.Slot, autoStoreBankReagent.PackSlot, true);
                _player.BankItem(dest, pItem, true);
            }
        }

        public void SendShowBank(ObjectGuid guid)
        {
            _player.PlayerTalkClass.GetInteractionData().Reset();
            _player.PlayerTalkClass.GetInteractionData().SourceGuid = guid;
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;
            npcInteraction.InteractionType = PlayerInteractionType.Banker;
            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }
    }
}
