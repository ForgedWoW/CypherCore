// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.Common.Loot;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Scripting.Interfaces.IItem;
using Game.Spells;
using Game.Common.Networking;
using Game.Common.Networking.Packets.GameObject;
using Game.Common.Networking.Packets.Pet;
using Game.Common.Networking.Packets.Spell;
using Game.Common.Networking.Packets.Totem;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.UseItem, Processing = PacketProcessing.Inplace)]
	void HandleUseItem(UseItem packet)
	{
		var user = Player;

		// ignore for remote control state
		if (user.UnitBeingMoved != user)
			return;

		var item = user.GetUseableItemByPos(packet.PackSlot, packet.Slot);

		if (item == null)
		{
			user.SendEquipError(InventoryResult.ItemNotFound);

			return;
		}

		if (item.GUID != packet.CastItem)
		{
			user.SendEquipError(InventoryResult.ItemNotFound);

			return;
		}

		var proto = item.Template;

		if (proto == null)
		{
			user.SendEquipError(InventoryResult.ItemNotFound, item);

			return;
		}

		// some item classes can be used only in equipped state
		if (proto.InventoryType != InventoryType.NonEquip && !item.IsEquipped)
		{
			user.SendEquipError(InventoryResult.ItemNotFound, item);

			return;
		}

		var msg = user.CanUseItem(item);

		if (msg != InventoryResult.Ok)
		{
			user.SendEquipError(msg, item);

			return;
		}

		// only allow conjured consumable, bandage, poisons (all should have the 2^21 item flag set in DB)
		if (proto.Class == ItemClass.Consumable && !proto.HasFlag(ItemFlags.IgnoreDefaultArenaRestrictions) && user.InArena)
		{
			user.SendEquipError(InventoryResult.NotDuringArenaMatch, item);

			return;
		}

		// don't allow items banned in arena
		if (proto.HasFlag(ItemFlags.NotUseableInArena) && user.InArena)
		{
			user.SendEquipError(InventoryResult.NotDuringArenaMatch, item);

			return;
		}

		if (user.IsInCombat)
			foreach (var effect in item.Effects)
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo((uint)effect.SpellID, user.Map.DifficultyID);

				if (spellInfo != null)
					if (!spellInfo.CanBeUsedInCombat)
					{
						user.SendEquipError(InventoryResult.NotInCombat, item);

						return;
					}
			}

		// check also  BIND_WHEN_PICKED_UP and BIND_QUEST_ITEM for .additem or .additemset case by GM (not binded at adding to inventory)
		if (item.Bonding == ItemBondingType.OnUse || item.Bonding == ItemBondingType.OnAcquire || item.Bonding == ItemBondingType.Quest)
			if (!item.IsSoulBound)
			{
				item.SetState(ItemUpdateState.Changed, user);
				item.SetBinding(true);
				CollectionMgr.AddItemAppearance(item);
			}

		user.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ItemUse);

		SpellCastTargets targets = new(user, packet.Cast);

		// Note: If script stop casting it must send appropriate data to client to prevent stuck item in gray state.
		if (!Global.ScriptMgr.RunScriptRet<IItemOnUse>(p => p.OnUse(user, item, targets, packet.Cast.CastID), item.ScriptId))
			// no script or script not process request by self
			user.CastItemUseSpell(item, targets, packet.Cast.CastID, packet.Cast.Misc);
	}

	[WorldPacketHandler(ClientOpcodes.OpenItem, Processing = PacketProcessing.Inplace)]
	void HandleOpenItem(OpenItem packet)
	{
		var player = Player;

		// ignore for remote control state
		if (player.UnitBeingMoved != player)
			return;

		// additional check, client outputs message on its own
		if (!player.IsAlive)
		{
			player.SendEquipError(InventoryResult.PlayerDead);

			return;
		}

		var item = player.GetItemByPos(packet.Slot, packet.PackSlot);

		if (!item)
		{
			player.SendEquipError(InventoryResult.ItemNotFound);

			return;
		}

		var proto = item.Template;

		if (proto == null)
		{
			player.SendEquipError(InventoryResult.ItemNotFound, item);

			return;
		}

		// Verify that the bag is an actual bag or wrapped item that can be used "normally"
		if (!proto.HasFlag(ItemFlags.HasLoot) && !item.IsWrapped)
		{
			player.SendEquipError(InventoryResult.ClientLockedOut, item);

			Log.outError(LogFilter.Network,
						"Possible hacking attempt: Player {0} [guid: {1}] tried to open item [guid: {2}, entry: {3}] which is not openable!",
						player.GetName(),
						player.GUID.ToString(),
						item.GUID.ToString(),
						proto.Id);

			return;
		}

		// locked item
		var lockId = proto.LockID;

		if (lockId != 0)
		{
			var lockInfo = CliDB.LockStorage.LookupByKey(lockId);

			if (lockInfo == null)
			{
				player.SendEquipError(InventoryResult.ItemLocked, item);
				Log.outError(LogFilter.Network, "WORLD:OpenItem: item [guid = {0}] has an unknown lockId: {1}!", item.GUID.ToString(), lockId);

				return;
			}

			// was not unlocked yet
			if (item.IsLocked)
			{
				player.SendEquipError(InventoryResult.ItemLocked, item);

				return;
			}
		}

		if (item.IsWrapped) // wrapped?
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GIFT_BY_ITEM);
			stmt.AddValue(0, item.GUID.Counter);

			var pos = item.Pos;
			var itemGuid = item.GUID;

			_queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt)
										.WithCallback(result => HandleOpenWrappedItemCallback(pos, itemGuid, result)));
		}
		else
		{
			// If item doesn't already have loot, attempt to load it. If that
			// fails then this is first time opening, generate loot
			if (!item.LootGenerated && !Global.LootItemStorage.LoadStoredLoot(item, player))
			{
				Loot loot = new(player.Map, item.GUID, LootType.Item, null);
				item.Loot = loot;
				loot.GenerateMoneyLoot(item.Template.MinMoneyLoot, item.Template.MaxMoneyLoot);
				loot.FillLoot(item.Entry, LootStorage.Items, player, true, loot.gold != 0);

				// Force save the loot and money items that were just rolled
				//  Also saves the container item ID in Loot struct (not to DB)
				if (loot.gold > 0 || loot.unlootedCount > 0)
					Global.LootItemStorage.AddNewStoredLoot(item.GUID.Counter, loot, player);
			}

			if (item.Loot != null)
				player.SendLoot(item.Loot);
			else
				player.SendLootError(ObjectGuid.Empty, item.GUID, LootError.NoLoot);
		}
	}

	void HandleOpenWrappedItemCallback(ushort pos, ObjectGuid itemGuid, SQLResult result)
	{
		if (!Player)
			return;

		var item = Player.GetItemByPos(pos);

		if (!item)
			return;

		if (item.GUID != itemGuid || !item.IsWrapped) // during getting result, gift was swapped with another item
			return;

		if (result.IsEmpty())
		{
			Log.outError(LogFilter.Network, $"Wrapped item {item.GUID} don't have record in character_gifts table and will deleted");
			Player.DestroyItem(item.BagSlot, item.Slot, true);

			return;
		}

		SQLTransaction trans = new();

		var entry = result.Read<uint>(0);
		var flags = result.Read<uint>(1);

		item.SetGiftCreator(ObjectGuid.Empty);
		item.Entry = entry;
		item.ReplaceAllItemFlags((ItemFieldFlags)flags);
		item.SetMaxDurability(item.Template.MaxDurability);
		item.SetState(ItemUpdateState.Changed, Player);

		Player.SaveInventoryAndGoldToDB(trans);

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
		stmt.AddValue(0, itemGuid.Counter);
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);
	}

	[WorldPacketHandler(ClientOpcodes.GameObjUse, Processing = PacketProcessing.Inplace)]
	void HandleGameObjectUse(GameObjUse packet)
	{
		var obj = Player.GetGameObjectIfCanInteractWith(packet.Guid);

		if (obj)
		{
			// ignore for remote control state
			if (Player.UnitBeingMoved != Player)
				if (!(Player.IsOnVehicle(Player.UnitBeingMoved) || Player.IsMounted) && !obj.Template.IsUsableMounted())
					return;

			obj.Use(Player);
		}
	}

	[WorldPacketHandler(ClientOpcodes.GameObjReportUse, Processing = PacketProcessing.Inplace)]
	void HandleGameobjectReportUse(GameObjReportUse packet)
	{
		// ignore for remote control state
		if (Player.UnitBeingMoved != Player)
			return;

		var go = Player.GetGameObjectIfCanInteractWith(packet.Guid);

		if (go)
		{
			if (go.AI.OnGossipHello(Player))
				return;

			Player.UpdateCriteria(CriteriaType.UseGameobject, go.Entry);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CastSpell, Processing = PacketProcessing.ThreadSafe)]
	void HandleCastSpell(CastSpell cast)
	{
		// ignore for remote control state (for player case)
		var mover = Player.UnitBeingMoved;

		if (mover != Player && mover.IsTypeId(TypeId.Player))
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(cast.Cast.SpellID, mover.Map.DifficultyID);

		if (spellInfo == null)
		{
			Log.outError(LogFilter.Network, "WORLD: unknown spell id {0}", cast.Cast.SpellID);

			return;
		}

		if (spellInfo.IsPassive)
			return;

		var caster = mover;

		if (caster.IsTypeId(TypeId.Unit) && !caster.AsCreature.HasSpell(spellInfo.Id))
		{
			// If the vehicle creature does not have the spell but it allows the passenger to cast own spells
			// change caster to player and let him cast
			if (!Player.IsOnVehicle(caster) || spellInfo.CheckVehicle(Player) != SpellCastResult.SpellCastOk)
				return;

			caster = Player;
		}

		var triggerFlag = TriggerCastFlags.None;

		// client provided targets
		SpellCastTargets targets = new(caster, cast.Cast);

		// check known spell or raid marker spell (which not requires player to know it)
		if (caster.IsTypeId(TypeId.Player) && !caster.AsPlayer.HasActiveSpell(spellInfo.Id) && !spellInfo.HasEffect(SpellEffectName.ChangeRaidMarker) && !spellInfo.HasAttribute(SpellAttr8.RaidMarker))
		{
			var allow = false;


			// allow casting of unknown spells for special lock cases
			var go = targets.GOTarget;

			if (go != null)
				if (go.GetSpellForLock(caster.AsPlayer) == spellInfo)
					allow = true;

			// allow casting of spells triggered by clientside periodic trigger auras
			if (caster.HasAuraTypeWithTriggerSpell(AuraType.PeriodicTriggerSpellFromClient, spellInfo.Id))
			{
				allow = true;
				triggerFlag = TriggerCastFlags.FullMask;
			}

			if (!allow)
				return;
		}

		// Check possible spell cast overrides
		spellInfo = caster.GetCastSpellInfo(spellInfo);

		// can't use our own spells when we're in possession of another unit,
		if (Player.IsPossessing)
			return;

		// Client is resending autoshot cast opcode when other spell is cast during shoot rotation
		// Skip it to prevent "interrupt" message
		// Also check targets! target may have changed and we need to interrupt current spell
		if (spellInfo.IsAutoRepeatRangedSpell)
		{
			var autoRepeatSpell = caster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat);

			if (autoRepeatSpell != null)
				if (autoRepeatSpell.SpellInfo == spellInfo && autoRepeatSpell.Targets.UnitTargetGUID == targets.UnitTargetGUID)
					return;
		}

		// auto-selection buff level base at target level (in spellInfo)
		if (targets.UnitTarget != null)
		{
			var actualSpellInfo = spellInfo.GetAuraRankForLevel(targets.UnitTarget.GetLevelForTarget(caster));

			// if rank not found then function return NULL but in explicit cast case original spell can be casted and later failed with appropriate error message
			if (actualSpellInfo != null)
				spellInfo = actualSpellInfo;
		}

		if (cast.Cast.MoveUpdate != null)
			HandleMovementOpcode(ClientOpcodes.MoveStop, cast.Cast.MoveUpdate);

		Spell spell = new(caster, spellInfo, triggerFlag);

		SpellPrepare spellPrepare = new();
		spellPrepare.ClientCastID = cast.Cast.CastID;
		spellPrepare.ServerCastID = spell.CastId;
		SendPacket(spellPrepare);

		spell.FromClient = true;
		spell.SpellMisc.Data0 = cast.Cast.Misc[0];
		spell.SpellMisc.Data1 = cast.Cast.Misc[1];
		spell.Prepare(targets);
	}

	[WorldPacketHandler(ClientOpcodes.CancelCast, Processing = PacketProcessing.ThreadSafe)]
	void HandleCancelCast(CancelCast packet)
	{
		if (Player.IsNonMeleeSpellCast(false))
			Player.InterruptNonMeleeSpells(false, packet.SpellID, false);
	}

	[WorldPacketHandler(ClientOpcodes.CancelAura, Processing = PacketProcessing.Inplace)]
	void HandleCancelAura(CancelAura cancelAura)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(cancelAura.SpellID, _player.Map.DifficultyID);

		if (spellInfo == null)
			return;

		// not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
		if (spellInfo.HasAttribute(SpellAttr0.NoAuraCancel))
			return;

		// channeled spell case (it currently casted then)
		if (spellInfo.IsChanneled)
		{
			var curSpell = Player.GetCurrentSpell(CurrentSpellTypes.Channeled);

			if (curSpell != null)
				if (curSpell.SpellInfo.Id == cancelAura.SpellID)
					Player.InterruptSpell(CurrentSpellTypes.Channeled);

			return;
		}

		// non channeled case:
		// don't allow remove non positive spells
		// don't allow cancelling passive auras (some of them are visible)
		if (!spellInfo.IsPositive || spellInfo.IsPassive)
			return;

		Player.RemoveOwnedAura(cancelAura.SpellID, cancelAura.CasterGUID, AuraRemoveMode.Cancel);
	}

	[WorldPacketHandler(ClientOpcodes.CancelGrowthAura, Processing = PacketProcessing.Inplace)]
	void HandleCancelGrowthAura(CancelGrowthAura cancelGrowthAura)
	{
		Player.RemoveAurasByType(AuraType.ModScale,
								aurApp =>
								{
									var spellInfo = aurApp.Base.SpellInfo;

									return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
								});
	}

	[WorldPacketHandler(ClientOpcodes.CancelMountAura, Processing = PacketProcessing.Inplace)]
	void HandleCancelMountAura(CancelMountAura packet)
	{
		Player.RemoveAurasByType(AuraType.Mounted,
								aurApp =>
								{
									var spellInfo = aurApp.Base.SpellInfo;

									return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
								});
	}

	[WorldPacketHandler(ClientOpcodes.PetCancelAura, Processing = PacketProcessing.Inplace)]
	void HandlePetCancelAura(PetCancelAura packet)
	{
		if (!Global.SpellMgr.HasSpellInfo(packet.SpellID, Difficulty.None))
		{
			Log.outError(LogFilter.Network, "WORLD: unknown PET spell id {0}", packet.SpellID);

			return;
		}

		var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_player, packet.PetGUID);

		if (pet == null)
		{
			Log.outError(LogFilter.Network, "HandlePetCancelAura: Attempt to cancel an aura for non-existant {0} by player '{1}'", packet.PetGUID.ToString(), Player.GetName());

			return;
		}

		if (pet != Player.GetGuardianPet() && pet != Player.Charmed)
		{
			Log.outError(LogFilter.Network, "HandlePetCancelAura: {0} is not a pet of player '{1}'", packet.PetGUID.ToString(), Player.GetName());

			return;
		}

		if (!pet.IsAlive)
		{
			pet.SendPetActionFeedback(PetActionFeedback.Dead, 0);

			return;
		}

		pet.RemoveOwnedAura(packet.SpellID, ObjectGuid.Empty, AuraRemoveMode.Cancel);
	}

	[WorldPacketHandler(ClientOpcodes.CancelModSpeedNoControlAuras, Processing = PacketProcessing.Inplace)]
	void HandleCancelModSpeedNoControlAuras(CancelModSpeedNoControlAuras cancelModSpeedNoControlAuras)
	{
		var mover = _player.UnitBeingMoved;

		if (mover == null || mover.GUID != cancelModSpeedNoControlAuras.TargetGUID)
			return;

		_player.RemoveAurasByType(AuraType.ModSpeedNoControl,
								aurApp =>
								{
									var spellInfo = aurApp.Base.SpellInfo;

									return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive && !spellInfo.IsPassive;
								});
	}

	[WorldPacketHandler(ClientOpcodes.CancelAutoRepeatSpell, Processing = PacketProcessing.Inplace)]
	void HandleCancelAutoRepeatSpell(CancelAutoRepeatSpell packet)
	{
		//may be better send SMSG_CANCEL_AUTO_REPEAT?
		//cancel and prepare for deleting
		_player.InterruptSpell(CurrentSpellTypes.AutoRepeat);
	}

	[WorldPacketHandler(ClientOpcodes.CancelChannelling, Processing = PacketProcessing.Inplace)]
	void HandleCancelChanneling(CancelChannelling cancelChanneling)
	{
		// ignore for remote control state (for player case)
		var mover = _player.UnitBeingMoved;

		if (mover != _player && mover.IsTypeId(TypeId.Player))
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo((uint)cancelChanneling.ChannelSpell, mover.Map.DifficultyID);

		if (spellInfo == null)
			return;

		// not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
		if (spellInfo.HasAttribute(SpellAttr0.NoAuraCancel))
			return;

		var spell = mover.GetCurrentSpell(CurrentSpellTypes.Channeled);

		if (spell == null || spell.SpellInfo.Id != spellInfo.Id)
			return;

		mover.InterruptSpell(CurrentSpellTypes.Channeled);
	}

	[WorldPacketHandler(ClientOpcodes.TotemDestroyed, Processing = PacketProcessing.Inplace)]
	void HandleTotemDestroyed(TotemDestroyed totemDestroyed)
	{
		// ignore for remote control state
		if (Player.UnitBeingMoved != Player)
			return;

		var slotId = totemDestroyed.Slot;
		slotId += (int)SummonSlot.Totem;

		if (slotId >= SharedConst.MaxTotemSlot)
			return;

		if (Player.SummonSlot[slotId].IsEmpty)
			return;

		var totem = ObjectAccessor.GetCreature(Player, _player.SummonSlot[slotId]);

		if (totem != null && totem.IsTotem) // && totem.GetGUID() == packet.TotemGUID)  Unknown why blizz doesnt send the guid when you right click it.
			totem.ToTotem().UnSummon();
	}

	[WorldPacketHandler(ClientOpcodes.SelfRes)]
	void HandleSelfRes(SelfRes selfRes)
	{
		List<uint> selfResSpells = _player.ActivePlayerData.SelfResSpells;

		if (!selfResSpells.Contains(selfRes.SpellId))
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(selfRes.SpellId, _player.Map.DifficultyID);

		if (spellInfo == null)
			return;

		if (_player.HasAuraType(AuraType.PreventResurrection) && !spellInfo.HasAttribute(SpellAttr7.BypassNoResurrectAura))
			return; // silent return, client should display error by itself and not send this opcode

		_player.CastSpell(_player, selfRes.SpellId, new CastSpellExtraArgs(_player.Map.DifficultyID));
		_player.RemoveSelfResSpell(selfRes.SpellId);
	}

	[WorldPacketHandler(ClientOpcodes.SpellClick, Processing = PacketProcessing.Inplace)]
	void HandleSpellClick(SpellClick packet)
	{
		// this will get something not in world. crash
		var unit = ObjectAccessor.GetCreatureOrPetOrVehicle(Player, packet.SpellClickUnitGuid);

		if (unit == null)
			return;

		// @todo Unit.SetCharmedBy: 28782 is not in world but 0 is trying to charm it! . crash
		if (!unit.IsInWorld)
			return;

		unit.HandleSpellClick(Player);
	}

	[WorldPacketHandler(ClientOpcodes.GetMirrorImageData)]
	void HandleMirrorImageDataRequest(GetMirrorImageData packet)
	{
		var guid = packet.UnitGUID;

		// Get unit for which data is needed by client
		var unit = Global.ObjAccessor.GetUnit(Player, guid);

		if (!unit)
			return;

		if (!unit.HasAuraType(AuraType.CloneCaster))
			return;

		// Get creator of the unit (SPELL_AURA_CLONE_CASTER does not stack)
		var creator = unit.GetAuraEffectsByType(AuraType.CloneCaster).FirstOrDefault().Caster;

		if (!creator)
			return;

		var player = creator.AsPlayer;

		if (player)
		{
			MirrorImageComponentedData mirrorImageComponentedData = new();
			mirrorImageComponentedData.UnitGUID = guid;
			mirrorImageComponentedData.DisplayID = (int)creator.DisplayId;
			mirrorImageComponentedData.RaceID = (byte)creator.Race;
			mirrorImageComponentedData.Gender = (byte)creator.Gender;
			mirrorImageComponentedData.ClassID = (byte)creator.Class;

			foreach (var customization in player.PlayerData.Customizations)
			{
				var chrCustomizationChoice = new ChrCustomizationChoice();
				chrCustomizationChoice.ChrCustomizationOptionID = customization.ChrCustomizationOptionID;
				chrCustomizationChoice.ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID;
				mirrorImageComponentedData.Customizations.Add(chrCustomizationChoice);
			}

			var guild = player.Guild;
			mirrorImageComponentedData.GuildGUID = (guild ? guild.GetGUID() : ObjectGuid.Empty);

			byte[] itemSlots =
			{
				EquipmentSlot.Head, EquipmentSlot.Shoulders, EquipmentSlot.Shirt, EquipmentSlot.Chest, EquipmentSlot.Waist, EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.Wrist, EquipmentSlot.Hands, EquipmentSlot.Tabard, EquipmentSlot.Cloak
			};

			// Display items in visible slots
			foreach (var slot in itemSlots)
			{
				uint itemDisplayId;
				var item = player.GetItemByPos(InventorySlots.Bag0, slot);

				if (item != null)
					itemDisplayId = item.GetDisplayId(player);
				else
					itemDisplayId = 0;

				mirrorImageComponentedData.ItemDisplayID.Add((int)itemDisplayId);
			}

			SendPacket(mirrorImageComponentedData);
		}
		else
		{
			MirrorImageCreatureData data = new();
			data.UnitGUID = guid;
			data.DisplayID = (int)creator.DisplayId;
			SendPacket(data);
		}
	}

	[WorldPacketHandler(ClientOpcodes.MissileTrajectoryCollision)]
	void HandleMissileTrajectoryCollision(MissileTrajectoryCollision packet)
	{
		var caster = Global.ObjAccessor.GetUnit(_player, packet.Target);

		if (caster == null)
			return;

		var spell = caster.FindCurrentSpellBySpellId(packet.SpellID);

		if (spell == null || !spell.Targets.HasDst)
			return;

		Position pos = spell.Targets.DstPos;
		pos.Relocate(packet.CollisionPos);
		spell.Targets.ModDst(pos);

		// we changed dest, recalculate flight time
		spell.RecalculateDelayMomentForDst();

		NotifyMissileTrajectoryCollision data = new();
		data.Caster = packet.Target;
		data.CastID = packet.CastID;
		data.CollisionPos = packet.CollisionPos;
		caster.SendMessageToSet(data, true);
	}

	[WorldPacketHandler(ClientOpcodes.UpdateMissileTrajectory)]
	void HandleUpdateMissileTrajectory(UpdateMissileTrajectory packet)
	{
		var caster = Global.ObjAccessor.GetUnit(Player, packet.Guid);
		var spell = caster ? caster.GetCurrentSpell(CurrentSpellTypes.Generic) : null;

		if (!spell || spell.SpellInfo.Id != packet.SpellID || spell.CastId != packet.CastID || !spell.Targets.HasDst || !spell.Targets.HasSrc)
			return;

		var pos = spell.Targets.SrcPos;
		pos.Relocate(packet.FirePos);
		spell.Targets.ModSrc(pos);

		pos = spell.Targets.DstPos;
		pos.Relocate(packet.ImpactPos);
		spell.Targets.ModDst(pos);

		spell.Targets.Pitch = packet.Pitch;
		spell.Targets.Speed = packet.Speed;

		if (packet.Status != null)
			Player.ValidateMovementInfo(packet.Status);
		/*public uint opcode;
			recvPacket >> opcode;
			recvPacket.SetOpcode(CMSG_MOVE_STOP); // always set to CMSG_MOVE_STOP in client SetOpcode
			//HandleMovementOpcodes(recvPacket);*/
	}

	[WorldPacketHandler(ClientOpcodes.RequestCategoryCooldowns, Processing = PacketProcessing.Inplace)]
	void HandleRequestCategoryCooldowns(RequestCategoryCooldowns requestCategoryCooldowns)
	{
		Player.SendSpellCategoryCooldowns();
	}

	[WorldPacketHandler(ClientOpcodes.KeyboundOverride, Processing = PacketProcessing.ThreadSafe)]
	void HandleKeyboundOverride(KeyboundOverride keyboundOverride)
	{
		var player = Player;

		if (!player.HasAuraTypeWithMiscvalue(AuraType.KeyboundOverride, keyboundOverride.OverrideID))
			return;

		var spellKeyboundOverride = CliDB.SpellKeyboundOverrideStorage.LookupByKey(keyboundOverride.OverrideID);

		if (spellKeyboundOverride == null)
			return;

		player.CastSpell(player, spellKeyboundOverride.Data);
	}

	[WorldPacketHandler(ClientOpcodes.SpellEmpowerRelease)]
	void HandleSpellEmpowerRelease(SpellEmpowerRelease packet)
	{
		Player.UpdateEmpowerState(EmpowerState.Canceled, packet.SpellID);
	}

	[WorldPacketHandler(ClientOpcodes.SpellEmpowerRestart)]
	void HandleSpellEmpowerRelestart(SpellEmpowerRelease packet)
	{
		Player.UpdateEmpowerState(EmpowerState.Empowering, packet.SpellID);
	}

	[WorldPacketHandler(ClientOpcodes.SetEmpowerMinHoldStagePercent)]
	void HandleSpellEmpowerMinHoldPct(SpellEmpowerMinHold packet)
	{
		Player.EmpoweredSpellMinHoldPct = packet.HoldPct;
	}
}