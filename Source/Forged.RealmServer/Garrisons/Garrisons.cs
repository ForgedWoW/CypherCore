// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Creatures;
using Forged.RealmServer.Entities.GameObjects;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Entities.Players;
using Forged.RealmServer.Networking.Packets.Garrison;

namespace Forged.RealmServer.Garrisons;

public class Garrison
{
	readonly Player _owner;
	readonly GarrisonType _garrisonType;
	readonly Dictionary<uint, Plot> _plots = new();
	readonly List<uint> _knownBuildings = new();
	readonly Dictionary<ulong, Follower> _followers = new();
	readonly List<uint> _followerIds = new();
	GarrSiteLevelRecord _siteLevel;
	uint _followerActivationsRemainingToday;

	public Garrison(Player owner)
	{
		_owner = owner;
		_followerActivationsRemainingToday = 1;
	}

	public bool LoadFromDB(SQLResult garrison, SQLResult blueprints, SQLResult buildings, SQLResult followers, SQLResult abilities)
	{
		if (garrison.IsEmpty())
			return false;

		_siteLevel = CliDB.GarrSiteLevelStorage.LookupByKey(garrison.Read<uint>(0));
		_followerActivationsRemainingToday = garrison.Read<uint>(1);

		if (_siteLevel == null)
			return false;

		InitializePlots();

		if (!blueprints.IsEmpty())
			do
			{
				var building = CliDB.GarrBuildingStorage.LookupByKey(blueprints.Read<uint>(0));

				if (building != null)
					_knownBuildings.Add(building.Id);
			} while (blueprints.NextRow());

		if (!buildings.IsEmpty())
			do
			{
				var plotInstanceId = buildings.Read<uint>(0);
				var buildingId = buildings.Read<uint>(1);
				var timeBuilt = buildings.Read<long>(2);
				var active = buildings.Read<bool>(3);

				var plot = GetPlot(plotInstanceId);

				if (plot == null)
					continue;

				if (!CliDB.GarrBuildingStorage.ContainsKey(buildingId))
					continue;

				plot.BuildingInfo.PacketInfo = new GarrisonBuildingInfo();
				plot.BuildingInfo.PacketInfo.GarrPlotInstanceID = plotInstanceId;
				plot.BuildingInfo.PacketInfo.GarrBuildingID = buildingId;
				plot.BuildingInfo.PacketInfo.TimeBuilt = timeBuilt;
				plot.BuildingInfo.PacketInfo.Active = active;
			} while (buildings.NextRow());

		if (!followers.IsEmpty())
		{
			do
			{
				var dbId = followers.Read<ulong>(0);
				var followerId = followers.Read<uint>(1);

				if (!CliDB.GarrFollowerStorage.ContainsKey(followerId))
					continue;

				_followerIds.Add(followerId);

				var follower = new Follower();
				follower.PacketInfo.DbID = dbId;
				follower.PacketInfo.GarrFollowerID = followerId;
				follower.PacketInfo.Quality = followers.Read<uint>(2);
				follower.PacketInfo.FollowerLevel = followers.Read<uint>(3);
				follower.PacketInfo.ItemLevelWeapon = followers.Read<uint>(4);
				follower.PacketInfo.ItemLevelArmor = followers.Read<uint>(5);
				follower.PacketInfo.Xp = followers.Read<uint>(6);
				follower.PacketInfo.CurrentBuildingID = followers.Read<uint>(7);
				follower.PacketInfo.CurrentMissionID = followers.Read<uint>(8);
				follower.PacketInfo.FollowerStatus = followers.Read<uint>(9);

				if (!CliDB.GarrBuildingStorage.ContainsKey(follower.PacketInfo.CurrentBuildingID))
					follower.PacketInfo.CurrentBuildingID = 0;

				//if (!sGarrMissionStore.LookupEntry(follower.PacketInfo.CurrentMissionID))
				//    follower.PacketInfo.CurrentMissionID = 0;
				_followers[followerId] = follower;
			} while (followers.NextRow());

			if (!abilities.IsEmpty())
				do
				{
					var dbId = abilities.Read<ulong>(0);
					var ability = CliDB.GarrAbilityStorage.LookupByKey(abilities.Read<uint>(1));

					if (ability == null)
						continue;

					var garrisonFollower = _followers.LookupByKey(dbId);

					if (garrisonFollower == null)
						continue;

					garrisonFollower.PacketInfo.AbilityID.Add(ability);
				} while (abilities.NextRow());
		}

		return true;
	}

	public void SaveToDB(SQLTransaction trans)
	{
		DeleteFromDB(_owner.GUID.Counter, trans);

		var stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON);
		stmt.AddValue(0, _owner.GUID.Counter);
		stmt.AddValue(1, (int)_garrisonType);
		stmt.AddValue(2, _siteLevel.Id);
		stmt.AddValue(3, _followerActivationsRemainingToday);
		trans.Append(stmt);

		foreach (var building in _knownBuildings)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_BLUEPRINTS);
			stmt.AddValue(0, _owner.GUID.Counter);
			stmt.AddValue(1, (int)_garrisonType);
			stmt.AddValue(2, building);
			trans.Append(stmt);
		}

		foreach (var plot in _plots.Values)
			if (plot.BuildingInfo.PacketInfo != null)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_BUILDINGS);
				stmt.AddValue(0, _owner.GUID.Counter);
				stmt.AddValue(1, (int)_garrisonType);
				stmt.AddValue(2, plot.BuildingInfo.PacketInfo.GarrPlotInstanceID);
				stmt.AddValue(3, plot.BuildingInfo.PacketInfo.GarrBuildingID);
				stmt.AddValue(4, plot.BuildingInfo.PacketInfo.TimeBuilt);
				stmt.AddValue(5, plot.BuildingInfo.PacketInfo.Active);
				trans.Append(stmt);
			}

		foreach (var follower in _followers.Values)
		{
			byte index = 0;
			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_FOLLOWERS);
			stmt.AddValue(index++, follower.PacketInfo.DbID);
			stmt.AddValue(index++, _owner.GUID.Counter);
			stmt.AddValue(index++, (int)_garrisonType);
			stmt.AddValue(index++, follower.PacketInfo.GarrFollowerID);
			stmt.AddValue(index++, follower.PacketInfo.Quality);
			stmt.AddValue(index++, follower.PacketInfo.FollowerLevel);
			stmt.AddValue(index++, follower.PacketInfo.ItemLevelWeapon);
			stmt.AddValue(index++, follower.PacketInfo.ItemLevelArmor);
			stmt.AddValue(index++, follower.PacketInfo.Xp);
			stmt.AddValue(index++, follower.PacketInfo.CurrentBuildingID);
			stmt.AddValue(index++, follower.PacketInfo.CurrentMissionID);
			stmt.AddValue(index++, follower.PacketInfo.FollowerStatus);
			trans.Append(stmt);

			byte slot = 0;

			foreach (var ability in follower.PacketInfo.AbilityID)
			{
				stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHARACTER_GARRISON_FOLLOWER_ABILITIES);
				stmt.AddValue(0, follower.PacketInfo.DbID);
				stmt.AddValue(1, ability.Id);
				stmt.AddValue(2, slot++);
				trans.Append(stmt);
			}
		}
	}

	public static void DeleteFromDB(ulong ownerGuid, SQLTransaction trans)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON);
		stmt.AddValue(0, ownerGuid);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON_BLUEPRINTS);
		stmt.AddValue(0, ownerGuid);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON_BUILDINGS);
		stmt.AddValue(0, ownerGuid);
		trans.Append(stmt);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHARACTER_GARRISON_FOLLOWERS);
		stmt.AddValue(0, ownerGuid);
		trans.Append(stmt);
	}

	public bool Create(uint garrSiteId)
	{
		var siteLevel = Global.GarrisonMgr.GetGarrSiteLevelEntry(garrSiteId, 1);

		if (siteLevel == null)
			return false;

		_siteLevel = siteLevel;

		InitializePlots();

		GarrisonCreateResult garrisonCreateResult = new();
		garrisonCreateResult.GarrSiteLevelID = _siteLevel.Id;
		_owner.SendPacket(garrisonCreateResult);
		PhasingHandler.OnConditionChange(_owner);
		SendRemoteInfo();

		return true;
	}

	public void Delete()
	{
		SQLTransaction trans = new();
		DeleteFromDB(_owner.GUID.Counter, trans);
		DB.Characters.CommitTransaction(trans);

		GarrisonDeleteResult garrisonDelete = new();
		garrisonDelete.Result = GarrisonError.Success;
		garrisonDelete.GarrSiteID = _siteLevel.GarrSiteID;
		_owner.SendPacket(garrisonDelete);
	}

	public uint GetFaction()
	{
		return _owner.Team == TeamFaction.Horde ? GarrisonFactionIndex.Horde : GarrisonFactionIndex.Alliance;
	}

	public GarrisonType GetGarrisonType()
	{
		return _garrisonType;
	}

	public GarrSiteLevelRecord GetSiteLevel()
	{
		return _siteLevel;
	}

	public ICollection<Plot> GetPlots()
	{
		return _plots.Values;
	}

	public Plot GetPlot(uint garrPlotInstanceId)
	{
		return _plots.LookupByKey(garrPlotInstanceId);
	}

	public bool HasBlueprint(uint garrBuildingId)
	{
		return _knownBuildings.Contains(garrBuildingId);
	}

	public void LearnBlueprint(uint garrBuildingId)
	{
		GarrisonLearnBlueprintResult learnBlueprintResult = new();
		learnBlueprintResult.GarrTypeID = GetGarrisonType();
		learnBlueprintResult.BuildingID = garrBuildingId;
		learnBlueprintResult.Result = GarrisonError.Success;

		if (!CliDB.GarrBuildingStorage.ContainsKey(garrBuildingId))
			learnBlueprintResult.Result = GarrisonError.InvalidBuildingId;
		else if (HasBlueprint(garrBuildingId))
			learnBlueprintResult.Result = GarrisonError.BlueprintExists;
		else
			_knownBuildings.Add(garrBuildingId);

		_owner.SendPacket(learnBlueprintResult);
	}

	public void PlaceBuilding(uint garrPlotInstanceId, uint garrBuildingId)
	{
		GarrisonPlaceBuildingResult placeBuildingResult = new();
		placeBuildingResult.GarrTypeID = GetGarrisonType();
		placeBuildingResult.Result = CheckBuildingPlacement(garrPlotInstanceId, garrBuildingId);

		if (placeBuildingResult.Result == GarrisonError.Success)
		{
			placeBuildingResult.BuildingInfo.GarrPlotInstanceID = garrPlotInstanceId;
			placeBuildingResult.BuildingInfo.GarrBuildingID = garrBuildingId;
			placeBuildingResult.BuildingInfo.TimeBuilt = _gameTime.GetGameTime;

			var plot = GetPlot(garrPlotInstanceId);
			uint oldBuildingId = 0;
			var map = FindMap();
			var building = CliDB.GarrBuildingStorage.LookupByKey(garrBuildingId);

			if (map)
				plot.DeleteGameObject(map);

			if (plot.BuildingInfo.PacketInfo != null)
			{
				oldBuildingId = plot.BuildingInfo.PacketInfo.GarrBuildingID;

				if (CliDB.GarrBuildingStorage.LookupByKey(oldBuildingId).BuildingType != building.BuildingType)
					plot.ClearBuildingInfo(GetGarrisonType(), _owner);
			}

			plot.SetBuildingInfo(placeBuildingResult.BuildingInfo, _owner);

			if (map)
			{
				var go = plot.CreateGameObject(map, GetFaction());

				if (go)
					map.AddToMap(go);
			}

			_owner.RemoveCurrency(building.CurrencyTypeID, building.CurrencyQty, CurrencyDestroyReason.Garrison);
			_owner.ModifyMoney(-building.GoldCost * MoneyConstants.Gold, false);

			if (oldBuildingId != 0)
			{
				GarrisonBuildingRemoved buildingRemoved = new();
				buildingRemoved.GarrTypeID = GetGarrisonType();
				buildingRemoved.Result = GarrisonError.Success;
				buildingRemoved.GarrPlotInstanceID = garrPlotInstanceId;
				buildingRemoved.GarrBuildingID = oldBuildingId;
				_owner.SendPacket(buildingRemoved);
			}

			_owner.UpdateCriteria(CriteriaType.PlaceGarrisonBuilding, garrBuildingId);
		}

		_owner.SendPacket(placeBuildingResult);
	}

	public void CancelBuildingConstruction(uint garrPlotInstanceId)
	{
		GarrisonBuildingRemoved buildingRemoved = new();
		buildingRemoved.GarrTypeID = GetGarrisonType();
		buildingRemoved.Result = CheckBuildingRemoval(garrPlotInstanceId);

		if (buildingRemoved.Result == GarrisonError.Success)
		{
			var plot = GetPlot(garrPlotInstanceId);

			buildingRemoved.GarrPlotInstanceID = garrPlotInstanceId;
			buildingRemoved.GarrBuildingID = plot.BuildingInfo.PacketInfo.GarrBuildingID;

			var map = FindMap();

			if (map)
				plot.DeleteGameObject(map);

			plot.ClearBuildingInfo(GetGarrisonType(), _owner);
			_owner.SendPacket(buildingRemoved);

			var constructing = CliDB.GarrBuildingStorage.LookupByKey(buildingRemoved.GarrBuildingID);
			// Refund construction/upgrade cost
			_owner.AddCurrency(constructing.CurrencyTypeID, (uint)constructing.CurrencyQty, CurrencyGainSource.GarrisonBuildingRefund);
			_owner.ModifyMoney(constructing.GoldCost * MoneyConstants.Gold, false);

			if (constructing.UpgradeLevel > 1)
			{
				// Restore previous level building
				var restored = Global.GarrisonMgr.GetPreviousLevelBuilding((byte)constructing.BuildingType, constructing.UpgradeLevel);

				GarrisonPlaceBuildingResult placeBuildingResult = new();
				placeBuildingResult.GarrTypeID = GetGarrisonType();
				placeBuildingResult.Result = GarrisonError.Success;
				placeBuildingResult.BuildingInfo.GarrPlotInstanceID = garrPlotInstanceId;
				placeBuildingResult.BuildingInfo.GarrBuildingID = restored;
				placeBuildingResult.BuildingInfo.TimeBuilt = _gameTime.GetGameTime;
				placeBuildingResult.BuildingInfo.Active = true;

				plot.SetBuildingInfo(placeBuildingResult.BuildingInfo, _owner);
				_owner.SendPacket(placeBuildingResult);
			}

			if (map)
			{
				var go = plot.CreateGameObject(map, GetFaction());

				if (go)
					map.AddToMap(go);
			}
		}
		else
		{
			_owner.SendPacket(buildingRemoved);
		}
	}

	public void ActivateBuilding(uint garrPlotInstanceId)
	{
		var plot = GetPlot(garrPlotInstanceId);

		if (plot != null)
			if (plot.BuildingInfo.CanActivate() && plot.BuildingInfo.PacketInfo != null && !plot.BuildingInfo.PacketInfo.Active)
			{
				plot.BuildingInfo.PacketInfo.Active = true;
				var map = FindMap();

				if (map)
				{
					plot.DeleteGameObject(map);
					var go = plot.CreateGameObject(map, GetFaction());

					if (go)
						map.AddToMap(go);
				}

				GarrisonBuildingActivated buildingActivated = new();
				buildingActivated.GarrPlotInstanceID = garrPlotInstanceId;
				_owner.SendPacket(buildingActivated);

				_owner.UpdateCriteria(CriteriaType.ActivateAnyGarrisonBuilding, plot.BuildingInfo.PacketInfo.GarrBuildingID);
			}
	}

	public void AddFollower(uint garrFollowerId)
	{
		GarrisonAddFollowerResult addFollowerResult = new();
		addFollowerResult.GarrTypeID = GetGarrisonType();
		var followerEntry = CliDB.GarrFollowerStorage.LookupByKey(garrFollowerId);

		if (_followerIds.Contains(garrFollowerId) || followerEntry == null)
		{
			addFollowerResult.Result = GarrisonError.FollowerExists;
			_owner.SendPacket(addFollowerResult);

			return;
		}

		_followerIds.Add(garrFollowerId);
		var dbId = Global.GarrisonMgr.GenerateFollowerDbId();

		Follower follower = new();
		follower.PacketInfo.DbID = dbId;
		follower.PacketInfo.GarrFollowerID = garrFollowerId;
		follower.PacketInfo.Quality = (uint)followerEntry.Quality; // TODO: handle magic upgrades
		follower.PacketInfo.FollowerLevel = followerEntry.FollowerLevel;
		follower.PacketInfo.ItemLevelWeapon = followerEntry.ItemLevelWeapon;
		follower.PacketInfo.ItemLevelArmor = followerEntry.ItemLevelArmor;
		follower.PacketInfo.Xp = 0;
		follower.PacketInfo.CurrentBuildingID = 0;
		follower.PacketInfo.CurrentMissionID = 0;
		follower.PacketInfo.AbilityID = Global.GarrisonMgr.RollFollowerAbilities(garrFollowerId, followerEntry, follower.PacketInfo.Quality, GetFaction(), true);
		follower.PacketInfo.FollowerStatus = 0;

		_followers[dbId] = follower;
		addFollowerResult.Follower = follower.PacketInfo;
		_owner.SendPacket(addFollowerResult);

		_owner.UpdateCriteria(CriteriaType.RecruitGarrisonFollower, follower.PacketInfo.DbID);
	}

	public Follower GetFollower(ulong dbId)
	{
		return _followers.LookupByKey(dbId);
	}

	public uint CountFollowers(Predicate<Follower> predicate)
	{
		uint count = 0;

		foreach (var pair in _followers)
			if (predicate(pair.Value))
				++count;

		return count;
	}

	public void SendInfo()
	{
		GetGarrisonInfoResult garrisonInfo = new();
		garrisonInfo.FactionIndex = GetFaction();

		GarrisonInfo garrison = new();
		garrison.GarrTypeID = GetGarrisonType();
		garrison.GarrSiteID = _siteLevel.GarrSiteID;
		garrison.GarrSiteLevelID = _siteLevel.Id;
		garrison.NumFollowerActivationsRemaining = _followerActivationsRemainingToday;

		foreach (var plot in _plots.Values)
		{
			garrison.Plots.Add(plot.PacketInfo);

			if (plot.BuildingInfo.PacketInfo != null)
				garrison.Buildings.Add(plot.BuildingInfo.PacketInfo);
		}

		foreach (var follower in _followers.Values)
			garrison.Followers.Add(follower.PacketInfo);

		garrisonInfo.Garrisons.Add(garrison);

		_owner.SendPacket(garrisonInfo);
	}

	public void SendRemoteInfo()
	{
		var garrisonMap = CliDB.MapStorage.LookupByKey(_siteLevel.MapID);

		if (garrisonMap == null || _owner.Location.MapId != garrisonMap.ParentMapID)
			return;

		GarrisonRemoteInfo remoteInfo = new();

		GarrisonRemoteSiteInfo remoteSiteInfo = new();
		remoteSiteInfo.GarrSiteLevelID = _siteLevel.Id;

		foreach (var p in _plots)
			if (p.Value.BuildingInfo.PacketInfo != null)
				remoteSiteInfo.Buildings.Add(new GarrisonRemoteBuildingInfo(p.Key, p.Value.BuildingInfo.PacketInfo.GarrBuildingID));

		remoteInfo.Sites.Add(remoteSiteInfo);
		_owner.SendPacket(remoteInfo);
	}

	public void SendBlueprintAndSpecializationData()
	{
		GarrisonRequestBlueprintAndSpecializationDataResult data = new();
		data.GarrTypeID = GetGarrisonType();
		data.BlueprintsKnown = _knownBuildings;
		_owner.SendPacket(data);
	}

	public void SendMapData(Player receiver)
	{
		GarrisonMapDataResponse mapData = new();

		foreach (var plot in _plots.Values)
			if (plot.BuildingInfo.PacketInfo != null)
			{
				var garrBuildingPlotInstId = Global.GarrisonMgr.GetGarrBuildingPlotInst(plot.BuildingInfo.PacketInfo.GarrBuildingID, plot.GarrSiteLevelPlotInstId);

				if (garrBuildingPlotInstId != 0)
					mapData.Buildings.Add(new GarrisonBuildingMapData(garrBuildingPlotInstId, plot.PacketInfo.PlotPos));
			}

		receiver.SendPacket(mapData);
	}

	public void ResetFollowerActivationLimit()
	{
		_followerActivationsRemainingToday = 1;
	}

	void InitializePlots()
	{
		var plots = Global.GarrisonMgr.GetGarrPlotInstForSiteLevel(_siteLevel.Id);

		for (var i = 0; i < plots.Count; ++i)
		{
			uint garrPlotInstanceId = plots[i].GarrPlotInstanceID;
			var plotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(garrPlotInstanceId);
			var gameObject = Global.GarrisonMgr.GetPlotGameObject(_siteLevel.MapID, garrPlotInstanceId);

			if (plotInstance == null || gameObject == null)
				continue;

			var plot = CliDB.GarrPlotStorage.LookupByKey(plotInstance.GarrPlotID);

			if (plot == null)
				continue;

			var plotInfo = _plots[garrPlotInstanceId];
			plotInfo.PacketInfo.GarrPlotInstanceID = garrPlotInstanceId;
			plotInfo.PacketInfo.PlotPos.Relocate(gameObject.Pos.X, gameObject.Pos.Y, gameObject.Pos.Z, 2 * (float)Math.Acos(gameObject.Rot[3]));
			plotInfo.PacketInfo.PlotType = plot.PlotType;
			plotInfo.Rotation = new Quaternion(gameObject.Rot[0], gameObject.Rot[1], gameObject.Rot[2], gameObject.Rot[3]);
			plotInfo.EmptyGameObjectId = gameObject.Id;
			plotInfo.GarrSiteLevelPlotInstId = plots[i].Id;
		}
	}

	void Upgrade() { }

	void Enter()
	{
		WorldLocation loc = new(_siteLevel.MapID);
		loc.Relocate(_owner.Location);
		_owner.TeleportTo(loc, TeleportToOptions.Seamless);
	}

	void Leave()
	{
		var map = CliDB.MapStorage.LookupByKey(_siteLevel.MapID);

		if (map != null)
		{
			WorldLocation loc = new((uint)map.ParentMapID);
			loc.Relocate(_owner.Location);
			_owner.TeleportTo(loc, TeleportToOptions.Seamless);
		}
	}

	void UnlearnBlueprint(uint garrBuildingId)
	{
		GarrisonUnlearnBlueprintResult unlearnBlueprintResult = new();
		unlearnBlueprintResult.GarrTypeID = GetGarrisonType();
		unlearnBlueprintResult.BuildingID = garrBuildingId;
		unlearnBlueprintResult.Result = GarrisonError.Success;

		if (!CliDB.GarrBuildingStorage.ContainsKey(garrBuildingId))
			unlearnBlueprintResult.Result = GarrisonError.InvalidBuildingId;
		else if (HasBlueprint(garrBuildingId))
			unlearnBlueprintResult.Result = GarrisonError.RequiresBlueprint;
		else
			_knownBuildings.Remove(garrBuildingId);

		_owner.SendPacket(unlearnBlueprintResult);
	}

	Map FindMap()
	{
		return Global.MapMgr.FindMap(_siteLevel.MapID, (uint)_owner.GUID.Counter);
	}

	GarrisonError CheckBuildingPlacement(uint garrPlotInstanceId, uint garrBuildingId)
	{
		var plotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(garrPlotInstanceId);
		var plot = GetPlot(garrPlotInstanceId);

		if (plotInstance == null || plot == null)
			return GarrisonError.InvalidPlotInstanceId;

		var building = CliDB.GarrBuildingStorage.LookupByKey(garrBuildingId);

		if (building == null)
			return GarrisonError.InvalidBuildingId;

		if (!Global.GarrisonMgr.IsPlotMatchingBuilding(plotInstance.GarrPlotID, garrBuildingId))
			return GarrisonError.InvalidPlotBuilding;

		// Cannot place buldings of higher level than garrison level
		if (building.UpgradeLevel > _siteLevel.MaxBuildingLevel)
			return GarrisonError.InvalidBuildingId;

		if (building.Flags.HasAnyFlag(GarrisonBuildingFlags.NeedsPlan))
		{
			if (HasBlueprint(garrBuildingId))
				return GarrisonError.RequiresBlueprint;
		}
		else // Building is built as a quest reward
		{
			return GarrisonError.InvalidBuildingId;
		}

		// Check all plots to find if we already have this building
		GarrBuildingRecord existingBuilding;

		foreach (var p in _plots)
			if (p.Value.BuildingInfo.PacketInfo != null)
			{
				existingBuilding = CliDB.GarrBuildingStorage.LookupByKey(p.Value.BuildingInfo.PacketInfo.GarrBuildingID);

				if (existingBuilding.BuildingType == building.BuildingType)
					if (p.Key != garrPlotInstanceId || existingBuilding.UpgradeLevel + 1 != building.UpgradeLevel) // check if its an upgrade in same plot
						return GarrisonError.BuildingExists;
			}

		if (!_owner.HasCurrency(building.CurrencyTypeID, (uint)building.CurrencyQty))
			return GarrisonError.NotEnoughCurrency;

		if (!_owner.HasEnoughMoney(building.GoldCost * MoneyConstants.Gold))
			return GarrisonError.NotEnoughGold;

		// New building cannot replace another building currently under construction
		if (plot.BuildingInfo.PacketInfo != null)
			if (!plot.BuildingInfo.PacketInfo.Active)
				return GarrisonError.NoBuilding;

		return GarrisonError.Success;
	}

	GarrisonError CheckBuildingRemoval(uint garrPlotInstanceId)
	{
		var plot = GetPlot(garrPlotInstanceId);

		if (plot == null)
			return GarrisonError.InvalidPlotInstanceId;

		if (plot.BuildingInfo.PacketInfo == null)
			return GarrisonError.NoBuilding;

		if (plot.BuildingInfo.CanActivate())
			return GarrisonError.BuildingExists;

		return GarrisonError.Success;
	}

	public class Building
	{
		public ObjectGuid Guid;
		public List<ObjectGuid> Spawns = new();
		public GarrisonBuildingInfo PacketInfo;

		public bool CanActivate()
		{
			if (PacketInfo != null)
			{
				var building = CliDB.GarrBuildingStorage.LookupByKey(PacketInfo.GarrBuildingID);

				if (PacketInfo.TimeBuilt + building.BuildSeconds <= _gameTime.GetGameTime)
					return true;
			}

			return false;
		}
	}

	public class Plot
	{
		public GarrisonPlotInfo PacketInfo;
		public Quaternion Rotation;
		public uint EmptyGameObjectId;
		public uint GarrSiteLevelPlotInstId;
		public Building BuildingInfo;

		public GameObject CreateGameObject(Map map, uint faction)
		{
			var entry = EmptyGameObjectId;

			if (BuildingInfo.PacketInfo != null)
			{
				var plotInstance = CliDB.GarrPlotInstanceStorage.LookupByKey(PacketInfo.GarrPlotInstanceID);
				var plot = CliDB.GarrPlotStorage.LookupByKey(plotInstance.GarrPlotID);
				var building = CliDB.GarrBuildingStorage.LookupByKey(BuildingInfo.PacketInfo.GarrBuildingID);

				entry = faction == GarrisonFactionIndex.Horde ? plot.HordeConstructObjID : plot.AllianceConstructObjID;

				if (BuildingInfo.PacketInfo.Active || entry == 0)
					entry = faction == GarrisonFactionIndex.Horde ? building.HordeGameObjectID : building.AllianceGameObjectID;
			}

			if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
			{
				Log.Logger.Error("Garrison attempted to spawn gameobject whose template doesn't exist ({0})", entry);

				return null;
			}

			var go = GameObject.CreateGameObject(entry, map, PacketInfo.PlotPos, Rotation, 255, GameObjectState.Ready);

			if (!go)
				return null;

			if (BuildingInfo.CanActivate() && BuildingInfo.PacketInfo != null && !BuildingInfo.PacketInfo.Active)
			{
				var finalizeInfo = Global.GarrisonMgr.GetPlotFinalizeGOInfo(PacketInfo.GarrPlotInstanceID);

				if (finalizeInfo != null)
				{
					var pos2 = finalizeInfo.factionInfo[faction].Pos;
					var finalizer = GameObject.CreateGameObject(finalizeInfo.factionInfo[faction].GameObjectId, map, pos2, Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos2.Orientation, 0.0f, 0.0f)), 255, GameObjectState.Ready);

					if (finalizer)
					{
						// set some spell id to make the object delete itself after use
						finalizer. // set some spell id to make the object delete itself after use
							SpellId = finalizer.Template.Goober.spell;

						finalizer.SetRespawnTime(0);

						var animKit = finalizeInfo.factionInfo[faction].AnimKitId;

						if (animKit != 0)
							finalizer.SetAnimKitId(animKit, false);

						map.AddToMap(finalizer);
					}
				}
			}

			if (go.GoType == GameObjectTypes.GarrisonBuilding && go.Template.GarrisonBuilding.SpawnMap != 0)
				foreach (var cellGuids in Global.ObjectMgr.GetMapObjectGuids((uint)go.Template.GarrisonBuilding.SpawnMap, map.DifficultyID))
				{
					foreach (var spawnId in cellGuids.Value.creatures)
					{
						var spawn = BuildingSpawnHelper<Creature>(go, spawnId, map);

						if (spawn)
							BuildingInfo.Spawns.Add(spawn.GUID);
					}

					foreach (var spawnId in cellGuids.Value.gameobjects)
					{
						var spawn = BuildingSpawnHelper<GameObject>(go, spawnId, map);

						if (spawn)
							BuildingInfo.Spawns.Add(spawn.GUID);
					}
				}

			BuildingInfo.Guid = go.GUID;

			return go;
		}

		public void DeleteGameObject(Map map)
		{
			if (BuildingInfo.Guid.IsEmpty)
				return;

			foreach (var guid in BuildingInfo.Spawns)
			{
				WorldObject obj;

				switch (guid.High)
				{
					case HighGuid.Creature:
						obj = map.GetCreature(guid);

						break;
					case HighGuid.GameObject:
						obj = map.GetGameObject(guid);

						break;
					default:
						continue;
				}

				if (obj)
					obj.AddObjectToRemoveList();
			}

			BuildingInfo.Spawns.Clear();

			var oldBuilding = map.GetGameObject(BuildingInfo.Guid);

			if (oldBuilding)
				oldBuilding.Delete();

			BuildingInfo.Guid.Clear();
		}

		public void ClearBuildingInfo(GarrisonType garrisonType, Player owner)
		{
			GarrisonPlotPlaced plotPlaced = new();
			plotPlaced.GarrTypeID = garrisonType;
			plotPlaced.PlotInfo = PacketInfo;
			owner.SendPacket(plotPlaced);

			BuildingInfo.PacketInfo = null;
		}

		public void SetBuildingInfo(GarrisonBuildingInfo buildingInfo, Player owner)
		{
			if (BuildingInfo.PacketInfo == null)
			{
				GarrisonPlotRemoved plotRemoved = new();
				plotRemoved.GarrPlotInstanceID = PacketInfo.GarrPlotInstanceID;
				owner.SendPacket(plotRemoved);
			}

			BuildingInfo.PacketInfo = buildingInfo;
		}

		T BuildingSpawnHelper<T>(GameObject building, ulong spawnId, Map map) where T : WorldObject, new()
		{
			T spawn = new();

			if (!spawn.LoadFromDB(spawnId, map, false, false))
				return null;

			var pos = spawn.Location.Copy();
			ITransport.CalculatePassengerPosition(pos, building.Location.X, building.Location.Y, building.Location.Z, building.Location.Orientation);

			spawn.Location.Relocate(pos);

			switch (spawn.TypeId)
			{
				case TypeId.Unit:
					spawn.AsCreature.HomePosition = pos;

					break;
				case TypeId.GameObject:
					spawn.AsGameObject.RelocateStationaryPosition(pos);

					break;
			}

			if (!spawn.Location.IsPositionValid)
				return null;

			if (!map.AddToMap(spawn))
				return null;

			return spawn;
		}
	}

	public class Follower
	{
		public GarrisonFollower PacketInfo = new();

		public uint GetItemLevel()
		{
			return (PacketInfo.ItemLevelWeapon + PacketInfo.ItemLevelArmor) / 2;
		}

		public bool HasAbility(uint garrAbilityId)
		{
			return PacketInfo.AbilityID.Any(garrAbility => { return garrAbility.Id == garrAbilityId; });
		}
	}
}