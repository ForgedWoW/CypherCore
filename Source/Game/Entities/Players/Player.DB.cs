// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Garrisons;
using Game.Mails;
using Game.Maps;
using Game.Maps.Grids;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Game.Entities;

public partial class Player
{
	public void LoadCorpse(SQLResult result)
	{
		if (IsAlive || HasAtLoginFlag(AtLoginFlags.Resurrect))
			SpawnCorpseBones(false);

		if (!IsAlive)
		{
			if (HasAtLoginFlag(AtLoginFlags.Resurrect))
			{
				ResurrectPlayer(0.5f);
			}
			else if (!result.IsEmpty())
			{
				_corpseLocation = new WorldLocation(result.Read<ushort>(0), result.Read<float>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4));

				if (!CliDB.MapStorage.LookupByKey(_corpseLocation.MapId).Instanceable())
					SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
				else
					RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
			}
		}

		RemoveAtLoginFlag(AtLoginFlags.Resurrect);
	}

	public void _LoadMail(SQLResult mailsResult, SQLResult mailItemsResult, SQLResult artifactResult, SQLResult azeriteItemResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult)
	{
		Dictionary<ulong, Mail> mailById = new();

		if (!mailsResult.IsEmpty())
			do
			{
				Mail m = new();

				m.messageID = mailsResult.Read<ulong>(0);
				m.messageType = (MailMessageType)mailsResult.Read<byte>(1);
				m.sender = mailsResult.Read<uint>(2);
				m.receiver = mailsResult.Read<uint>(3);
				m.subject = mailsResult.Read<string>(4);
				m.body = mailsResult.Read<string>(5);
				m.expire_time = mailsResult.Read<long>(6);
				m.deliver_time = mailsResult.Read<long>(7);
				m.money = mailsResult.Read<ulong>(8);
				m.COD = mailsResult.Read<ulong>(9);
				m.checkMask = (MailCheckMask)mailsResult.Read<byte>(10);
				m.stationery = (MailStationery)mailsResult.Read<byte>(11);
				m.mailTemplateId = mailsResult.Read<ushort>(12);

				if (m.mailTemplateId != 0 && !CliDB.MailTemplateStorage.ContainsKey(m.mailTemplateId))
				{
					Log.outError(LogFilter.Player, $"Player:_LoadMail - Mail ({m.messageID}) have not existed MailTemplateId ({m.mailTemplateId}), remove at load");
					m.mailTemplateId = 0;
				}

				m.state = MailState.Unchanged;

				_mail.Add(m);
				mailById[m.messageID] = m;
			} while (mailsResult.NextRow());

		if (!mailItemsResult.IsEmpty())
		{
			Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
			ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteItemResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

			do
			{
				var mailId = mailItemsResult.Read<ulong>(52);
				_LoadMailedItem(GUID, this, mailId, mailById[mailId], mailItemsResult.GetFields(), additionalData.LookupByKey(mailItemsResult.Read<ulong>(0)));
			} while (mailItemsResult.NextRow());
		}

		UpdateNextMailTimeAndUnreads();
	}

	public static void SavePlayerCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
		stmt.AddValue(0, guid);
		trans.Append(stmt);

		foreach (var customization in customizations)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_CUSTOMIZATION);
			stmt.AddValue(0, guid);
			stmt.AddValue(1, customization.ChrCustomizationOptionID);
			stmt.AddValue(2, customization.ChrCustomizationChoiceID);
			trans.Append(stmt);
		}
	}

	public static void SaveCustomizations(SQLTransaction trans, ulong guid, List<ChrCustomizationChoice> customizations)
	{
		SavePlayerCustomizations(trans, guid, customizations);
	}

	public void _SaveMail(SQLTransaction trans)
	{
		PreparedStatement stmt;

		foreach (var m in _mail)
			if (m.state == MailState.Changed)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_MAIL);
				stmt.AddValue(0, m.HasItems() ? 1 : 0);
				stmt.AddValue(1, m.expire_time);
				stmt.AddValue(2, m.deliver_time);
				stmt.AddValue(3, m.money);
				stmt.AddValue(4, m.COD);
				stmt.AddValue(5, (byte)m.checkMask);
				stmt.AddValue(6, m.messageID);

				trans.Append(stmt);

				if (!m.removedItems.Empty())
				{
					foreach (var id in m.removedItems)
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
						stmt.AddValue(0, id);
						trans.Append(stmt);
					}

					m.removedItems.Clear();
				}

				m.state = MailState.Unchanged;
			}
			else if (m.state == MailState.Deleted)
			{
				if (m.HasItems())
					foreach (var mailItemInfo in m.items)
					{
						Item.DeleteFromDB(trans, mailItemInfo.item_guid);
						AzeriteItem.DeleteFromDB(trans, mailItemInfo.item_guid);
						AzeriteEmpoweredItem.DeleteFromDB(trans, mailItemInfo.item_guid);
					}

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
				stmt.AddValue(0, m.messageID);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
				stmt.AddValue(0, m.messageID);
				trans.Append(stmt);
			}

		//deallocate deleted mails...
		foreach (var m in Mails.ToList())
			if (m.state == MailState.Deleted)
				_mail.Remove(m);

		MailsUpdated = false;
	}

	public void SaveGoldToDB(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_MONEY);
		stmt.AddValue(0, Money);
		stmt.AddValue(1, GUID.Counter);
		trans.Append(stmt);
	}

	public void SaveInventoryAndGoldToDB(SQLTransaction trans)
	{
		_SaveInventory(trans);
		_SaveCurrency(trans);
		SaveGoldToDB(trans);
	}

	public bool LoadFromDB(ObjectGuid guid, SQLQueryHolder<PlayerLoginQueryLoad> holder)
	{
		var result = holder.GetResult(PlayerLoginQueryLoad.From);

		if (result.IsEmpty())
		{
			Global.CharacterCacheStorage.GetCharacterNameByGuid(guid, out var cacheName);
			Log.outError(LogFilter.Player, "Player {0} {1} not found in table `characters`, can't load. ", cacheName, guid.ToString());

			return false;
		}

		var fieldIndex = 1;
		var accountId = result.Read<uint>(fieldIndex++);
		var name = result.Read<string>(fieldIndex++);
		var race = (Race)result.Read<byte>(fieldIndex++);
		var class_ = (PlayerClass)result.Read<byte>(fieldIndex++);
		var gender = (Gender)result.Read<byte>(fieldIndex++);
		var level = result.Read<byte>(fieldIndex++);
		var xp = result.Read<uint>(fieldIndex++);
		var money = result.Read<ulong>(fieldIndex++);
		var inventorySlots = result.Read<byte>(fieldIndex++);
		var bankSlots = result.Read<byte>(fieldIndex++);
		var restState = (PlayerRestState)result.Read<byte>(fieldIndex++);
		var playerFlags = (PlayerFlags)result.Read<uint>(fieldIndex++);
		var playerFlagsEx = (PlayerFlagsEx)result.Read<uint>(fieldIndex++);
		var position_x = result.Read<float>(fieldIndex++);
		var position_y = result.Read<float>(fieldIndex++);
		var position_z = result.Read<float>(fieldIndex++);
		uint mapId = result.Read<ushort>(fieldIndex++);
		var orientation = result.Read<float>(fieldIndex++);
		var taximask = result.Read<string>(fieldIndex++);
		var createTime = result.Read<long>(fieldIndex++);
		var createMode = (PlayerCreateMode)result.Read<byte>(fieldIndex++);
		var cinematic = result.Read<byte>(fieldIndex++);
		var totaltime = result.Read<uint>(fieldIndex++);
		var leveltime = result.Read<uint>(fieldIndex++);
		var rest_bonus = result.Read<float>(fieldIndex++);
		var logout_time = result.Read<long>(fieldIndex++);
		var is_logout_resting = result.Read<byte>(fieldIndex++);
		var resettalents_cost = result.Read<uint>(fieldIndex++);
		var resettalents_time = result.Read<long>(fieldIndex++);
		var primarySpecialization = result.Read<uint>(fieldIndex++);
		var trans_x = result.Read<float>(fieldIndex++);
		var trans_y = result.Read<float>(fieldIndex++);
		var trans_z = result.Read<float>(fieldIndex++);
		var trans_o = result.Read<float>(fieldIndex++);
		var transguid = result.Read<ulong>(fieldIndex++);
		var extra_flags = (PlayerExtraFlags)result.Read<ushort>(fieldIndex++);
		var summonedPetNumber = result.Read<uint>(fieldIndex++);
		var at_login = result.Read<ushort>(fieldIndex++);
		var zone = result.Read<ushort>(fieldIndex++);
		var online = result.Read<byte>(fieldIndex++);
		var death_expire_time = result.Read<long>(fieldIndex++);
		var taxi_path = result.Read<string>(fieldIndex++);
		var dungeonDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
		var totalKills = result.Read<uint>(fieldIndex++);
		var todayKills = result.Read<ushort>(fieldIndex++);
		var yesterdayKills = result.Read<ushort>(fieldIndex++);
		var chosenTitle = result.Read<uint>(fieldIndex++);
		var watchedFaction = result.Read<uint>(fieldIndex++);
		var drunk = result.Read<byte>(fieldIndex++);
		var health = result.Read<uint>(fieldIndex++);

		var powers = new uint[(int)PowerType.MaxPerClass];

		for (var i = 0; i < powers.Length; ++i)
			powers[i] = result.Read<uint>(fieldIndex++);

		var instance_id = result.Read<uint>(fieldIndex++);
		var activeTalentGroup = result.Read<byte>(fieldIndex++);
		var lootSpecId = result.Read<uint>(fieldIndex++);
		var exploredZones = result.Read<string>(fieldIndex++);
		var knownTitles = result.Read<string>(fieldIndex++);
		var actionBars = result.Read<byte>(fieldIndex++);
		var raidDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
		var legacyRaidDifficulty = (Difficulty)result.Read<byte>(fieldIndex++);
		var fishingSteps = result.Read<byte>(fieldIndex++);
		var honor = result.Read<uint>(fieldIndex++);
		var honorLevel = result.Read<uint>(fieldIndex++);
		var honorRestState = (PlayerRestState)result.Read<byte>(fieldIndex++);
		var honorRestBonus = result.Read<float>(fieldIndex++);
		var numRespecs = result.Read<byte>(fieldIndex++);


		// check if the character's account in the db and the logged in account match.
		// player should be able to load/delete character only with correct account!
		if (accountId != Session.AccountId)
		{
			Log.outError(LogFilter.Player, "Player (GUID: {0}) loading from wrong account (is: {1}, should be: {2})", GUID.ToString(), Session.AccountId, accountId);

			return false;
		}

		var banResult = holder.GetResult(PlayerLoginQueryLoad.Banned);

		if (!banResult.IsEmpty())
		{
			Log.outError(LogFilter.Player, "{0} is banned, can't load.", guid.ToString());

			return false;
		}

		Create(guid);

		SetName(name);

		// check name limitations
		if (ObjectManager.CheckPlayerName(GetName(), Session.SessionDbcLocale) != ResponseCodes.CharNameSuccess ||
			(!Session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(GetName())))
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
			stmt.AddValue(0, (ushort)AtLoginFlags.Rename);
			stmt.AddValue(1, guid.Counter);
			DB.Characters.Execute(stmt);

			return false;
		}


		SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.WowAccount), Session.AccountGUID);
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.BnetAccount), Session.BattlenetAccountGUID);

		if (gender >= Gender.None)
		{
			Log.outError(LogFilter.Player, "Player {0} has wrong gender ({1}), can't be loaded.", guid.ToString(), gender);

			return false;
		}

		Race = race;
		Class = class_;
		Gender = gender;

		// check if race/class combination is valid
		var info = Global.ObjectMgr.GetPlayerInfo(Race, Class);

		if (info == null)
		{
			Log.outError(LogFilter.Player, "Player {0} has wrong race/class ({1}/{2}), can't be loaded.", guid.ToString(), Race, Class);

			return false;
		}

		SetLevel(level);
		XP = xp;

		StringArray exploredZonesStrings = new(exploredZones, ' ');

		for (var i = 0; i < exploredZonesStrings.Length && i / 2 < ActivePlayerData.ExploredZonesSize; ++i)
			SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ExploredZones, i / 2), (ulong)((long.Parse(exploredZonesStrings[i])) << (32 * (i % 2))));

		StringArray knownTitlesStrings = new(knownTitles, ' ');

		if ((knownTitlesStrings.Length % 2) == 0)
			for (var i = 0; i < knownTitlesStrings.Length; ++i)
				SetUpdateFieldFlagValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.KnownTitles, i / 2), (ulong)((long.Parse(knownTitlesStrings[i])) << (32 * (i % 2))));

		ObjectScale = 1.0f;
		SetHoverHeight(1.0f);

		// load achievements before anything else to prevent multiple gains for the same achievement/criteria on every loading (as loading does call UpdateAchievementCriteria)
		_AchievementSys.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Achievements), holder.GetResult(PlayerLoginQueryLoad.CriteriaProgress));
		_questObjectiveCriteriaManager.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria), holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress));

		Money = Math.Min(money, PlayerConst.MaxMoneyAmount);

		List<ChrCustomizationChoice> customizations = new();
		var customizationsResult = holder.GetResult(PlayerLoginQueryLoad.Customizations);

		if (!customizationsResult.IsEmpty())
			do
			{
				ChrCustomizationChoice choice = new();
				choice.ChrCustomizationOptionID = customizationsResult.Read<uint>(0);
				choice.ChrCustomizationChoiceID = customizationsResult.Read<uint>(1);
				customizations.Add(choice);
			} while (customizationsResult.NextRow());

		SetCustomizations(customizations, false);
		SetInventorySlotCount(inventorySlots);
		SetBankBagSlotCount(bankSlots);
		NativeGender = gender;
		SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.Inebriation), drunk);
		ReplaceAllPlayerFlags(playerFlags);
		ReplaceAllPlayerFlagsEx(playerFlagsEx);
		SetWatchedFactionIndex(watchedFaction);

		LoginFlags = (AtLoginFlags)at_login;

		if (!Session.ValidateAppearance(Race, Class, gender, customizations))
		{
			Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), can't be loaded.", guid.ToString());

			return false;
		}

		// set which actionbars the client has active - DO NOT REMOVE EVER AGAIN (can be changed though, if it does change fieldwise)
		SetMultiActionBars(actionBars);

		_fishingSteps = fishingSteps;

		InitDisplayIds();

		//Need to call it to initialize m_team (m_team can be calculated from race)
		//Other way is to saves m_team into characters table.
		SetFactionForRace(Race);

		// load home bind and check in same time class/race pair, it used later for restore broken positions
		if (!_LoadHomeBind(holder.GetResult(PlayerLoginQueryLoad.HomeBind)))
			return false;

		InitializeSkillFields();
		InitPrimaryProfessions(); // to max set before any spell loaded

		// init saved position, and fix it later if problematic
		Location.Relocate(position_x, position_y, position_z, orientation);

		DungeonDifficultyId = CheckLoadedDungeonDifficultyId(dungeonDifficulty);
		RaidDifficultyId = CheckLoadedRaidDifficultyId(raidDifficulty);
		LegacyRaidDifficultyId = CheckLoadedLegacyRaidDifficultyId(legacyRaidDifficulty);

		var RelocateToHomebind = new Action(() =>
		{
			mapId = _homebind.MapId;
			instance_id = 0;
			Location.Relocate(_homebind);
		});

		_LoadGroup(holder.GetResult(PlayerLoginQueryLoad.Group));

		_LoadCurrency(holder.GetResult(PlayerLoginQueryLoad.Currency));
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LifetimeHonorableKills), totalKills);
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), todayKills);
		SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), yesterdayKills);

		_LoadInstanceTimeRestrictions(holder.GetResult(PlayerLoginQueryLoad.InstanceLockTimes));
		_LoadBGData(holder.GetResult(PlayerLoginQueryLoad.BgData));

		Session.Player = this;

		Map map = null;
		var player_at_bg = false;
		var mapEntry = CliDB.MapStorage.LookupByKey(mapId);

		if (mapEntry == null || !Location.IsPositionValid)
		{
			Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid coordinates (MapId: {1} {2}). Teleport to default race/class locations.", guid.ToString(), mapId, Location);
			RelocateToHomebind();
		}
		else if (mapEntry.IsBattlegroundOrArena())
		{
			Battleground currentBg = null;

			if (_bgData.BgInstanceId != 0) //saved in Battleground
				currentBg = Global.BattlegroundMgr.GetBattleground(_bgData.BgInstanceId, BattlegroundTypeId.None);

			player_at_bg = currentBg != null && currentBg.IsPlayerInBattleground(GUID);

			if (player_at_bg && currentBg.GetStatus() != BattlegroundStatus.WaitLeave)
			{
				map = currentBg.GetBgMap();

				var bgQueueTypeId = currentBg.GetQueueId();
				AddBattlegroundQueueId(bgQueueTypeId);

				_bgData.BgTypeId = currentBg.GetTypeID();

				//join player to Battlegroundgroup
				currentBg.EventPlayerLoggedIn(this);

				SetInviteForBattlegroundQueueType(bgQueueTypeId, currentBg.GetInstanceID());
				SetMercenaryForBattlegroundQueueType(bgQueueTypeId, currentBg.IsPlayerMercenaryInBattleground(GUID));
			}
			// Bg was not found - go to Entry Point
			else
			{
				// leave bg
				if (player_at_bg)
				{
					player_at_bg = false;
					currentBg.RemovePlayerAtLeave(GUID, false, true);
				}

				// Do not look for instance if bg not found
				var _loc = BattlegroundEntryPoint;
				mapId = _loc.MapId;
				instance_id = 0;

				if (mapId == 0xFFFFFFFF) // BattlegroundEntry Point not found (???)
				{
					Log.outError(LogFilter.Player, "Player (guidlow {0}) was in BG in database, but BG was not found, and entry point was invalid! Teleport to default race/class locations.", guid.ToString());
					RelocateToHomebind();
				}
				else
				{
					Location.Relocate(_loc);
				}

				// We are not in BG anymore
				_bgData.BgInstanceId = 0;
			}
		}
		// currently we do not support transport in bg
		else if (transguid != 0)
		{
			var transGUID = ObjectGuid.Create(HighGuid.Transport, transguid);

			Transport transport = null;
			var transportMap = Global.MapMgr.CreateMap(mapId, this);

			if (transportMap != null)
			{
				var transportOnMap = transportMap.GetTransport(transGUID);

				if (transportOnMap != null)
				{
					if (transportOnMap.GetExpectedMapId() != mapId)
					{
						mapId = transportOnMap.GetExpectedMapId();
						InstanceId = 0;
						transportMap = Global.MapMgr.CreateMap(mapId, this);

						if (transportMap)
							transport = transportMap.GetTransport(transGUID);
					}
					else
					{
						transport = transportOnMap;
					}
				}
			}

			if (transport)
			{
				var pos = new Position(trans_x, trans_y, trans_z, trans_o);

				MovementInfo.Transport.Pos = pos.Copy();
				transport.CalculatePassengerPosition(pos);

				if (!GridDefines.IsValidMapCoord(pos) ||
					// transport size limited
					Math.Abs(MovementInfo.Transport.Pos.X) > 250.0f ||
					Math.Abs(MovementInfo.Transport.Pos.Y) > 250.0f ||
					Math.Abs(MovementInfo.Transport.Pos.Z) > 250.0f)
				{
					Log.outError(LogFilter.Player, "Player (guidlow {0}) have invalid transport coordinates ({1}). Teleport to bind location.", guid.ToString(), pos.ToString());

					MovementInfo.Transport.Reset();
					RelocateToHomebind();
				}
				else
				{
					Location.Relocate(pos);
					mapId = transport.Location.MapId;

					transport.AddPassenger(this);
				}
			}
			else
			{
				Log.outError(LogFilter.Player, "Player (guidlow {0}) have problems with transport guid ({1}). Teleport to bind location.", guid.ToString(), transguid);

				RelocateToHomebind();
			}
		}
		// currently we do not support taxi in instance
		else if (!taxi_path.IsEmpty())
		{
			instance_id = 0;

			// Not finish taxi flight path
			if (_bgData.HasTaxiPath())
				for (var i = 0; i < 2; ++i)
					Taxi.AddTaxiDestination(_bgData.TaxiPath[i]);

			if (!Taxi.LoadTaxiDestinationsFromString(taxi_path, Team))
			{
				// problems with taxi path loading
				TaxiNodesRecord nodeEntry = null;
				var node_id = Taxi.GetTaxiSource();

				if (node_id != 0)
					nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(node_id);

				if (nodeEntry == null) // don't know taxi start node, to homebind
				{
					Log.outError(LogFilter.Player, "Character {0} have wrong data in taxi destination list, teleport to homebind.", GUID.ToString());
					RelocateToHomebind();
				}
				else // have start node, to it
				{
					Log.outError(LogFilter.Player, "Character {0} have too short taxi destination list, teleport to original node.", GUID.ToString());
					mapId = nodeEntry.ContinentID;
					Location.Relocate(nodeEntry.Pos.X, nodeEntry.Pos.Y, nodeEntry.Pos.Z, 0.0f);
				}

				Taxi.ClearTaxiDestinations();
			}

			var nodeid = Taxi.GetTaxiSource();

			if (nodeid != 0)
			{
				// save source node as recall coord to prevent recall and fall from sky
				var nodeEntry = CliDB.TaxiNodesStorage.LookupByKey(nodeid);

				if (nodeEntry != null && nodeEntry.ContinentID == Location.MapId)
				{
					Cypher.Assert(nodeEntry != null); // checked in m_taxi.LoadTaxiDestinationsFromString
					mapId = nodeEntry.ContinentID;
					Location.Relocate(nodeEntry.Pos.X, nodeEntry.Pos.Y, nodeEntry.Pos.Z, 0.0f);
				}

				// flight will started later
			}
		}
		else if (mapEntry.IsDungeon() && InstanceId != 0)
		{
			// try finding instance by id first
			map = Global.MapMgr.FindMap(mapId, InstanceId);
		}

		// Map could be changed before
		mapEntry = CliDB.MapStorage.LookupByKey(mapId);

		// client without expansion support
		if (mapEntry != null)
			if (Session.Expansion < mapEntry.Expansion())
			{
				Log.outDebug(LogFilter.Player, "Player {0} using client without required expansion tried login at non accessible map {1}", GetName(), mapId);
				RelocateToHomebind();
			}

		// NOW player must have valid map
		// load the player's map here if it's not already loaded
		if (!map)
			map = Global.MapMgr.CreateMap(mapId, this);

		AreaTriggerStruct areaTrigger = null;
		var check = false;

		if (!map)
		{
			areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
			check = true;
		}
		else if (map.IsDungeon) // if map is dungeon...
		{
			var denyReason = map.CannotEnter(this); // ... and can't enter map, then look for entry point.

			if (denyReason != null)
			{
				SendTransferAborted(map.Id, denyReason.Reason, denyReason.Arg, denyReason.MapDifficultyXConditionId);
				areaTrigger = Global.ObjectMgr.GetGoBackTrigger(mapId);
				check = true;
			}
			else if (instance_id != 0 && Global.InstanceLockMgr.FindActiveInstanceLock(guid, new MapDb2Entries(mapId, map.DifficultyID)) != null) // ... and instance is reseted then look for entrance.
			{
				areaTrigger = Global.ObjectMgr.GetMapEntranceTrigger(mapId);
				check = true;
			}
		}

		if (check)                   // in case of special event when creating map...
			if (areaTrigger != null) // ... if we have an areatrigger, then relocate to new map/coordinates.
			{
				Location.Relocate(areaTrigger.target_X, areaTrigger.target_Y, areaTrigger.target_Z, Location.Orientation);

				if (mapId != areaTrigger.target_mapId)
				{
					mapId = areaTrigger.target_mapId;
					map = Global.MapMgr.CreateMap(mapId, this);
				}
			}

		if (!map)
		{
			RelocateToHomebind();
			map = Global.MapMgr.CreateMap(mapId, this);

			if (!map)
			{
				Log.outError(LogFilter.Player, "Player {0} {1} Map: {2}, {3}. Invalid default map coordinates or instance couldn't be created.", GetName(), guid.ToString(), mapId, Location);

				return false;
			}
		}

		Map = map;
		UpdatePositionData();

		// now that map position is determined, check instance validity
		if (!CheckInstanceValidity(true) && !IsInstanceLoginGameMasterException())
			InstanceValid = false;

		if (player_at_bg)
			map.ToBattlegroundMap.GetBG().AddPlayer(this);

		// randomize first save time in range [CONFIG_INTERVAL_SAVE] around [CONFIG_INTERVAL_SAVE]
		// this must help in case next save after mass player load after server startup
		_nextSave = RandomHelper.URand(_nextSave / 2, _nextSave * 3 / 2);

		SaveRecallPosition();

		var now = GameTime.GetGameTime();
		var logoutTime = logout_time;

		SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.LogoutTime), logoutTime);

		// since last logout (in seconds)
		var time_diff = (uint)(now - logoutTime);

		// set value, including drunk invisibility detection
		// calculate sobering. after 15 minutes logged out, the player will be sober again
		if (time_diff < (uint)DrunkValue * 9)
			SetDrunkValue((byte)(DrunkValue - time_diff / 9));
		else
			SetDrunkValue(0);

		_createTime = createTime;
		_createMode = createMode;
		_cinematic = cinematic;
		_playedTimeTotal = totaltime;
		_playedTimeLevel = leveltime;

		SetTalentResetCost(resettalents_cost);
		SetTalentResetTime(resettalents_time);

		Taxi.LoadTaxiMask(taximask); // must be before InitTaxiNodesForLevel

		_LoadPetStable(summonedPetNumber, holder.GetResult(PlayerLoginQueryLoad.PetSlots));

		// Honor system
		// Update Honor kills data
		_lastHonorUpdateTime = logoutTime;
		UpdateHonorFields();

		_deathExpireTime = death_expire_time;

		if (_deathExpireTime > now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep)
			_deathExpireTime = now + PlayerConst.MaxDeathCount * PlayerConst.DeathExpireStep - 1;

		RemoveUnitFlag2(UnitFlags2.ForceMovement);

		// make sure the unit is considered out of combat for proper loading
		ClearInCombat();

		// reset stats before loading any modifiers
		InitStatsForLevel();
		InitTaxiNodesForLevel();
		InitRunes();

		// rest bonus can only be calculated after InitStatsForLevel()
		_restMgr.LoadRestBonus(RestTypes.XP, restState, rest_bonus);

		// load skills after InitStatsForLevel because it triggering aura apply also
		_LoadSkills(holder.GetResult(PlayerLoginQueryLoad.Skills));
		UpdateSkillsForLevel();

		SetNumRespecs(numRespecs);
		SetPrimarySpecialization(primarySpecialization);
		SetActiveTalentGroup(activeTalentGroup);
		var primarySpec = CliDB.ChrSpecializationStorage.LookupByKey(GetPrimarySpecialization());

		if (primarySpec == null || primarySpec.ClassID != (byte)Class || GetActiveTalentGroup() >= PlayerConst.MaxSpecializations)
			ResetTalentSpecialization();

		var chrSpec = CliDB.ChrSpecializationStorage.LookupByKey(lootSpecId);

		if (chrSpec != null)
			if (chrSpec.ClassID == (uint)Class)
				SetLootSpecId(lootSpecId);

		UpdateDisplayPower();
		_LoadTalents(holder.GetResult(PlayerLoginQueryLoad.Talents));
		_LoadPvpTalents(holder.GetResult(PlayerLoginQueryLoad.PvpTalents));
		_LoadSpells(holder.GetResult(PlayerLoginQueryLoad.Spells), holder.GetResult(PlayerLoginQueryLoad.SpellFavorites));
		Session.CollectionMgr.LoadToys();
		Session.CollectionMgr.LoadHeirlooms();
		Session.CollectionMgr.LoadMounts();
		Session.CollectionMgr.LoadItemAppearances();
		Session.CollectionMgr.LoadTransmogIllusions();

		LearnSpecializationSpells();

		_LoadGlyphs(holder.GetResult(PlayerLoginQueryLoad.Glyphs));
		_LoadAuras(holder.GetResult(PlayerLoginQueryLoad.Auras), holder.GetResult(PlayerLoginQueryLoad.AuraEffects), time_diff);
		_LoadGlyphAuras();

		// add ghost flag (must be after aura load: PLAYER_FLAGS_GHOST set in aura)
		if (HasPlayerFlag(PlayerFlags.Ghost))
			DeathState = DeathState.Dead;

		// Load spell locations - must be after loading auras
		_LoadStoredAuraTeleportLocations(holder.GetResult(PlayerLoginQueryLoad.AuraStoredLocations));

		// after spell load, learn rewarded spell if need also
		_LoadQuestStatus(holder.GetResult(PlayerLoginQueryLoad.QuestStatus));
		_LoadQuestStatusObjectives(holder.GetResult(PlayerLoginQueryLoad.QuestStatusObjectives));
		_LoadQuestStatusRewarded(holder.GetResult(PlayerLoginQueryLoad.QuestStatusRew));
		_LoadDailyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.DailyQuestStatus));
		_LoadWeeklyQuestStatus(holder.GetResult(PlayerLoginQueryLoad.WeeklyQuestStatus));
		_LoadSeasonalQuestStatus(holder.GetResult(PlayerLoginQueryLoad.SeasonalQuestStatus));
		_LoadRandomBGStatus(holder.GetResult(PlayerLoginQueryLoad.RandomBg));

		// after spell and quest load
		InitTalentForLevel();
		LearnDefaultSkills();
		LearnCustomSpells();

		_LoadTraits(holder.GetResult(PlayerLoginQueryLoad.TraitConfigs), holder.GetResult(PlayerLoginQueryLoad.TraitEntries)); // must be after loading spells

		// must be before inventory (some items required reputation check)
		_reputationMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Reputation));

		_LoadInventory(holder.GetResult(PlayerLoginQueryLoad.Inventory),
						holder.GetResult(PlayerLoginQueryLoad.Artifacts),
						holder.GetResult(PlayerLoginQueryLoad.Azerite),
						holder.GetResult(PlayerLoginQueryLoad.AzeriteMilestonePowers),
						holder.GetResult(PlayerLoginQueryLoad.AzeriteUnlockedEssences),
						holder.GetResult(PlayerLoginQueryLoad.AzeriteEmpowered),
						time_diff);

		if (IsVoidStorageUnlocked())
			_LoadVoidStorage(holder.GetResult(PlayerLoginQueryLoad.VoidStorage));

		// update items with duration and realtime
		UpdateItemDuration(time_diff, true);

		StartLoadingActionButtons();

		// unread mails and next delivery time, actual mails not loaded
		_LoadMail(holder.GetResult(PlayerLoginQueryLoad.Mails),
				holder.GetResult(PlayerLoginQueryLoad.MailItems),
				holder.GetResult(PlayerLoginQueryLoad.MailItemsArtifact),
				holder.GetResult(PlayerLoginQueryLoad.MailItemsAzerite),
				holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteMilestonePower),
				holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteUnlockedEssence),
				holder.GetResult(PlayerLoginQueryLoad.MailItemsAzeriteEmpowered));

		_social = Global.SocialMgr.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.SocialList), GUID);

		// check PLAYER_CHOSEN_TITLE compatibility with PLAYER__FIELD_KNOWN_TITLES
		// note: PLAYER__FIELD_KNOWN_TITLES updated at quest status loaded
		if (chosenTitle != 0 && !HasTitle(chosenTitle))
			chosenTitle = 0;

		SetChosenTitle(chosenTitle);

		// has to be called after last Relocate() in Player.LoadFromDB
		SetFallInformation(0, Location.Z);

		SpellHistory.LoadFromDb<Player>(holder.GetResult(PlayerLoginQueryLoad.SpellCooldowns), holder.GetResult(PlayerLoginQueryLoad.SpellCharges));

		var savedHealth = health;

		if (savedHealth == 0)
			DeathState = DeathState.Corpse;

		// Spell code allow apply any auras to dead character in load time in aura/spell/item loading
		// Do now before stats re-calculation cleanup for ghost state unexpected auras
		if (!IsAlive)
			RemoveAllAurasOnDeath();
		else
			RemoveAllAurasRequiringDeadTarget();

		//apply all stat bonuses from items and auras
		SetCanModifyStats(true);
		UpdateAllStats();

		// restore remembered power/health values (but not more max values)
		SetHealth(savedHealth > MaxHealth ? MaxHealth : savedHealth);
		var loadedPowers = 0;

		for (PowerType i = 0; i < PowerType.Max; ++i)
			if (Global.DB2Mgr.GetPowerIndexByClass(i, Class) != (int)PowerType.Max)
			{
				var savedPower = powers[loadedPowers];
				var maxPower = UnitData.MaxPower[loadedPowers];
				SetPower(i, (int)(savedPower > maxPower ? maxPower : savedPower));

				if (++loadedPowers >= (int)PowerType.MaxPerClass)
					break;
			}

		for (; loadedPowers < (int)PowerType.MaxPerClass; ++loadedPowers)
			SetUpdateFieldValue(ref Values.ModifyValue(UnitData).ModifyValue(UnitData.Power, loadedPowers), 0);

		SetPower(PowerType.LunarPower, 0);

		// Init rune recharge
		if (GetPowerIndex(PowerType.Runes) != (int)PowerType.Max)
		{
			var runes = GetPower(PowerType.Runes);
			var maxRunes = GetMaxPower(PowerType.Runes);
			var runeCooldown = GetRuneBaseCooldown();

			while (runes < maxRunes)
			{
				SetRuneCooldown((byte)runes, runeCooldown);
				++runes;
			}
		}

		Log.outDebug(LogFilter.Player, "The value of player {0} after load item and aura is: ", GetName());

		// GM state
		if (Session.HasPermission(RBACPermissions.RestoreSavedGmState))
		{
			switch (WorldConfig.GetIntValue(WorldCfg.GmLoginState))
			{
				default:
				case 0:
					break; // disable
				case 1:
					SetGameMaster(true);

					break; // enable
				case 2:    // save state
					if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMOn))
						SetGameMaster(true);

					break;
			}

			switch (WorldConfig.GetIntValue(WorldCfg.GmVisibleState))
			{
				default:
				case 0:
					SetGMVisible(false);

					break; // invisible
				case 1:
					break; // visible
				case 2:    // save state
					if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMInvisible))
						SetGMVisible(false);

					break;
			}

			switch (WorldConfig.GetIntValue(WorldCfg.GmChat))
			{
				default:
				case 0:
					break; // disable
				case 1:
					SetGMChat(true);

					break; // enable
				case 2:    // save state
					if (extra_flags.HasAnyFlag(PlayerExtraFlags.GMChat))
						SetGMChat(true);

					break;
			}

			switch (WorldConfig.GetIntValue(WorldCfg.GmWhisperingTo))
			{
				default:
				case 0:
					break; // disable
				case 1:
					SetAcceptWhispers(true);

					break; // enable
				case 2:    // save state
					if (extra_flags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers))
						SetAcceptWhispers(true);

					break;
			}
		}

		InitPvP();

		// RaF stuff.
		if (Session.IsARecruiter || (Session.RecruiterId != 0))
			SetDynamicFlag(UnitDynFlags.ReferAFriend);

		_LoadDeclinedNames(holder.GetResult(PlayerLoginQueryLoad.DeclinedNames));

		_LoadEquipmentSets(holder.GetResult(PlayerLoginQueryLoad.EquipmentSets));
		_LoadTransmogOutfits(holder.GetResult(PlayerLoginQueryLoad.TransmogOutfits));

		_LoadCUFProfiles(holder.GetResult(PlayerLoginQueryLoad.CufProfiles));

		var garrison = new Garrison(this);

		if (garrison.LoadFromDB(holder.GetResult(PlayerLoginQueryLoad.Garrison),
								holder.GetResult(PlayerLoginQueryLoad.GarrisonBlueprints),
								holder.GetResult(PlayerLoginQueryLoad.GarrisonBuildings),
								holder.GetResult(PlayerLoginQueryLoad.GarrisonFollowers),
								holder.GetResult(PlayerLoginQueryLoad.GarrisonFollowerAbilities)))
			_garrison = garrison;

		_InitHonorLevelOnLoadFromDB(honor, honorLevel);

		_restMgr.LoadRestBonus(RestTypes.Honor, honorRestState, honorRestBonus);

		if (time_diff > 0)
		{
			//speed collect rest bonus in offline, in logout, far from tavern, city (section/in hour)
			var bubble0 = 0.031f;
			//speed collect rest bonus in offline, in logout, in tavern, city (section/in hour)
			var bubble1 = 0.125f;

			var bubble = is_logout_resting > 0
							? bubble1 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInTavernOrCity)
							: bubble0 * WorldConfig.GetFloatValue(WorldCfg.RateRestOfflineInWilderness);

			_restMgr.AddRestBonus(RestTypes.XP, time_diff * _restMgr.CalcExtraPerSec(RestTypes.XP, bubble));
		}

		// Unlock battle pet system if it's enabled in bnet account
		if (Session.BattlePetMgr.IsBattlePetSystemEnabled)
			LearnSpell(SharedConst.SpellBattlePetTraining, false);

		_AchievementSys.CheckAllAchievementCriteria(this);
		_questObjectiveCriteriaManager.CheckAllQuestObjectiveCriteria(this);

		PushQuests();

		foreach (var transmogIllusion in CliDB.TransmogIllusionStorage.Values)
		{
			if (!transmogIllusion.GetFlags().HasFlag(TransmogIllusionFlags.PlayerConditionGrantsOnLogin))
				continue;

			if (Session.CollectionMgr.HasTransmogIllusion(transmogIllusion.Id))
				continue;

			var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(transmogIllusion.UnlockConditionID);

			if (playerCondition != null)
				if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
					continue;

			Session.CollectionMgr.AddTransmogIllusion(transmogIllusion.Id);
		}

		return true;
	}

	public void SaveToDB(bool create = false)
	{
		SQLTransaction loginTransaction = new();
		SQLTransaction characterTransaction = new();

		SaveToDB(loginTransaction, characterTransaction, create);

		DB.Characters.CommitTransaction(characterTransaction);
		DB.Login.CommitTransaction(loginTransaction);
	}

	public void SaveToDB(SQLTransaction loginTransaction, SQLTransaction characterTransaction, bool create = false)
	{
		// delay auto save at any saves (manual, in code, or autosave)
		_nextSave = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);

		//lets allow only players in world to be saved
		if (IsBeingTeleportedFar)
		{
			ScheduleDelayedOperation(PlayerDelayedOperations.SavePlayer);

			return;
		}

		// first save/honor gain after midnight will also update the player's honor fields
		UpdateHonorFields();

		Log.outDebug(LogFilter.Player, $"Player::SaveToDB: The value of player {GetName()} at save: ");

		if (!create)
			Global.ScriptMgr.ForEach<IPlayerOnSave>(p => p.OnSave(this));

		PreparedStatement stmt;
		byte index = 0;

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
		stmt.AddValue(0, GUID.Counter);
		characterTransaction.Append(stmt);

		static float finiteAlways(float f)
		{
			return float.IsFinite(f) ? f : 0.0f;
		}

		;

		if (create)
		{
			//! Insert query
			/// @todo: Filter out more redundant fields that can take their default value at player create
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER);
			stmt.AddValue(index++, GUID.Counter);
			stmt.AddValue(index++, Session.AccountId);
			stmt.AddValue(index++, GetName());
			stmt.AddValue(index++, (byte)Race);
			stmt.AddValue(index++, (byte)Class);
			stmt.AddValue(index++, (byte)NativeGender); // save gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
			stmt.AddValue(index++, Level);
			stmt.AddValue(index++, XP);
			stmt.AddValue(index++, Money);
			stmt.AddValue(index++, GetInventorySlotCount());
			stmt.AddValue(index++, GetBankBagSlotCount());
			stmt.AddValue(index++, ActivePlayerData.RestInfo[(int)RestTypes.XP].StateID);
			stmt.AddValue(index++, PlayerData.PlayerFlags);
			stmt.AddValue(index++, PlayerData.PlayerFlagsEx);
			stmt.AddValue(index++, (ushort)Location.MapId);
			stmt.AddValue(index++, InstanceId);
			stmt.AddValue(index++, (byte)DungeonDifficultyId);
			stmt.AddValue(index++, (byte)RaidDifficultyId);
			stmt.AddValue(index++, (byte)LegacyRaidDifficultyId);
			stmt.AddValue(index++, finiteAlways(Location.X));
			stmt.AddValue(index++, finiteAlways(Location.Y));
			stmt.AddValue(index++, finiteAlways(Location.Z));
			stmt.AddValue(index++, finiteAlways(Location.Orientation));
			stmt.AddValue(index++, finiteAlways(TransOffsetX));
			stmt.AddValue(index++, finiteAlways(TransOffsetY));
			stmt.AddValue(index++, finiteAlways(TransOffsetZ));
			stmt.AddValue(index++, finiteAlways(TransOffsetO));
			ulong transLowGUID = 0;
			var transport = GetTransport<Transport>();

			if (transport != null)
				transLowGUID = transport.GUID.Counter;

			stmt.AddValue(index++, transLowGUID);

			StringBuilder ss = new();

			lock (Taxi.TaxiLock)
			{
				for (var i = 0; i < Taxi.Taximask.Length; ++i)
					ss.Append(Taxi.Taximask[i] + " ");
			}

			stmt.AddValue(index++, ss.ToString());
			stmt.AddValue(index++, _createTime);
			stmt.AddValue(index++, (byte)_createMode);
			stmt.AddValue(index++, _cinematic);
			stmt.AddValue(index++, _playedTimeTotal);
			stmt.AddValue(index++, _playedTimeLevel);
			stmt.AddValue(index++, finiteAlways((float)_restMgr.GetRestBonus(RestTypes.XP)));
			stmt.AddValue(index++, GameTime.GetGameTime());
			stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
			//save, far from tavern/city
			//save, but in tavern/city
			stmt.AddValue(index++, GetTalentResetCost());
			stmt.AddValue(index++, GetTalentResetTime());
			stmt.AddValue(index++, GetPrimarySpecialization());
			stmt.AddValue(index++, (ushort)_extraFlags);
			stmt.AddValue(index++, 0); // summonedPetNumber
			stmt.AddValue(index++, (ushort)LoginFlags);
			stmt.AddValue(index++, _deathExpireTime);

			ss.Clear();
			ss.Append(Taxi.SaveTaxiDestinationsToString());

			stmt.AddValue(index++, ss.ToString());
			stmt.AddValue(index++, ActivePlayerData.LifetimeHonorableKills);
			stmt.AddValue(index++, ActivePlayerData.TodayHonorableKills);
			stmt.AddValue(index++, ActivePlayerData.YesterdayHonorableKills);
			stmt.AddValue(index++, PlayerData.PlayerTitle);
			stmt.AddValue(index++, ActivePlayerData.WatchedFactionIndex);
			stmt.AddValue(index++, DrunkValue);
			stmt.AddValue(index++, Health);

			var storedPowers = 0;

			for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
				if (GetPowerIndex(powerType) != (int)PowerType.Max)
				{
					stmt.AddValue(index++, UnitData.Power[storedPowers]);

					if (++storedPowers >= (int)PowerType.MaxPerClass)
						break;
				}

			for (; storedPowers < (int)PowerType.MaxPerClass; ++storedPowers)
				stmt.AddValue(index++, 0);

			stmt.AddValue(index++, Session.Latency);
			stmt.AddValue(index++, GetActiveTalentGroup());
			stmt.AddValue(index++, GetLootSpecId());

			ss.Clear();

			for (var i = 0; i < PlayerConst.ExploredZonesSize; ++i)
				ss.Append($"{(uint)(ActivePlayerData.ExploredZones[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.ExploredZones[i] >> 32) & 0xFFFFFFFF)} ");

			stmt.AddValue(index++, ss.ToString());

			ss.Clear();

			// cache equipment...
			for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
				{
					ss.Append($"{(uint)item.Template.InventoryType} {item.GetDisplayId(this)} ");
					var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));

					if (enchant != null)
						ss.Append(enchant.ItemVisual);
					else
						ss.Append('0');

					ss.Append($" {(uint)CliDB.ItemStorage.LookupByKey(item.GetVisibleEntry(this)).SubclassID} {(uint)item.GetVisibleSecondaryModifiedAppearanceId(this)} ");
				}
				else
				{
					ss.Append("0 0 0 0 0 ");
				}
			}

			stmt.AddValue(index++, ss.ToString());

			ss.Clear();

			for (var i = 0; i < ActivePlayerData.KnownTitles.Size(); ++i)
				ss.Append($"{(uint)(ActivePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

			stmt.AddValue(index++, ss.ToString());

			stmt.AddValue(index++, ActivePlayerData.MultiActionBars);
			stmt.AddValue(index++, Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.Realm.Build));
		}
		else
		{
			// Update query
			stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER);
			stmt.AddValue(index++, GetName());
			stmt.AddValue(index++, (byte)Race);
			stmt.AddValue(index++, (byte)Class);
			stmt.AddValue(index++, (byte)NativeGender); // save gender from PLAYER_BYTES_3, UNIT_BYTES_0 changes with every transform effect
			stmt.AddValue(index++, Level);
			stmt.AddValue(index++, XP);
			stmt.AddValue(index++, Money);
			stmt.AddValue(index++, GetInventorySlotCount());
			stmt.AddValue(index++, GetBankBagSlotCount());
			stmt.AddValue(index++, ActivePlayerData.RestInfo[(int)RestTypes.XP].StateID);
			stmt.AddValue(index++, PlayerData.PlayerFlags);
			stmt.AddValue(index++, PlayerData.PlayerFlagsEx);

			if (!IsBeingTeleported)
			{
				stmt.AddValue(index++, (ushort)Location.MapId);
				stmt.AddValue(index++, InstanceId);
				stmt.AddValue(index++, (byte)DungeonDifficultyId);
				stmt.AddValue(index++, (byte)RaidDifficultyId);
				stmt.AddValue(index++, (byte)LegacyRaidDifficultyId);
				stmt.AddValue(index++, finiteAlways(Location.X));
				stmt.AddValue(index++, finiteAlways(Location.Y));
				stmt.AddValue(index++, finiteAlways(Location.Z));
				stmt.AddValue(index++, finiteAlways(Location.Orientation));
			}
			else
			{
				stmt.AddValue(index++, (ushort)TeleportDest.MapId);
				stmt.AddValue(index++, 0);
				stmt.AddValue(index++, (byte)DungeonDifficultyId);
				stmt.AddValue(index++, (byte)RaidDifficultyId);
				stmt.AddValue(index++, (byte)LegacyRaidDifficultyId);
				stmt.AddValue(index++, finiteAlways(TeleportDest.X));
				stmt.AddValue(index++, finiteAlways(TeleportDest.Y));
				stmt.AddValue(index++, finiteAlways(TeleportDest.Z));
				stmt.AddValue(index++, finiteAlways(TeleportDest.Orientation));
			}

			stmt.AddValue(index++, finiteAlways(TransOffsetX));
			stmt.AddValue(index++, finiteAlways(TransOffsetY));
			stmt.AddValue(index++, finiteAlways(TransOffsetZ));
			stmt.AddValue(index++, finiteAlways(TransOffsetO));
			ulong transLowGUID = 0;
			var transport = GetTransport<Transport>();

			if (transport != null)
				transLowGUID = transport.GUID.Counter;

			stmt.AddValue(index++, transLowGUID);

			StringBuilder ss = new();

			lock (Taxi.TaxiLock)
			{
				for (var i = 0; i < Taxi.Taximask.Length; ++i)
					ss.Append(Taxi.Taximask[i] + " ");
			}

			stmt.AddValue(index++, ss.ToString());
			stmt.AddValue(index++, _cinematic);
			stmt.AddValue(index++, _playedTimeTotal);
			stmt.AddValue(index++, _playedTimeLevel);
			stmt.AddValue(index++, finiteAlways((float)_restMgr.GetRestBonus(RestTypes.XP)));
			stmt.AddValue(index++, GameTime.GetGameTime());
			stmt.AddValue(index++, (HasPlayerFlag(PlayerFlags.Resting) ? 1 : 0));
			//save, far from tavern/city
			//save, but in tavern/city
			stmt.AddValue(index++, GetTalentResetCost());
			stmt.AddValue(index++, GetTalentResetTime());
			stmt.AddValue(index++, NumRespecs);
			stmt.AddValue(index++, GetPrimarySpecialization());
			stmt.AddValue(index++, (ushort)_extraFlags);
			var petStable = PetStable1;

			if (petStable != null)
				stmt.AddValue(index++, petStable.GetCurrentPet() != null && petStable.GetCurrentPet().Health > 0 ? petStable.GetCurrentPet().PetNumber : 0); // summonedPetNumber
			else
				stmt.AddValue(index++, 0); // summonedPetNumber

			stmt.AddValue(index++, (ushort)LoginFlags);
			stmt.AddValue(index++, Zone);
			stmt.AddValue(index++, _deathExpireTime);

			ss.Clear();
			ss.Append(Taxi.SaveTaxiDestinationsToString());

			stmt.AddValue(index++, ss.ToString());
			stmt.AddValue(index++, ActivePlayerData.LifetimeHonorableKills);
			stmt.AddValue(index++, ActivePlayerData.TodayHonorableKills);
			stmt.AddValue(index++, ActivePlayerData.YesterdayHonorableKills);
			stmt.AddValue(index++, PlayerData.PlayerTitle);
			stmt.AddValue(index++, ActivePlayerData.WatchedFactionIndex);
			stmt.AddValue(index++, DrunkValue);
			stmt.AddValue(index++, Health);

			var storedPowers = 0;

			for (PowerType powerType = 0; powerType < PowerType.Max; ++powerType)
				if (GetPowerIndex(powerType) != (int)PowerType.Max)
				{
					stmt.AddValue(index++, UnitData.Power[storedPowers]);

					if (++storedPowers >= (int)PowerType.MaxPerClass)
						break;
				}

			for (; storedPowers < (int)PowerType.MaxPerClass; ++storedPowers)
				stmt.AddValue(index++, 0);

			stmt.AddValue(index++, Session.Latency);
			stmt.AddValue(index++, GetActiveTalentGroup());
			stmt.AddValue(index++, GetLootSpecId());

			ss.Clear();

			for (var i = 0; i < PlayerConst.ExploredZonesSize; ++i)
				ss.Append($"{(uint)(ActivePlayerData.ExploredZones[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.ExploredZones[i] >> 32) & 0xFFFFFFFF)} ");

			stmt.AddValue(index++, ss.ToString());

			ss.Clear();

			// cache equipment...
			for (byte i = 0; i < InventorySlots.ReagentBagEnd; ++i)
			{
				var item = GetItemByPos(InventorySlots.Bag0, i);

				if (item != null)
				{
					ss.Append($"{(uint)item.Template.InventoryType} {item.GetDisplayId(this)} ");
					var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetVisibleEnchantmentId(this));

					if (enchant != null)
						ss.Append(enchant.ItemVisual);
					else
						ss.Append('0');

					ss.Append($" {(uint)CliDB.ItemStorage.LookupByKey(item.GetVisibleEntry(this)).SubclassID} {(uint)item.GetVisibleSecondaryModifiedAppearanceId(this)} ");
				}
				else
				{
					ss.Append("0 0 0 0 0 ");
				}
			}

			stmt.AddValue(index++, ss.ToString());

			ss.Clear();

			for (var i = 0; i < ActivePlayerData.KnownTitles.Size(); ++i)
				ss.Append($"{(uint)(ActivePlayerData.KnownTitles[i] & 0xFFFFFFFF)} {(uint)((ActivePlayerData.KnownTitles[i] >> 32) & 0xFFFFFFFF)} ");

			stmt.AddValue(index++, ss.ToString());
			stmt.AddValue(index++, ActivePlayerData.MultiActionBars);

			stmt.AddValue(index++, IsInWorld && !Session.PlayerLogout ? 1 : 0);
			stmt.AddValue(index++, ActivePlayerData.Honor);
			stmt.AddValue(index++, HonorLevel);
			stmt.AddValue(index++, ActivePlayerData.RestInfo[(int)RestTypes.Honor].StateID);
			stmt.AddValue(index++, finiteAlways((float)_restMgr.GetRestBonus(RestTypes.Honor)));
			stmt.AddValue(index++, Global.RealmMgr.GetMinorMajorBugfixVersionForBuild(Global.WorldMgr.Realm.Build));

			// Index
			stmt.AddValue(index, GUID.Counter);
		}

		characterTransaction.Append(stmt);

		if (_fishingSteps != 0)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_FISHINGSTEPS);
			index = 0;
			stmt.AddValue(index++, GUID.Counter);
			stmt.AddValue(index++, _fishingSteps);
			characterTransaction.Append(stmt);
		}

		if (MailsUpdated) //save mails only when needed
			_SaveMail(characterTransaction);

		_SaveCustomizations(characterTransaction);
		_SaveBGData(characterTransaction);
		_SaveInventory(characterTransaction);
		_SaveVoidStorage(characterTransaction);
		_SaveQuestStatus(characterTransaction);
		_SaveDailyQuestStatus(characterTransaction);
		_SaveWeeklyQuestStatus(characterTransaction);
		_SaveSeasonalQuestStatus(characterTransaction);
		_SaveMonthlyQuestStatus(characterTransaction);
		_SaveGlyphs(characterTransaction);
		_SaveTalents(characterTransaction);
		_SaveTraits(characterTransaction);
		_SaveSpells(characterTransaction);
		SpellHistory.SaveToDb<Player>(characterTransaction);
		_SaveActions(characterTransaction);
		_SaveAuras(characterTransaction);
		_SaveSkills(characterTransaction);
		_SaveStoredAuraTeleportLocations(characterTransaction);
		_AchievementSys.SaveToDB(characterTransaction);
		_reputationMgr.SaveToDB(characterTransaction);
		_questObjectiveCriteriaManager.SaveToDB(characterTransaction);
		_SaveEquipmentSets(characterTransaction);
		Session.SaveTutorialsData(characterTransaction); // changed only while character in game
		_SaveInstanceTimeRestrictions(characterTransaction);
		_SaveCurrency(characterTransaction);
		_SaveCUFProfiles(characterTransaction);

		if (_garrison != null)
			_garrison.SaveToDB(characterTransaction);

		// check if stats should only be saved on logout
		// save stats can be out of transaction
		if (Session.IsLogingOut || !WorldConfig.GetBoolValue(WorldCfg.StatsSaveOnlyOnLogout))
			_SaveStats(characterTransaction);

		// TODO: Move this out
		Session.
			// TODO: Move this out
			CollectionMgr.SaveAccountToys(loginTransaction);

		Session.BattlePetMgr.SaveToDB(loginTransaction);
		Session.CollectionMgr.SaveAccountHeirlooms(loginTransaction);
		Session.CollectionMgr.SaveAccountMounts(loginTransaction);
		Session.CollectionMgr.SaveAccountItemAppearances(loginTransaction);
		Session.CollectionMgr.SaveAccountTransmogIllusions(loginTransaction);

		stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BNET_LAST_PLAYER_CHARACTERS);
		stmt.AddValue(0, Session.AccountId);
		stmt.AddValue(1, Global.WorldMgr.RealmId.Region);
		stmt.AddValue(2, Global.WorldMgr.RealmId.Site);
		loginTransaction.Append(stmt);

		stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_LAST_PLAYER_CHARACTERS);
		stmt.AddValue(0, Session.AccountId);
		stmt.AddValue(1, Global.WorldMgr.RealmId.Region);
		stmt.AddValue(2, Global.WorldMgr.RealmId.Site);
		stmt.AddValue(3, Global.WorldMgr.RealmId.Index);
		stmt.AddValue(4, GetName());
		stmt.AddValue(5, GUID.Counter);
		stmt.AddValue(6, GameTime.GetGameTime());
		loginTransaction.Append(stmt);

		// save pet (hunter pet level and experience and all type pets health/mana).
		var pet = CurrentPet;

		if (pet)
			pet.SavePetToDB(PetSaveMode.AsCurrent);
	}

	public static uint GetZoneIdFromDB(ObjectGuid guid)
	{
		var guidLow = guid.Counter;
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_ZONE);
		stmt.AddValue(0, guidLow);
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
			return 0;

		uint zone = result.Read<ushort>(0);

		if (zone == 0)
		{
			// stored zone is zero, use generic and slow zone detection
			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION_XYZ);
			stmt.AddValue(0, guidLow);
			result = DB.Characters.Query(stmt);

			if (result.IsEmpty())
				return 0;

			uint map = result.Read<ushort>(0);
			var posx = result.Read<float>(1);
			var posy = result.Read<float>(2);
			var posz = result.Read<float>(3);

			if (!CliDB.MapStorage.ContainsKey(map))
				return 0;

			zone = Global.TerrainMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, map, posx, posy, posz);

			if (zone > 0)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ZONE);

				stmt.AddValue(0, zone);
				stmt.AddValue(1, guidLow);

				DB.Characters.Execute(stmt);
			}
		}

		return zone;
	}

	public static void RemovePetitionsAndSigns(ObjectGuid guid)
	{
		Global.PetitionMgr.RemoveSignaturesBySigner(guid);
		Global.PetitionMgr.RemovePetitionsByOwner(guid);
	}

	public static void DeleteFromDB(ObjectGuid playerGuid, uint accountId, bool updateRealmChars = true, bool deleteFinally = false)
	{
		// Avoid realm-update for non-existing account
		if (accountId == 0)
			updateRealmChars = false;

		// Convert guid to low GUID for CharacterNameData, but also other methods on success
		var guid = playerGuid.Counter;
		var charDelete_method = (CharDeleteMethod)WorldConfig.GetIntValue(WorldCfg.ChardeleteMethod);
		var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(playerGuid);
		var name = "<Unknown>";

		if (characterInfo != null)
			name = characterInfo.Name;

		if (deleteFinally)
		{
			charDelete_method = CharDeleteMethod.Remove;
		}
		else if (characterInfo != null) // To avoid a Select, we select loaded data. If it doesn't exist, return.
		{
			// Define the required variables
			uint charDeleteMinLvl;

			if (characterInfo.ClassId == PlayerClass.Deathknight)
				charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteDeathKnightMinLevel);
			else if (characterInfo.ClassId == PlayerClass.DemonHunter)
				charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteDemonHunterMinLevel);
			else
				charDeleteMinLvl = WorldConfig.GetUIntValue(WorldCfg.ChardeleteMinLevel);

			// if we want to finalize the character removal or the character does not meet the level requirement of either heroic or non-heroic settings,
			// we set it to mode CHAR_DELETE_REMOVE
			if (characterInfo.Level < charDeleteMinLvl)
				charDelete_method = CharDeleteMethod.Remove;
		}

		SQLTransaction trans = new();
		SQLTransaction loginTransaction = new();

		var guildId = Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(playerGuid);

		if (guildId != 0)
		{
			var guild = Global.GuildMgr.GetGuildById(guildId);

			if (guild)
				guild.DeleteMember(trans, playerGuid, false, false, true);
		}

		// remove from arena teams
		LeaveAllArenaTeams(playerGuid);

		// the player was uninvited already on logout so just remove from group
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
		stmt.AddValue(0, guid);
		var resultGroup = DB.Characters.Query(stmt);

		if (!resultGroup.IsEmpty())
		{
			var group = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));

			if (group)
				RemoveFromGroup(group, playerGuid);
		}

		// Remove signs from petitions (also remove petitions if owner);
		RemovePetitionsAndSigns(playerGuid);

		switch (charDelete_method)
		{
			// Completely remove from the database
			case CharDeleteMethod.Remove:
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_COD_ITEM_MAIL);
				stmt.AddValue(0, guid);
				var resultMail = DB.Characters.Query(stmt);

				if (!resultMail.IsEmpty())
				{
					MultiMap<ulong, Item> itemsByMail = new();

					stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
					stmt.AddValue(0, guid);
					var resultItems = DB.Characters.Query(stmt);

					if (!resultItems.IsEmpty())
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
						stmt.AddValue(0, guid);
						var artifactResult = DB.Characters.Query(stmt);

						stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
						stmt.AddValue(0, guid);
						var azeriteResult = DB.Characters.Query(stmt);

						stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
						stmt.AddValue(0, guid);
						var azeriteItemMilestonePowersResult = DB.Characters.Query(stmt);

						stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
						stmt.AddValue(0, guid);
						var azeriteItemUnlockedEssencesResult = DB.Characters.Query(stmt);

						stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
						stmt.AddValue(0, guid);
						var azeriteEmpoweredItemResult = DB.Characters.Query(stmt);

						Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
						ItemAdditionalLoadInfo.Init(additionalData, artifactResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

						do
						{
							var mailId = resultItems.Read<ulong>(52);
							var mailItem = _LoadMailedItem(playerGuid, null, mailId, null, resultItems.GetFields(), additionalData.LookupByKey(resultItems.Read<ulong>(0)));

							if (mailItem != null)
								itemsByMail.Add(mailId, mailItem);
						} while (resultItems.NextRow());
					}

					do
					{
						var mail_id = resultMail.Read<ulong>(0);
						var mailType = (MailMessageType)resultMail.Read<byte>(1);
						var mailTemplateId = resultMail.Read<ushort>(2);
						var sender = resultMail.Read<uint>(3);
						var subject = resultMail.Read<string>(4);
						var body = resultMail.Read<string>(5);
						var money = resultMail.Read<ulong>(6);
						var has_items = resultMail.Read<bool>(7);

						// We can return mail now
						// So firstly delete the old one
						stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_BY_ID);
						stmt.AddValue(0, mail_id);
						trans.Append(stmt);

						// Mail is not from player
						if (mailType != MailMessageType.Normal)
						{
							if (has_items)
							{
								stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
								stmt.AddValue(0, mail_id);
								trans.Append(stmt);
							}

							continue;
						}

						MailDraft draft = new(subject, body);

						if (mailTemplateId != 0)
							draft = new MailDraft(mailTemplateId, false); // items are already included

						var itemsList = itemsByMail.LookupByKey(mail_id);

						if (itemsList != null)
						{
							foreach (var item in itemsList)
								draft.AddItem(item);

							itemsByMail.Remove(mail_id);
						}

						stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM_BY_ID);
						stmt.AddValue(0, mail_id);
						trans.Append(stmt);

						var pl_account = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(ObjectGuid.Create(HighGuid.Player, guid));

						draft.AddMoney(money).SendReturnToSender(pl_account, guid, sender, trans);
					} while (resultMail.NextRow());

					// Free remaining items
					foreach (var pair in itemsByMail.KeyValueList)
						pair.Value.Dispose();
				}

				// Unsummon and delete for pets in world is not required: player deleted from CLI or character list with not loaded pet.
				// NOW we can finally clear other DB data related to character
				stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PET_IDS);
				stmt.AddValue(0, guid);
				var resultPets = DB.Characters.Query(stmt);

				if (!resultPets.IsEmpty())
					do
					{
						var petguidlow = resultPets.Read<uint>(0);
						Pet.DeleteFromDB(petguidlow);
					} while (resultPets.NextRow());

				// Delete char from social list of online chars
				stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_SOCIAL);
				stmt.AddValue(0, guid);
				var resultFriends = DB.Characters.Query(stmt);

				if (!resultFriends.IsEmpty())
					do
					{
						var playerFriend = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, resultFriends.Read<ulong>(0)));

						if (playerFriend)
						{
							playerFriend.Social.RemoveFromSocialList(playerGuid, SocialFlag.All);
							Global.SocialMgr.SendFriendStatus(playerFriend, FriendsResult.Removed, playerGuid);
						}
					} while (resultFriends.NextRow());

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_CUSTOMIZATIONS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_ACCOUNT_DATA);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_ARENA_STATS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_CURRENCY);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GIFT);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_INSTANCE_LOCK_BY_GUID);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_REPUTATION);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_COOLDOWNS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEMS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME_BY_OWNER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENTS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_PROGRESS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_EQUIPMENTSETS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRANSMOG_OUTFITS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG_BY_PLAYER);
				stmt.AddValue(0, guid);
				stmt.AddValue(1, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG_BY_PLAYER);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILLS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_CHAR_GUID);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_FISHINGSTEPS);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_FAVORITE_AUCTIONS_BY_CHAR);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATIONS_BY_GUID);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PET_DECLINED_NAME_BY_OWNER);
				stmt.AddValue(0, guid);
				stmt.AddValue(1, Global.WorldMgr.RealmId.Index);
				loginTransaction.Append(stmt);

				stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_BATTLE_PETS_BY_OWNER);
				stmt.AddValue(0, guid);
				stmt.AddValue(1, Global.WorldMgr.RealmId.Index);
				loginTransaction.Append(stmt);

				Corpse.DeleteFromDB(playerGuid, trans);

				Garrison.DeleteFromDB(guid, trans);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES_BY_CHAR);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS_BY_CHAR);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				Global.CharacterCacheStorage.DeleteCharacterCacheEntry(playerGuid, name);

				break;
			}
			// The character gets unlinked from the account, the name gets freed up and appears as deleted ingame
			case CharDeleteMethod.Unlink:
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_DELETE_INFO);
				stmt.AddValue(0, guid);
				trans.Append(stmt);

				Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(playerGuid, true);

				break;
			}
			default:
				Log.outError(LogFilter.Player, "Player:DeleteFromDB: Unsupported delete method: {0}.", charDelete_method);

				if (trans.commands.Count > 0)
					DB.Characters.CommitTransaction(trans);

				return;
		}

		DB.Login.CommitTransaction(loginTransaction);
		DB.Characters.CommitTransaction(trans);

		if (updateRealmChars)
			Global.WorldMgr.UpdateRealmCharCount(accountId);
	}

	public static void DeleteOldCharacters()
	{
		var keepDays = WorldConfig.GetIntValue(WorldCfg.ChardeleteKeepDays);

		if (keepDays == 0)
			return;

		DeleteOldCharacters(keepDays);
	}

	public static void DeleteOldCharacters(int keepDays)
	{
		Log.outInfo(LogFilter.Player, "Player:DeleteOldChars: Deleting all characters which have been deleted {0} days before...", keepDays);

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_OLD_CHARS);
		stmt.AddValue(0, (uint)(GameTime.GetGameTime() - keepDays * Time.Day));
		var result = DB.Characters.Query(stmt);

		if (!result.IsEmpty())
		{
			var count = 0;

			do
			{
				DeleteFromDB(ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0)), result.Read<uint>(1), true, true);
				count++;
			} while (result.NextRow());

			Log.outDebug(LogFilter.Player, "Player:DeleteOldChars: Deleted {0} character(s)", count);
		}
	}

	public static void SavePositionInDB(WorldLocation loc, uint zoneId, ObjectGuid guid, SQLTransaction trans = null)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHARACTER_POSITION);
		stmt.AddValue(0, loc.X);
		stmt.AddValue(1, loc.Y);
		stmt.AddValue(2, loc.Z);
		stmt.AddValue(3, loc.Orientation);
		stmt.AddValue(4, (ushort)loc.MapId);
		stmt.AddValue(5, zoneId);
		stmt.AddValue(6, guid.Counter);

		DB.Characters.ExecuteOrAppend(trans, stmt);
	}

	public static bool LoadPositionFromDB(out WorldLocation loc, out bool inFlight, ObjectGuid guid)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_POSITION);
		stmt.AddValue(0, guid.Counter);
		var result = DB.Characters.Query(stmt);

		loc = new WorldLocation();
		inFlight = false;

		if (result.IsEmpty())
			return false;

		loc.X = result.Read<float>(0);
		loc.Y = result.Read<float>(1);
		loc.Z = result.Read<float>(2);
		loc.Orientation = result.Read<float>(3);
		loc.MapId = result.Read<ushort>(4);
		inFlight = !string.IsNullOrEmpty(result.Read<string>(5));

		return true;
	}

	void _LoadInventory(SQLResult result, SQLResult artifactsResult, SQLResult azeriteResult, SQLResult azeriteItemMilestonePowersResult, SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult, uint timeDiff)
	{
		Dictionary<ulong, ItemAdditionalLoadInfo> additionalData = new();
		ItemAdditionalLoadInfo.Init(additionalData, artifactsResult, azeriteResult, azeriteItemMilestonePowersResult, azeriteItemUnlockedEssencesResult, azeriteEmpoweredItemResult);

		if (!result.IsEmpty())
		{
			var zoneId = Zone;
			Dictionary<ObjectGuid, Bag> bagMap = new();         // fast guid lookup for bags
			Dictionary<ObjectGuid, Item> invalidBagMap = new(); // fast guid lookup for bags
			Queue<Item> problematicItems = new();
			SQLTransaction trans = new();

			// Prevent items from being added to the queue while loading
			ItemUpdateQueueBlocked = true;

			do
			{
				var item = _LoadItem(trans, zoneId, timeDiff, result.GetFields());

				if (item != null)
				{
					var addionalData = additionalData.LookupByKey(item.GUID.Counter);

					if (addionalData != null)
					{
						if (item.Template.ArtifactID != 0 && addionalData.Artifact != null)
							item.LoadArtifactData(this, addionalData.Artifact.Xp, addionalData.Artifact.ArtifactAppearanceId, addionalData.Artifact.ArtifactTierId, addionalData.Artifact.ArtifactPowers);

						if (addionalData.AzeriteItem != null)
						{
							var azeriteItem = item.AsAzeriteItem;

							if (azeriteItem != null)
								azeriteItem.LoadAzeriteItemData(this, addionalData.AzeriteItem);
						}

						if (addionalData.AzeriteEmpoweredItem != null)
						{
							var azeriteEmpoweredItem = item.AsAzeriteEmpoweredItem;

							if (azeriteEmpoweredItem != null)
								azeriteEmpoweredItem.LoadAzeriteEmpoweredItemData(this, addionalData.AzeriteEmpoweredItem);
						}
					}


					var counter = result.Read<ulong>(51);
					var bagGuid = counter != 0 ? ObjectGuid.Create(HighGuid.Item, counter) : ObjectGuid.Empty;
					var slot = result.Read<byte>(52);

					Session.CollectionMgr.CheckHeirloomUpgrades(item);
					Session.CollectionMgr.AddItemAppearance(item);

					var err = InventoryResult.Ok;

					if (item.HasItemFlag(ItemFieldFlags.Child))
					{
						var parent = GetItemByGuid(item.Creator);

						if (parent)
						{
							parent.SetChildItem(item.GUID);
							item.CopyArtifactDataFromParent(parent);
						}
						else
						{
							Log.outError(LogFilter.Player, $"Player._LoadInventory: Player '{GetName()}' ({GUID}) has child item ({item.GUID}, entry: {item.Entry}) which can't be loaded into inventory because parent item was not found (Bag {bagGuid}, slot: {slot}). Item will be sent by mail.");
							item.DeleteFromInventoryDB(trans);
							problematicItems.Enqueue(item);

							continue;
						}
					}

					// Item is not in bag
					if (bagGuid.IsEmpty)
					{
						item.SetContainer(null);
						item.SetSlot(slot);

						if (IsInventoryPos(InventorySlots.Bag0, slot))
						{
							List<ItemPosCount> dest = new();
							err = CanStoreItem(InventorySlots.Bag0, slot, dest, item, false);

							if (err == InventoryResult.Ok)
								item = StoreItem(dest, item, true);
						}
						else if (IsEquipmentPos(InventorySlots.Bag0, slot))
						{
							err = CanEquipItem(slot, out var dest, item, false, false);

							if (err == InventoryResult.Ok)
								QuickEquipItem(dest, item);
						}
						else if (IsBankPos(InventorySlots.Bag0, slot))
						{
							List<ItemPosCount> dest = new();
							err = CanBankItem(InventorySlots.Bag0, slot, dest, item, false, false);

							if (err == InventoryResult.Ok)
								item = BankItem(dest, item, true);
						}

						// Remember bags that may contain items in them
						if (err == InventoryResult.Ok)
						{
							if (IsBagPos(item.Pos))
							{
								var pBag = item.AsBag;

								if (pBag != null)
									bagMap.Add(item.GUID, pBag);
							}
						}
						else if (IsBagPos(item.Pos))
						{
							if (item.IsBag)
								invalidBagMap.Add(item.GUID, item);
						}
					}
					else
					{
						item.SetSlot(ItemConst.NullSlot);
						// Item is in the bag, find the bag
						var bag = bagMap.LookupByKey(bagGuid);

						if (bag != null)
						{
							List<ItemPosCount> dest = new();
							err = CanStoreItem(bag.Slot, slot, dest, item);

							if (err == InventoryResult.Ok)
								item = StoreItem(dest, item, true);
						}
						else if (invalidBagMap.ContainsKey(bagGuid))
						{
							var invalidBag = invalidBagMap.LookupByKey(bagGuid);

							if (problematicItems.Contains(invalidBag))
								err = InventoryResult.InternalBagError;
						}
						else
						{
							Log.outError(LogFilter.Player,
										"LoadInventory: player (GUID: {0}, name: '{1}') has item (GUID: {2}, entry: {3}) which doesnt have a valid bag (Bag GUID: {4}, slot: {5}). Possible cheat?",
										GUID.ToString(),
										GetName(),
										item.GUID.ToString(),
										item.Entry,
										bagGuid,
										slot);

							item.DeleteFromInventoryDB(trans);

							continue;
						}
					}

					// Item's state may have changed after storing
					if (err == InventoryResult.Ok)
					{
						item.SetState(ItemUpdateState.Unchanged, this);
					}
					else
					{
						Log.outError(LogFilter.Player,
									"LoadInventory: player (GUID: {0}, name: '{1}') has item (GUID: {2}, entry: {3}) which can't be loaded into inventory (Bag GUID: {4}, slot: {5}) by reason {6}. " +
									"Item will be sent by mail.",
									GUID.ToString(),
									GetName(),
									item.GUID.ToString(),
									item.Entry,
									bagGuid,
									slot,
									err);

						item.DeleteFromInventoryDB(trans);
						problematicItems.Enqueue(item);
					}
				}
			} while (result.NextRow());

			ItemUpdateQueueBlocked = false;

			// Send problematic items by mail
			while (problematicItems.Count != 0)
			{
				var subject = Global.ObjectMgr.GetCypherString(CypherStrings.NotEquippedItem);
				MailDraft draft = new(subject, "There were problems with equipping item(s).");

				for (var i = 0; problematicItems.Count != 0 && i < SharedConst.MaxMailItems; ++i)
					draft.AddItem(problematicItems.Dequeue());

				draft.SendMailTo(trans, this, new MailSender(this, MailStationery.Gm), MailCheckMask.Copied);
			}

			DB.Characters.CommitTransaction(trans);
		}

		_ApplyAllItemMods();
		// Apply all azerite item mods, azerite empowered item mods will get applied through its spell script
		ApplyAllAzeriteItemMods(true);
	}

	Item _LoadItem(SQLTransaction trans, uint zoneId, uint timeDiff, SQLFields fields)
	{
		Item item = null;
		var itemGuid = fields.Read<ulong>(0);
		var itemEntry = fields.Read<uint>(1);
		var proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

		if (proto != null)
		{
			var remove = false;
			item = Item.NewItemOrBag(proto);

			if (item.LoadFromDB(itemGuid, GUID, fields, itemEntry))
			{
				PreparedStatement stmt;

				// Do not allow to have item limited to another map/zone in alive state
				if (IsAlive && item.IsLimitedToAnotherMapOrZone(Location.MapId, zoneId))
				{
					Log.outDebug(LogFilter.Player,
								"LoadInventory: player (GUID: {0}, name: '{1}', map: {2}) has item (GUID: {3}, entry: {4}) limited to another map ({5}). Deleting item.",
								GUID.ToString(),
								GetName(),
								Location.MapId,
								item.GUID.ToString(),
								item.Entry,
								zoneId);

					remove = true;
				}
				// "Conjured items disappear if you are logged out for more than 15 minutes"
				else if (timeDiff > 15 * Time.Minute && proto.HasFlag(ItemFlags.Conjured))
				{
					Log.outDebug(LogFilter.Player,
								"LoadInventory: player (GUID: {0}, name: {1}, diff: {2}) has conjured item (GUID: {3}, entry: {4}) with expired lifetime (15 minutes). Deleting item.",
								GUID.ToString(),
								GetName(),
								timeDiff,
								item.GUID.ToString(),
								item.Entry);

					remove = true;
				}

				if (item.IsRefundable)
				{
					if (item.PlayedTime > (2 * Time.Hour))
					{
						Log.outDebug(LogFilter.Player,
									"LoadInventory: player (GUID: {0}, name: {1}) has item (GUID: {2}, entry: {3}) with expired refund time ({4}). Deleting refund data and removing " +
									"efundable flag.",
									GUID.ToString(),
									GetName(),
									item.GUID.ToString(),
									item.Entry,
									item.PlayedTime);

						stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
						stmt.AddValue(0, item.GUID.ToString());
						trans.Append(stmt);

						item.RemoveItemFlag(ItemFieldFlags.Refundable);
					}
					else
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_REFUNDS);
						stmt.AddValue(0, item.GUID.Counter);
						stmt.AddValue(1, GUID.Counter);
						var result = DB.Characters.Query(stmt);

						if (!result.IsEmpty())
						{
							item.SetRefundRecipient(GUID);
							item.SetPaidMoney(result.Read<ulong>(0));
							item.SetPaidExtendedCost(result.Read<ushort>(1));
							AddRefundReference(item.GUID);
						}
						else
						{
							Log.outDebug(LogFilter.Player,
										"LoadInventory: player (GUID: {0}, name: {1}) has item (GUID: {2}, entry: {3}) with refundable flags, but without data in item_refund_instance. Removing flag.",
										GUID.ToString(),
										GetName(),
										item.GUID.ToString(),
										item.Entry);

							item.RemoveItemFlag(ItemFieldFlags.Refundable);
						}
					}
				}
				else if (item.IsBOPTradeable)
				{
					stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_BOP_TRADE);
					stmt.AddValue(0, item.GUID.ToString());
					var result = DB.Characters.Query(stmt);

					if (!result.IsEmpty())
					{
						var strGUID = result.Read<string>(0);
						var GUIDlist = new StringArray(strGUID, ' ');
						List<ObjectGuid> looters = new();

						for (var i = 0; i < GUIDlist.Length; ++i)
							if (ulong.TryParse(GUIDlist[i], out var guid))
								looters.Add(ObjectGuid.Create(HighGuid.Item, guid));

						if (looters.Count > 1 && item.Template.MaxStackSize == 1 && item.IsSoulBound)
						{
							item.SetSoulboundTradeable(looters);
							AddTradeableItem(item);
						}
						else
						{
							item.ClearSoulboundTradeable(this);
						}
					}
					else
					{
						Log.outDebug(LogFilter.ServerLoading,
									"LoadInventory: player ({0}, name: {1}) has item ({2}, entry: {3}) with ITEM_FLAG_BOP_TRADEABLE flag, " +
									"but without data in item_soulbound_trade_data. Removing flag.",
									GUID.ToString(),
									GetName(),
									item.GUID.ToString(),
									item.Entry);

						item.RemoveItemFlag(ItemFieldFlags.BopTradeable);
					}
				}
				else if (proto.HolidayID != 0)
				{
					remove = true;
					var events = Global.GameEventMgr.GetEventMap();
					var activeEventsList = Global.GameEventMgr.GetActiveEventList();

					foreach (var id in activeEventsList)
						if (events[id].holiday_id == proto.HolidayID)
						{
							remove = false;

							break;
						}
				}
			}
			else
			{
				Log.outError(LogFilter.Player,
							"LoadInventory: player (GUID: {0}, name: {1}) has broken item (GUID: {2}, entry: {3}) in inventory. Deleting item.",
							GUID.ToString(),
							GetName(),
							itemGuid,
							itemEntry);

				remove = true;
			}

			// Remove item from inventory if necessary
			if (remove)
			{
				Item.DeleteFromInventoryDB(trans, itemGuid);
				item.FSetState(ItemUpdateState.Removed);
				item.SaveToDB(trans); // it also deletes item object!
				item = null;
			}
		}
		else
		{
			Log.outError(LogFilter.Player,
						"LoadInventory: player (GUID: {0}, name: {1}) has unknown item (entry: {2}) in inventory. Deleting item.",
						GUID.ToString(),
						GetName(),
						itemEntry);

			Item.DeleteFromInventoryDB(trans, itemGuid);
			Item.DeleteFromDB(trans, itemGuid);
			AzeriteItem.DeleteFromDB(trans, itemGuid);
			AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);
		}

		return item;
	}

	void _LoadSkills(SQLResult result)
	{
		var race = Race;
		uint count = 0;
		Dictionary<uint, uint> loadedSkillValues = new();
		List<ushort> loadedProfessionsWithoutSlot = new(); // fixup old characters

		if (!result.IsEmpty())
			do
			{
				if (_skillStatus.Count >= SkillConst.MaxPlayerSkills) // client limit
				{
					Log.outError(LogFilter.Player, $"Player::_LoadSkills: Player '{GetName()}' ({GUID}) has more than {SkillConst.MaxPlayerSkills} skills.");

					break;
				}

				var skill = result.Read<ushort>(0);
				var value = result.Read<ushort>(1);
				var max = result.Read<ushort>(2);
				var professionSlot = result.Read<sbyte>(3);

				var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(skill, race, Class);

				if (rcEntry == null)
				{
					Log.outError(LogFilter.Player, $"Player::_LoadSkills: Player '{GetName()}' ({GUID}, Race: {race}, Class: {Class}) has forbidden skill {skill} for his race/class combination");
					_skillStatus.Add(skill, new SkillStatusData((uint)_skillStatus.Count, SkillState.Deleted));

					continue;
				}

				// set fixed skill ranges
				switch (Global.SpellMgr.GetSkillRangeType(rcEntry))
				{
					case SkillRangeType.Language:
						value = max = 300;

						break;
					case SkillRangeType.Mono:
						value = max = 1;

						break;
					case SkillRangeType.Level:
						max = GetMaxSkillValueForLevel();

						break;
					default:
						break;
				}

				if (!_skillStatus.ContainsKey(skill))
					_skillStatus.Add(skill, new SkillStatusData((uint)_skillStatus.Count, SkillState.Unchanged));

				var skillStatusData = _skillStatus[skill];
				ushort step = 0;

				var skillLine = CliDB.SkillLineStorage.LookupByKey(rcEntry.SkillID);

				if (skillLine != null)
				{
					if (skillLine.CategoryID == SkillCategory.Secondary)
						step = (ushort)(max / 75);

					if (skillLine.CategoryID == SkillCategory.Profession)
					{
						step = (ushort)(max / 75);

						if (skillLine.ParentSkillLineID != 0 && skillLine.ParentTierIndex != 0)
						{
							if (professionSlot != -1)
								SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, professionSlot), skill);
							else
								loadedProfessionsWithoutSlot.Add(skill);
						}
					}
				}

				SetSkillLineId(skillStatusData.Pos, skill);
				SetSkillStep(skillStatusData.Pos, step);
				SetSkillRank(skillStatusData.Pos, value);
				SetSkillStartingRank(skillStatusData.Pos, 1);
				SetSkillMaxRank(skillStatusData.Pos, max);
				SetSkillTempBonus(skillStatusData.Pos, 0);
				SetSkillPermBonus(skillStatusData.Pos, 0);

				loadedSkillValues[skill] = value;
			} while (result.NextRow());

		// Learn skill rewarded spells after all skills have been loaded to prevent learning a skill from them before its loaded with proper value from DB
		foreach (var skill in loadedSkillValues)
		{
			LearnSkillRewardedSpells(skill.Key, skill.Value, race);
			var childSkillLines = Global.DB2Mgr.GetSkillLinesForParentSkill(skill.Key);

			if (childSkillLines != null)
				foreach (var childItr in childSkillLines)
				{
					if (_skillStatus.Count >= SkillConst.MaxPlayerSkills)
						break;

					if (!_skillStatus.ContainsKey(childItr.Id))
					{
						SetSkillLineId(count, (ushort)childItr.Id);
						SetSkillStartingRank(count, 1);
						_skillStatus.Add(childItr.Id, new SkillStatusData(count, SkillState.Unchanged));
					}
				}
		}

		foreach (var skill in loadedProfessionsWithoutSlot)
		{
			var emptyProfessionSlot = FindEmptyProfessionSlotFor(skill);

			if (emptyProfessionSlot != -1)
			{
				SetUpdateFieldValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ProfessionSkillLine, emptyProfessionSlot), skill);
				_skillStatus[skill].State = SkillState.Changed;
			}
		}

		if (HasSkill(SkillType.FistWeapons))
			SetSkill(SkillType.FistWeapons, 0, GetSkillValue(SkillType.Unarmed), GetMaxSkillValueForLevel());
	}

	void _LoadSpells(SQLResult result, SQLResult favoritesResult)
	{
		if (!result.IsEmpty())
			do
			{
				AddSpell(result.Read<uint>(0), result.Read<bool>(1), false, false, result.Read<bool>(2), true);
			} while (result.NextRow());

		if (!favoritesResult.IsEmpty())
			do
			{
				var spell = _spells.LookupByKey(favoritesResult.Read<uint>(0));

				if (spell != null)
					spell.Favorite = true;
			} while (favoritesResult.NextRow());
	}

	void _LoadAuras(SQLResult auraResult, SQLResult effectResult, uint timediff)
	{
		Log.outDebug(LogFilter.Player, "Loading auras for player {0}", GUID.ToString());

		ObjectGuid casterGuid = new();
		ObjectGuid itemGuid = new();
		Dictionary<AuraKey, AuraLoadEffectInfo> effectInfo = new();

		if (!effectResult.IsEmpty())
			do
			{
				int effectIndex = effectResult.Read<byte>(4);
				casterGuid.SetRawValue(effectResult.Read<byte[]>(0));
				itemGuid.SetRawValue(effectResult.Read<byte[]>(1));

				AuraKey key = new(casterGuid, itemGuid, effectResult.Read<uint>(2), effectResult.Read<uint>(3));

				if (!effectInfo.ContainsKey(key))
					effectInfo[key] = new AuraLoadEffectInfo();

				var info = effectInfo[key];
				info.Amounts[effectIndex] = effectResult.Read<int>(5);
				info.BaseAmounts[effectIndex] = effectResult.Read<int>(6);
			} while (effectResult.NextRow());

		if (!auraResult.IsEmpty())
			do
			{
				casterGuid.SetRawValue(auraResult.Read<byte[]>(0));
				itemGuid.SetRawValue(auraResult.Read<byte[]>(1));
				AuraKey key = new(casterGuid, itemGuid, auraResult.Read<uint>(2), auraResult.Read<uint>(3));
				var recalculateMask = auraResult.Read<uint>(4);
				var difficulty = (Difficulty)auraResult.Read<byte>(5);
				var stackCount = auraResult.Read<byte>(6);
				var maxDuration = auraResult.Read<int>(7);
				var remainTime = auraResult.Read<int>(8);
				var remainCharges = auraResult.Read<byte>(9);
				var castItemId = auraResult.Read<uint>(10);
				var castItemLevel = auraResult.Read<int>(11);

				var spellInfo = Global.SpellMgr.GetSpellInfo(key.SpellId, difficulty);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Player, "Unknown aura (spellid {0}), ignore.", key.SpellId);

					continue;
				}

				if (difficulty != Difficulty.None && !CliDB.DifficultyStorage.ContainsKey(difficulty))
				{
					Log.outError(LogFilter.Player, $"Player._LoadAuras: Player '{GetName()}' ({GUID}) has an invalid aura difficulty {difficulty} (SpellID: {key.SpellId}), ignoring.");

					continue;
				}

				// negative effects should continue counting down after logout
				if (remainTime != -1 && (!spellInfo.IsPositive || spellInfo.HasAttribute(SpellAttr4.AuraExpiresOffline)))
				{
					if (remainTime / Time.InMilliseconds <= timediff)
						continue;

					remainTime -= (int)(timediff * Time.InMilliseconds);
				}

				// prevent wrong values of remaincharges
				if (spellInfo.ProcCharges != 0)
				{
					// we have no control over the order of applying auras and modifiers allow auras
					// to have more charges than value in SpellInfo
					if (remainCharges <= 0)
						remainCharges = (byte)spellInfo.ProcCharges;
				}
				else
				{
					remainCharges = 0;
				}

				var info = effectInfo[key];
				var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, Location.MapId, spellInfo.Id, Map.GenerateLowGuid(HighGuid.Cast));

				AuraCreateInfo createInfo = new(castId, spellInfo, difficulty, key.EffectMask.ExplodeMask(SpellConst.MaxEffects), this);
				createInfo.SetCasterGuid(casterGuid);
				createInfo.SetBaseAmount(info.BaseAmounts);
				createInfo.SetCastItem(itemGuid, castItemId, castItemLevel);

				var aura = Aura.TryCreate(createInfo);

				if (aura != null)
				{
					if (!aura.CanBeSaved())
					{
						aura.Remove();

						continue;
					}

					aura.SetLoadedState(maxDuration, remainTime, remainCharges, stackCount, recalculateMask, info.Amounts);
					aura.ApplyForTargets();
					Log.outInfo(LogFilter.Player, "Added aura spellid {0}, effectmask {1}", spellInfo.Id, key.EffectMask);
				}
			} while (auraResult.NextRow());
	}

	bool _LoadHomeBind(SQLResult result)
	{
		var info = Global.ObjectMgr.GetPlayerInfo(Race, Class);

		if (info == null)
		{
			Log.outError(LogFilter.Player, "Player (Name {0}) has incorrect race/class ({1}/{2}) pair. Can't be loaded.", GetName(), Race, Class);

			return false;
		}

		var ok = false;

		if (!result.IsEmpty())
		{
			_homebind.WorldRelocate(result.Read<uint>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
			_homebindAreaId = result.Read<uint>(1);

			var map = CliDB.MapStorage.LookupByKey(_homebind.MapId);

			// accept saved data only for valid position (and non instanceable), and accessable
			if (GridDefines.IsValidMapCoord(_homebind) &&
				!map.Instanceable() &&
				Session.Expansion >= map.Expansion())
			{
				ok = true;
			}
			else
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
				stmt.AddValue(0, GUID.Counter);
				DB.Characters.Execute(stmt);
			}
		}

		void saveHomebindToDb()
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
			stmt.AddValue(0, GUID.Counter);
			stmt.AddValue(1, _homebind.MapId);
			stmt.AddValue(2, _homebindAreaId);
			stmt.AddValue(3, _homebind.X);
			stmt.AddValue(4, _homebind.Y);
			stmt.AddValue(5, _homebind.Z);
			stmt.AddValue(6, _homebind.Orientation);
			DB.Characters.Execute(stmt);
		}

		;

		if (!ok && HasAtLoginFlag(AtLoginFlags.FirstLogin))
		{
			var createPosition = _createMode == PlayerCreateMode.NPE && info.CreatePositionNpe.HasValue ? info.CreatePositionNpe.Value : info.CreatePosition;

			if (!createPosition.TransportGuid.HasValue)
			{
				_homebind.WorldRelocate(createPosition.Loc);
				_homebindAreaId = Global.TerrainMgr.GetAreaId(PhasingHandler.EmptyPhaseShift, _homebind);

				saveHomebindToDb();
				ok = true;
			}
		}

		if (!ok)
		{
			var loc = Global.ObjectMgr.GetDefaultGraveYard(Team);

			if (loc == null && Race == Race.PandarenNeutral)
				loc = Global.ObjectMgr.GetWorldSafeLoc(3295); // The Wandering Isle, Starting Area GY

			Cypher.Assert(loc != null, "Missing fallback graveyard location for faction {GetTeamId()}");

			_homebind.WorldRelocate(loc.Loc);
			_homebindAreaId = Global.TerrainMgr.GetAreaId(PhasingHandler.EmptyPhaseShift, loc.Loc);

			saveHomebindToDb();
		}

		Log.outDebug(LogFilter.Player, $"Setting player home position - mapid: {_homebind.MapId}, areaid: {_homebindAreaId}, {_homebind}");

		return true;
	}

	void _LoadCurrency(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			var currencyID = result.Read<ushort>(0);

			var currency = CliDB.CurrencyTypesStorage.LookupByKey(currencyID);

			if (currency == null)
				continue;

			PlayerCurrency cur = new();
			cur.State = PlayerCurrencyState.Unchanged;
			cur.Quantity = result.Read<uint>(1);
			cur.WeeklyQuantity = result.Read<uint>(2);
			cur.TrackedQuantity = result.Read<uint>(3);
			cur.IncreasedCapQuantity = result.Read<uint>(4);
			cur.EarnedQuantity = result.Read<uint>(5);
			cur.Flags = (CurrencyDbFlags)result.Read<byte>(6);

			_currencyStorage.Add(currencyID, cur);
		} while (result.NextRow());
	}

	void LoadActions(SQLResult result)
	{
		_LoadActions(result);

		SendActionButtons(1);
	}

	void _LoadActions(SQLResult result)
	{
		_actionButtons.Clear();

		if (!result.IsEmpty())
			do
			{
				var button = result.Read<byte>(0);
				var action = result.Read<ulong>(1);
				var type = result.Read<byte>(2);

				var ab = AddActionButton(button, action, type);

				if (ab != null)
				{
					ab.UState = ActionButtonUpdateState.UnChanged;
				}
				else
				{
					Log.outError(LogFilter.Player, $"Player::_LoadActions: Player '{GetName()}' ({GUID}) has an invalid action button (Button: {button}, Action: {action}, Type: {type}). It will be deleted at next save. This can be due to a player changing their talents.");

					// Will deleted in DB at next save (it can create data until save but marked as deleted)
					_actionButtons[button] = new ActionButton();
					_actionButtons[button].UState = ActionButtonUpdateState.Deleted;
				}
			} while (result.NextRow());
	}

	void _LoadQuestStatus(SQLResult result)
	{
		ushort slot = 0;

		if (!result.IsEmpty())
			do
			{
				var questId = result.Read<uint>(0);
				// used to be new, no delete?
				var quest = Global.ObjectMgr.GetQuestTemplate(questId);

				if (quest != null)
				{
					// find or create
					QuestStatusData questStatusData = new();

					var qstatus = result.Read<byte>(1);

					if (qstatus < (byte)QuestStatus.Max)
					{
						questStatusData.Status = (QuestStatus)qstatus;
					}
					else
					{
						questStatusData.Status = QuestStatus.Incomplete;

						Log.outError(LogFilter.Player,
									"Player {0} (GUID: {1}) has invalid quest {2} status ({3}), replaced by QUEST_STATUS_INCOMPLETE(3).",
									GetName(),
									GUID.ToString(),
									questId,
									qstatus);
					}

					questStatusData.Explored = result.Read<byte>(2) > 0;

					var acceptTime = result.Read<long>(3);
					var endTime = result.Read<long>(4);

					if (quest.LimitTime != 0 && !GetQuestRewardStatus(questId))
					{
						AddTimedQuest(questId);

						if (endTime <= GameTime.GetGameTime())
							questStatusData.Timer = 1;
						else
							questStatusData.Timer = (uint)((endTime - GameTime.GetGameTime()) * Time.InMilliseconds);
					}
					else
					{
						endTime = 0;
					}

					// add to quest log
					if (slot < SharedConst.MaxQuestLogSize && questStatusData.Status != QuestStatus.None)
					{
						questStatusData.Slot = slot;

						foreach (var obj in quest.Objectives)
							_questObjectiveStatus.Add((obj.Type, obj.ObjectID),
													new QuestObjectiveStatusData()
													{
														QuestStatusPair = (questId, questStatusData),
														Objective = obj
													});

						SetQuestSlot(slot, questId);
						SetQuestSlotEndTime(slot, endTime);
						SetQuestSlotAcceptTime(slot, acceptTime);

						if (questStatusData.Status == QuestStatus.Complete)
							SetQuestSlotState(slot, QuestSlotStateMask.Complete);
						else if (questStatusData.Status == QuestStatus.Failed)
							SetQuestSlotState(slot, QuestSlotStateMask.Fail);

						++slot;
					}

					_mQuestStatus[questId] = questStatusData;
					Log.outDebug(LogFilter.ServerLoading, "Quest status is {0} for quest {1} for player (GUID: {2})", questStatusData.Status, questId, GUID.ToString());
				}
			} while (result.NextRow());

		// clear quest log tail
		for (var i = slot; i < SharedConst.MaxQuestLogSize; ++i)
			SetQuestSlot(i, 0);
	}

	void _LoadQuestStatusObjectives(SQLResult result)
	{
		if (!result.IsEmpty())
			do
			{
				var questID = result.Read<uint>(0);

				var quest = Global.ObjectMgr.GetQuestTemplate(questID);

				var questStatusData = _mQuestStatus.LookupByKey(questID);

				if (questStatusData != null && questStatusData.Slot < SharedConst.MaxQuestLogSize && quest != null)
				{
					var storageIndex = result.Read<byte>(1);

					var objective = quest.Objectives.FirstOrDefault(objective => objective.StorageIndex == storageIndex);

					if (objective != null)
					{
						var data = result.Read<int>(2);

						if (!objective.IsStoringFlag())
							SetQuestSlotCounter(questStatusData.Slot, storageIndex, (ushort)data);
						else if (data != 0)
							SetQuestSlotObjectiveFlag(questStatusData.Slot, (sbyte)storageIndex);
					}
					else
					{
						Log.outError(LogFilter.Player, $"Player {GetName()} ({GUID}) has quest {questID} out of range objective index {storageIndex}.");
					}
				}
				else
				{
					Log.outError(LogFilter.Player, $"Player {GetName()} ({GUID}) does not have quest {questID} but has objective data for it.");
				}
			} while (result.NextRow());
	}

	void _LoadQuestStatusRewarded(SQLResult result)
	{
		if (!result.IsEmpty())
			do
			{
				var quest_id = result.Read<uint>(0);
				// used to be new, no delete?
				var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

				if (quest != null)
				{
					// learn rewarded spell if unknown
					LearnQuestRewardedSpells(quest);

					// set rewarded title if any
					if (quest.RewardTitleId != 0)
					{
						var titleEntry = CliDB.CharTitlesStorage.LookupByKey(quest.RewardTitleId);

						if (titleEntry != null)
							SetTitle(titleEntry);
					}

					// Skip loading special quests - they are also added to rewarded quests but only once and remain there forever
					// instead add them separately from load daily/weekly/monthly/seasonal
					if (!quest.IsDailyOrWeekly && !quest.IsMonthly && !quest.IsSeasonal)
					{
						var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

						if (questBit != 0)
							SetQuestCompletedBit(questBit, true);
					}

					for (uint i = 0; i < quest.RewChoiceItemsCount; ++i)
						Session.CollectionMgr.AddItemAppearance(quest.RewardChoiceItemId[i]);

					for (uint i = 0; i < quest.RewItemsCount; ++i)
						Session.CollectionMgr.AddItemAppearance(quest.RewardItemId[i]);

					var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);

					if (questPackageItems != null)
						foreach (var questPackageItem in questPackageItems)
						{
							var rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);

							if (rewardProto != null)
								if (rewardProto.ItemSpecClassMask.HasAnyFlag(ClassMask))
									Session.CollectionMgr.AddItemAppearance(questPackageItem.ItemID);
						}

					if (quest.CanIncreaseRewardedQuestCounters())
						_rewardedQuests.Add(quest_id);
				}
			} while (result.NextRow());
	}

	void _LoadDailyQuestStatus(SQLResult result)
	{
		_dfQuests.Clear();

		//QueryResult* result = CharacterDatabase.PQuery("SELECT quest, time FROM character_queststatus_daily WHERE guid = '{0}'");
		if (!result.IsEmpty())
			do
			{
				var quest_id = result.Read<uint>(0);
				var qQuest = Global.ObjectMgr.GetQuestTemplate(quest_id);

				if (qQuest != null)
					if (qQuest.IsDFQuest)
					{
						_dfQuests.Add(qQuest.Id);
						_lastDailyQuestTime = result.Read<uint>(1);

						continue;
					}

				// save _any_ from daily quest times (it must be after last reset anyway)
				_lastDailyQuestTime = result.Read<long>(1);

				var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

				if (quest == null)
					continue;

				AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DailyQuestsCompleted), quest_id);
				var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

				if (questBit != 0)
					SetQuestCompletedBit(questBit, true);

				Log.outDebug(LogFilter.Player, "Daily quest ({0}) cooldown for player (GUID: {1})", quest_id, GUID.ToString());
			} while (result.NextRow());

		_dailyQuestChanged = false;
	}

	void _LoadWeeklyQuestStatus(SQLResult result)
	{
		_weeklyquests.Clear();

		if (!result.IsEmpty())
			do
			{
				var quest_id = result.Read<uint>(0);
				var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

				if (quest == null)
					continue;

				_weeklyquests.Add(quest_id);
				var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

				if (questBit != 0)
					SetQuestCompletedBit(questBit, true);

				Log.outDebug(LogFilter.Player, "Weekly quest {0} cooldown for player (GUID: {1})", quest_id, GUID.ToString());
			} while (result.NextRow());

		_weeklyQuestChanged = false;
	}

	void _LoadSeasonalQuestStatus(SQLResult result)
	{
		_seasonalquests.Clear();

		if (!result.IsEmpty())
			do
			{
				var quest_id = result.Read<uint>(0);
				var event_id = result.Read<uint>(1);
				var completedTime = result.Read<long>(2);
				var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

				if (quest == null)
					continue;

				if (!_seasonalquests.ContainsKey(event_id))
					_seasonalquests[event_id] = new Dictionary<uint, long>();

				_seasonalquests[event_id][quest_id] = completedTime;

				var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

				if (questBit != 0)
					SetQuestCompletedBit(questBit, true);

				Log.outDebug(LogFilter.Player, "Seasonal quest {0} cooldown for player (GUID: {1})", quest_id, GUID.ToString());
			} while (result.NextRow());

		_seasonalQuestChanged = false;
	}

	void _LoadMonthlyQuestStatus()
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
		stmt.AddValue(0, GUID.Counter);
		var result = DB.Characters.Query(stmt);

		_monthlyquests.Clear();

		if (!result.IsEmpty())
			do
			{
				var quest_id = result.Read<uint>(0);
				var quest = Global.ObjectMgr.GetQuestTemplate(quest_id);

				if (quest == null)
					continue;

				_monthlyquests.Add(quest_id);
				var questBit = Global.DB2Mgr.GetQuestUniqueBitFlag(quest_id);

				if (questBit != 0)
					SetQuestCompletedBit(questBit, true);

				Log.outDebug(LogFilter.Player, "Monthly quest {0} cooldown for player (GUID: {1})", quest_id, GUID.ToString());
			} while (result.NextRow());

		_monthlyQuestChanged = false;
	}

	void _LoadTalents(SQLResult result)
	{
		if (!result.IsEmpty())
			do
			{
				var talent = CliDB.TalentStorage.LookupByKey(result.Read<uint>(0));

				if (talent != null)
					AddTalent(talent, result.Read<byte>(1), false);
			} while (result.NextRow());
	}

	void _LoadPvpTalents(SQLResult result)
	{
		// "SELECT talentID0, talentID1, talentID2, talentID3, talentGroup FROM character_pvp_talent WHERE guid = ?"
		if (!result.IsEmpty())
			do
			{
				for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
				{
					var talent = CliDB.PvpTalentStorage.LookupByKey(result.Read<uint>(slot));

					if (talent != null)
						AddPvpTalent(talent, result.Read<byte>(4), slot);
				}
			} while (result.NextRow());
	}

	void _LoadTraits(SQLResult configsResult, SQLResult entriesResult)
	{
		MultiMap<int, TraitEntryPacket> traitEntriesByConfig = new();

		if (!entriesResult.IsEmpty())
			//                    0            1,                2     3             4
			// SELECT traitConfigId, traitNodeId, traitNodeEntryId, rank, grantedRanks FROM character_trait_entry WHERE guid = ?
			do
			{
				TraitEntryPacket traitEntry = new();
				traitEntry.TraitNodeID = entriesResult.Read<int>(1);
				traitEntry.TraitNodeEntryID = entriesResult.Read<int>(2);
				traitEntry.Rank = entriesResult.Read<int>(3);
				traitEntry.GrantedRanks = entriesResult.Read<int>(4);

				if (!TraitMgr.IsValidEntry(traitEntry))
					continue;

				traitEntriesByConfig.Add(entriesResult.Read<int>(0), traitEntry);
			} while (entriesResult.NextRow());

		if (!configsResult.IsEmpty())
			//                    0     1                    2                  3                4            5              6      7
			// SELECT traitConfigId, type, chrSpecializationId, combatConfigFlags, localIdentifier, skillLineId, traitSystemId, `name` FROM character_trait_config WHERE guid = ?
			do
			{
				TraitConfigPacket traitConfig = new();
				traitConfig.ID = configsResult.Read<int>(0);
				traitConfig.Type = (TraitConfigType)configsResult.Read<int>(1);

				switch (traitConfig.Type)
				{
					case TraitConfigType.Combat:
						traitConfig.ChrSpecializationID = configsResult.Read<int>(2);
						traitConfig.CombatConfigFlags = (TraitCombatConfigFlags)configsResult.Read<int>(3);
						traitConfig.LocalIdentifier = configsResult.Read<int>(4);

						break;
					case TraitConfigType.Profession:
						traitConfig.SkillLineID = configsResult.Read<uint>(5);

						break;
					case TraitConfigType.Generic:
						traitConfig.TraitSystemID = configsResult.Read<int>(6);

						break;
					default:
						break;
				}

				traitConfig.Name = configsResult.Read<string>(7);

				foreach (var traitEntry in traitEntriesByConfig.LookupByKey(traitConfig.ID))
					traitConfig.AddEntry(traitEntry);

				if (TraitMgr.ValidateConfig(traitConfig, this) != TalentLearnResult.LearnOk)
				{
					traitConfig.Entries.Clear();

					foreach (var grantedEntry in TraitMgr.GetGrantedTraitEntriesForConfig(traitConfig, this))
						traitConfig.AddEntry(new TraitEntryPacket(grantedEntry));
				}

				AddTraitConfig(traitConfig);
			} while (configsResult.NextRow());

		bool hasConfigForSpec(int specId)
		{
			return ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.Type == (int)TraitConfigType.Combat && traitConfig.ChrSpecializationID == specId && (traitConfig.CombatConfigFlags & (int)TraitCombatConfigFlags.ActiveForSpec) != 0; }) >= 0;
		}

		int findFreeLocalIdentifier(int specId)
		{
			var index = 1;

			while (ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.Type == (int)TraitConfigType.Combat && traitConfig.ChrSpecializationID == specId && traitConfig.LocalIdentifier == index; }) >= 0)
				++index;

			return index;
		}

		for (uint i = 0; i < PlayerConst.MaxSpecializations - 1 /*initial spec doesnt get a config*/; ++i)
		{
			var spec = Global.DB2Mgr.GetChrSpecializationByIndex(Class, i);

			if (spec != null)
			{
				if (hasConfigForSpec((int)spec.Id))
					continue;

				TraitConfigPacket traitConfig = new();
				traitConfig.Type = TraitConfigType.Combat;
				traitConfig.ChrSpecializationID = (int)spec.Id;
				traitConfig.CombatConfigFlags = TraitCombatConfigFlags.ActiveForSpec;
				traitConfig.LocalIdentifier = findFreeLocalIdentifier((int)spec.Id);
				traitConfig.Name = spec.Name[Session.SessionDbcLocale];

				CreateTraitConfig(traitConfig);
			}
		}

		var activeConfig = ActivePlayerData.TraitConfigs.FindIndexIf(traitConfig => { return traitConfig.Type == (int)TraitConfigType.Combat && traitConfig.ChrSpecializationID == GetPrimarySpecialization() && (traitConfig.CombatConfigFlags & (int)TraitCombatConfigFlags.ActiveForSpec) != 0; });

		if (activeConfig >= 0)
			SetActiveCombatTraitConfigID(ActivePlayerData.TraitConfigs[activeConfig].ID);

		foreach (var traitConfig in ActivePlayerData.TraitConfigs)
		{
			switch ((TraitConfigType)(int)traitConfig.Type)
			{
				case TraitConfigType.Combat:
					if (traitConfig.ID != ActivePlayerData.ActiveCombatTraitConfigID)
						continue;

					break;
				case TraitConfigType.Profession:
					if (!HasSkill((uint)(int)traitConfig.SkillLineID))
						continue;

					break;
				default:
					break;
			}

			ApplyTraitConfig(traitConfig.ID, true);
		}
	}

	void _LoadGlyphs(SQLResult result)
	{
		// SELECT talentGroup, glyphId from character_glyphs WHERE guid = ?
		if (result.IsEmpty())
			return;

		do
		{
			var spec = result.Read<byte>(0);

			if (spec >= PlayerConst.MaxSpecializations || Global.DB2Mgr.GetChrSpecializationByIndex(Class, spec) == null)
				continue;

			var glyphId = result.Read<ushort>(1);

			if (!CliDB.GlyphPropertiesStorage.ContainsKey(glyphId))
				continue;

			GetGlyphs(spec).Add(glyphId);
		} while (result.NextRow());
	}

	void _LoadGlyphAuras()
	{
		foreach (var glyphId in GetGlyphs(GetActiveTalentGroup()))
			CastSpell(this, CliDB.GlyphPropertiesStorage.LookupByKey(glyphId).SpellID, true);
	}

	void _LoadVoidStorage(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			// SELECT itemId, itemEntry, slot, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs FROM character_void_storage WHERE playerGuid = ?
			var itemId = result.Read<ulong>(0);
			var itemEntry = result.Read<uint>(1);
			var slot = result.Read<byte>(2);
			var creatorGuid = result.Read<ulong>(3) != 0 ? ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3)) : ObjectGuid.Empty;
			var randomBonusListId = result.Read<uint>(4);
			var fixedScalingLevel = result.Read<uint>(5);
			var artifactKnowledgeLevel = result.Read<uint>(6);
			var context = (ItemContext)result.Read<byte>(7);
			List<uint> bonusListIDs = new();
			var bonusListIdTokens = new StringArray(result.Read<string>(8), ' ');

			for (var i = 0; i < bonusListIdTokens.Length; ++i)
				if (uint.TryParse(bonusListIdTokens[i], out var id))
					bonusListIDs.Add(id);

			if (itemId == 0)
			{
				Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, name: {1}) has an item with an invalid id (item id: item id: {2}, entry: {3}).", GUID.ToString(), GetName(), itemId, itemEntry);

				continue;
			}

			if (Global.ObjectMgr.GetItemTemplate(itemEntry) == null)
			{
				Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, name: {1}) has an item with an invalid entry (item id: item id: {2}, entry: {3}).", GUID.ToString(), GetName(), itemId, itemEntry);

				continue;
			}

			if (slot >= SharedConst.VoidStorageMaxSlot)
			{
				Log.outError(LogFilter.Player, "Player:_LoadVoidStorage - Player (GUID: {0}, name: {1}) has an item with an invalid slot (item id: item id: {2}, entry: {3}, slot: {4}).", GUID.ToString(), GetName(), itemId, itemEntry, slot);

				continue;
			}

			_voidStorageItems[slot] = new VoidStorageItem(itemId, itemEntry, creatorGuid, randomBonusListId, fixedScalingLevel, artifactKnowledgeLevel, context, bonusListIDs);

			BonusData bonus = new(new ItemInstance(_voidStorageItems[slot]));
			Session.CollectionMgr.AddItemAppearance(itemEntry, bonus.AppearanceModID);
		} while (result.NextRow());
	}

	static Item _LoadMailedItem(ObjectGuid playerGuid, Player player, ulong mailId, Mail mail, SQLFields fields, ItemAdditionalLoadInfo addionalData)
	{
		var itemGuid = fields.Read<ulong>(0);
		var itemEntry = fields.Read<uint>(1);

		var proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

		if (proto == null)
		{
			Log.outError(LogFilter.Player, $"Player {(player != null ? player.GetName() : "<unknown>")} ({playerGuid}) has unknown item in mailed items (GUID: {itemGuid} template: {itemEntry}) in mail ({mailId}), deleted.");

			SQLTransaction trans = new();

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_MAIL_ITEM);
			stmt.AddValue(0, itemGuid);
			trans.Append(stmt);

			Item.DeleteFromDB(trans, itemGuid);
			AzeriteItem.DeleteFromDB(trans, itemGuid);
			AzeriteEmpoweredItem.DeleteFromDB(trans, itemGuid);

			DB.Characters.CommitTransaction(trans);

			return null;
		}

		var item = Item.NewItemOrBag(proto);
		var ownerGuid = fields.Read<ulong>(51) != 0 ? ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(51)) : ObjectGuid.Empty;

		if (!item.LoadFromDB(itemGuid, ownerGuid, fields, itemEntry))
		{
			Log.outError(LogFilter.Player, $"Player._LoadMailedItems: Item (GUID: {itemGuid}) in mail ({mailId}) doesn't exist, deleted from mail.");

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_MAIL_ITEM);
			stmt.AddValue(0, itemGuid);
			DB.Characters.Execute(stmt);

			item.FSetState(ItemUpdateState.Removed);

			item.SaveToDB(null); // it also deletes item object !

			return null;
		}

		if (addionalData != null)
		{
			if (item.Template.ArtifactID != 0 && addionalData.Artifact != null)
				item.LoadArtifactData(player,
									addionalData.Artifact.Xp,
									addionalData.Artifact.ArtifactAppearanceId,
									addionalData.Artifact.ArtifactTierId,
									addionalData.Artifact.ArtifactPowers);

			if (addionalData.AzeriteItem != null)
			{
				var azeriteItem = item.AsAzeriteItem;

				if (azeriteItem != null)
					azeriteItem.LoadAzeriteItemData(player, addionalData.AzeriteItem);
			}

			if (addionalData.AzeriteEmpoweredItem != null)
			{
				var azeriteEmpoweredItem = item.AsAzeriteEmpoweredItem;

				if (azeriteEmpoweredItem != null)
					azeriteEmpoweredItem.LoadAzeriteEmpoweredItemData(player, addionalData.AzeriteEmpoweredItem);
			}
		}

		if (mail != null)
			mail.AddItem(itemGuid, itemEntry);

		if (player != null)
			player.AddMItem(item);

		return item;
	}

	void _LoadDeclinedNames(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		_declinedname = new DeclinedName();

		for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
			_declinedname.Name[i] = result.Read<string>(i);
	}

	void _LoadArenaTeamInfo(SQLResult result)
	{
		// arenateamid, played_week, played_season, personal_rating
		ushort[] personalRatingCache =
		{
			0, 0, 0
		};

		if (!result.IsEmpty())
			do
			{
				var arenaTeamId = result.Read<uint>(0);

				var arenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(arenaTeamId);

				if (arenaTeam == null)
				{
					Log.outError(LogFilter.Player, "Player:_LoadArenaTeamInfo: couldn't load arenateam {0}", arenaTeamId);

					continue;
				}

				var arenaSlot = arenaTeam.GetSlot();

				personalRatingCache[arenaSlot] = result.Read<ushort>(4);

				SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.Id, arenaTeamId);
				SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.Type, arenaTeam.GetArenaType());
				SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.Member, (uint)(arenaTeam.GetCaptain() == GUID ? 0 : 1));
				SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.GamesWeek, result.Read<ushort>(1));
				SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.GamesSeason, result.Read<ushort>(2));
				SetArenaTeamInfoField(arenaSlot, ArenaTeamInfoType.WinsSeason, result.Read<ushort>(3));
			} while (result.NextRow());

		for (byte slot = 0; slot <= 2; ++slot)
			SetArenaTeamInfoField(slot, ArenaTeamInfoType.PersonalRating, personalRatingCache[slot]);
	}

	void _LoadStoredAuraTeleportLocations(SQLResult result)
	{
		//                                                       0      1      2          3          4          5
		//QueryResult* result = CharacterDatabase.PQuery("SELECT Spell, MapId, PositionX, PositionY, PositionZ, Orientation FROM character_spell_location WHERE Guid = ?", GetGUIDLow());

		_storedAuraTeleportLocations.Clear();

		if (!result.IsEmpty())
			do
			{
				var spellId = result.Read<uint>(0);

				if (!Global.SpellMgr.HasSpellInfo(spellId, Difficulty.None))
				{
					Log.outError(LogFilter.Spells, $"Player._LoadStoredAuraTeleportLocations: Player {GetName()} ({GUID}) spell (ID: {spellId}) does not exist");

					continue;
				}

				WorldLocation location = new(result.Read<uint>(1), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));

				if (!GridDefines.IsValidMapCoord(location))
				{
					Log.outError(LogFilter.Spells, $"Player._LoadStoredAuraTeleportLocations: Player {GetName()} ({GUID}) spell (ID: {spellId}) has invalid position on map {location.MapId}, {location}.");

					continue;
				}

				StoredAuraTeleportLocation storedLocation = new();
				storedLocation.Loc = location;
				storedLocation.CurrentState = StoredAuraTeleportLocation.State.Unchanged;

				_storedAuraTeleportLocations[spellId] = storedLocation;
			} while (result.NextRow());
	}

	void _LoadGroup(SQLResult result)
	{
		if (!result.IsEmpty())
		{
			var group = Global.GroupMgr.GetGroupByDbStoreId(result.Read<uint>(0));

			if (group)
			{
				if (group.IsLeader(GUID))
					SetPlayerFlag(PlayerFlags.GroupLeader);

				var subgroup = group.GetMemberGroup(GUID);
				SetGroup(group, subgroup);
				SetPartyType(group.GroupCategory, GroupType.Normal);
				ResetGroupUpdateSequenceIfNeeded(group);

				// the group leader may change the instance difficulty while the player is offline
				DungeonDifficultyId = group.DungeonDifficultyID;
				RaidDifficultyId = group.RaidDifficultyID;
				LegacyRaidDifficultyId = group.LegacyRaidDifficultyID;
			}
		}

		if (!Group || !Group.IsLeader(GUID))
			RemovePlayerFlag(PlayerFlags.GroupLeader);
	}

	void _LoadInstanceTimeRestrictions(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			_instanceResetTimes.Add(result.Read<uint>(0), result.Read<long>(1));
		} while (result.NextRow());
	}

	void _LoadEquipmentSets(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			EquipmentSetInfo eqSet = new();
			eqSet.Data.Guid = result.Read<ulong>(0);
			eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Equipment;
			eqSet.Data.SetId = result.Read<byte>(1);
			eqSet.Data.SetName = result.Read<string>(2);
			eqSet.Data.SetIcon = result.Read<string>(3);
			eqSet.Data.IgnoreMask = result.Read<uint>(4);
			eqSet.Data.AssignedSpecIndex = result.Read<int>(5);
			eqSet.State = EquipmentSetUpdateState.Unchanged;

			for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
			{
				ulong guid = result.Read<uint>(6 + i);

				if (guid != 0)
					eqSet.Data.Pieces[i] = ObjectGuid.Create(HighGuid.Item, guid);
			}

			if (eqSet.Data.SetId >= ItemConst.MaxEquipmentSetIndex) // client limit
				continue;

			_equipmentSets[eqSet.Data.Guid] = eqSet;
		} while (result.NextRow());
	}

	void _LoadTransmogOutfits(SQLResult result)
	{
		//             0         1     2         3            4            5            6            7            8            9
		//SELECT setguid, setindex, name, iconname, ignore_mask, appearance0, appearance1, appearance2, appearance3, appearance4,
		//             10           11           12           13           14            15            16            17            18            19            20            21
		//    appearance5, appearance6, appearance7, appearance8, appearance9, appearance10, appearance11, appearance12, appearance13, appearance14, appearance15, appearance16,
		//              22            23               24              25
		//    appearance17, appearance18, mainHandEnchant, offHandEnchant FROM character_transmog_outfits WHERE guid = ? ORDER BY setindex
		if (result.IsEmpty())
			return;

		do
		{
			EquipmentSetInfo eqSet = new();

			eqSet.Data.Guid = result.Read<ulong>(0);
			eqSet.Data.Type = EquipmentSetInfo.EquipmentSetType.Transmog;
			eqSet.Data.SetId = result.Read<byte>(1);
			eqSet.Data.SetName = result.Read<string>(2);
			eqSet.Data.SetIcon = result.Read<string>(3);
			eqSet.Data.IgnoreMask = result.Read<uint>(4);
			eqSet.State = EquipmentSetUpdateState.Unchanged;

			for (int i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
				eqSet.Data.Appearances[i] = result.Read<int>(5 + i);

			for (var i = 0; i < eqSet.Data.Enchants.Length; ++i)
				eqSet.Data.Enchants[i] = result.Read<int>(24 + i);

			if (eqSet.Data.SetId >= ItemConst.MaxEquipmentSetIndex) // client limit
				continue;

			_equipmentSets[eqSet.Data.Guid] = eqSet;
		} while (result.NextRow());
	}

	void _LoadCUFProfiles(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		do
		{
			var id = result.Read<byte>(0);
			var name = result.Read<string>(1);
			var frameHeight = result.Read<ushort>(2);
			var frameWidth = result.Read<ushort>(3);
			var sortBy = result.Read<byte>(4);
			var healthText = result.Read<byte>(5);
			var boolOptions = result.Read<uint>(6);
			var topPoint = result.Read<byte>(7);
			var bottomPoint = result.Read<byte>(8);
			var leftPoint = result.Read<byte>(9);
			var topOffset = result.Read<ushort>(10);
			var bottomOffset = result.Read<ushort>(11);
			var leftOffset = result.Read<ushort>(12);

			if (id > PlayerConst.MaxCUFProfiles)
			{
				Log.outError(LogFilter.Player, "Player._LoadCUFProfiles - Player (GUID: {0}, name: {1}) has an CUF profile with invalid id (id: {2}), max is {3}.", GUID.ToString(), GetName(), id, PlayerConst.MaxCUFProfiles);

				continue;
			}

			_cufProfiles[id] = new CufProfile(name, frameHeight, frameWidth, sortBy, healthText, boolOptions, topPoint, bottomPoint, leftPoint, topOffset, bottomOffset, leftOffset);
		} while (result.NextRow());
	}

	void _LoadRandomBGStatus(SQLResult result)
	{
		if (!result.IsEmpty())
			_isBgRandomWinner = true;
	}

	void _LoadBGData(SQLResult result)
	{
		if (result.IsEmpty())
			return;

		// Expecting only one row
		//        0           1     2      3      4      5      6          7          8        9
		// SELECT instanceId, team, joinX, joinY, joinZ, joinO, joinMapId, taxiStart, taxiEnd, mountSpell FROM character_Battleground_data WHERE guid = ?
		_bgData.BgInstanceId = result.Read<uint>(0);
		_bgData.BgTeam = result.Read<ushort>(1);
		_bgData.JoinPos = new WorldLocation(result.Read<ushort>(6), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), result.Read<float>(5));
		_bgData.TaxiPath[0] = result.Read<uint>(7);
		_bgData.TaxiPath[1] = result.Read<uint>(8);
		_bgData.MountSpell = result.Read<uint>(9);
	}

	void _LoadPetStable(uint summonedPetNumber, SQLResult result)
	{
		if (result.IsEmpty())
			return;

		_petStable = new PetStable();

		//         0      1        2      3    4           5     6     7        8          9       10      11        12              13       14              15
		// SELECT id, entry, modelid, level, exp, Reactstate, slot, name, renamed, curhealth, curmana, abdata, savetime, CreatedBySpell, PetType, specialization FROM character_pet WHERE owner = ?
		if (!result.IsEmpty())
			do
			{
				PetStable.PetInfo petInfo = new();
				petInfo.PetNumber = result.Read<uint>(0);
				petInfo.CreatureId = result.Read<uint>(1);
				petInfo.DisplayId = result.Read<uint>(2);
				petInfo.Level = result.Read<byte>(3);
				petInfo.Experience = result.Read<uint>(4);
				petInfo.ReactState = (ReactStates)result.Read<byte>(5);
				var slot = (PetSaveMode)result.Read<short>(6);
				petInfo.Name = result.Read<string>(7);
				petInfo.WasRenamed = result.Read<bool>(8);
				petInfo.Health = result.Read<uint>(9);
				petInfo.Mana = result.Read<uint>(10);
				petInfo.ActionBar = result.Read<string>(11);
				petInfo.LastSaveTime = result.Read<uint>(12);
				petInfo.CreatedBySpellId = result.Read<uint>(13);
				petInfo.Type = (PetType)result.Read<byte>(14);
				petInfo.SpecializationId = result.Read<ushort>(15);

				if (slot >= PetSaveMode.FirstActiveSlot && slot < PetSaveMode.LastActiveSlot)
					_petStable.ActivePets[(int)slot] = petInfo;
				else if (slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot)
					_petStable.StabledPets[slot - PetSaveMode.FirstStableSlot] = petInfo;
				else if (slot == PetSaveMode.NotInSlot)
					_petStable.UnslottedPets.Add(petInfo);
			} while (result.NextRow());

		if (Pet.GetLoadPetInfo(_petStable, 0, summonedPetNumber, null).Item1 != null)
			_temporaryUnsummonedPetNumber = summonedPetNumber;
	}


	void _SaveInventory(SQLTransaction trans)
	{
		PreparedStatement stmt;

		// force items in buyback slots to new state
		// and remove those that aren't already
		for (var i = InventorySlots.BuyBackStart; i < InventorySlots.BuyBackEnd; ++i)
		{
			var item = _items[i];

			if (item == null)
				continue;

			var itemTemplate = item.Template;

			if (item.State == ItemUpdateState.New)
			{
				if (itemTemplate != null)
					if (itemTemplate.HasFlag(ItemFlags.HasLoot))
						Global.LootItemStorage.RemoveStoredLootForContainer(item.GUID.Counter);

				continue;
			}

			item.DeleteFromInventoryDB(trans);
			item.DeleteFromDB(trans);
			_items[i].FSetState(ItemUpdateState.New);

			if (itemTemplate != null)
				if (itemTemplate.HasFlag(ItemFlags.HasLoot))
					Global.LootItemStorage.RemoveStoredLootForContainer(item.GUID.Counter);
		}

		// Updated played time for refundable items. We don't do this in Player.Update because there's simply no need for it,
		// the client auto counts down in real time after having received the initial played time on the first
		// SMSG_ITEM_REFUND_INFO_RESPONSE packet.
		// Item.UpdatePlayedTime is only called when needed, which is in DB saves, and item refund info requests.
		foreach (var guid in _refundableItems)
		{
			var item = GetItemByGuid(guid);

			if (item != null)
			{
				item.UpdatePlayedTime(this);

				continue;
			}
			else
			{
				Log.outError(LogFilter.Player, "Can't find item guid {0} but is in refundable storage for player {1} ! Removing.", guid, GUID.ToString());
				_refundableItems.Remove(guid);
			}
		}

		// update enchantment durations
		foreach (var enchant in _enchantDurations)
			enchant.Item.SetEnchantmentDuration(enchant.Slot, enchant.Leftduration, this);

		// if no changes
		if (ItemUpdateQueue.Count == 0)
			return;

		for (var i = 0; i < ItemUpdateQueue.Count; ++i)
		{
			var item = ItemUpdateQueue[i];

			if (item == null)
				continue;

			var container = item.Container;

			if (item.State != ItemUpdateState.Removed)
			{
				var test = GetItemByPos(item.BagSlot, item.Slot);

				if (test == null)
				{
					ulong bagTestGUID = 0;
					var test2 = GetItemByPos(InventorySlots.Bag0, item.BagSlot);

					if (test2 != null)
						bagTestGUID = test2.GUID.Counter;

					Log.outError(LogFilter.Player,
								"Player(GUID: {0} Name: {1}).SaveInventory - the bag({2}) and slot({3}) values for the item with guid {4} (state {5}) are incorrect, " +
								"the player doesn't have an item at that position!",
								GUID.ToString(),
								GetName(),
								item.BagSlot,
								item.Slot,
								item.GUID.ToString(),
								item.State);

					// according to the test that was just performed nothing should be in this slot, delete
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_BAG_SLOT);
					stmt.AddValue(0, bagTestGUID);
					stmt.AddValue(1, item.Slot);
					stmt.AddValue(2, GUID.Counter);
					trans.Append(stmt);

					RemoveTradeableItem(item);
					RemoveEnchantmentDurationsReferences(item);
					RemoveItemDurations(item);

					// also THIS item should be somewhere else, cheat attempt
					item.FSetState(ItemUpdateState.Removed); // we are IN updateQueue right now, can't use SetState which modifies the queue
					DeleteRefundReference(item.GUID);
				}
				else if (test != item)
				{
					Log.outError(LogFilter.Player,
								"Player(GUID: {0} Name: {1}).SaveInventory - the bag({2}) and slot({3}) values for the item with guid {4} are incorrect, " +
								"the item with guid {5} is there instead!",
								GUID.ToString(),
								GetName(),
								item.BagSlot,
								item.Slot,
								item.GUID.ToString(),
								test.GUID.ToString());

					// save all changes to the item...
					if (item.State != ItemUpdateState.New) // only for existing items, no dupes
						item.SaveToDB(trans);

					// ...but do not save position in inventory
					continue;
				}
			}

			switch (item.State)
			{
				case ItemUpdateState.New:
				case ItemUpdateState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_INVENTORY_ITEM);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, container ? container.GUID.Counter : 0);
					stmt.AddValue(2, item.Slot);
					stmt.AddValue(3, item.GUID.Counter);
					trans.Append(stmt);

					break;
				case ItemUpdateState.Removed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
					stmt.AddValue(0, item.GUID.Counter);
					trans.Append(stmt);

					break;
				case ItemUpdateState.Unchanged:
					break;
			}

			item.SaveToDB(trans); // item have unchanged inventory record and can be save standalone
		}

		ItemUpdateQueue.Clear();
	}

	void _SaveSkills(SQLTransaction trans)
	{
		PreparedStatement stmt; // = null;

		SkillInfo skillInfoField = ActivePlayerData.Skill;

		foreach (var pair in _skillStatus.ToList())
		{
			if (pair.Value.State == SkillState.Unchanged)
				continue;

			var value = skillInfoField.SkillRank[pair.Value.Pos];
			var max = skillInfoField.SkillMaxRank[pair.Value.Pos];
			var professionSlot = (sbyte)GetProfessionSlotFor(pair.Key);

			switch (pair.Value.State)
			{
				case SkillState.New:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SKILLS);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, (ushort)pair.Key);
					stmt.AddValue(2, value);
					stmt.AddValue(3, max);
					stmt.AddValue(4, professionSlot);
					trans.Append(stmt);

					break;
				case SkillState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_SKILLS);
					stmt.AddValue(0, value);
					stmt.AddValue(1, max);
					stmt.AddValue(2, professionSlot);
					stmt.AddValue(3, GUID.Counter);
					stmt.AddValue(4, (ushort)pair.Key);
					trans.Append(stmt);

					break;
				case SkillState.Deleted:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_BY_SKILL);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, pair.Key);
					trans.Append(stmt);

					break;
				default:
					break;
			}

			pair.Value.State = SkillState.Unchanged;
		}
	}

	void _SaveSpells(SQLTransaction trans)
	{
		PreparedStatement stmt;

		foreach (var (id, spell) in _spells.ToList())
		{
			if (spell.State == PlayerSpellState.Removed || spell.State == PlayerSpellState.Changed)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
				stmt.AddValue(0, id);
				stmt.AddValue(1, GUID.Counter);
				trans.Append(stmt);
			}

			if (spell.State == PlayerSpellState.New || spell.State == PlayerSpellState.Changed)
			{
				// add only changed/new not dependent spells
				if (!spell.Dependent)
				{
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, id);
					stmt.AddValue(2, spell.Active);
					stmt.AddValue(3, spell.Disabled);
					trans.Append(stmt);
				}

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_FAVORITE);
				stmt.AddValue(0, id);
				stmt.AddValue(1, GUID.Counter);
				trans.Append(stmt);

				if (spell.Favorite)
				{
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SPELL_FAVORITE);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, id);
					trans.Append(stmt);
				}
			}

			if (spell.State == PlayerSpellState.Removed)
			{
				_spells.Remove(id);

				continue;
			}

			if (spell.State != PlayerSpellState.Temporary)
				spell.State = PlayerSpellState.Unchanged;
		}
	}

	void _SaveAuras(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_EFFECT);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		byte index;

		foreach (var aura in GetAuraQuery().CanBeSaved().GetResults())
		{
			var key = aura.GenerateKey(out var recalculateMask);

			index = 0;
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AURA);
			stmt.AddValue(index++, GUID.Counter);
			stmt.AddValue(index++, key.Caster.GetRawValue());
			stmt.AddValue(index++, key.Item.GetRawValue());
			stmt.AddValue(index++, key.SpellId);
			stmt.AddValue(index++, key.EffectMask);
			stmt.AddValue(index++, recalculateMask);
			stmt.AddValue(index++, (byte)aura.CastDifficulty);
			stmt.AddValue(index++, aura.StackAmount);
			stmt.AddValue(index++, aura.MaxDuration);
			stmt.AddValue(index++, aura.Duration);
			stmt.AddValue(index++, aura.Charges);
			stmt.AddValue(index++, aura.CastItemId);
			stmt.AddValue(index, aura.CastItemLevel);
			trans.Append(stmt);

			foreach (var effect in aura.AuraEffects)
			{
				index = 0;
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AURA_EFFECT);
				stmt.AddValue(index++, GUID.Counter);
				stmt.AddValue(index++, key.Caster.GetRawValue());
				stmt.AddValue(index++, key.Item.GetRawValue());
				stmt.AddValue(index++, key.SpellId);
				stmt.AddValue(index++, key.EffectMask);
				stmt.AddValue(index++, effect.Value.EffIndex);
				stmt.AddValue(index++, effect.Value.Amount);
				stmt.AddValue(index++, effect.Value.BaseAmount);
				trans.Append(stmt);
			}
		}
	}

	void _SaveGlyphs(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_GLYPHS);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		for (byte spec = 0; spec < PlayerConst.MaxSpecializations; ++spec)
			foreach (var glyphId in GetGlyphs(spec))
			{
				byte index = 0;

				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_GLYPHS);
				stmt.AddValue(index++, GUID.Counter);
				stmt.AddValue(index++, spec);
				stmt.AddValue(index++, glyphId);

				trans.Append(stmt);
			}
	}

	void _SaveCurrency(SQLTransaction trans)
	{
		PreparedStatement stmt;

		foreach (var (id, currency) in _currencyStorage)
		{
			var entry = CliDB.CurrencyTypesStorage.LookupByKey(id);

			if (entry == null) // should never happen
				continue;

			switch (currency.State)
			{
				case PlayerCurrencyState.New:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_PLAYER_CURRENCY);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, id);
					stmt.AddValue(2, currency.Quantity);
					stmt.AddValue(3, currency.WeeklyQuantity);
					stmt.AddValue(4, currency.TrackedQuantity);
					stmt.AddValue(5, currency.IncreasedCapQuantity);
					stmt.AddValue(6, currency.EarnedQuantity);
					stmt.AddValue(7, (byte)currency.Flags);
					trans.Append(stmt);

					break;
				case PlayerCurrencyState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_PLAYER_CURRENCY);
					stmt.AddValue(0, currency.Quantity);
					stmt.AddValue(1, currency.WeeklyQuantity);
					stmt.AddValue(2, currency.TrackedQuantity);
					stmt.AddValue(3, currency.IncreasedCapQuantity);
					stmt.AddValue(4, currency.EarnedQuantity);
					stmt.AddValue(5, (byte)currency.Flags);
					stmt.AddValue(6, GUID.Counter);
					stmt.AddValue(7, id);
					trans.Append(stmt);

					break;
				default:
					break;
			}

			currency.State = PlayerCurrencyState.Unchanged;
		}
	}

	void _SaveCustomizations(SQLTransaction trans)
	{
		if (!_customizationsChanged)
			return;

		_customizationsChanged = false;

		SavePlayerCustomizations(trans, GUID.Counter, PlayerData.Customizations);
	}

	void _SaveActions(SQLTransaction trans)
	{
		var traitConfigId = 0;

		var traitConfig = GetTraitConfig((int)(uint)ActivePlayerData.ActiveCombatTraitConfigID);

		if (traitConfig != null)
		{
			var usedSavedTraitConfigIndex = ActivePlayerData.TraitConfigs.FindIndexIf(savedConfig => { return (TraitConfigType)(int)savedConfig.Type == TraitConfigType.Combat && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.ActiveForSpec) == TraitCombatConfigFlags.None && ((TraitCombatConfigFlags)(int)savedConfig.CombatConfigFlags & TraitCombatConfigFlags.SharedActionBars) == TraitCombatConfigFlags.None && savedConfig.LocalIdentifier == traitConfig.LocalIdentifier; });

			if (usedSavedTraitConfigIndex >= 0)
				traitConfigId = ActivePlayerData.TraitConfigs[usedSavedTraitConfigIndex].ID;
		}

		PreparedStatement stmt;

		foreach (var pair in _actionButtons.ToList())
			switch (pair.Value.UState)
			{
				case ActionButtonUpdateState.New:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_ACTION);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, GetActiveTalentGroup());
					stmt.AddValue(2, traitConfigId);
					stmt.AddValue(3, pair.Key);
					stmt.AddValue(4, pair.Value.GetAction());
					stmt.AddValue(5, (byte)pair.Value.GetButtonType());
					trans.Append(stmt);

					pair.Value.UState = ActionButtonUpdateState.UnChanged;

					break;
				case ActionButtonUpdateState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_ACTION);
					stmt.AddValue(0, pair.Value.GetAction());
					stmt.AddValue(1, (byte)pair.Value.GetButtonType());
					stmt.AddValue(2, GUID.Counter);
					stmt.AddValue(3, pair.Key);
					stmt.AddValue(4, GetActiveTalentGroup());
					stmt.AddValue(5, traitConfigId);
					trans.Append(stmt);

					pair.Value.UState = ActionButtonUpdateState.UnChanged;

					break;
				case ActionButtonUpdateState.Deleted:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_BUTTON_SPEC);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, pair.Key);
					stmt.AddValue(2, GetActiveTalentGroup());
					stmt.AddValue(3, traitConfigId);
					trans.Append(stmt);

					_actionButtons.Remove(pair.Key);

					break;
				default:
					break;
			}
	}

	void _SaveQuestStatus(SQLTransaction trans)
	{
		var isTransaction = trans != null;

		if (!isTransaction)
			trans = new SQLTransaction();

		PreparedStatement stmt;
		var keepAbandoned = !Global.WorldMgr.CleaningFlags.HasAnyFlag(CleaningFlags.Queststatus);

		foreach (var save in _questStatusSave)
			if (save.Value == QuestSaveType.Default)
			{
				var data = _mQuestStatus.LookupByKey(save.Key);

				if (data != null && (keepAbandoned || data.Status != QuestStatus.None))
				{
					stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, save.Key);
					stmt.AddValue(2, (byte)data.Status);
					stmt.AddValue(3, data.Explored);
					stmt.AddValue(4, (long)GetQuestSlotAcceptTime(data.Slot));
					stmt.AddValue(5, (long)GetQuestSlotEndTime(data.Slot));
					trans.Append(stmt);

					// Save objectives
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, save.Key);
					trans.Append(stmt);

					var quest = Global.ObjectMgr.GetQuestTemplate(save.Key);

					foreach (var obj in quest.Objectives)
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_QUESTSTATUS_OBJECTIVES);
						stmt.AddValue(0, GUID.Counter);
						stmt.AddValue(1, save.Key);
						stmt.AddValue(2, obj.StorageIndex);
						stmt.AddValue(3, GetQuestSlotObjectiveData(data.Slot, obj));
						trans.Append(stmt);
					}
				}
			}
			else
			{
				// Delete
				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_BY_QUEST);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, save.Key);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_OBJECTIVES_BY_QUEST);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, save.Key);
				trans.Append(stmt);
			}

		_questStatusSave.Clear();

		foreach (var save in _rewardedQuestsSave)
			if (save.Value == QuestSaveType.Default)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_QUESTSTATUS_REWARDED);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, save.Key);
				trans.Append(stmt);
			}
			else if (save.Value == QuestSaveType.ForceDelete || !keepAbandoned)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, save.Key);
				trans.Append(stmt);
			}

		_rewardedQuestsSave.Clear();

		if (!isTransaction)
			DB.Characters.CommitTransaction(trans);
	}

	void _SaveDailyQuestStatus(SQLTransaction trans)
	{
		if (!_dailyQuestChanged)
			return;

		_dailyQuestChanged = false;

		// save last daily quest time for all quests: we need only mostly reset time for reset check anyway

		// we don't need transactions here.
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_DAILY);
		stmt.AddValue(0, GUID.Counter);

		foreach (int questId in ActivePlayerData.DailyQuestsCompleted)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
			stmt.AddValue(0, GUID.Counter);
			stmt.AddValue(1, questId);
			stmt.AddValue(2, _lastDailyQuestTime);
			trans.Append(stmt);
		}

		if (!_dfQuests.Empty())
			foreach (var id in _dfQuests)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_DAILY);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, id);
				stmt.AddValue(2, _lastDailyQuestTime);
				trans.Append(stmt);
			}
	}

	void _SaveWeeklyQuestStatus(SQLTransaction trans)
	{
		if (!_weeklyQuestChanged || _weeklyquests.Empty())
			return;

		// we don't need transactions here.
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_WEEKLY);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		foreach (var quest_id in _weeklyquests)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_WEEKLY);
			stmt.AddValue(0, GUID.Counter);
			stmt.AddValue(1, quest_id);
			trans.Append(stmt);
		}

		_weeklyQuestChanged = false;
	}

	void _SaveSeasonalQuestStatus(SQLTransaction trans)
	{
		if (!_seasonalQuestChanged)
			return;

		// we don't need transactions here.
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_SEASONAL);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		_seasonalQuestChanged = false;

		if (_seasonalquests.Empty())
			return;

		foreach (var (eventId, dictionary) in _seasonalquests)
		{
			foreach (var (questId, completedTime) in dictionary)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_SEASONAL);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, questId);
				stmt.AddValue(2, eventId);
				stmt.AddValue(3, completedTime);
				trans.Append(stmt);
			}
		}
	}

	void _SaveMonthlyQuestStatus(SQLTransaction trans)
	{
		if (!_monthlyQuestChanged || _monthlyquests.Empty())
			return;

		// we don't need transactions here.
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_QUESTSTATUS_MONTHLY);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		foreach (var questId in _monthlyquests)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_QUESTSTATUS_MONTHLY);
			stmt.AddValue(0, GUID.Counter);
			stmt.AddValue(1, questId);
			trans.Append(stmt);
		}

		_monthlyQuestChanged = false;
	}

	void _SaveTalents(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TALENT);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		for (byte group = 0; group < PlayerConst.MaxSpecializations; ++group)
		{
			var talents = GetTalentMap(group);

			foreach (var pair in talents.ToList())
			{
				if (pair.Value == PlayerSpellState.Removed)
				{
					talents.Remove(pair.Key);

					continue;
				}

				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_TALENT);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, pair.Key);
				stmt.AddValue(2, group);
				trans.Append(stmt);
			}
		}

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PVP_TALENT);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		for (byte group = 0; group < PlayerConst.MaxSpecializations; ++group)
		{
			var talents = GetPvpTalentMap(group);
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_PVP_TALENT);
			stmt.AddValue(0, GUID.Counter);
			stmt.AddValue(1, talents[0]);
			stmt.AddValue(2, talents[1]);
			stmt.AddValue(3, talents[2]);
			stmt.AddValue(4, talents[3]);
			stmt.AddValue(5, group);
			trans.Append(stmt);
		}
	}

	void _SaveTraits(SQLTransaction trans)
	{
		PreparedStatement stmt = null;

		foreach (var (traitConfigId, state) in _traitConfigStates)
			switch (state)
			{
				case PlayerSpellState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, traitConfigId);
					trans.Append(stmt);

					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, traitConfigId);
					trans.Append(stmt);

					var traitConfig = GetTraitConfig(traitConfigId);

					if (traitConfig != null)
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_TRAIT_CONFIGS);
						stmt.AddValue(0, GUID.Counter);
						stmt.AddValue(1, traitConfig.ID);
						stmt.AddValue(2, traitConfig.Type);

						switch ((TraitConfigType)(int)traitConfig.Type)
						{
							case TraitConfigType.Combat:
								stmt.AddValue(3, traitConfig.ChrSpecializationID);
								stmt.AddValue(4, traitConfig.CombatConfigFlags);
								stmt.AddValue(5, traitConfig.LocalIdentifier);
								stmt.AddNull(6);
								stmt.AddNull(7);

								break;
							case TraitConfigType.Profession:
								stmt.AddNull(3);
								stmt.AddNull(4);
								stmt.AddNull(5);
								stmt.AddValue(6, traitConfig.SkillLineID);
								stmt.AddNull(7);

								break;
							case TraitConfigType.Generic:
								stmt.AddNull(3);
								stmt.AddNull(4);
								stmt.AddNull(5);
								stmt.AddNull(6);
								stmt.AddValue(7, traitConfig.TraitSystemID);

								break;
							default:
								break;
						}

						stmt.AddValue(8, traitConfig.Name);
						trans.Append(stmt);

						foreach (var traitEntry in traitConfig.Entries)
						{
							stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_TRAIT_ENTRIES);
							stmt.AddValue(0, GUID.Counter);
							stmt.AddValue(1, traitConfig.ID);
							stmt.AddValue(2, traitEntry.TraitNodeID);
							stmt.AddValue(3, traitEntry.TraitNodeEntryID);
							stmt.AddValue(4, traitEntry.Rank);
							stmt.AddValue(5, traitEntry.GrantedRanks);
							trans.Append(stmt);
						}
					}

					break;
				case PlayerSpellState.Removed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_ENTRIES);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, traitConfigId);
					trans.Append(stmt);

					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_TRAIT_CONFIGS);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, traitConfigId);
					trans.Append(stmt);

					stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACTION_BY_TRAIT_CONFIG);
					stmt.AddValue(0, GUID.Counter);
					stmt.AddValue(1, traitConfigId);
					trans.Append(stmt);

					break;
				default:
					break;
			}

		_traitConfigStates.Clear();
	}

	void _SaveStoredAuraTeleportLocations(SQLTransaction trans)
	{
		foreach (var pair in _storedAuraTeleportLocations.ToList())
		{
			var storedLocation = pair.Value;

			if (storedLocation.CurrentState == StoredAuraTeleportLocation.State.Deleted)
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATION);
				stmt.AddValue(0, GUID.Counter);
				trans.Append(stmt);
				_storedAuraTeleportLocations.Remove(pair.Key);

				continue;
			}

			if (storedLocation.CurrentState == StoredAuraTeleportLocation.State.Changed)
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_AURA_STORED_LOCATION);
				stmt.AddValue(0, GUID.Counter);
				trans.Append(stmt);

				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_AURA_STORED_LOCATION);
				stmt.AddValue(0, GUID.Counter);
				stmt.AddValue(1, pair.Key);
				stmt.AddValue(2, storedLocation.Loc.MapId);
				stmt.AddValue(3, storedLocation.Loc.X);
				stmt.AddValue(4, storedLocation.Loc.Y);
				stmt.AddValue(5, storedLocation.Loc.Z);
				stmt.AddValue(6, storedLocation.Loc.Orientation);
				trans.Append(stmt);
			}
		}
	}

	void _SaveStats(SQLTransaction trans)
	{
		// check if stat saving is enabled and if char level is high enough
		if (WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave) == 0 || Level < WorldConfig.GetIntValue(WorldCfg.MinLevelStatSave))
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_STATS);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		byte index = 0;
		stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_STATS);
		stmt.AddValue(index++, GUID.Counter);
		stmt.AddValue(index++, MaxHealth);

		for (byte i = 0; i < (int)PowerType.MaxPerClass; ++i)
			stmt.AddValue(index++, UnitData.MaxPower[i]);

		for (byte i = 0; i < (int)Stats.Max; ++i)
			stmt.AddValue(index++, GetStat((Stats)i));

		for (var i = 0; i < (int)SpellSchools.Max; ++i)
			stmt.AddValue(index++, GetResistance((SpellSchools)i));

		stmt.AddValue(index++, ActivePlayerData.BlockPercentage);
		stmt.AddValue(index++, ActivePlayerData.DodgePercentage);
		stmt.AddValue(index++, ActivePlayerData.ParryPercentage);
		stmt.AddValue(index++, ActivePlayerData.CritPercentage);
		stmt.AddValue(index++, ActivePlayerData.RangedCritPercentage);
		stmt.AddValue(index++, ActivePlayerData.SpellCritPercentage);
		stmt.AddValue(index++, UnitData.AttackPower);
		stmt.AddValue(index++, UnitData.RangedAttackPower);
		stmt.AddValue(index++, GetBaseSpellPowerBonus());
		stmt.AddValue(index, ActivePlayerData.CombatRatings[(int)CombatRating.ResiliencePlayerDamage]);

		trans.Append(stmt);
	}

	void _SaveEquipmentSets(SQLTransaction trans)
	{
		foreach (var pair in _equipmentSets)
		{
			var eqSet = pair.Value;
			PreparedStatement stmt;
			byte j = 0;

			switch (eqSet.State)
			{
				case EquipmentSetUpdateState.Unchanged:
					break; // do nothing
				case EquipmentSetUpdateState.Changed:
					if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_EQUIP_SET);
						stmt.AddValue(j++, eqSet.Data.SetName);
						stmt.AddValue(j++, eqSet.Data.SetIcon);
						stmt.AddValue(j++, eqSet.Data.IgnoreMask);
						stmt.AddValue(j++, eqSet.Data.AssignedSpecIndex);

						for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
							stmt.AddValue(j++, eqSet.Data.Pieces[i].Counter);

						stmt.AddValue(j++, GUID.Counter);
						stmt.AddValue(j++, eqSet.Data.Guid);
						stmt.AddValue(j, eqSet.Data.SetId);
					}
					else
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_TRANSMOG_OUTFIT);
						stmt.AddValue(j++, eqSet.Data.SetName);
						stmt.AddValue(j++, eqSet.Data.SetIcon);
						stmt.AddValue(j++, eqSet.Data.IgnoreMask);

						for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
							stmt.AddValue(j++, eqSet.Data.Appearances[i]);

						for (var i = 0; i < eqSet.Data.Enchants.Length; ++i)
							stmt.AddValue(j++, eqSet.Data.Enchants[i]);

						stmt.AddValue(j++, GUID.Counter);
						stmt.AddValue(j++, eqSet.Data.Guid);
						stmt.AddValue(j, eqSet.Data.SetId);
					}

					trans.Append(stmt);
					eqSet.State = EquipmentSetUpdateState.Unchanged;

					break;
				case EquipmentSetUpdateState.New:
					if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_EQUIP_SET);
						stmt.AddValue(j++, GUID.Counter);
						stmt.AddValue(j++, eqSet.Data.Guid);
						stmt.AddValue(j++, eqSet.Data.SetId);
						stmt.AddValue(j++, eqSet.Data.SetName);
						stmt.AddValue(j++, eqSet.Data.SetIcon);
						stmt.AddValue(j++, eqSet.Data.IgnoreMask);
						stmt.AddValue(j++, eqSet.Data.AssignedSpecIndex);

						for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
							stmt.AddValue(j++, eqSet.Data.Pieces[i].Counter);
					}
					else
					{
						stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_TRANSMOG_OUTFIT);
						stmt.AddValue(j++, GUID.Counter);
						stmt.AddValue(j++, eqSet.Data.Guid);
						stmt.AddValue(j++, eqSet.Data.SetId);
						stmt.AddValue(j++, eqSet.Data.SetName);
						stmt.AddValue(j++, eqSet.Data.SetIcon);
						stmt.AddValue(j++, eqSet.Data.IgnoreMask);

						for (var i = EquipmentSlot.Start; i < EquipmentSlot.End; ++i)
							stmt.AddValue(j++, eqSet.Data.Appearances[i]);

						for (var i = 0; i < eqSet.Data.Enchants.Length; ++i)
							stmt.AddValue(j++, eqSet.Data.Enchants[i]);
					}

					trans.Append(stmt);
					eqSet.State = EquipmentSetUpdateState.Unchanged;

					break;
				case EquipmentSetUpdateState.Deleted:
					if (eqSet.Data.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
						stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EQUIP_SET);
					else
						stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_TRANSMOG_OUTFIT);

					stmt.AddValue(0, eqSet.Data.Guid);
					trans.Append(stmt);
					_equipmentSets.Remove(pair.Key);

					break;
			}
		}
	}

	void _SaveVoidStorage(SQLTransaction trans)
	{
		PreparedStatement stmt;

		for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
		{
			if (_voidStorageItems[i] == null) // unused item
			{
				// DELETE FROM void_storage WHERE slot = ? AND playerGuid = ?
				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_VOID_STORAGE_ITEM_BY_SLOT);
				stmt.AddValue(0, i);
				stmt.AddValue(1, GUID.Counter);
			}

			else
			{
				// REPLACE INTO character_void_storage (itemId, playerGuid, itemEntry, slot, creatorGuid, randomPropertyType, randomProperty, upgradeId, fixedScalingLevel, artifactKnowledgeLevel, bonusListIDs) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
				stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_VOID_STORAGE_ITEM);
				stmt.AddValue(0, _voidStorageItems[i].ItemId);
				stmt.AddValue(1, GUID.Counter);
				stmt.AddValue(2, _voidStorageItems[i].ItemEntry);
				stmt.AddValue(3, i);
				stmt.AddValue(4, _voidStorageItems[i].CreatorGuid.Counter);
				stmt.AddValue(5, (byte)_voidStorageItems[i].RandomBonusListId);
				stmt.AddValue(6, _voidStorageItems[i].FixedScalingLevel);
				stmt.AddValue(7, _voidStorageItems[i].ArtifactKnowledgeLevel);
				stmt.AddValue(8, (byte)_voidStorageItems[i].Context);

				StringBuilder bonusListIDs = new();

				foreach (var bonusListID in _voidStorageItems[i].BonusListIDs)
					bonusListIDs.AppendFormat("{0} ", bonusListID);

				stmt.AddValue(9, bonusListIDs.ToString());
			}

			trans.Append(stmt);
		}
	}

	void _SaveCUFProfiles(SQLTransaction trans)
	{
		PreparedStatement stmt;
		var lowGuid = GUID.Counter;

		for (byte i = 0; i < PlayerConst.MaxCUFProfiles; ++i)
		{
			if (_cufProfiles[i] == null) // unused profile
			{
				// DELETE FROM character_cuf_profiles WHERE guid = ? and id = ?
				stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_CUF_PROFILES_BY_ID);
				stmt.AddValue(0, lowGuid);
				stmt.AddValue(1, i);
			}
			else
			{
				// REPLACE INTO character_cuf_profiles (guid, id, name, frameHeight, frameWidth, sortBy, healthText, boolOptions, unk146, unk147, unk148, unk150, unk152, unk154) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
				stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CHAR_CUF_PROFILES);
				stmt.AddValue(0, lowGuid);
				stmt.AddValue(1, i);
				stmt.AddValue(2, _cufProfiles[i].ProfileName);
				stmt.AddValue(3, _cufProfiles[i].FrameHeight);
				stmt.AddValue(4, _cufProfiles[i].FrameWidth);
				stmt.AddValue(5, _cufProfiles[i].SortBy);
				stmt.AddValue(6, _cufProfiles[i].HealthText);
				stmt.AddValue(7, (uint)_cufProfiles[i].GetUlongOptionValue()); // 25 of 32 fields used, fits in an int
				stmt.AddValue(8, _cufProfiles[i].TopPoint);
				stmt.AddValue(9, _cufProfiles[i].BottomPoint);
				stmt.AddValue(10, _cufProfiles[i].LeftPoint);
				stmt.AddValue(11, _cufProfiles[i].TopOffset);
				stmt.AddValue(12, _cufProfiles[i].BottomOffset);
				stmt.AddValue(13, _cufProfiles[i].LeftOffset);
			}

			trans.Append(stmt);
		}
	}

	void _SaveInstanceTimeRestrictions(SQLTransaction trans)
	{
		if (_instanceResetTimes.Empty())
			return;

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ACCOUNT_INSTANCE_LOCK_TIMES);
		stmt.AddValue(0, Session.AccountId);
		trans.Append(stmt);

		foreach (var pair in _instanceResetTimes)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ACCOUNT_INSTANCE_LOCK_TIMES);
			stmt.AddValue(0, Session.AccountId);
			stmt.AddValue(1, pair.Key);
			stmt.AddValue(2, pair.Value);
			trans.Append(stmt);
		}
	}

	void _SaveBGData(SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_BGDATA);
		stmt.AddValue(0, GUID.Counter);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_BGDATA);
		stmt.AddValue(0, GUID.Counter);
		stmt.AddValue(1, _bgData.BgInstanceId);
		stmt.AddValue(2, _bgData.BgTeam);
		stmt.AddValue(3, _bgData.JoinPos.X);
		stmt.AddValue(4, _bgData.JoinPos.Y);
		stmt.AddValue(5, _bgData.JoinPos.Z);
		stmt.AddValue(6, _bgData.JoinPos.Orientation);
		stmt.AddValue(7, (ushort)_bgData.JoinPos.MapId);
		stmt.AddValue(8, _bgData.TaxiPath[0]);
		stmt.AddValue(9, _bgData.TaxiPath[1]);
		stmt.AddValue(10, _bgData.MountSpell);
		trans.Append(stmt);
	}

	void DeleteSpellFromAllPlayers(uint spellId)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INVALID_SPELL_SPELLS);
		stmt.AddValue(0, spellId);
		DB.Characters.Execute(stmt);
	}
}