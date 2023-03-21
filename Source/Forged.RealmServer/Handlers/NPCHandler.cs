// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendTabardVendorActivate(ObjectGuid guid)
	{
		NPCInteractionOpenResult npcInteraction = new();
		npcInteraction.Npc = guid;
		npcInteraction.InteractionType = PlayerInteractionType.TabardVendor;
		npcInteraction.Success = true;
		SendPacket(npcInteraction);
	}

	public void SendTrainerList(Creature npc, uint trainerId)
	{
		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var trainer = Global.ObjectMgr.GetTrainer(trainerId);

		if (trainer == null)
		{
			Log.outDebug(LogFilter.Network, $"WORLD: SendTrainerList - trainer spells not found for trainer {npc.GUID} id {trainerId}");

			return;
		}

		_player.PlayerTalkClass.GetInteractionData().Reset();
		_player.PlayerTalkClass.GetInteractionData().SourceGuid = npc.GUID;
		_player.PlayerTalkClass.GetInteractionData().TrainerId = trainerId;
		trainer.SendSpells(npc, _player, SessionDbLocaleIndex);
	}

	public void SendStablePet(ObjectGuid guid)
	{
		PetStableList packet = new();
		packet.StableMaster = guid;

		var petStable = Player.PetStable1;

		if (petStable == null)
		{
			SendPacket(packet);

			return;
		}

		for (uint petSlot = 0; petSlot < petStable.ActivePets.Length; ++petSlot)
		{
			if (petStable.ActivePets[petSlot] == null)
				continue;

			var pet = petStable.ActivePets[petSlot];
			PetStableInfo stableEntry;
			stableEntry.PetSlot = petSlot + (int)PetSaveMode.FirstActiveSlot;
			stableEntry.PetNumber = pet.PetNumber;
			stableEntry.CreatureID = pet.CreatureId;
			stableEntry.DisplayID = pet.DisplayId;
			stableEntry.ExperienceLevel = pet.Level;
			stableEntry.PetFlags = PetStableinfo.Active;
			stableEntry.PetName = pet.Name;

			packet.Pets.Add(stableEntry);
		}

		for (uint petSlot = 0; petSlot < petStable.StabledPets.Length; ++petSlot)
		{
			if (petStable.StabledPets[petSlot] == null)
				continue;

			var pet = petStable.StabledPets[petSlot];
			PetStableInfo stableEntry;
			stableEntry.PetSlot = petSlot + (int)PetSaveMode.FirstStableSlot;
			stableEntry.PetNumber = pet.PetNumber;
			stableEntry.CreatureID = pet.CreatureId;
			stableEntry.DisplayID = pet.DisplayId;
			stableEntry.ExperienceLevel = pet.Level;
			stableEntry.PetFlags = PetStableinfo.Inactive;
			stableEntry.PetName = pet.Name;

			packet.Pets.Add(stableEntry);
		}

		SendPacket(packet);
	}

	public void SendListInventory(ObjectGuid vendorGuid)
	{
		var vendor = Player.GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor, NPCFlags2.None);

		if (vendor == null)
		{
			Log.outDebug(LogFilter.Network, "WORLD: SendListInventory - {0} not found or you can not interact with him.", vendorGuid.ToString());
			Player.SendSellError(SellResult.CantFindVendor, null, ObjectGuid.Empty);

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		// Stop the npc if moving
		var pause = vendor.MovementTemplate.GetInteractionPauseTimer();

		if (pause != 0)
			vendor.PauseMovement(pause);

		vendor.HomePosition = vendor.Location;

		var vendorItems = vendor.VendorItems;
		var rawItemCount = vendorItems != null ? vendorItems.GetItemCount() : 0;

		VendorInventory packet = new();
		packet.Vendor = vendor.GUID;

		var discountMod = Player.GetReputationPriceDiscount(vendor);
		byte count = 0;

		for (uint slot = 0; slot < rawItemCount; ++slot)
		{
			var vendorItem = vendorItems.GetItem(slot);

			if (vendorItem == null)
				continue;

			VendorItemPkt item = new();

			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(vendorItem.PlayerConditionId);

			if (playerCondition != null)
				if (!ConditionManager.IsPlayerMeetingCondition(_player, playerCondition))
					item.PlayerConditionFailed = (int)playerCondition.Id;

			if (vendorItem.Type == ItemVendorType.Item)
			{
				var itemTemplate = Global.ObjectMgr.GetItemTemplate(vendorItem.Item);

				if (itemTemplate == null)
					continue;

				var leftInStock = vendorItem.Maxcount == 0 ? -1 : (int)vendor.GetVendorItemCurrentCount(vendorItem);

				if (!Player.IsGameMaster)
				{
					if (!Convert.ToBoolean(itemTemplate.AllowableClass & Player.ClassMask) && itemTemplate.Bonding == ItemBondingType.OnAcquire)
						continue;

					if ((itemTemplate.HasFlag(ItemFlags2.FactionHorde) && Player.Team == TeamFaction.Alliance) ||
						(itemTemplate.HasFlag(ItemFlags2.FactionAlliance) && Player.Team == TeamFaction.Horde))
						continue;

					if (leftInStock == 0)
						continue;
				}

				if (!Global.ConditionMgr.IsObjectMeetingVendorItemConditions(vendor.Entry, vendorItem.Item, _player, vendor))
				{
					Log.outDebug(LogFilter.Condition, "SendListInventory: conditions not met for creature entry {0} item {1}", vendor.Entry, vendorItem.Item);

					continue;
				}

				var price = (ulong)Math.Floor(itemTemplate.BuyPrice * discountMod);
				price = itemTemplate.BuyPrice > 0 ? Math.Max(1ul, price) : price;

				var priceMod = Player.GetTotalAuraModifier(AuraType.ModVendorItemsPrices);

				if (priceMod != 0)
					price -= MathFunctions.CalculatePct(price, priceMod);

				item.MuID = (int)slot + 1;
				item.Durability = (int)itemTemplate.MaxDurability;
				item.ExtendedCostID = (int)vendorItem.ExtendedCost;
				item.Type = (int)vendorItem.Type;
				item.Quantity = leftInStock;
				item.StackCount = (int)itemTemplate.BuyCount;
				item.Price = (ulong)price;
				item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;
				item.Refundable = itemTemplate.HasFlag(ItemFlags.ItemPurchaseRecord) && vendorItem.ExtendedCost != 0 && itemTemplate.MaxStackSize == 1;

				item.Item.ItemID = vendorItem.Item;

				if (!vendorItem.BonusListIDs.Empty())
				{
					item.Item.ItemBonus = new ItemBonuses();
					item.Item.ItemBonus.BonusListIDs = vendorItem.BonusListIDs;
				}

				packet.Items.Add(item);
			}
			else if (vendorItem.Type == ItemVendorType.Currency)
			{
				var currencyTemplate = CliDB.CurrencyTypesStorage.LookupByKey(vendorItem.Item);

				if (currencyTemplate == null)
					continue;

				if (vendorItem.ExtendedCost == 0)
					continue; // there's no price defined for currencies, only extendedcost is used

				item.MuID = (int)slot + 1; // client expects counting to start at 1
				item.ExtendedCostID = (int)vendorItem.ExtendedCost;
				item.Item.ItemID = vendorItem.Item;
				item.Type = (int)vendorItem.Type;
				item.StackCount = (int)vendorItem.Maxcount;
				item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;

				packet.Items.Add(item);
			}
			else
			{
				continue;
			}

			if (++count >= SharedConst.MaxVendorItems)
				break;
		}

		packet.Reason = (byte)(count != 0 ? VendorInventoryReason.None : VendorInventoryReason.Empty);

		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.TabardVendorActivate, Processing = PacketProcessing.Inplace)]
	void HandleTabardVendorActivate(Hello packet)
	{
		var unit = Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.TabardDesigner, NPCFlags2.None);

		if (!unit)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleTabardVendorActivateOpcode - {0} not found or you can not interact with him.", packet.Unit.ToString());

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		SendTabardVendorActivate(packet.Unit);
	}

	void SendTrainerBuyFailed(ObjectGuid trainerGUID, uint spellID, TrainerFailReason trainerFailedReason)
	{
		TrainerBuyFailed trainerBuyFailed = new();
		trainerBuyFailed.TrainerGUID = trainerGUID;
		trainerBuyFailed.SpellID = spellID;                         // should be same as in packet from client
		trainerBuyFailed.TrainerFailedReason = trainerFailedReason; // 1 == "Not enough money for trainer service." 0 == "Trainer service %d unavailable."
		SendPacket(trainerBuyFailed);
	}

	[WorldPacketHandler(ClientOpcodes.RequestStabledPets, Processing = PacketProcessing.Inplace)]
	void HandleRequestStabledPets(RequestStabledPets packet)
	{
		if (!CheckStableMaster(packet.StableMaster))
			return;

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		// remove mounts this fix bug where getting pet from stable while mounted deletes pet.
		if (Player.IsMounted)
			Player.RemoveAurasByType(AuraType.Mounted);

		SendStablePet(packet.StableMaster);
	}

	void SendPetStableResult(StableResult result)
	{
		PetStableResult petStableResult = new();
		petStableResult.Result = result;
		SendPacket(petStableResult);
	}

	[WorldPacketHandler(ClientOpcodes.SetPetSlot)]
	void HandleSetPetSlot(SetPetSlot setPetSlot)
	{
		if (!CheckStableMaster(setPetSlot.StableMaster) || setPetSlot.DestSlot >= (byte)PetSaveMode.LastStableSlot)
		{
			SendPetStableResult(StableResult.InternalError);

			return;
		}

		Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);

		var petStable = Player.PetStable1;

		if (petStable == null)
		{
			SendPetStableResult(StableResult.InternalError);

			return;
		}

		(var srcPet, var srcPetSlot) = Pet.GetLoadPetInfo(petStable, 0, setPetSlot.PetNumber, null);
		var dstPetSlot = (PetSaveMode)setPetSlot.DestSlot;
		var dstPet = Pet.GetLoadPetInfo(petStable, 0, 0, dstPetSlot).Item1;

		if (srcPet == null || srcPet.Type != PetType.Hunter)
		{
			SendPetStableResult(StableResult.InternalError);

			return;
		}

		if (dstPet != null && dstPet.Type != PetType.Hunter)
		{
			SendPetStableResult(StableResult.InternalError);

			return;
		}

		PetStable.PetInfo src = null;
		PetStable.PetInfo dst = null;
		PetSaveMode? newActivePetIndex = null;

		if (SharedConst.IsActivePetSlot(srcPetSlot) && SharedConst.IsActivePetSlot(dstPetSlot))
		{
			// active<.active: only swap ActivePets and CurrentPetIndex (do not despawn pets)
			src = petStable.ActivePets[srcPetSlot - PetSaveMode.FirstActiveSlot];
			dst = petStable.ActivePets[dstPetSlot - PetSaveMode.FirstActiveSlot];

			if (petStable.GetCurrentActivePetIndex().Value == (uint)srcPetSlot)
				newActivePetIndex = dstPetSlot;
			else if (petStable.GetCurrentActivePetIndex().Value == (uint)dstPetSlot)
				newActivePetIndex = srcPetSlot;
		}
		else if (SharedConst.IsStabledPetSlot(srcPetSlot) && SharedConst.IsStabledPetSlot(dstPetSlot))
		{
			// stabled<.stabled: only swap StabledPets
			src = petStable.StabledPets[srcPetSlot - PetSaveMode.FirstStableSlot];
			dst = petStable.StabledPets[dstPetSlot - PetSaveMode.FirstStableSlot];
		}
		else if (SharedConst.IsActivePetSlot(srcPetSlot) && SharedConst.IsStabledPetSlot(dstPetSlot))
		{
			// active<.stabled: swap petStable contents and despawn active pet if it is involved in swap
			if (petStable.CurrentPetIndex.Value == (uint)srcPetSlot)
			{
				var oldPet = _player.CurrentPet;

				if (oldPet != null && !oldPet.IsAlive)
				{
					SendPetStableResult(StableResult.InternalError);

					return;
				}

				_player.RemovePet(oldPet, PetSaveMode.NotInSlot);
			}

			if (dstPet != null)
			{
				var creatureInfo = Global.ObjectMgr.GetCreatureTemplate(dstPet.CreatureId);

				if (creatureInfo == null || !creatureInfo.IsTameable(_player.CanTameExoticPets))
				{
					SendPetStableResult(StableResult.CantControlExotic);

					return;
				}
			}

			src = petStable.ActivePets[srcPetSlot - PetSaveMode.FirstActiveSlot];
			dst = petStable.StabledPets[dstPetSlot - PetSaveMode.FirstStableSlot];
		}
		else if (SharedConst.IsStabledPetSlot(srcPetSlot) && SharedConst.IsActivePetSlot(dstPetSlot))
		{
			// stabled<.active: swap petStable contents and despawn active pet if it is involved in swap
			if (petStable.CurrentPetIndex.Value == (uint)dstPetSlot)
			{
				var oldPet = _player.CurrentPet;

				if (oldPet != null && !oldPet.IsAlive)
				{
					SendPetStableResult(StableResult.InternalError);

					return;
				}

				_player.RemovePet(oldPet, PetSaveMode.NotInSlot);
			}

			var creatureInfo = Global.ObjectMgr.GetCreatureTemplate(srcPet.CreatureId);

			if (creatureInfo == null || !creatureInfo.IsTameable(_player.CanTameExoticPets))
			{
				SendPetStableResult(StableResult.CantControlExotic);

				return;
			}

			src = petStable.StabledPets[srcPetSlot - PetSaveMode.FirstStableSlot];
			dst = petStable.ActivePets[dstPetSlot - PetSaveMode.FirstActiveSlot];
		}

		SQLTransaction trans = new();

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_ID);
		stmt.AddValue(0, (short)dstPetSlot);
		stmt.AddValue(1, _player.GUID.Counter);
		stmt.AddValue(2, srcPet.PetNumber);
		trans.Append(stmt);

		if (dstPet != null)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_SLOT_BY_ID);
			stmt.AddValue(0, (short)srcPetSlot);
			stmt.AddValue(1, _player.GUID.Counter);
			stmt.AddValue(2, dstPet.PetNumber);
			trans.Append(stmt);
		}

		AddTransactionCallback(DB.Characters.AsyncCommitTransaction(trans))
			.AfterComplete(success =>
			{
				var currentPlayerGuid = _player.GUID;

				if (_player && _player.GUID == currentPlayerGuid)
				{
					if (success)
					{
						Extensions.Swap(ref src, ref dst);

						if (newActivePetIndex.HasValue)
							Player.PetStable1.SetCurrentActivePetIndex((uint)newActivePetIndex.Value);

						SendPetStableResult(StableResult.StableSuccess);
					}
					else
					{
						SendPetStableResult(StableResult.InternalError);
					}
				}
			});
	}
}