// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Item;
using Game.Common.Networking.Packets.VoidStorage;

namespace Game;

public partial class WorldSession
{
	public void SendVoidStorageTransferResult(VoidTransferError result)
	{
		SendPacket(new VoidTransferResult(result));
	}

	[WorldPacketHandler(ClientOpcodes.UnlockVoidStorage, Processing = PacketProcessing.Inplace)]
	void HandleVoidStorageUnlock(UnlockVoidStorage unlockVoidStorage)
	{
		var unit = Player.GetNPCIfCanInteractWith(unlockVoidStorage.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);

		if (!unit)
		{
			Log.Logger.Debug("WORLD: HandleVoidStorageUnlock - {0} not found or player can't interact with it.", unlockVoidStorage.Npc.ToString());

			return;
		}

		if (Player.IsVoidStorageUnlocked())
		{
			Log.Logger.Debug("WORLD: HandleVoidStorageUnlock - Player({0}, name: {1}) tried to unlock void storage a 2nd time.", Player.GUID.ToString(), Player.GetName());

			return;
		}

		Player.ModifyMoney(-SharedConst.VoidStorageUnlockCost);
		Player.UnlockVoidStorage();
	}

	[WorldPacketHandler(ClientOpcodes.QueryVoidStorage, Processing = PacketProcessing.Inplace)]
	void HandleVoidStorageQuery(QueryVoidStorage queryVoidStorage)
	{
		var player = Player;

		var unit = player.GetNPCIfCanInteractWith(queryVoidStorage.Npc, NPCFlags.Transmogrifier | NPCFlags.VaultKeeper, NPCFlags2.None);

		if (!unit)
		{
			Log.Logger.Debug("WORLD: HandleVoidStorageQuery - {0} not found or player can't interact with it.", queryVoidStorage.Npc.ToString());
			SendPacket(new VoidStorageFailed());

			return;
		}

		if (!Player.IsVoidStorageUnlocked())
		{
			Log.Logger.Debug("WORLD: HandleVoidStorageQuery - {0} name: {1} queried void storage without unlocking it.", player.GUID.ToString(), player.GetName());
			SendPacket(new VoidStorageFailed());

			return;
		}

		VoidStorageContents voidStorageContents = new();

		for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
		{
			var item = player.GetVoidStorageItem(i);

			if (item == null)
				continue;

			VoidItem voidItem = new();
			voidItem.Guid = ObjectGuid.Create(HighGuid.Item, item.ItemId);
			voidItem.Creator = item.CreatorGuid;
			voidItem.Slot = i;
			voidItem.Item = new ItemInstance(item);

			voidStorageContents.Items.Add(voidItem);
		}

		SendPacket(voidStorageContents);
	}

	[WorldPacketHandler(ClientOpcodes.VoidStorageTransfer)]
	void HandleVoidStorageTransfer(VoidStorageTransfer voidStorageTransfer)
	{
		var player = Player;

		var unit = player.GetNPCIfCanInteractWith(voidStorageTransfer.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);

		if (!unit)
		{
			Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} not found or player can't interact with it.", voidStorageTransfer.Npc.ToString());

			return;
		}

		if (!player.IsVoidStorageUnlocked())
		{
			Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - Player ({0}, name: {1}) queried void storage without unlocking it.", player.GUID.ToString(), player.GetName());

			return;
		}

		if (voidStorageTransfer.Deposits.Length > player.GetNumOfVoidStorageFreeSlots())
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
				var bag = player.GetBagByPos(i);

				if (bag)
					freeBagSlots += bag.GetFreeSlots();
			}

			var inventoryEnd = InventorySlots.ItemStart + _player.GetInventorySlotCount();

			for (var i = InventorySlots.ItemStart; i < inventoryEnd; i++)
				if (!player.GetItemByPos(InventorySlots.Bag0, i))
					++freeBagSlots;
		}

		if (voidStorageTransfer.Withdrawals.Length > freeBagSlots)
		{
			SendVoidStorageTransferResult(VoidTransferError.InventoryFull);

			return;
		}

		if (!player.HasEnoughMoney((voidStorageTransfer.Deposits.Length * SharedConst.VoidStorageStoreItemCost)))
		{
			SendVoidStorageTransferResult(VoidTransferError.NotEnoughMoney);

			return;
		}

		VoidStorageTransferChanges voidStorageTransferChanges = new();

		byte depositCount = 0;

		for (var i = 0; i < voidStorageTransfer.Deposits.Length; ++i)
		{
			var item = player.GetItemByGuid(voidStorageTransfer.Deposits[i]);

			if (!item)
			{
				Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} {1} wants to deposit an invalid item ({2}).", player.GUID.ToString(), player.GetName(), voidStorageTransfer.Deposits[i].ToString());

				continue;
			}

			VoidStorageItem itemVS = new(Global.ObjectMgr.GenerateVoidStorageItemId(),
										item.Entry,
										item.Creator,
										item.ItemRandomBonusListId,
										item.GetModifier(ItemModifier.TimewalkerLevel),
										item.GetModifier(ItemModifier.ArtifactKnowledgeLevel),
										item.GetContext(),
										item.GetBonusListIDs());

			VoidItem voidItem;
			voidItem.Guid = ObjectGuid.Create(HighGuid.Item, itemVS.ItemId);
			voidItem.Creator = item.Creator;
			voidItem.Item = new ItemInstance(itemVS);
			voidItem.Slot = _player.AddVoidStorageItem(itemVS);

			voidStorageTransferChanges.AddedItems.Add(voidItem);

			player.DestroyItem(item.BagSlot, item.Slot, true);
			++depositCount;
		}

		long cost = depositCount * SharedConst.VoidStorageStoreItemCost;

		player.ModifyMoney(-cost);

		for (var i = 0; i < voidStorageTransfer.Withdrawals.Length; ++i)
		{
			var itemVS = player.GetVoidStorageItem(voidStorageTransfer.Withdrawals[i].Counter, out var slot);

			if (itemVS == null)
			{
				Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} {1} tried to withdraw an invalid item ({2})", player.GUID.ToString(), player.GetName(), voidStorageTransfer.Withdrawals[i].ToString());

				continue;
			}

			List<ItemPosCount> dest = new();
			var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemVS.ItemEntry, 1);

			if (msg != InventoryResult.Ok)
			{
				SendVoidStorageTransferResult(VoidTransferError.InventoryFull);
				Log.Logger.Debug("WORLD: HandleVoidStorageTransfer - {0} {1} couldn't withdraw {2} because inventory was full.", player.GUID.ToString(), player.GetName(), voidStorageTransfer.Withdrawals[i].ToString());

				return;
			}

			var item = player.StoreNewItem(dest, itemVS.ItemEntry, true, itemVS.RandomBonusListId, null, itemVS.Context, itemVS.BonusListIDs);
			item.SetCreator(itemVS.CreatorGuid);
			item.SetBinding(true);
			CollectionMgr.AddItemAppearance(item);

			voidStorageTransferChanges.RemovedItems.Add(ObjectGuid.Create(HighGuid.Item, itemVS.ItemId));

			player.DeleteVoidStorageItem(slot);
		}

		SendPacket(voidStorageTransferChanges);
		SendVoidStorageTransferResult(VoidTransferError.Ok);
	}

	[WorldPacketHandler(ClientOpcodes.SwapVoidItem, Processing = PacketProcessing.Inplace)]
	void HandleVoidSwapItem(SwapVoidItem swapVoidItem)
	{
		var player = Player;

		var unit = player.GetNPCIfCanInteractWith(swapVoidItem.Npc, NPCFlags.VaultKeeper, NPCFlags2.None);

		if (!unit)
		{
			Log.Logger.Debug("WORLD: HandleVoidSwapItem - {0} not found or player can't interact with it.", swapVoidItem.Npc.ToString());

			return;
		}

		if (!player.IsVoidStorageUnlocked())
		{
			Log.Logger.Debug("WORLD: HandleVoidSwapItem - Player ({0}, name: {1}) queried void storage without unlocking it.", player.GUID.ToString(), player.GetName());

			return;
		}

		if (player.GetVoidStorageItem(swapVoidItem.VoidItemGuid.Counter, out var oldSlot) == null)
		{
			Log.Logger.Debug("WORLD: HandleVoidSwapItem - Player (GUID: {0}, name: {1}) requested swapping an invalid item (slot: {2}, itemid: {3}).", player.GUID.ToString(), player.GetName(), swapVoidItem.DstSlot, swapVoidItem.VoidItemGuid.ToString());

			return;
		}

		var usedDestSlot = player.GetVoidStorageItem((byte)swapVoidItem.DstSlot) != null;
		var itemIdDest = ObjectGuid.Empty;

		if (usedDestSlot)
			itemIdDest = ObjectGuid.Create(HighGuid.Item, player.GetVoidStorageItem((byte)swapVoidItem.DstSlot).ItemId);

		if (!player.SwapVoidStorageItem(oldSlot, (byte)swapVoidItem.DstSlot))
		{
			SendVoidStorageTransferResult(VoidTransferError.InternalError1);

			return;
		}

		VoidItemSwapResponse voidItemSwapResponse = new();
		voidItemSwapResponse.VoidItemA = swapVoidItem.VoidItemGuid;
		voidItemSwapResponse.VoidItemSlotA = swapVoidItem.DstSlot;

		if (usedDestSlot)
		{
			voidItemSwapResponse.VoidItemB = itemIdDest;
			voidItemSwapResponse.VoidItemSlotB = oldSlot;
		}

		SendPacket(voidItemSwapResponse);
	}
}