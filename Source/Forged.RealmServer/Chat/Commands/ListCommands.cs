// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Chat.Commands;

[CommandGroup("list")]
class ListCommands
{
	[Command("creature", RBACPermissions.CommandListCreature, true)]
	static bool HandleListCreatureCommand(CommandHandler handler, uint creatureId, uint? countArg)
	{
		var cInfo = Global.ObjectMgr.GetCreatureTemplate(creatureId);

		if (cInfo == null)
		{
			handler.SendSysMessage(CypherStrings.CommandInvalidcreatureid, creatureId);

			return false;
		}

		var count = countArg.GetValueOrDefault(10);

		if (count == 0)
			return false;

		uint creatureCount = 0;
		var result = DB.World.Query("SELECT COUNT(guid) FROM creature WHERE id='{0}'", creatureId);

		if (!result.IsEmpty())
			creatureCount = result.Read<uint>(0);

		if (handler.Session != null)
		{
			var player = handler.Session.Player;

			result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map, (POW(position_x - '{0}', 2) + POW(position_y - '{1}', 2) + POW(position_z - '{2}', 2)) AS order_ FROM creature WHERE id = '{3}' ORDER BY order_ ASC LIMIT {4}",
									player.Location.X,
									player.Location.Y,
									player.Location.Z,
									creatureId,
									count);
		}
		else
		{
			result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map FROM creature WHERE id = '{0}' LIMIT {1}",
									creatureId,
									count);
		}

		if (!result.IsEmpty())
			do
			{
				var guid = result.Read<ulong>(0);
				var x = result.Read<float>(1);
				var y = result.Read<float>(2);
				var z = result.Read<float>(3);
				var mapId = result.Read<ushort>(4);
				var liveFound = false;

				// Get map (only support base map from console)
				Map thisMap = null;

				if (handler.Session != null)
					thisMap = handler.Session.Player.Map;

				// If map found, try to find active version of this creature
				if (thisMap)
				{
					var creBounds = thisMap.CreatureBySpawnIdStore.LookupByKey(guid);

					foreach (var creature in creBounds)
						handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, cInfo.Name, x, y, z, mapId, creature.GUID.ToString(), creature.IsAlive ? "*" : " ");

					liveFound = !creBounds.Empty();
				}

				if (!liveFound)
				{
					if (handler.Session)
						handler.SendSysMessage(CypherStrings.CreatureListChat, guid, guid, cInfo.Name, x, y, z, mapId, "", "");
					else
						handler.SendSysMessage(CypherStrings.CreatureListConsole, guid, cInfo.Name, x, y, z, mapId, "", "");
				}
			} while (result.NextRow());

		handler.SendSysMessage(CypherStrings.CommandListcreaturemessage, creatureId, creatureCount);

		return true;
	}

	[Command("item", RBACPermissions.CommandListItem, true)]
	static bool HandleListItemCommand(CommandHandler handler, uint itemId, uint? countArg)
	{
		var count = countArg.GetValueOrDefault(10);

		if (count == 0)
			return false;

		// inventory case
		uint inventoryCount = 0;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_COUNT_ITEM);
		stmt.AddValue(0, itemId);
		var result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			inventoryCount = result.Read<uint>(0);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_INVENTORY_ITEM_BY_ENTRY);
		stmt.AddValue(0, itemId);
		stmt.AddValue(1, count);
		result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			do
			{
				var itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
				var itemBag = result.Read<uint>(1);
				var itemSlot = result.Read<byte>(2);
				var ownerGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3));
				var ownerAccountId = result.Read<uint>(4);
				var ownerName = result.Read<string>(5);

				string itemPos;

				if (Player.IsEquipmentPos((byte)itemBag, itemSlot))
					itemPos = "[equipped]";
				else if (Player.IsInventoryPos((byte)itemBag, itemSlot))
					itemPos = "[in inventory]";
				else if (Player.IsReagentBankPos((byte)itemBag, itemSlot))
					itemPos = "[in reagent bank]";
				else if (Player.IsBankPos((byte)itemBag, itemSlot))
					itemPos = "[in bank]";
				else
					itemPos = "";

				handler.SendSysMessage(CypherStrings.ItemlistSlot, itemGuid.ToString(), ownerName, ownerGuid.ToString(), ownerAccountId, itemPos);

				count--;
			} while (result.NextRow());

		// mail case
		uint mailCount = 0;

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_COUNT_ITEM);
		stmt.AddValue(0, itemId);
		result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			mailCount = result.Read<uint>(0);

		if (count > 0)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_ITEMS_BY_ENTRY);
			stmt.AddValue(0, itemId);
			stmt.AddValue(1, count);
			result = DB.Characters.Query(stmt);
		}
		else
		{
			result = null;
		}

		if (result != null && !result.IsEmpty())
			do
			{
				var itemGuid = result.Read<ulong>(0);
				var itemSender = result.Read<ulong>(1);
				var itemReceiver = result.Read<ulong>(2);
				var itemSenderAccountId = result.Read<uint>(3);
				var itemSenderName = result.Read<string>(4);
				var itemReceiverAccount = result.Read<uint>(5);
				var itemReceiverName = result.Read<string>(6);

				var itemPos = "[in mail]";

				handler.SendSysMessage(CypherStrings.ItemlistMail, itemGuid, itemSenderName, itemSender, itemSenderAccountId, itemReceiverName, itemReceiver, itemReceiverAccount, itemPos);

				count--;
			} while (result.NextRow());

		// auction case
		uint auctionCount = 0;

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTIONHOUSE_COUNT_ITEM);
		stmt.AddValue(0, itemId);
		result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			auctionCount = result.Read<uint>(0);

		if (count > 0)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTIONHOUSE_ITEM_BY_ENTRY);
			stmt.AddValue(0, itemId);
			stmt.AddValue(1, count);
			result = DB.Characters.Query(stmt);
		}
		else
		{
			result = null;
		}

		if (result != null && !result.IsEmpty())
			do
			{
				var itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
				var owner = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
				var ownerAccountId = result.Read<uint>(2);
				var ownerName = result.Read<string>(3);

				var itemPos = "[in auction]";

				handler.SendSysMessage(CypherStrings.ItemlistAuction, itemGuid.ToString(), ownerName, owner.ToString(), ownerAccountId, itemPos);
			} while (result.NextRow());

		// guild bank case
		uint guildCount = 0;

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_COUNT_ITEM);
		stmt.AddValue(0, itemId);
		result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			guildCount = result.Read<uint>(0);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_BANK_ITEM_BY_ENTRY);
		stmt.AddValue(0, itemId);
		stmt.AddValue(1, count);
		result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
			do
			{
				var itemGuid = ObjectGuid.Create(HighGuid.Item, result.Read<ulong>(0));
				var guildGuid = ObjectGuid.Create(HighGuid.Guild, result.Read<ulong>(1));
				var guildName = result.Read<string>(2);

				var itemPos = "[in guild bank]";

				handler.SendSysMessage(CypherStrings.ItemlistGuild, itemGuid.ToString(), guildName, guildGuid.ToString(), itemPos);

				count--;
			} while (result.NextRow());

		if (inventoryCount + mailCount + auctionCount + guildCount == 0)
		{
			handler.SendSysMessage(CypherStrings.CommandNoitemfound);

			return false;
		}

		handler.SendSysMessage(CypherStrings.CommandListitemmessage, itemId, inventoryCount + mailCount + auctionCount + guildCount, inventoryCount, mailCount, auctionCount, guildCount);

		return true;
	}

	[Command("mail", RBACPermissions.CommandListMail, true)]
	static bool HandleListMailCommand(CommandHandler handler, PlayerIdentifier player)
	{
		if (player == null)
			player = PlayerIdentifier.FromTargetOrSelf(handler);

		if (player == null)
			return false;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_COUNT);
		stmt.AddValue(0, player.GetGUID().Counter);
		var result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
		{
			var countMail = result.Read<uint>(0);

			var nameLink = handler.PlayerLink(player.GetName());
			handler.SendSysMessage(CypherStrings.ListMailHeader, countMail, nameLink, player.GetGUID().ToString());
			handler.SendSysMessage(CypherStrings.AccountListBar);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_INFO);
			stmt.AddValue(0, player.GetGUID().Counter);
			var result1 = DB.Characters.Query(stmt);

			if (!result1.IsEmpty())
				do
				{
					var messageId = result1.Read<uint>(0);
					var senderId = result1.Read<ulong>(1);
					var sender = result1.Read<string>(2);
					var receiverId = result1.Read<ulong>(3);
					var receiver = result1.Read<string>(4);
					var subject = result1.Read<string>(5);
					var deliverTime = result1.Read<long>(6);
					var expireTime = result1.Read<long>(7);
					var money = result1.Read<ulong>(8);
					var hasItem = result1.Read<byte>(9);
					var gold = (uint)(money / MoneyConstants.Gold);
					var silv = (uint)(money % MoneyConstants.Gold) / MoneyConstants.Silver;
					var copp = (uint)(money % MoneyConstants.Gold) % MoneyConstants.Silver;
					var receiverStr = handler.PlayerLink(receiver);
					var senderStr = handler.PlayerLink(sender);
					handler.SendSysMessage(CypherStrings.ListMailInfo1, messageId, subject, gold, silv, copp);
					handler.SendSysMessage(CypherStrings.ListMailInfo2, senderStr, senderId, receiverStr, receiverId);
					handler.SendSysMessage(CypherStrings.ListMailInfo3, Time.UnixTimeToDateTime(deliverTime).ToLongDateString(), Time.UnixTimeToDateTime(expireTime).ToLongDateString());

					if (hasItem == 1)
					{
						var result2 = DB.Characters.Query("SELECT item_guid FROM mail_items WHERE mail_id = '{0}'", messageId);

						if (!result2.IsEmpty())
							do
							{
								var item_guid = result2.Read<uint>(0);
								stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAIL_LIST_ITEMS);
								stmt.AddValue(0, item_guid);
								var result3 = DB.Characters.Query(stmt);

								if (!result3.IsEmpty())
									do
									{
										var item_entry = result3.Read<uint>(0);
										var item_count = result3.Read<uint>(1);

										var itemTemplate = Global.ObjectMgr.GetItemTemplate(item_entry);

										if (itemTemplate == null)
											continue;

										if (handler.Session != null)
										{
											var color = ItemConst.ItemQualityColors[(int)itemTemplate.Quality];
											var itemStr = $"|c{color}|Hitem:{item_entry}:0:0:0:0:0:0:0:{handler.Session.Player.Level}:0:0:0:0:0|h[{itemTemplate.GetName(handler.SessionDbcLocale)}]|h|r";
											handler.SendSysMessage(CypherStrings.ListMailInfoItem, itemStr, item_entry, item_guid, item_count);
										}
										else
										{
											handler.SendSysMessage(CypherStrings.ListMailInfoItem, itemTemplate.GetName(handler.SessionDbcLocale), item_entry, item_guid, item_count);
										}
									} while (result3.NextRow());
							} while (result2.NextRow());
					}

					handler.SendSysMessage(CypherStrings.AccountListBar);
				} while (result1.NextRow());
			else
				handler.SendSysMessage(CypherStrings.ListMailNotFound);

			return true;
		}
		else
		{
			handler.SendSysMessage(CypherStrings.ListMailNotFound);
		}

		return true;
	}

	[Command("object", RBACPermissions.CommandListObject, true)]
	static bool HandleListObjectCommand(CommandHandler handler, uint gameObjectId, uint? countArg)
	{
		var gInfo = Global.ObjectMgr.GetGameObjectTemplate(gameObjectId);

		if (gInfo == null)
		{
			handler.SendSysMessage(CypherStrings.CommandListobjinvalidid, gameObjectId);

			return false;
		}

		var count = countArg.GetValueOrDefault(10);

		if (count == 0)
			return false;

		uint objectCount = 0;
		var result = DB.World.Query("SELECT COUNT(guid) FROM gameobject WHERE id='{0}'", gameObjectId);

		if (!result.IsEmpty())
			objectCount = result.Read<uint>(0);

		if (handler.Session != null)
		{
			var player = handler.Session.Player;

			result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map, id, (POW(position_x - '{0}', 2) + POW(position_y - '{1}', 2) + POW(position_z - '{2}', 2)) AS order_ FROM gameobject WHERE id = '{3}' ORDER BY order_ ASC LIMIT {4}",
									player.Location.X,
									player.Location.Y,
									player.Location.Z,
									gameObjectId,
									count);
		}
		else
		{
			result = DB.World.Query("SELECT guid, position_x, position_y, position_z, map, id FROM gameobject WHERE id = '{0}' LIMIT {1}",
									gameObjectId,
									count);
		}

		if (!result.IsEmpty())
			do
			{
				var guid = result.Read<ulong>(0);
				var x = result.Read<float>(1);
				var y = result.Read<float>(2);
				var z = result.Read<float>(3);
				var mapId = result.Read<ushort>(4);
				var entry = result.Read<uint>(5);
				var liveFound = false;

				// Get map (only support base map from console)
				Map thisMap = null;

				if (handler.Session != null)
					thisMap = handler.Session.Player.Map;

				// If map found, try to find active version of this object
				if (thisMap)
				{
					var goBounds = thisMap.GameObjectBySpawnIdStore.LookupByKey(guid);

					foreach (var go in goBounds)
						handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gInfo.name, x, y, z, mapId, go.GUID.ToString(), go.IsSpawned ? "*" : " ");

					liveFound = !goBounds.Empty();
				}

				if (!liveFound)
				{
					if (handler.Session)
						handler.SendSysMessage(CypherStrings.GoListChat, guid, entry, guid, gInfo.name, x, y, z, mapId, "", "");
					else
						handler.SendSysMessage(CypherStrings.GoListConsole, guid, gInfo.name, x, y, z, mapId, "", "");
				}
			} while (result.NextRow());

		handler.SendSysMessage(CypherStrings.CommandListobjmessage, gameObjectId, objectCount);

		return true;
	}

	[Command("respawns", RBACPermissions.CommandListRespawns)]
	static bool HandleListRespawnsCommand(CommandHandler handler, uint? range)
	{
		var player = handler.Session.Player;
		var map = player.Map;

		var locale = handler.Session.SessionDbcLocale;
		var stringOverdue = Global.ObjectMgr.GetCypherString(CypherStrings.ListRespawnsOverdue, locale);

		var zoneId = player.Zone;
		var zoneName = GetZoneName(zoneId, locale);

		for (SpawnObjectType type = 0; type < SpawnObjectType.NumSpawnTypes; type++)
		{
			if (range.HasValue)
				handler.SendSysMessage(CypherStrings.ListRespawnsRange, type, range.Value);
			else
				handler.SendSysMessage(CypherStrings.ListRespawnsZone, type, zoneName, zoneId);

			handler.SendSysMessage(CypherStrings.ListRespawnsListheader);
			List<RespawnInfo> respawns = new();
			map.GetRespawnInfo(respawns, (SpawnObjectTypeMask)(1 << (int)type));

			foreach (var ri in respawns)
			{
				var data = Global.ObjectMgr.GetSpawnMetadata(ri.ObjectType, ri.SpawnId);

				if (data == null)
					continue;

				uint respawnZoneId = 0;
				var edata = data.ToSpawnData();

				if (edata != null)
				{
					respawnZoneId = map.GetZoneId(PhasingHandler.EmptyPhaseShift, edata.SpawnPoint);

					if (range.HasValue)
					{
						if (!player.Location.IsInDist(edata.SpawnPoint, range.Value))
							continue;
					}
					else
					{
						if (zoneId != respawnZoneId)
							continue;
					}
				}

				var gridY = ri.GridId / MapConst.MaxGrids;
				var gridX = ri.GridId % MapConst.MaxGrids;

				var respawnTime = ri.RespawnTime > GameTime.GetGameTime() ? Time.secsToTimeString((ulong)(ri.RespawnTime - GameTime.GetGameTime()), TimeFormat.ShortText) : stringOverdue;
				handler.SendSysMessage($"{ri.SpawnId} | {ri.Entry} | [{gridX:2},{gridY:2}] | {GetZoneName(respawnZoneId, locale)} ({respawnZoneId}) | {respawnTime}{(map.IsSpawnGroupActive(data.SpawnGroupData.GroupId) ? "" : " (inactive)")}");
			}
		}

		return true;
	}

	[Command("scenes", RBACPermissions.CommandListScenes)]
	static bool HandleListScenesCommand(CommandHandler handler)
	{
		var target = handler.SelectedPlayer;

		if (!target)
			target = handler.Session.Player;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var instanceByPackageMap = target.SceneMgr.GetSceneTemplateByInstanceMap();

		handler.SendSysMessage(CypherStrings.DebugSceneObjectList, target.SceneMgr.GetActiveSceneCount());

		foreach (var instanceByPackage in instanceByPackageMap)
			handler.SendSysMessage(CypherStrings.DebugSceneObjectDetail, instanceByPackage.Value.ScenePackageId, instanceByPackage.Key);

		return true;
	}

	[Command("spawnpoints", RBACPermissions.CommandListSpawnpoints)]
	static bool HandleListSpawnPointsCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;
		var map = player.Map;
		var mapId = map.Id;
		var showAll = map.IsBattlegroundOrArena || map.IsDungeon;
		handler.SendSysMessage($"Listing all spawn points in map {mapId} ({map.MapName}){(showAll ? "" : " within 5000yd")}:");

		foreach (var pair in Global.ObjectMgr.GetAllCreatureData())
		{
			SpawnData data = pair.Value;

			if (data.MapId != mapId)
				continue;

			var cTemp = Global.ObjectMgr.GetCreatureTemplate(data.Id);

			if (cTemp == null)
				continue;

			if (showAll || data.SpawnPoint.IsInDist2d(player.Location, 5000.0f))
				handler.SendSysMessage($"Type: {data.Type} | SpawnId: {data.SpawnId} | Entry: {data.Id} ({cTemp.Name}) | X: {data.SpawnPoint.X:3} | Y: {data.SpawnPoint.Y:3} | Z: {data.SpawnPoint.Z:3}");
		}

		foreach (var pair in Global.ObjectMgr.GetAllGameObjectData())
		{
			SpawnData data = pair.Value;

			if (data.MapId != mapId)
				continue;

			var goTemp = Global.ObjectMgr.GetGameObjectTemplate(data.Id);

			if (goTemp == null)
				continue;

			if (showAll || data.SpawnPoint.IsInDist2d(player.Location, 5000.0f))
				handler.SendSysMessage($"Type: {data.Type} | SpawnId: {data.SpawnId} | Entry: {data.Id} ({goTemp.name}) | X: {data.SpawnPoint.X:3} | Y: {data.SpawnPoint.Y:3} | Z: {data.SpawnPoint.Z:3}");
		}

		return true;
	}

	static string GetZoneName(uint zoneId, Locale locale)
	{
		var zoneEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);

		return zoneEntry != null ? zoneEntry.AreaName[locale] : "<unknown zone>";
	}

	[CommandGroup("auras")]
	class ListAuraCommands
	{
		[Command("", RBACPermissions.CommandListAuras)]
		static bool HandleListAllAurasCommand(CommandHandler handler)
		{
			return ListAurasCommand(handler, null, null);
		}

		[Command("id", RBACPermissions.CommandListAuras)]
		static bool HandleListAurasByIdCommand(CommandHandler handler, uint spellId)
		{
			return ListAurasCommand(handler, spellId, null);
		}

		[Command("name", RBACPermissions.CommandListAuras)]
		static bool HandleListAurasByNameCommand(CommandHandler handler, Tail namePart)
		{
			return ListAurasCommand(handler, null, namePart);
		}

		static bool ListAurasCommand(CommandHandler handler, uint? spellId, string namePart)
		{
			var unit = handler.SelectedUnit;

			if (!unit)
			{
				handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

				return false;
			}

			var talentStr = handler.GetCypherString(CypherStrings.Talent);
			var passiveStr = handler.GetCypherString(CypherStrings.Passive);

			var auras = unit.AppliedAuras;
			handler.SendSysMessage(CypherStrings.CommandTargetListauras, unit.AppliedAurasCount);

			foreach (var aurApp in auras)
			{
				var aura = aurApp.Base;
				var name = aura.SpellInfo.SpellName[handler.SessionDbcLocale];
				var talent = aura.SpellInfo.HasAttribute(SpellCustomAttributes.IsTalent);

				if (!ShouldListAura(aura.SpellInfo, spellId, namePart, handler.SessionDbcLocale))
					continue;

				var ss_name = "|cffffffff|Hspell:" + aura.Id + "|h[" + name + "]|h|r";

				handler.SendSysMessage(CypherStrings.CommandTargetAuradetail,
										aura.Id,
										(handler.Session != null ? ss_name : name),
										aurApp.EffectMask.ToMask(),
										aura.Charges,
										aura.StackAmount,
										aurApp.Slot,
										aura.Duration,
										aura.MaxDuration,
										(aura.IsPassive ? passiveStr : ""),
										(talent ? talentStr : ""),
										aura.CasterGuid.IsPlayer ? "player" : "creature",
										aura.CasterGuid.ToString());
			}

			for (AuraType auraType = 0; auraType < AuraType.Total; ++auraType)
			{
				var auraList = unit.GetAuraEffectsByType(auraType);

				if (auraList.Empty())
					continue;

				var sizeLogged = false;

				foreach (var effect in auraList)
				{
					if (!ShouldListAura(effect.SpellInfo, spellId, namePart, handler.SessionDbcLocale))
						continue;

					if (!sizeLogged)
					{
						sizeLogged = true;
						handler.SendSysMessage(CypherStrings.CommandTargetListauratype, auraList.Count, auraType);
					}

					handler.SendSysMessage(CypherStrings.CommandTargetAurasimple, effect.Id, effect.EffIndex, effect.Amount);
				}
			}

			return true;
		}

		static bool ShouldListAura(SpellInfo spellInfo, uint? spellId, string namePart, Locale locale)
		{
			if (spellId.HasValue)
				return spellInfo.Id == spellId.Value;

			if (!namePart.IsEmpty())
			{
				var name = spellInfo.SpellName[locale];

				return name.Like(namePart);
			}

			return true;
		}
	}
}