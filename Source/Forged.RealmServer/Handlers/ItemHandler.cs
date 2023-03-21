// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.BattlePets;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendEnchantmentLog(ObjectGuid owner, ObjectGuid caster, ObjectGuid itemGuid, uint itemId, uint enchantId, uint enchantSlot)
	{
		EnchantmentLog packet = new();
		packet.Owner = owner;
		packet.Caster = caster;
		packet.ItemGUID = itemGuid;
		packet.ItemID = itemId;
		packet.Enchantment = enchantId;
		packet.EnchantSlot = enchantSlot;

		Player.SendMessageToSet(packet, true);
	}

	public void SendItemEnchantTimeUpdate(ObjectGuid Playerguid, ObjectGuid Itemguid, uint slot, uint Duration)
	{
		ItemEnchantTimeUpdate data = new();
		data.ItemGuid = Itemguid;
		data.DurationLeft = Duration;
		data.Slot = slot;
		data.OwnerGuid = Playerguid;
		SendPacket(data);
	}

	[WorldPacketHandler(ClientOpcodes.SplitItem, Processing = PacketProcessing.Inplace)]
	void HandleSplitItem(SplitItem splitItem)
	{
		if (splitItem.Inv.Items.Count != 0)
		{
			Log.outError(LogFilter.Network, "WORLD: HandleSplitItemOpcode - Invalid itemCount ({0})", splitItem.Inv.Items.Count);

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

	[WorldPacketHandler(ClientOpcodes.AutoEquipItemSlot, Processing = PacketProcessing.Inplace)]
	void HandleAutoEquipItemSlot(AutoEquipItemSlot packet)
	{
		// cheating attempt, client should never send opcode in that case
		if (packet.Inv.Items.Count != 1 || !Player.IsEquipmentPos(InventorySlots.Bag0, packet.ItemDstSlot))
			return;

		var item = Player.GetItemByGuid(packet.Item);
		var dstPos = (ushort)(packet.ItemDstSlot | (InventorySlots.Bag0 << 8));
		var srcPos = (ushort)(packet.Inv.Items[0].Slot | (packet.Inv.Items[0].ContainerSlot << 8));

		if (item == null || item.Pos != srcPos || srcPos == dstPos)
			return;

		Player.SwapItem(srcPos, dstPos);
	}

	[WorldPacketHandler(ClientOpcodes.WrapItem)]
	void HandleWrapItem(WrapItem packet)
	{
		if (packet.Inv.Items.Count != 2)
		{
			Log.outError(LogFilter.Network, "HandleWrapItem - Invalid itemCount ({0})", packet.Inv.Items.Count);

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

	[WorldPacketHandler(ClientOpcodes.ItemPurchaseRefund, Processing = PacketProcessing.Inplace)]
	void HandleItemRefund(ItemPurchaseRefund packet)
	{
		var item = Player.GetItemByGuid(packet.ItemGUID);

		if (!item)
		{
			Log.outDebug(LogFilter.Network, "WorldSession.HandleItemRefund: Item {0} not found!", packet.ItemGUID.ToString());

			return;
		}

		// Don't try to refund item currently being disenchanted
		if (Player.GetLootGUID() == packet.ItemGUID)
			return;

		Player.RefundItem(item);
	}

	bool CanUseBank(ObjectGuid bankerGUID = default)
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

	[WorldPacketHandler(ClientOpcodes.UseCritterItem)]
	void HandleUseCritterItem(UseCritterItem useCritterItem)
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

	[WorldPacketHandler(ClientOpcodes.SortBankBags, Processing = PacketProcessing.Inplace)]
	void HandleSortBankBags(SortBankBags sortBankBags)
	{
		// TODO: Implement sorting
		// Placeholder to prevent completely locking out bags clientside
		SendPacket(new BagCleanupFinished());
	}

	[WorldPacketHandler(ClientOpcodes.SortReagentBankBags, Processing = PacketProcessing.Inplace)]
	void HandleSortReagentBankBags(SortReagentBankBags sortReagentBankBags)
	{
		// TODO: Implement sorting
		// Placeholder to prevent completely locking out bags clientside
		SendPacket(new BagCleanupFinished());
	}
}