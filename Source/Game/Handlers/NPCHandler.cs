﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

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

	[WorldPacketHandler(ClientOpcodes.TrainerList, Processing = PacketProcessing.Inplace)]
	void HandleTrainerList(Hello packet)
	{
		var npc = Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Trainer, NPCFlags2.None);

		if (!npc)
		{
			Log.outDebug(LogFilter.Network, $"WorldSession.SendTrainerList - {packet.Unit} not found or you can not interact with him.");

			return;
		}

		var trainerId = Global.ObjectMgr.GetCreatureDefaultTrainer(npc.Entry);

		if (trainerId != 0)
			SendTrainerList(npc, trainerId);
		else
			Log.outDebug(LogFilter.Network, $"WorldSession.SendTrainerList - Creature id {npc.Entry} has no trainer data.");
	}

	[WorldPacketHandler(ClientOpcodes.TrainerBuySpell, Processing = PacketProcessing.Inplace)]
	void HandleTrainerBuySpell(TrainerBuySpell packet)
	{
		var npc = _player.GetNPCIfCanInteractWith(packet.TrainerGUID, NPCFlags.Trainer, NPCFlags2.None);

		if (npc == null)
		{
			Log.outDebug(LogFilter.Network, $"WORLD: HandleTrainerBuySpell - {packet.TrainerGUID} not found or you can not interact with him.");

			return;
		}

		// remove fake death
		if (_player.HasUnitState(UnitState.Died))
			_player.RemoveAurasByType(AuraType.FeignDeath);

		if (_player.PlayerTalkClass.GetInteractionData().SourceGuid != packet.TrainerGUID)
			return;

		if (_player.PlayerTalkClass.GetInteractionData().TrainerId != packet.TrainerID)
			return;

		// check present spell in trainer spell list
		var trainer = Global.ObjectMgr.GetTrainer(packet.TrainerID);

		if (trainer == null)
			return;

		trainer.TeachSpell(npc, _player, packet.SpellID);
	}

	void SendTrainerBuyFailed(ObjectGuid trainerGUID, uint spellID, TrainerFailReason trainerFailedReason)
	{
		TrainerBuyFailed trainerBuyFailed = new();
		trainerBuyFailed.TrainerGUID = trainerGUID;
		trainerBuyFailed.SpellID = spellID;                         // should be same as in packet from client
		trainerBuyFailed.TrainerFailedReason = trainerFailedReason; // 1 == "Not enough money for trainer service." 0 == "Trainer service %d unavailable."
		SendPacket(trainerBuyFailed);
	}

	[WorldPacketHandler(ClientOpcodes.TalkToGossip, Processing = PacketProcessing.Inplace)]
	void HandleGossipHello(Hello packet)
	{
		var unit = Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Gossip, NPCFlags2.None);

		if (unit == null)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleGossipHello - {0} not found or you can not interact with him.", packet.Unit.ToString());

			return;
		}

		// set faction visible if needed
		var factionTemplateEntry = CliDB.FactionTemplateStorage.LookupByKey(unit.Faction);

		if (factionTemplateEntry != null)
			Player.ReputationMgr.SetVisible(factionTemplateEntry);

		Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);

		// Stop the npc if moving
		var pause = unit.MovementTemplate.GetInteractionPauseTimer();

		if (pause != 0)
			unit.PauseMovement(pause);

		unit.HomePosition = unit.Location;

		// If spiritguide, no need for gossip menu, just put player into resurrect queue
		if (unit.IsSpiritGuide)
		{
			var bg = Player.Battleground;

			if (bg)
			{
				bg.AddPlayerToResurrectQueue(unit.GUID, Player.GUID);
				Global.BattlegroundMgr.SendAreaSpiritHealerQuery(Player, bg, unit.GUID);

				return;
			}
		}

		_player.PlayerTalkClass.ClearMenus();

		if (!unit.AI.OnGossipHello(_player))
		{
			Player.PrepareGossipMenu(unit, unit.Template.GossipMenuId, true);
			Player.SendPreparedGossip(unit);
		}
	}

	[WorldPacketHandler(ClientOpcodes.GossipSelectOption)]
	void HandleGossipSelectOption(GossipSelectOption packet)
	{
		var gossipMenuItem = _player.PlayerTalkClass.GetGossipMenu().GetItem(packet.GossipOptionID);

		if (gossipMenuItem == null)
			return;

		// Prevent cheating on C# scripted menus
		if (Player.PlayerTalkClass.GetInteractionData().SourceGuid != packet.GossipUnit)
			return;

		Creature unit = null;
		GameObject go = null;

		if (packet.GossipUnit.IsCreatureOrVehicle)
		{
			unit = Player.GetNPCIfCanInteractWith(packet.GossipUnit, NPCFlags.Gossip, NPCFlags2.None);

			if (unit == null)
			{
				Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - {0} not found or you can't interact with him.", packet.GossipUnit.ToString());

				return;
			}
		}
		else if (packet.GossipUnit.IsGameObject)
		{
			go = Player.GetGameObjectIfCanInteractWith(packet.GossipUnit);

			if (go == null)
			{
				Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - {0} not found or you can't interact with it.", packet.GossipUnit.ToString());

				return;
			}
		}
		else
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - unsupported {0}.", packet.GossipUnit.ToString());

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		if ((unit && unit.GetScriptId() != unit.LastUsedScriptID) || (go != null && go.ScriptId != go.LastUsedScriptID))
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - Script reloaded while in use, ignoring and set new scipt id");

			if (unit != null)
				unit.LastUsedScriptID = unit.GetScriptId();

			if (go != null)
				go.LastUsedScriptID = go.ScriptId;

			Player.PlayerTalkClass.SendCloseGossip();

			return;
		}

		if (!string.IsNullOrEmpty(packet.PromotionCode))
		{
			if (unit != null)
			{
				if (!unit.AI.OnGossipSelectCode(_player, packet.GossipID, gossipMenuItem.OrderIndex, packet.PromotionCode))
					Player.OnGossipSelect(unit, packet.GossipOptionID, packet.GossipID);
			}
			else
			{
				if (!go.AI.OnGossipSelectCode(_player, packet.GossipID, gossipMenuItem.OrderIndex, packet.PromotionCode))
					_player.OnGossipSelect(go, packet.GossipOptionID, packet.GossipID);
			}
		}
		else
		{
			if (unit != null)
			{
				if (!unit.AI.OnGossipSelect(_player, packet.GossipID, gossipMenuItem.OrderIndex))
					Player.OnGossipSelect(unit, packet.GossipOptionID, packet.GossipID);
			}
			else
			{
				if (!go.AI.OnGossipSelect(_player, packet.GossipID, gossipMenuItem.OrderIndex))
					Player.OnGossipSelect(go, packet.GossipOptionID, packet.GossipID);
			}
		}
	}

	[WorldPacketHandler(ClientOpcodes.SpiritHealerActivate)]
	void HandleSpiritHealerActivate(SpiritHealerActivate packet)
	{
		var unit = Player.GetNPCIfCanInteractWith(packet.Healer, NPCFlags.SpiritHealer, NPCFlags2.None);

		if (!unit)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleSpiritHealerActivateOpcode - {0} not found or you can not interact with him.", packet.Healer.ToString());

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		SendSpiritResurrect();
	}

	void SendSpiritResurrect()
	{
		Player.ResurrectPlayer(0.5f, true);

		Player.DurabilityLossAll(0.25f, true);

		// get corpse nearest graveyard
		WorldSafeLocsEntry corpseGrave = null;
		var corpseLocation = Player.CorpseLocation;

		if (Player.HasCorpse)
			corpseGrave = Global.ObjectMgr.GetClosestGraveYard(corpseLocation, Player.Team, Player);

		// now can spawn bones
		Player.SpawnCorpseBones();

		// teleport to nearest from corpse graveyard, if different from nearest to player ghost
		if (corpseGrave != null)
		{
			var ghostGrave = Global.ObjectMgr.GetClosestGraveYard(Player.Location, Player.Team, Player);

			if (corpseGrave != ghostGrave)
				Player.TeleportTo(corpseGrave.Loc);
		}
	}

	[WorldPacketHandler(ClientOpcodes.BinderActivate, Processing = PacketProcessing.Inplace)]
	void HandleBinderActivate(Hello packet)
	{
		if (!Player.IsInWorld || !Player.IsAlive)
			return;

		var unit = Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Innkeeper, NPCFlags2.None);

		if (!unit)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleBinderActivate - {0} not found or you can not interact with him.", packet.Unit.ToString());

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		SendBindPoint(unit);
	}

	void SendBindPoint(Creature npc)
	{
		// prevent set homebind to instances in any case
		if (Player.Map.Instanceable)
			return;

		uint bindspell = 3286;

		// send spell for homebinding (3286)
		npc.CastSpell(Player, bindspell, true);

		Player.PlayerTalkClass.SendCloseGossip();
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

	[WorldPacketHandler(ClientOpcodes.RepairItem, Processing = PacketProcessing.Inplace)]
	void HandleRepairItem(RepairItem packet)
	{
		var unit = Player.GetNPCIfCanInteractWith(packet.NpcGUID, NPCFlags.Repair, NPCFlags2.None);

		if (!unit)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleRepairItemOpcode - {0} not found or you can not interact with him.", packet.NpcGUID.ToString());

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		// reputation discount
		var discountMod = Player.GetReputationPriceDiscount(unit);

		if (!packet.ItemGUID.IsEmpty)
		{
			Log.outDebug(LogFilter.Network, "ITEM: Repair {0}, at {1}", packet.ItemGUID.ToString(), packet.NpcGUID.ToString());

			var item = Player.GetItemByGuid(packet.ItemGUID);

			if (item)
				Player.DurabilityRepair(item.Pos, true, discountMod);
		}
		else
		{
			Log.outDebug(LogFilter.Network, "ITEM: Repair all items at {0}", packet.NpcGUID.ToString());
			Player.DurabilityRepairAll(true, discountMod, packet.UseGuildBank);
		}
	}

	[WorldPacketHandler(ClientOpcodes.ListInventory, Processing = PacketProcessing.Inplace)]
	void HandleListInventory(Hello packet)
	{
		if (!Player.IsAlive)
			return;

		SendListInventory(packet.Unit);
	}
}