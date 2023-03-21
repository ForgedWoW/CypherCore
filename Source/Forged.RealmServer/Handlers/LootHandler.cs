// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Loots;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void DoLootRelease(Loot loot)
	{
		var lguid = loot.GetOwnerGUID();
		var player = Player;

		if (player.GetLootGUID() == lguid)
			player.SetLootGUID(ObjectGuid.Empty);

		//Player is not looking at loot list, he doesn't need to see updates on the loot list
		loot.RemoveLooter(player.GUID);
		player.SendLootRelease(lguid);
		player.GetAELootView().Remove(loot.GetGUID());

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
			if (loot.loot_type == LootType.Prospecting || loot.loot_type == LootType.Milling)
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
				if (player.GUID == loot.roundRobinPlayer)
				{
					loot.roundRobinPlayer.Clear();
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

	[WorldPacketHandler(ClientOpcodes.MasterLootItem)]
	void HandleLootMasterGive(MasterLootItem masterLootItem)
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
				Log.outInfo(LogFilter.Cheat, $"MasterLootItem: Player {Player.GetName()} tried to give an item to ineligible player {target.GetName()} !");

				return;
			}

			if (!loot.HasAllowedLooter(masterLootItem.Target))
			{
				_player.SendLootError(req.Object, loot.GetOwnerGUID(), LootError.MasterOther);

				return;
			}

			if (req.LootListID >= loot.items.Count)
			{
				Log.outDebug(LogFilter.Loot, $"MasterLootItem: Player {Player.GetName()} might be using a hack! (slot {req.LootListID}, size {loot.items.Count})");

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
			target.SendNewItem(resultValue.item, resultValue.count, false, false, true);
			target.UpdateCriteria(CriteriaType.LootItem, resultValue.item.Entry, resultValue.count);
			target.UpdateCriteria(CriteriaType.GetLootByType, resultValue.item.Entry, resultValue.count, (ulong)resultValue.lootType);
			target.UpdateCriteria(CriteriaType.LootAnyItem, resultValue.item.Entry, resultValue.count);
		}
	}

	class AELootCreatureCheck : ICheck<Creature>
	{
		public static readonly float LootDistance = 30.0f;

		readonly Player _looter;
		readonly ObjectGuid _mainLootTarget;

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

		bool IsValidAELootTarget(Creature creature)
		{
			if (creature.GUID == _mainLootTarget)
				return false;

			return IsValidLootTarget(creature);
		}
	}
}