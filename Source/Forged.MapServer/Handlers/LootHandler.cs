// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Loot;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;
using Forged.MapServer.LootManagement;

namespace Forged.MapServer.Handlers;

public class LootHandler : IWorldSessionHandler
{
	public void DoLootRelease(Forged.MapServer.LootManagement.Loot loot)
	{
		var lguid = loot.GetOwnerGuid();
		var player = Player;

		if (player.GetLootGUID() == lguid)
			player.SetLootGUID(ObjectGuid.Empty);

		//Player is not looking at loot list, he doesn't need to see updates on the loot list
		loot.RemoveLooter(player.GUID);
		player.SendLootRelease(lguid);
		player.GetAELootView().Remove(loot.GetGuid());

		if (player.GetAELootView().Empty())
			player.RemoveUnitFlag(UnitFlags.Looting);

		if (!player.IsInWorld)
			return;

		if (lguid.IsGameObject)
		{
			var go = player.Map.GetGameObject(lguid);

			// not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
			if (!go || ((go.OwnerGUID != player.GUID && go.GoType != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player)))
				return;

			if (loot.IsLooted() || go.GoType == GameObjectTypes.FishingNode || go.GoType == GameObjectTypes.FishingHole)
			{
				if (go.GoType == GameObjectTypes.FishingNode)
				{
					go.SetLootState(LootState.JustDeactivated);
				}
				else if (go.GoType == GameObjectTypes.FishingHole)
				{
					// The fishing hole used once more
					go.AddUse(); // if the max usage is reached, will be despawned in next tick

					if (go.UseCount >= go.GoValue.FishingHole.MaxOpens)
						go.SetLootState(LootState.JustDeactivated);
					else
						go.SetLootState(LootState.Ready);
				}
				else if (go.GoType != GameObjectTypes.GatheringNode && go.IsFullyLooted)
				{
					go.SetLootState(LootState.JustDeactivated);
				}

				go.OnLootRelease(player);
			}
			else
			{
				// not fully looted object
				go.SetLootState(LootState.Activated, player);
			}
		}
		else if (lguid.IsCorpse) // ONLY remove insignia at BG
		{
			var corpse = ObjectAccessor.GetCorpse(player, lguid);

			if (!corpse || !corpse.IsWithinDistInMap(player, SharedConst.InteractionDistance))
				return;

			if (loot.IsLooted())
			{
				corpse.Loot = null;
				corpse.RemoveCorpseDynamicFlag(CorpseDynFlags.Lootable);
			}
		}
		else if (lguid.IsItem)
		{
			var pItem = player.GetItemByGuid(lguid);

			if (!pItem)
				return;

			var proto = pItem.Template;

			// destroy only 5 items from stack in case prospecting and milling
			if (loot.LootType == LootType.Prospecting || loot.LootType == LootType.Milling)
			{
				pItem.LootGenerated = false;
				pItem.Loot = null;

				var count = pItem.Count;

				// >=5 checked in spell code, but will work for cheating cases also with removing from another stacks.
				if (count > 5)
					count = 5;

				player.DestroyItemCount(pItem, ref count, true);
			}
			else
			{
				// Only delete item if no loot or money (unlooted loot is saved to db) or if it isn't an openable item
				if (loot.IsLooted() || !proto.HasFlag(ItemFlags.HasLoot))
					player.DestroyItem(pItem.BagSlot, pItem.Slot, true);
			}

			return; // item can be looted only single player
		}
		else
		{
			var creature = player.Map.GetCreature(lguid);

			if (creature == null)
				return;

			if (loot.IsLooted())
			{
				if (creature.IsFullyLooted)
				{
					creature.RemoveDynamicFlag(UnitDynFlags.Lootable);

					// skip pickpocketing loot for speed, skinning timer reduction is no-op in fact
					if (!creature.IsAlive)
						creature.AllLootRemovedFromCorpse();
				}
			}
			else
			{
				// if the round robin player release, reset it.
				if (player.GUID == loot.RoundRobinPlayer)
				{
					loot.RoundRobinPlayer.Clear();
					loot.NotifyLootList(creature.Map);
				}
			}

			// force dynflag update to update looter and lootable info
			creature.Values.ModifyValue(creature.ObjectData).ModifyValue(creature.ObjectData.DynamicFlags);
			creature.ForceUpdateFieldChange();
		}
	}

	public void DoLootReleaseAll()
	{
		var lootView = _player.GetAELootView();

		foreach (var (_, loot) in lootView)
			DoLootRelease(loot);
	}

	[WorldPacketHandler(ClientOpcodes.LootItem)]
    private void HandleAutostoreLootItem(LootItemPkt packet)
	{
		var player = Player;
		var aeResult = player.GetAELootView().Count > 1 ? new AELootResult() : null;

		// @todo Implement looting by LootObject guid
		foreach (var req in packet.Loot)
		{
			var loot = player.GetAELootView().LookupByKey(req.Object);

			if (loot == null)
			{
				player.SendLootRelease(ObjectGuid.Empty);

				continue;
			}

			var lguid = loot.GetOwnerGUID();

			if (lguid.IsGameObject)
			{
				var go = player.Map.GetGameObject(lguid);

				// not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
				if (!go || ((go.OwnerGUID != player.GUID && go.GoType != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player)))
				{
					player.SendLootRelease(lguid);

					continue;
				}
			}
			else if (lguid.IsCreatureOrVehicle)
			{
				var creature = player.Map.GetCreature(lguid);

				if (creature == null)
				{
					player.SendLootError(req.Object, lguid, LootError.NoLoot);

					continue;
				}

				if (!creature.IsWithinDistInMap(player, AELootCreatureCheck.LootDistance))
				{
					player.SendLootError(req.Object, lguid, LootError.TooFar);

					continue;
				}
			}

			player.StoreLootItem(lguid, req.LootListID, loot, aeResult);

			// If player is removing the last LootItem, delete the empty container.
			if (loot.IsLooted() && lguid.IsItem)
				player.Session.DoLootRelease(loot);
		}

		if (aeResult != null)
			foreach (var resultValue in aeResult.GetByOrder())
			{
				player.SendNewItem(resultValue.Item, resultValue.Count, false, false, true, resultValue.DungeonEncounterId);
				player.UpdateCriteria(CriteriaType.LootItem, resultValue.Item.Entry, resultValue.Count);
				player.UpdateCriteria(CriteriaType.GetLootByType, resultValue.Item.Entry, resultValue.Count, (ulong)resultValue.LootType);
				player.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.Item.Entry, resultValue.Count);
			}

		Unit.ProcSkillsAndAuras(player, null, new ProcFlagsInit(ProcFlags.Looted), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
	}

	[WorldPacketHandler(ClientOpcodes.LootMoney)]
    private void HandleLootMoney(LootMoney lootMoney)
	{
		var player = Player;

		foreach (var lootView in player.GetAELootView())
		{
			var loot = lootView.Value;
			var guid = loot.GetOwnerGUID();
			var shareMoney = loot.loot_type == LootType.Corpse;

			loot.NotifyMoneyRemoved(player.Map);

			if (shareMoney && player.Group != null) //item, pickpocket and players can be looted only single player
			{
				var group = player.Group;

				List<Player> playersNear = new();

				for (var refe = group.FirstMember; refe != null; refe = refe.Next())
				{
					var member = refe.Source;

					if (!member)
						continue;

					if (!loot.HasAllowedLooter(member.GUID))
						continue;

					if (player.IsAtGroupRewardDistance(member))
						playersNear.Add(member);
				}

				var goldPerPlayer = (ulong)(loot.gold / playersNear.Count);

				foreach (var pl in playersNear)
				{
					var goldMod = MathFunctions.CalculatePct(goldPerPlayer, pl.GetTotalAuraModifierByMiscValue(AuraType.ModMoneyGain, 1));

					pl.ModifyMoney((long)(goldPerPlayer + goldMod));
					pl.UpdateCriteria(CriteriaType.MoneyLootedFromCreatures, goldPerPlayer);

					LootMoneyNotify packet = new();
					packet.Money = goldPerPlayer;
					packet.MoneyMod = (ulong)goldMod;
					packet.SoleLooter = playersNear.Count <= 1 ? true : false;
					pl.SendPacket(packet);
				}
			}
			else
			{
				var goldMod = MathFunctions.CalculatePct((uint)loot.gold, (double)player.GetTotalAuraModifierByMiscValue(AuraType.ModMoneyGain, 1));

				player.ModifyMoney((long)(loot.gold + goldMod));
				player.UpdateCriteria(CriteriaType.MoneyLootedFromCreatures, loot.gold);

				LootMoneyNotify packet = new();
				packet.Money = loot.gold;
				packet.MoneyMod = (ulong)goldMod;
				packet.SoleLooter = true; // "You loot..."
				SendPacket(packet);
			}

			loot.gold = 0;

			// Delete the money loot record from the DB
			if (loot.loot_type == LootType.Item)
				Global.LootItemStorage.RemoveStoredMoneyForContainer(guid.Counter);

			// Delete container if empty
			if (loot.IsLooted() && guid.IsItem)
				player.Session.DoLootRelease(loot);
		}
	}

	[WorldPacketHandler(ClientOpcodes.LootUnit)]
    private void HandleLoot(LootUnit packet)
	{
		// Check possible cheat
		if (!Player.IsAlive || !packet.Unit.IsCreatureOrVehicle)
			return;

		var lootTarget = ObjectAccessor.GetCreature(Player, packet.Unit);

		if (!lootTarget)
			return;

		AELootCreatureCheck check = new(_player, packet.Unit);

		if (!check.IsValidLootTarget(lootTarget))
			return;

		// interrupt cast
		if (Player.IsNonMeleeSpellCast(false))
			Player.InterruptNonMeleeSpells(false);

		Player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Looting);

		List<Creature> corpses = new();
		CreatureListSearcher searcher = new(_player, corpses, check, GridType.Grid);
		Cell.VisitGrid(_player, searcher, AELootCreatureCheck.LootDistance);

		if (!corpses.Empty())
			SendPacket(new AELootTargets((uint)corpses.Count + 1));

		Player.SendLoot(lootTarget.GetLootForPlayer(Player));

		if (!corpses.Empty())
		{
			// main target
			SendPacket(new AELootTargetsAck());

			foreach (var creature in corpses)
			{
				Player.SendLoot(creature.GetLootForPlayer(Player), true);
				SendPacket(new AELootTargetsAck());
			}
		}
	}

	[WorldPacketHandler(ClientOpcodes.LootRelease)]
    private void HandleLootRelease(LootRelease packet)
	{
		// cheaters can modify lguid to prevent correct apply loot release code and re-loot
		// use internal stored guid
		var loot = Player.GetLootByWorldObjectGUID(packet.Unit);

		if (loot != null)
			DoLootRelease(loot);
	}

	[WorldPacketHandler(ClientOpcodes.MasterLootItem)]
    private void HandleLootMasterGive(MasterLootItem masterLootItem)
	{
		AELootResult aeResult = new();

		if (Player.Group == null || Player.Group.LooterGuid != Player.GUID)
		{
			Player.SendLootError(ObjectGuid.Empty, ObjectGuid.Empty, LootError.DidntKill);

			return;
		}

		// player on other map
		var target = Global.ObjAccessor.GetPlayer(_player, masterLootItem.Target);

		if (!target)
		{
			Player.SendLootError(ObjectGuid.Empty, ObjectGuid.Empty, LootError.PlayerNotFound);

			return;
		}

		foreach (var req in masterLootItem.Loot)
		{
			var loot = _player.GetAELootView().LookupByKey(req.Object);

			if (loot == null || loot.GetLootMethod() != LootMethod.MasterLoot)
				return;

			if (!_player.IsInRaidWith(target) || !_player.IsInMap(target))
			{
				_player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);
				Log.Logger.Information($"MasterLootItem: Player {Player.GetName()} tried to give an item to ineligible player {target.GetName()} !");

				return;
			}

			if (!loot.HasAllowedLooter(masterLootItem.Target))
			{
				_player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);

				return;
			}

			if (req.LootListID >= loot.items.Count)
			{
				Log.Logger.Debug($"MasterLootItem: Player {Player.GetName()} might be using a hack! (slot {req.LootListID}, size {loot.items.Count})");

				return;
			}

			var item = loot.items[req.LootListID];

			List<ItemPosCount> dest = new();
			var msg = target.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.itemid, item.count);

			if (!item.HasAllowedLooter(target.GUID))
				msg = InventoryResult.CantEquipEver;

			if (msg != InventoryResult.Ok)
			{
				if (msg == InventoryResult.ItemMaxCount)
					_player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterUniqueItem);
				else if (msg == InventoryResult.InvFull)
					_player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterInvFull);
				else
					_player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);

				return;
			}

			// now move item from loot to target inventory
			var newitem = target.StoreNewItem(dest, item.itemid, true, item.randomBonusListId, item.GetAllowedLooters(), item.context, item.BonusListIDs);
			aeResult.Add(newitem, item.count, loot.loot_type, loot.GetDungeonEncounterId());

			// mark as looted
			item.count = 0;
			item.is_looted = true;

			loot.NotifyItemRemoved(req.LootListID, Player.Map);
			--loot.unlootedCount;
		}

		foreach (var resultValue in aeResult.GetByOrder())
		{
			target.SendNewItem(resultValue.Item, resultValue.Count, false, false, true);
			target.UpdateCriteria(CriteriaType.LootItem, resultValue.Item.Entry, resultValue.Count);
			target.UpdateCriteria(CriteriaType.GetLootByType, resultValue.Item.Entry, resultValue.Count, (ulong)resultValue.LootType);
			target.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.Item.Entry, resultValue.Count);
		}
	}

	[WorldPacketHandler(ClientOpcodes.LootRoll)]
    private void HandleLootRoll(LootRollPacket packet)
	{
		var lootRoll = Player.GetLootRoll(packet.LootObj, packet.LootListID);

		if (lootRoll == null)
			return;

		lootRoll.PlayerVote(Player, packet.RollType);
	}

	[WorldPacketHandler(ClientOpcodes.SetLootSpecialization)]
    private void HandleSetLootSpecialization(SetLootSpecialization packet)
	{
		if (packet.SpecID != 0)
		{
			var chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(packet.SpecID);

			if (chrSpec != null)
				if (chrSpec.ClassID == (uint)Player.Class)
					Player.SetLootSpecId(packet.SpecID);
		}
		else
		{
			Player.SetLootSpecId(0);
		}
	}

    private class AELootCreatureCheck : ICheck<Creature>
	{
		public static readonly float LootDistance = 30.0f;

        private readonly Player _looter;
        private readonly ObjectGuid _mainLootTarget;

		public AELootCreatureCheck(Player looter, ObjectGuid mainLootTarget)
		{
			_looter = looter;
			_mainLootTarget = mainLootTarget;
		}

		public bool Invoke(Creature creature)
		{
			return IsValidAELootTarget(creature);
		}

		public bool IsValidLootTarget(Creature creature)
		{
			if (creature.IsAlive)
				return false;

			if (!_looter.IsWithinDist(creature, LootDistance))
				return false;

			return _looter.IsAllowedToLoot(creature);
		}

        private bool IsValidAELootTarget(Creature creature)
		{
			if (creature.GUID == _mainLootTarget)
				return false;

			return IsValidLootTarget(creature);
		}
	}
}