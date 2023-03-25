// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Phasing;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Server;
using Framework.Constants;
using Transport = Forged.MapServer.Entities.Transport;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("debug")]
class DebugCommands
{
	[Command("anim", RBACPermissions.CommandDebug)]
	static bool HandleDebugAnimCommand(CommandHandler handler, Emote emote)
	{
		var unit = handler.SelectedUnit;

		if (unit)
			unit.HandleEmoteCommand(emote);

		handler.SendSysMessage($"Playing emote {emote}");

		return true;
	}

	[Command("areatriggers", RBACPermissions.CommandDebug)]
	static bool HandleDebugAreaTriggersCommand(CommandHandler handler)
	{
		var player = handler.Player;

		if (!player.IsDebugAreaTriggers)
		{
			handler.SendSysMessage(CypherStrings.DebugAreatriggerOn);
			player.IsDebugAreaTriggers = true;
		}
		else
		{
			handler.SendSysMessage(CypherStrings.DebugAreatriggerOff);
			player.IsDebugAreaTriggers = false;
		}

		return true;
	}

	[Command("arena", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugArenaCommand(CommandHandler handler)
	{
		Global.BattlegroundMgr.ToggleArenaTesting();

		return true;
	}

	[Command("bg", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugBattlegroundCommand(CommandHandler handler)
	{
		Global.BattlegroundMgr.ToggleTesting();

		return true;
	}

	[Command("boundary", RBACPermissions.CommandDebug)]
	static bool HandleDebugBoundaryCommand(CommandHandler handler, string fill, uint durationArg)
	{
		var player = handler.Player;

		if (!player)
			return false;

		var target = handler.SelectedCreature;

		if (!target || !target.IsAIEnabled)
			return false;

		var duration = durationArg != 0 ? TimeSpan.FromSeconds(durationArg) : TimeSpan.Zero;

		if (duration <= TimeSpan.Zero || duration >= TimeSpan.FromMinutes(30)) // arbitrary upper limit
			duration = TimeSpan.FromMinutes(3);

		var errMsg = target.AI.VisualizeBoundary(duration, player, fill == "fill");

		if (errMsg > 0)
			handler.SendSysMessage(errMsg);

		return true;
	}

	[Command("combat", RBACPermissions.CommandDebug)]
	static bool HandleDebugCombatListCommand(CommandHandler handler)
	{
		var target = handler.SelectedUnit;

		if (!target)
			target = handler.Player;

		handler.SendSysMessage($"Combat refs: (Combat state: {target.IsInCombat} | Manager state: {target.GetCombatManager().HasCombat})");

		foreach (var refe in target.GetCombatManager().PvPCombatRefs)
		{
			var unit = refe.Value.GetOther(target);
			handler.SendSysMessage($"[PvP] {unit.GetName()} (SpawnID {(unit.IsCreature ? unit.AsCreature.SpawnId : 0)})");
		}

		foreach (var refe in target.GetCombatManager().PvECombatRefs)
		{
			var unit = refe.Value.GetOther(target);
			handler.SendSysMessage($"[PvE] {unit.GetName()} (SpawnID {(unit.IsCreature ? unit.AsCreature.SpawnId : 0)})");
		}

		return true;
	}

	[Command("conversation", RBACPermissions.CommandDebug)]
	static bool HandleDebugConversationCommand(CommandHandler handler, uint conversationEntry)
	{
		var target = handler.SelectedPlayerOrSelf;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		return Conversation.CreateConversation(conversationEntry, target, target.Location, target.GUID) != null;
	}

	[Command("dummy", RBACPermissions.CommandDebug)]
	static bool HandleDebugDummyCommand(CommandHandler handler)
	{
		handler.SendSysMessage("This command does nothing right now. Edit your local core (DebugCommands.cs) to make it do whatever you need for testing.");

		return true;
	}

	[Command("entervehicle", RBACPermissions.CommandDebug)]
	static bool HandleDebugEnterVehicleCommand(CommandHandler handler, uint entry, sbyte seatId = -1)
	{
		var target = handler.SelectedUnit;

		if (!target || !target.IsVehicle)
			return false;

		if (entry == 0)
		{
			handler.Player.EnterVehicle(target, seatId);
		}
		else
		{
			var check = new AllCreaturesOfEntryInRange(handler.Player, entry, 20.0f);
			var searcher = new CreatureSearcher(handler.Player, check, GridType.All);
			Cell.VisitGrid(handler.Player, searcher, 30.0f);
			var passenger = searcher.GetTarget();

			if (!passenger || passenger == target)
				return false;

			passenger.EnterVehicle(target, seatId);
		}

		handler.SendSysMessage("Unit {0} entered vehicle {1}", entry, seatId);

		return true;
	}

	[Command("getitemstate", RBACPermissions.CommandDebug)]
	static bool HandleDebugGetItemStateCommand(CommandHandler handler, string itemState)
	{
		var state = ItemUpdateState.Unchanged;
		var listQueue = false;
		var checkAll = false;

		if (itemState == "unchanged")
			state = ItemUpdateState.Unchanged;
		else if (itemState == "changed")
			state = ItemUpdateState.Changed;
		else if (itemState == "new")
			state = ItemUpdateState.New;
		else if (itemState == "removed")
			state = ItemUpdateState.Removed;
		else if (itemState == "queue")
			listQueue = true;
		else if (itemState == "check_all")
			checkAll = true;
		else
			return false;

		var player = handler.SelectedPlayer;

		if (!player)
			player = handler.Player;

		if (!listQueue && !checkAll)
		{
			itemState = "The player has the following " + itemState + " items: ";
			handler.SendSysMessage(itemState);

			for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
			{
				if (i is >= InventorySlots.BuyBackStart and < InventorySlots.BuyBackEnd)
					continue;

				var item = player.GetItemByPos(InventorySlots.Bag0, i);

				if (item)
				{
					var bag = item.AsBag;

					if (bag)
						for (byte j = 0; j < bag.GetBagSize(); ++j)
						{
							var item2 = bag.GetItemByPos(j);

							if (item2)
								if (item2.State == state)
									handler.SendSysMessage("bag: 255 slot: {0} guid: {1} owner: {2}", item2.Slot, item2.GUID.ToString(), item2.OwnerGUID.ToString());
						}
					else if (item.State == state)
						handler.SendSysMessage("bag: 255 slot: {0} guid: {1} owner: {2}", item.Slot, item.GUID.ToString(), item.OwnerGUID.ToString());
				}
			}
		}

		if (listQueue)
		{
			var updateQueue = player.ItemUpdateQueue;

			for (var i = 0; i < updateQueue.Count; ++i)
			{
				var item = updateQueue[i];

				if (!item)
					continue;

				var container = item.Container;
				var bagSlot = container ? container.Slot : InventorySlots.Bag0;

				var st = "";

				switch (item.State)
				{
					case ItemUpdateState.Unchanged:
						st = "unchanged";

						break;
					case ItemUpdateState.Changed:
						st = "changed";

						break;
					case ItemUpdateState.New:
						st = "new";

						break;
					case ItemUpdateState.Removed:
						st = "removed";

						break;
				}

				handler.SendSysMessage("bag: {0} slot: {1} guid: {2} - state: {3}", bagSlot, item.Slot, item.GUID.ToString(), st);
			}

			if (updateQueue.Empty())
				handler.SendSysMessage("The player's updatequeue is empty");
		}

		if (checkAll)
		{
			var error = false;
			var updateQueue = player.ItemUpdateQueue;

			for (byte i = (int)PlayerSlots.Start; i < (int)PlayerSlots.End; ++i)
			{
				if (i is >= InventorySlots.BuyBackStart and < InventorySlots.BuyBackEnd)
					continue;

				var item = player.GetItemByPos(InventorySlots.Bag0, i);

				if (!item)
					continue;

				if (item.Slot != i)
				{
					handler.SendSysMessage("Item with slot {0} and guid {1} has an incorrect slot value: {2}", i, item.GUID.ToString(), item.Slot);
					error = true;

					continue;
				}

				if (item.OwnerGUID != player.GUID)
				{
					handler.SendSysMessage("The item with slot {0} and itemguid {1} does have non-matching owner guid ({2}) and player guid ({3}) !", item.Slot, item.GUID.ToString(), item.OwnerGUID.ToString(), player.GUID.ToString());
					error = true;

					continue;
				}

				var container = item.Container;

				if (container)
				{
					handler.SendSysMessage("The item with slot {0} and guid {1} has a container (slot: {2}, guid: {3}) but shouldn't!", item.Slot, item.GUID.ToString(), container.Slot, container.GUID.ToString());
					error = true;

					continue;
				}

				if (item.IsInUpdateQueue)
				{
					var qp = (ushort)item.QueuePos;

					if (qp > updateQueue.Count)
					{
						handler.SendSysMessage("The item with slot {0} and guid {1} has its queuepos ({2}) larger than the update queue size! ", item.Slot, item.GUID.ToString(), qp);
						error = true;

						continue;
					}

					if (updateQueue[qp] == null)
					{
						handler.SendSysMessage("The item with slot {0} and guid {1} has its queuepos ({2}) pointing to NULL in the queue!", item.Slot, item.GUID.ToString(), qp);
						error = true;

						continue;
					}

					if (updateQueue[qp] != item)
					{
						handler.SendSysMessage("The item with slot {0} and guid {1} has a queuepos ({2}) that points to another item in the queue (bag: {3}, slot: {4}, guid: {5})", item.Slot, item.GUID.ToString(), qp, updateQueue[qp].BagSlot, updateQueue[qp].Slot, updateQueue[qp].GUID.ToString());
						error = true;

						continue;
					}
				}
				else if (item.State != ItemUpdateState.Unchanged)
				{
					handler.SendSysMessage("The item with slot {0} and guid {1} is not in queue but should be (state: {2})!", item.Slot, item.GUID.ToString(), item.State);
					error = true;

					continue;
				}

				var bag = item.AsBag;

				if (bag)
					for (byte j = 0; j < bag.GetBagSize(); ++j)
					{
						var item2 = bag.GetItemByPos(j);

						if (!item2)
							continue;

						if (item2.Slot != j)
						{
							handler.SendSysMessage("The item in bag {0} and slot {1} (guid: {2}) has an incorrect slot value: {3}", bag.Slot, j, item2.GUID.ToString(), item2.Slot);
							error = true;

							continue;
						}

						if (item2.OwnerGUID != player.GUID)
						{
							handler.SendSysMessage("The item in bag {0} at slot {1} and with itemguid {2}, the owner's guid ({3}) and the player's guid ({4}) don't match!", bag.Slot, item2.Slot, item2.GUID.ToString(), item2.OwnerGUID.ToString(), player.GUID.ToString());
							error = true;

							continue;
						}

						var container1 = item2.Container;

						if (!container1)
						{
							handler.SendSysMessage("The item in bag {0} at slot {1} with guid {2} has no container!", bag.Slot, item2.Slot, item2.GUID.ToString());
							error = true;

							continue;
						}

						if (container1 != bag)
						{
							handler.SendSysMessage("The item in bag {0} at slot {1} with guid {2} has a different container(slot {3} guid {4})!", bag.Slot, item2.Slot, item2.GUID.ToString(), container1.Slot, container1.GUID.ToString());
							error = true;

							continue;
						}

						if (item2.IsInUpdateQueue)
						{
							var qp = (ushort)item2.QueuePos;

							if (qp > updateQueue.Count)
							{
								handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} has a queuepos ({3}) larger than the update queue size! ", bag.Slot, item2.Slot, item2.GUID.ToString(), qp);
								error = true;

								continue;
							}

							if (updateQueue[qp] == null)
							{
								handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} has a queuepos ({3}) that points to NULL in the queue!", bag.Slot, item2.Slot, item2.GUID.ToString(), qp);
								error = true;

								continue;
							}

							if (updateQueue[qp] != item2)
							{
								handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} has a queuepos ({3}) that points to another item in the queue (bag: {4}, slot: {5}, guid: {6})", bag.Slot, item2.Slot, item2.GUID.ToString(), qp, updateQueue[qp].BagSlot, updateQueue[qp].Slot, updateQueue[qp].GUID.ToString());
								error = true;

								continue;
							}
						}
						else if (item2.State != ItemUpdateState.Unchanged)
						{
							handler.SendSysMessage("The item in bag {0} at slot {1} having guid {2} is not in queue but should be (state: {3})!", bag.Slot, item2.Slot, item2.GUID.ToString(), item2.State);
							error = true;

							continue;
						}
					}
			}

			for (var i = 0; i < updateQueue.Count; ++i)
			{
				var item = updateQueue[i];

				if (!item)
					continue;

				if (item.OwnerGUID != player.GUID)
				{
					handler.SendSysMessage("queue({0}): For the item with guid {0}, the owner's guid ({1}) and the player's guid ({2}) don't match!", i, item.GUID.ToString(), item.OwnerGUID.ToString(), player.GUID.ToString());
					error = true;

					continue;
				}

				if (item.QueuePos != i)
				{
					handler.SendSysMessage("queue({0}): For the item with guid {1}, the queuepos doesn't match it's position in the queue!", i, item.GUID.ToString());
					error = true;

					continue;
				}

				if (item.State == ItemUpdateState.Removed)
					continue;

				var test = player.GetItemByPos(item.BagSlot, item.Slot);

				if (test == null)
				{
					handler.SendSysMessage("queue({0}): The bag({1}) and slot({2}) values for the item with guid {3} are incorrect, the player doesn't have any item at that position!", i, item.BagSlot, item.Slot, item.GUID.ToString());
					error = true;

					continue;
				}

				if (test != item)
				{
					handler.SendSysMessage("queue({0}): The bag({1}) and slot({2}) values for the item with guid {3} are incorrect, an item which guid is {4} is there instead!", i, item.BagSlot, item.Slot, item.GUID.ToString(), test.GUID.ToString());
					error = true;

					continue;
				}
			}

			if (!error)
				handler.SendSysMessage("All OK!");
		}

		return true;
	}

	[Command("guidlimits", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugGuidLimitsCommand(CommandHandler handler, uint mapId)
	{
		if (mapId != 0)
			Global.MapMgr.DoForAllMapsWithMapId(mapId, map => HandleDebugGuidLimitsMap(handler, map));
		else
			Global.MapMgr.DoForAllMaps(map => HandleDebugGuidLimitsMap(handler, map));

		handler.SendSysMessage($"Guid Warn Level: {GetDefaultValue("Respawn.GuidWarnLevel", 12000000)}");
		handler.SendSysMessage($"Guid Alert Level: {GetDefaultValue("Respawn.GuidAlertLevel", 16000000)}");

		return true;
	}

	[Command("instancespawn", RBACPermissions.CommandDebug)]
	static bool HandleDebugInstanceSpawns(CommandHandler handler, [VariantArg(typeof(uint), typeof(string))] object optArg)
	{
		var player = handler.Player;

		if (player == null)
			return false;

		var explain = false;
		uint groupID = 0;

		if (optArg is string && (optArg as string).Equals("explain", StringComparison.OrdinalIgnoreCase))
			explain = true;
		else
			groupID = (uint)optArg;

		if (groupID != 0 && Global.ObjectMgr.GetSpawnGroupData(groupID) == null)
		{
			handler.SendSysMessage($"There is no spawn group with ID {groupID}.");

			return false;
		}

		var map = player.Map;
		var mapName = map.MapName;
		var instance = player.InstanceScript;

		if (instance == null)
		{
			handler.SendSysMessage($"{mapName} has no instance script.");

			return false;
		}

		var spawnGroups = instance.GetInstanceSpawnGroups();

		if (spawnGroups.Empty())
		{
			handler.SendSysMessage($"{mapName}'s instance script does not manage any spawn groups.");

			return false;
		}

		MultiMap<uint, Tuple<bool, byte, byte>> store = new();

		foreach (var info in spawnGroups)
		{
			if (groupID != 0 && info.SpawnGroupId != groupID)
				continue;

			bool isSpawn;

			if (info.Flags.HasFlag(InstanceSpawnGroupFlags.BlockSpawn))
				isSpawn = false;
			else if (info.Flags.HasFlag(InstanceSpawnGroupFlags.ActivateSpawn))
				isSpawn = true;
			else
				continue;

			store.Add(info.SpawnGroupId, Tuple.Create(isSpawn, info.BossStateId, info.BossStates));
		}

		if (groupID != 0 && !store.ContainsKey(groupID))
		{
			handler.SendSysMessage($"{mapName}'s instance script does not manage group '{Global.ObjectMgr.GetSpawnGroupData(groupID).Name}'.");

			return false;
		}

		if (groupID == 0)
			handler.SendSysMessage($"Spawn groups managed by {mapName} ({map.Id}):");

		foreach (var key in store.Keys)
		{
			var groupData = Global.ObjectMgr.GetSpawnGroupData(key);

			if (groupData == null)
				continue;

			if (explain)
			{
				handler.SendSysMessage(" |-- '{}' ({})", groupData.Name, key);
				bool isBlocked = false, isSpawned = false;

				foreach (var tuple in store[key])
				{
					var isSpawn = tuple.Item1;
					var bossStateId = tuple.Item2;
					var actualState = instance.GetBossState(bossStateId);

					if ((tuple.Item3 & (1 << (int)actualState)) != 0)
					{
						if (isSpawn)
						{
							isSpawned = true;

							if (isBlocked)
								handler.SendSysMessage($" | |-- '{groupData.Name}' would be allowed to spawn by boss state {bossStateId} being {(EncounterState)actualState}, but this is overruled");
							else
								handler.SendSysMessage($" | |-- '{groupData.Name}' is allowed to spawn because boss state {bossStateId} is {(EncounterState)bossStateId}.");
						}
						else
						{
							isBlocked = true;
							handler.SendSysMessage($" | |-- '{groupData.Name}' is blocked from spawning because boss state {bossStateId} is {(EncounterState)bossStateId}.");
						}
					}
					else
					{
						handler.SendSysMessage($" | |-- '{groupData.Name}' could've been {(isSpawn ? "allowed to spawn" : "blocked from spawning")} if boss state {bossStateId} matched mask 0x{tuple.Item3:X2}; but it is {(EncounterState)actualState} . 0x{(1 << (int)actualState):X2}, which does not match.");
					}
				}

				if (isBlocked)
					handler.SendSysMessage($" | |=> '{groupData.Name}' is not active due to a blocking rule being matched");
				else if (isSpawned)
					handler.SendSysMessage($" | |=> '{groupData.Name}' is active due to a spawn rule being matched");
				else
					handler.SendSysMessage($" | |=> '{groupData.Name}' is not active due to none of its rules being matched");
			}
			else
			{
				handler.SendSysMessage($" - '{groupData.Name}' ({key}) is {(map.IsSpawnGroupActive(key) ? "" : "not ")}active");
			}
		}

		return true;
	}

	[Command("itemexpire", RBACPermissions.CommandDebug)]
	static bool HandleDebugItemExpireCommand(CommandHandler handler, ulong guid)
	{
		var item = handler.Player.GetItemByGuid(ObjectGuid.Create(HighGuid.Item, guid));

		if (!item)
			return false;

		handler.Player.DestroyItem(item.BagSlot, item.Slot, true);
		var itemTemplate = item.Template;
		Global.ScriptMgr.RunScriptRet<IItemOnExpire>(p => p.OnExpire(handler.Player, itemTemplate), itemTemplate.ScriptId);

		return true;
	}

	[Command("loadcells", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugLoadCellsCommand(CommandHandler handler, uint? mapId, uint? tileX, uint? tileY)
	{
		if (mapId.HasValue)
		{
			Global.MapMgr.DoForAllMapsWithMapId(mapId.Value, map => HandleDebugLoadCellsCommandHelper(handler, map, tileX, tileY));

			return true;
		}

		var player = handler.Player;

		if (player != null)
			// Fallback to player's map if no map has been specified
			return HandleDebugLoadCellsCommandHelper(handler, player.Map, tileX, tileY);

		return false;
	}

	static bool HandleDebugLoadCellsCommandHelper(CommandHandler handler, Map map, uint? tileX, uint? tileY)
	{
		if (!map)
			return false;

		// Load 1 single tile if specified, otherwise load the whole map
		if (tileX.HasValue && tileY.HasValue)
		{
			handler.SendSysMessage($"Loading cell (mapId: {map.Id} tile: {tileX}, {tileY}). Current GameObjects {map.ObjectsStore.Count(p => p.Value is GameObject)}, Creatures {map.ObjectsStore.Count(p => p.Value is Creature)}");

			// Some unit convertions to go from TileXY to GridXY to WorldXY
			var x = (((float)(64 - 1 - tileX.Value) - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
			var y = (((float)(64 - 1 - tileY.Value) - 0.5f - MapConst.CenterGridId) * MapConst.SizeofGrids) + (MapConst.CenterGridOffset * 2);
			map.LoadGrid(x, y);

			handler.SendSysMessage($"Cell loaded (mapId: {map.Id} tile: {tileX}, {tileY}) After load - GameObject {map.ObjectsStore.Count(p => p.Value is GameObject)}, Creatures {map.ObjectsStore.Count(p => p.Value is Creature)}");
		}
		else
		{
			handler.SendSysMessage($"Loading all cells (mapId: {map.Id}). Current GameObjects {map.ObjectsStore.Count(p => p.Value is GameObject)}, Creatures {map.ObjectsStore.Count(p => p.Value is Creature)}");

			map.LoadAllCells();

			handler.SendSysMessage($"Cells loaded (mapId: {map.Id}) After load - GameObject {map.ObjectsStore.Count(p => p.Value is GameObject)}, Creatures {map.ObjectsStore.Count(p => p.Value is Creature)}");
		}

		return true;
	}

	[Command("lootrecipient", RBACPermissions.CommandDebug)]
	static bool HandleDebugGetLootRecipientCommand(CommandHandler handler)
	{
		var target = handler.SelectedCreature;

		if (!target)
			return false;

		handler.SendSysMessage($"Loot recipients for creature {target.GetName()} ({target.GUID}, SpawnID {target.SpawnId}) are:");

		foreach (var tapperGuid in target.TapList)
		{
			var tapper = Global.ObjAccessor.GetPlayer(target, tapperGuid);
			handler.SendSysMessage($"* {(tapper != null ? tapper.GetName() : "offline")}");
		}

		return true;
	}

	[Command("los", RBACPermissions.CommandDebug)]
	static bool HandleDebugLoSCommand(CommandHandler handler)
	{
		var unit = handler.SelectedUnit;

		if (unit)
		{
			var player = handler.Player;
			handler.SendSysMessage($"Checking LoS {player.GetName()} . {unit.GetName()}:");
			handler.SendSysMessage($"    VMAP LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Vmap) ? "clear" : "obstructed")}");
			handler.SendSysMessage($"    GObj LoS: {(player.IsWithinLOSInMap(unit, LineOfSightChecks.Gobject) ? "clear" : "obstructed")}");
			handler.SendSysMessage($"{unit.GetName()} is {(player.IsWithinLOSInMap(unit) ? "" : "not ")}in line of sight of {player.GetName()}.");

			return true;
		}

		return false;
	}

	[Command("moveflags", RBACPermissions.CommandDebug)]
	static bool HandleDebugMoveflagsCommand(CommandHandler handler, uint? moveFlags, uint? moveFlagsExtra)
	{
		var target = handler.SelectedUnit;

		if (!target)
			target = handler.Player;

		if (!moveFlags.HasValue)
		{
			//! Display case
			handler.SendSysMessage(CypherStrings.MoveflagsGet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
		}
		else
		{
			// @fixme: port master's HandleDebugMoveflagsCommand; flags need different handling

			target.SetUnitMovementFlags((MovementFlag)moveFlags);

			if (moveFlagsExtra.HasValue)
				target.SetUnitMovementFlags2((MovementFlag2)moveFlagsExtra);

			if (!target.IsTypeId(TypeId.Player))
			{
				target.DestroyForNearbyPlayers(); // Force new SMSG_UPDATE_OBJECT:CreateObject
			}
			else
			{
				MoveUpdate moveUpdate = new()
				{
					Status = target.MovementInfo
				};

				target.SendMessageToSet(moveUpdate, true);
			}

			handler.SendSysMessage(CypherStrings.MoveflagsSet, target.GetUnitMovementFlags(), target.GetUnitMovementFlags2());
		}

		return true;
	}

	[Command("neargraveyard", RBACPermissions.CommandDebug)]
	static bool HandleDebugNearGraveyard(CommandHandler handler, string linked)
	{
		var player = handler.Player;
		WorldSafeLocsEntry nearestLoc = null;

		if (linked == "linked")
		{
			var bg = player.Battleground;

			if (bg)
			{
				nearestLoc = bg.GetClosestGraveYard(player);
			}
			else
			{
				var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(player.Map, player.Zone);

				if (bf != null)
					nearestLoc = bf.GetClosestGraveYard(player);
				else
					nearestLoc = Global.ObjectMgr.GetClosestGraveYard(player.Location, player.Team, player);
			}
		}
		else
		{
			var x = player.Location.X;
			var y = player.Location.Y;
			var z = player.Location.Z;
			var distNearest = float.MaxValue;

			foreach (var pair in Global.ObjectMgr.GetWorldSafeLocs())
			{
				var worldSafe = pair.Value;

				if (worldSafe.Loc.MapId == player.Location.MapId)
				{
					var dist = (worldSafe.Loc.X - x) * (worldSafe.Loc.X - x) + (worldSafe.Loc.Y - y) * (worldSafe.Loc.Y - y) + (worldSafe.Loc.Z - z) * (worldSafe.Loc.Z - z);

					if (dist < distNearest)
					{
						distNearest = dist;
						nearestLoc = worldSafe;
					}
				}
			}
		}

		if (nearestLoc != null)
			handler.SendSysMessage(CypherStrings.CommandNearGraveyard, nearestLoc.Id, nearestLoc.Loc.X, nearestLoc.Loc.Y, nearestLoc.Loc.Z);
		else
			handler.SendSysMessage(CypherStrings.CommandNearGraveyardNotfound);

		return true;
	}

	[Command("objectcount", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugObjectCountCommand(CommandHandler handler, uint? mapId)
	{
		void HandleDebugObjectCountMap(Map map)
		{
			handler.SendSysMessage($"Map Id: {map.Id} Name: '{map.MapName}' Instance Id: {map.InstanceId} Creatures: {map.ObjectsStore.OfType<Creature>().Count()} GameObjects: {map.ObjectsStore.OfType<GameObject>().Count()} SetActive Objects: {map.ActiveNonPlayersCount}");

			Dictionary<uint, uint> creatureIds = new();

			foreach (var p in map.ObjectsStore)
				if (p.Value.IsCreature)
				{
					if (!creatureIds.ContainsKey(p.Value.Entry))
						creatureIds[p.Value.Entry] = 0;

					creatureIds[p.Value.Entry]++;
				}

			var orderedCreatures = creatureIds.OrderBy(p => p.Value).Where(p => p.Value > 5);

			handler.SendSysMessage("Top Creatures count:");

			foreach (var p in orderedCreatures)
				handler.SendSysMessage($"Entry: {p.Key} Count: {p.Value}");
		}

		if (mapId.HasValue)
			Global.MapMgr.DoForAllMapsWithMapId(mapId.Value, map => HandleDebugObjectCountMap(map));
		else
			Global.MapMgr.DoForAllMaps(map => HandleDebugObjectCountMap(map));

		return true;
	}

	[Command("phase", RBACPermissions.CommandDebug)]
	static bool HandleDebugPhaseCommand(CommandHandler handler)
	{
		var target = handler.SelectedUnit;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.SelectCreature);

			return false;
		}

		if (target.DBPhase > 0)
			handler.SendSysMessage($"Target creature's PhaseId in DB: {target.DBPhase}");
		else if (target.DBPhase < 0)
			handler.SendSysMessage($"Target creature's PhaseGroup in DB: {Math.Abs(target.DBPhase)}");

		PhasingHandler.PrintToChat(handler, target);

		return true;
	}

	[Command("pvp warmode", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugWarModeBalanceCommand(CommandHandler handler, string command, int? rewardValue)
	{
		// USAGE: .debug pvp fb <alliance|horde|neutral|off> [pct]
		// neutral     Sets faction balance off.
		// alliance    Set faction balance to alliance.
		// horde       Set faction balance to horde.
		// off         Reset the faction balance and use the calculated value of it
		switch (command)
		{
			case "alliance":
				Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamIds.Alliance, rewardValue.GetValueOrDefault(0));

				break;
			case "horde":
				Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamIds.Horde, rewardValue.GetValueOrDefault(0));

				break;
			case "neutral":
				Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamIds.Neutral);

				break;
			case "off":
				Global.WorldMgr.DisableForcedWarModeFactionBalanceState();

				break;
			default:
				handler.SendSysMessage(CypherStrings.BadValue);

				return false;
		}

		return true;
	}

	[Command("questreset", RBACPermissions.CommandDebug)]
	static bool HandleDebugQuestResetCommand(CommandHandler handler, string arg)
	{
		bool daily = false, weekly = false, monthly = false;

		if (arg == "ALL")
			daily = weekly = monthly = true;
		else if (arg == "DAILY")
			daily = true;
		else if (arg == "WEEKLY")
			weekly = true;
		else if (arg == "MONTHLY")
			monthly = true;
		else
			return false;

		var now = GameTime.GetGameTime();

		if (daily)
		{
			Global.WorldMgr.DailyReset();
			handler.SendSysMessage($"Daily quests have been reset. Next scheduled reset: {Time.UnixTimeToDateTime(Global.WorldMgr.GetPersistentWorldVariable(WorldManager.NextDailyQuestResetTimeVarId)).ToShortTimeString()}");
		}

		if (weekly)
		{
			Global.WorldMgr.ResetWeeklyQuests();
			handler.SendSysMessage($"Weekly quests have been reset. Next scheduled reset: {Time.UnixTimeToDateTime(Global.WorldMgr.GetPersistentWorldVariable(WorldManager.NextWeeklyQuestResetTimeVarId)).ToShortTimeString()}");
		}

		if (monthly)
		{
			Global.WorldMgr.ResetMonthlyQuests();
			handler.SendSysMessage($"Monthly quests have been reset. Next scheduled reset: {Time.UnixTimeToDateTime(Global.WorldMgr.GetPersistentWorldVariable(WorldManager.NextMonthlyQuestResetTimeVarId)).ToShortTimeString()}");
		}

		return true;
	}

	[Command("raidreset", RBACPermissions.CommandDebug)]
	static bool HandleDebugRaidResetCommand(CommandHandler handler, uint mapId, uint difficulty)
	{
		var mEntry = CliDB.MapStorage.LookupByKey(mapId);

		if (mEntry == null)
		{
			handler.SendSysMessage("Invalid map specified.");

			return true;
		}

		if (!mEntry.IsDungeon())
		{
			handler.SendSysMessage($"'{mEntry.MapName[handler.SessionDbcLocale]}' is not a dungeon map.");

			return true;
		}

		if (difficulty != 0 && CliDB.DifficultyStorage.HasRecord(difficulty))
		{
			handler.SendSysMessage($"Invalid difficulty {difficulty}.");

			return false;
		}

		if (difficulty != 0 && Global.DB2Mgr.GetMapDifficultyData(mEntry.Id, (Difficulty)difficulty) == null)
		{
			handler.SendSysMessage($"Difficulty {(Difficulty)difficulty} is not valid for '{mEntry.MapName[handler.SessionDbcLocale]}'.");

			return true;
		}

		return true;
	}

	[Command("setaurastate", RBACPermissions.CommandDebug)]
	static bool HandleDebugSetAuraStateCommand(CommandHandler handler, AuraStateType? state, bool apply)
	{
		var unit = handler.SelectedUnit;

		if (!unit)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		if (!state.HasValue)
		{
			// reset all states
			for (AuraStateType s = 0; s < AuraStateType.Max; ++s)
				unit.ModifyAuraState(s, false);

			return true;
		}

		unit.ModifyAuraState(state.GetValueOrDefault(0), apply);

		return true;
	}

	[Command("spawnvehicle", RBACPermissions.CommandDebug)]
	static bool HandleDebugSpawnVehicleCommand(CommandHandler handler, uint entry, uint id)
	{
		var pos = new Position
		{
			Orientation = handler.Player.Location.Orientation
		};

		handler.Player.GetClosePoint(pos, handler.Player.CombatReach);

		if (id == 0)
			return handler.Player.SummonCreature(entry, pos);

		var creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(entry);

		if (creatureTemplate == null)
			return false;

		var vehicleRecord = CliDB.VehicleStorage.LookupByKey(id);

		if (vehicleRecord == null)
			return false;

		var map = handler.Player.Map;

		var creature = Creature.CreateCreature(entry, map, pos, id);

		if (!creature)
			return false;

		map.AddToMap(creature);

		return true;
	}

	[Command("threat", RBACPermissions.CommandDebug)]
	static bool HandleDebugThreatListCommand(CommandHandler handler)
	{
		var target = handler.SelectedUnit;

		if (target == null)
			target = handler.Player;

		var mgr = target.GetThreatManager();

		if (!target.IsAlive)
		{
			handler.SendSysMessage($"{target.GetName()} ({target.GUID}) is not alive.");

			return true;
		}

		uint count = 0;
		var threatenedByMe = target.GetThreatManager().ThreatenedByMeList;

		if (threatenedByMe.Empty())
		{
			handler.SendSysMessage($"{target.GetName()} ({target.GUID}) does not threaten any units.");
		}
		else
		{
			handler.SendSysMessage($"List of units threatened by {target.GetName()} ({target.GUID})");

			foreach (var pair in threatenedByMe)
			{
				Unit unit = pair.Value.Owner;
				handler.SendSysMessage($"   {++count}.   {unit.GetName()}   ({unit.GUID}, SpawnID {(unit.IsCreature ? unit.AsCreature.SpawnId : 0)})  - threat {pair.Value.Threat}");
			}

			handler.SendSysMessage("End of threatened-by-me list.");
		}

		if (mgr.CanHaveThreatList)
		{
			if (!mgr.IsThreatListEmpty(true))
			{
				if (target.IsEngaged)
					handler.SendSysMessage($"Threat list of {target.GetName()} ({target.GUID}, SpawnID {(target.IsCreature ? target.AsCreature.SpawnId : 0)}):");
				else
					handler.SendSysMessage($"{target.GetName()} ({target.GUID}, SpawnID {(target.IsCreature ? target.AsCreature.SpawnId : 0)}) is not engaged, but still has a threat list? Well, here it is:");

				count = 0;
				var fixateVictim = mgr.GetFixateTarget();

				foreach (var refe in mgr.SortedThreatList)
				{
					var unit = refe.Victim;
					handler.SendSysMessage($"   {++count}.   {unit.GetName()}   ({unit.GUID})  - threat {refe.Threat}[{(unit == fixateVictim ? "FIXATE" : refe.TauntState)}][{refe.OnlineState}]");
				}

				handler.SendSysMessage("End of threat list.");
			}
			else if (!target.IsEngaged)
			{
				handler.SendSysMessage($"{target.GetName()} ({target.GUID}, SpawnID {(target.IsCreature ? target.AsCreature.SpawnId : 0)}) is not currently engaged.");
			}
			else
			{
				handler.SendSysMessage($"{target.GetName()} ({target.GUID}, SpawnID {(target.IsCreature ? target.AsCreature.SpawnId : 0)}) seems to be engaged, but does not have a threat list??");
			}
		}
		else if (target.IsEngaged)
		{
			handler.SendSysMessage($"{target.GetName()} ({target.GUID}) is currently engaged. (This unit cannot have a threat list.)");
		}
		else
		{
			handler.SendSysMessage($"{target.GetName()} ({target.GUID}) is not currently engaged. (This unit cannot have a threat list.)");
		}

		return true;
	}

	[Command("threatinfo", RBACPermissions.CommandDebug)]
	static bool HandleDebugThreatInfoCommand(CommandHandler handler)
	{
		var target = handler.SelectedUnit;

		if (target == null)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		handler.SendSysMessage($"Threat info for {target.GetName()} ({target.GUID}):");

		var mgr = target.GetThreatManager();

		// _singleSchoolModifiers
		{
			var mods = mgr._singleSchoolModifiers;
			handler.SendSysMessage(" - Single-school threat modifiers:");
			handler.SendSysMessage($" |-- Physical: {mods[(int)SpellSchools.Normal] * 100.0f:0.##}");
			handler.SendSysMessage($" |-- Holy    : {mods[(int)SpellSchools.Holy] * 100.0f:0.##}");
			handler.SendSysMessage($" |-- Fire    : {mods[(int)SpellSchools.Fire] * 100.0f:0.##}");
			handler.SendSysMessage($" |-- Nature  : {mods[(int)SpellSchools.Nature] * 100.0f:0.##}");
			handler.SendSysMessage($" |-- Frost   : {mods[(int)SpellSchools.Frost] * 100.0f:0.##}");
			handler.SendSysMessage($" |-- Shadow  : {mods[(int)SpellSchools.Shadow] * 100.0f:0.##}");
			handler.SendSysMessage($" |-- Arcane  : {mods[(int)SpellSchools.Arcane] * 100.0f:0.##}");
		}

		// _multiSchoolModifiers
		{
			var mods = mgr._multiSchoolModifiers;
			handler.SendSysMessage($"- Multi-school threat modifiers ({mods.Count} entries):");

			foreach (var pair in mods)
				handler.SendSysMessage($" |-- Mask {pair.Key:X}: {pair.Value:0.XX}");
		}

		// _redirectInfo
		{
			var redirectInfo = mgr._redirectInfo;

			if (redirectInfo.Empty())
			{
				handler.SendSysMessage(" - No redirects being applied");
			}
			else
			{
				handler.SendSysMessage($" - {redirectInfo.Count} redirects being applied:");

				foreach (var pair in redirectInfo)
				{
					var unit = Global.ObjAccessor.GetUnit(target, pair.Item1);
					handler.SendSysMessage($" |-- {pair.Item2:D2} to {(unit != null ? unit.GetName() : pair.Item1)}");
				}
			}
		}

		// _redirectRegistry
		{
			var redirectRegistry = mgr._redirectRegistry;

			if (redirectRegistry.Empty())
			{
				handler.SendSysMessage(" - No redirects are registered");
			}
			else
			{
				handler.SendSysMessage($" - {redirectRegistry.Count} spells may have redirects registered");

				foreach (var outerPair in redirectRegistry) // (spellId, (guid, pct))
				{
					var spell = Global.SpellMgr.GetSpellInfo(outerPair.Key, Difficulty.None);
					handler.SendSysMessage($" |-- #{outerPair.Key} {(spell != null ? spell.SpellName[Global.WorldMgr.DefaultDbcLocale] : "<unknown>")} ({outerPair.Value.Count} entries):");

					foreach (var innerPair in outerPair.Value) // (guid, pct)
					{
						var unit = Global.ObjAccessor.GetUnit(target, innerPair.Key);
						handler.SendSysMessage($"   |-- {innerPair.Value} to {(unit != null ? unit.GetName() : innerPair.Key)}");
					}
				}
			}
		}

		return true;
	}

	[Command("transport", RBACPermissions.CommandDebug)]
	static bool HandleDebugTransportCommand(CommandHandler handler, string operation)
	{
		var transport = handler.Player.GetTransport<Transport>();

		if (!transport)
			return false;

		var start = false;

		if (operation == "stop")
		{
			transport.EnableMovement(false);
		}
		else if (operation == "start")
		{
			transport.EnableMovement(true);
			start = true;
		}
		else
		{
			Position pos = transport.Location;
			handler.SendSysMessage("Transport {0} is {1}", transport.GetName(), transport.GoState == GameObjectState.Ready ? "stopped" : "moving");
			handler.SendSysMessage("Transport position: {0}", pos.ToString());

			return true;
		}

		handler.SendSysMessage("Transport {0} {1}", transport.GetName(), start ? "started" : "stopped");

		return true;
	}

	[Command("warden force", RBACPermissions.CommandDebug, true)]
	static bool HandleDebugWardenForce(CommandHandler handler, ushort[] checkIds)
	{
		/*if (checkIds.Empty())
			return false;

		Warden  warden = handler.GetSession().GetWarden();
		if (warden == null)
		{
			handler.SendSysMessage("Warden system is not enabled");
			return true;
		}

		size_t const nQueued = warden->DEBUG_ForceSpecificChecks(checkIds);
		handler->PSendSysMessage("%zu/%zu checks queued for your Warden, they should be sent over the next few minutes (depending on settings)", nQueued, checkIds.size());*/
		return true;
	}

	[Command("worldstate", RBACPermissions.CommandDebug)]
	static bool HandleDebugUpdateWorldStateCommand(CommandHandler handler, uint variable, uint value)
	{
		handler.Player.SendUpdateWorldState(variable, value);

		return true;
	}

	[CommandNonGroup("wpgps", RBACPermissions.CommandDebug)]
	static bool HandleWPGPSCommand(CommandHandler handler)
	{
		var player = handler.Player;

		Log.Logger.Information($"(@PATH, XX, {player.Location.X:3F}, {player.Location.Y:3F}, {player.Location.Z:5F}, {player.Location.Orientation:5F}, 0, 0, 0, 100, 0)");

		handler.SendSysMessage("Waypoint SQL written to SQL Developer log");

		return true;
	}

	[Command("wsexpression", RBACPermissions.CommandDebug)]
	static bool HandleDebugWSExpressionCommand(CommandHandler handler, uint expressionId)
	{
		var target = handler.SelectedPlayerOrSelf;

		if (target == null)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var wsExpressionEntry = CliDB.WorldStateExpressionStorage.LookupByKey(expressionId);

		if (wsExpressionEntry == null)
			return false;

		if (ConditionManager.IsPlayerMeetingExpression(target, wsExpressionEntry))
			handler.SendSysMessage($"Expression {expressionId} meet");
		else
			handler.SendSysMessage($"Expression {expressionId} not meet");

		return true;
	}

	static void HandleDebugGuidLimitsMap(CommandHandler handler, Map map)
	{
		handler.SendSysMessage($"Map Id: {map.Id} Name: '{map.MapName}' Instance Id: {map.InstanceId} Highest Guid Creature: {map.GenerateLowGuid(HighGuid.Creature)} GameObject: {map.GetMaxLowGuid(HighGuid.GameObject)}");
	}

	[CommandGroup("asan")]
	class DebugAsanCommands
	{
		[Command("memoryleak", RBACPermissions.CommandDebug, true)]
		static bool HandleDebugMemoryLeak(CommandHandler handler)
		{
			return true;
		}

		[Command("outofbounds", RBACPermissions.CommandDebug, true)]
		static bool HandleDebugOutOfBounds(CommandHandler handler)
		{
			return true;
		}
	}

	[CommandGroup("play")]
	class DebugPlayCommands
	{
		[Command("cinematic", RBACPermissions.CommandDebug)]
		static bool HandleDebugPlayCinematicCommand(CommandHandler handler, uint cinematicId)
		{
			var cineSeq = CliDB.CinematicSequencesStorage.LookupByKey(cinematicId);

			if (cineSeq == null)
			{
				handler.SendSysMessage(CypherStrings.CinematicNotExist, cinematicId);

				return false;
			}

			// Dump camera locations
			var list = M2Storage.GetFlyByCameras(cineSeq.Camera[0]);

			if (list != null)
			{
				handler.SendSysMessage("Waypoints for sequence {0}, camera {1}", cinematicId, cineSeq.Camera[0]);
				uint count = 1;

				foreach (var cam in list)
				{
					handler.SendSysMessage("{0} - {1}ms [{2}, {3}, {4}] Facing {5} ({6} degrees)", count, cam.timeStamp, cam.locations.X, cam.locations.Y, cam.locations.Z, cam.locations.W, cam.locations.W * (180 / Math.PI));
					count++;
				}

				handler.SendSysMessage("{0} waypoints dumped", list.Count);
			}

			handler.Player.SendCinematicStart(cinematicId);

			return true;
		}

		[Command("movie", RBACPermissions.CommandDebug)]
		static bool HandleDebugPlayMovieCommand(CommandHandler handler, uint movieId)
		{
			if (!CliDB.MovieStorage.ContainsKey(movieId))
			{
				handler.SendSysMessage(CypherStrings.MovieNotExist, movieId);

				return false;
			}

			handler.Player.SendMovieStart(movieId);

			return true;
		}

		[Command("music", RBACPermissions.CommandDebug)]
		static bool HandleDebugPlayMusicCommand(CommandHandler handler, uint musicId)
		{
			if (!CliDB.SoundKitStorage.ContainsKey(musicId))
			{
				handler.SendSysMessage(CypherStrings.SoundNotExist, musicId);

				return false;
			}

			var player = handler.Player;

			player.PlayDirectMusic(musicId, player);

			handler.SendSysMessage(CypherStrings.YouHearSound, musicId);

			return true;
		}

		[Command("sound", RBACPermissions.CommandDebug)]
		static bool HandleDebugPlaySoundCommand(CommandHandler handler, uint soundId, uint broadcastTextId)
		{
			if (!CliDB.SoundKitStorage.ContainsKey(soundId))
			{
				handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);

				return false;
			}

			var player = handler.Player;

			var unit = handler.SelectedUnit;

			if (!unit)
			{
				handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

				return false;
			}

			if (!player.Target.IsEmpty)
				unit.PlayDistanceSound(soundId, player);
			else
				unit.PlayDirectSound(soundId, player, broadcastTextId);

			handler.SendSysMessage(CypherStrings.YouHearSound, soundId);

			return true;
		}
	}

	[CommandGroup("pvp")]
	class DebugPvpCommands
	{
		[Command("warmode", RBACPermissions.CommandDebug)]
		static bool HandleDebugWarModeFactionBalanceCommand(CommandHandler handler, string command, int rewardValue = 0)
		{
			// USAGE: .debug pvp fb <alliance|horde|neutral|off> [pct]
			// neutral     Sets faction balance off.
			// alliance    Set faction balance to alliance.
			// horde       Set faction balance to horde.
			// off         Reset the faction balance and use the calculated value of it
			switch (command.ToLower())
			{
				default: // workaround for Variant of only ExactSequences not being supported
					handler.SendSysMessage(CypherStrings.BadValue);

					return false;
				case "alliance":
					Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamIds.Alliance, rewardValue);

					break;
				case "horde":
					Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamIds.Horde, rewardValue);

					break;
				case "neutral":
					Global.WorldMgr.SetForcedWarModeFactionBalanceState(TeamIds.Neutral);

					break;
				case "off":
					Global.WorldMgr.DisableForcedWarModeFactionBalanceState();

					break;
			}

			return true;
		}
	}

	[CommandGroup("send")]
	class DebugSendCommands
	{
		[Command("buyerror", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendBuyErrorCommand(CommandHandler handler, BuyResult error)
		{
			handler.Player.SendBuyError(error, null, 0);

			return true;
		}

		[Command("channelnotify", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendChannelNotifyCommand(CommandHandler handler, ChatNotify type)
		{
			ChannelNotify packet = new()
			{
				Type = type,
				Channel = "test"
			};

			handler.Session.SendPacket(packet);

			return true;
		}

		[Command("chatmessage", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendChatMsgCommand(CommandHandler handler, ChatMsg type)
		{
			ChatPkt data = new();
			data.Initialize(type, Language.Universal, handler.Player, handler.Player, "testtest", 0, "chan");
			handler.Session.SendPacket(data);

			return true;
		}

		[Command("equiperror", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendEquipErrorCommand(CommandHandler handler, InventoryResult error)
		{
			handler.Player.SendEquipError(error);

			return true;
		}

		[Command("largepacket", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendLargePacketCommand(CommandHandler handler)
		{
			StringBuilder ss = new();

			while (ss.Length < 128000)
				ss.Append("This is a dummy string to push the packet's size beyond 128000 bytes. ");

			handler.SendSysMessage(ss.ToString());

			return true;
		}

		[Command("opcode", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendOpcodeCommand(CommandHandler handler)
		{
			handler.SendSysMessage(CypherStrings.CmdInvalid);

			return true;
		}

		[Command("playerchoice", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendPlayerChoiceCommand(CommandHandler handler, int choiceId)
		{
			var player = handler.Player;
			player.SendPlayerChoice(player.GUID, choiceId);

			return true;
		}

		[Command("qpartymsg", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendQuestPartyMsgCommand(CommandHandler handler, QuestPushReason msg)
		{
			handler.Player.SendPushToPartyResponse(handler.Player, msg);

			return true;
		}

		[Command("qinvalidmsg", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendQuestInvalidMsgCommand(CommandHandler handler, QuestFailedReasons msg)
		{
			handler.Player.SendCanTakeQuestResponse(msg);

			return true;
		}

		[Command("sellerror", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendSellErrorCommand(CommandHandler handler, SellResult error)
		{
			handler.Player.SendSellError(error, null, ObjectGuid.Empty);

			return true;
		}

		[Command("setphaseshift", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendSetPhaseShiftCommand(CommandHandler handler, uint phaseId, uint visibleMapId, uint uiMapPhaseId)
		{
			PhaseShift phaseShift = new();

			if (phaseId != 0)
				phaseShift.AddPhase(phaseId, PhaseFlags.None, null);

			if (visibleMapId != 0)
				phaseShift.AddVisibleMapId(visibleMapId, null);

			if (uiMapPhaseId != 0)
				phaseShift.AddUiMapPhaseId(uiMapPhaseId);

			PhasingHandler.SendToPlayer(handler.Player, phaseShift);

			return true;
		}

		[Command("spellfail", RBACPermissions.CommandDebug)]
		static bool HandleDebugSendSpellFailCommand(CommandHandler handler, SpellCastResult result, int? failArg1, int? failArg2)
		{
			CastFailed castFailed = new()
			{
				CastID = ObjectGuid.Empty,
				SpellID = 133,
				Reason = result,
				FailedArg1 = failArg1.GetValueOrDefault(-1),
				FailedArg2 = failArg2.GetValueOrDefault(-1)
			};

			handler.Session.SendPacket(castFailed);

			return true;
		}
	}

	[CommandGroup("warden")]
	class DebugWardenCommands
	{
		[Command("force", RBACPermissions.CommandDebug, true)]
		static bool HandleDebugWardenForce(CommandHandler handler, ushort[] checkIds)
		{
			/*if (checkIds.Empty())
				return false;

			Warden  warden = handler.GetSession().GetWarden();
			if (warden == null)
			{
				handler.SendSysMessage("Warden system is not enabled");
				return true;
			}

			size_t const nQueued = warden->DEBUG_ForceSpecificChecks(checkIds);
			handler->PSendSysMessage("%zu/%zu checks queued for your Warden, they should be sent over the next few minutes (depending on settings)", nQueued, checkIds.size());*/
			return true;
		}
	}
}