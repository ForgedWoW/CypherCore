// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Garrison;

namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonInfo
{
	public GarrisonType GarrTypeID;
	public uint GarrSiteID;
	public uint GarrSiteLevelID;
	public uint NumFollowerActivationsRemaining;
	public uint NumMissionsStartedToday; // might mean something else, but sending 0 here enables follower abilities "Increase success chance of the first mission of the day by %."
	public int MinAutoTroopLevel;
	public List<GarrisonPlotInfo> Plots = new();
	public List<GarrisonBuildingInfo> Buildings = new();
	public List<GarrisonFollower> Followers = new();
	public List<GarrisonFollower> AutoTroops = new();
	public List<GarrisonMission> Missions = new();
	public List<List<GarrisonMissionReward>> MissionRewards = new();
	public List<List<GarrisonMissionReward>> MissionOvermaxRewards = new();
	public List<GarrisonMissionBonusAbility> MissionAreaBonuses = new();
	public List<GarrisonTalent> Talents = new();
	public List<GarrisonCollection> Collections = new();
	public List<GarrisonEventList> EventLists = new();
	public List<GarrisonSpecGroup> SpecGroups = new();
	public List<bool> CanStartMission = new();
	public List<int> ArchivedMissions = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt32((uint)GarrTypeID);
		data.WriteUInt32(GarrSiteID);
		data.WriteUInt32(GarrSiteLevelID);
		data.WriteInt32(Buildings.Count);
		data.WriteInt32(Plots.Count);
		data.WriteInt32(Followers.Count);
		data.WriteInt32(AutoTroops.Count);
		data.WriteInt32(Missions.Count);
		data.WriteInt32(MissionRewards.Count);
		data.WriteInt32(MissionOvermaxRewards.Count);
		data.WriteInt32(MissionAreaBonuses.Count);
		data.WriteInt32(Talents.Count);
		data.WriteInt32(Collections.Count);
		data.WriteInt32(EventLists.Count);
		data.WriteInt32(SpecGroups.Count);
		data.WriteInt32(CanStartMission.Count);
		data.WriteInt32(ArchivedMissions.Count);
		data.WriteUInt32(NumFollowerActivationsRemaining);
		data.WriteUInt32(NumMissionsStartedToday);
		data.WriteInt32(MinAutoTroopLevel);

		foreach (var plot in Plots)
			plot.Write(data);

		foreach (var mission in Missions)
			mission.Write(data);

		foreach (var missionReward in MissionRewards)
			data.WriteInt32(missionReward.Count);

		foreach (var missionReward in MissionOvermaxRewards)
			data.WriteInt32(missionReward.Count);

		foreach (var areaBonus in MissionAreaBonuses)
			areaBonus.Write(data);

		foreach (var collection in Collections)
			collection.Write(data);

		foreach (var eventList in EventLists)
			eventList.Write(data);

		foreach (var specGroup in SpecGroups)
			specGroup.Write(data);

		foreach (var id in ArchivedMissions)
			data.WriteInt32(id);

		foreach (var building in Buildings)
			building.Write(data);

		foreach (var canStartMission in CanStartMission)
			data.WriteBit(canStartMission);

		data.FlushBits();

		foreach (var follower in Followers)
			follower.Write(data);

		foreach (var follower in AutoTroops)
			follower.Write(data);

		foreach (var talent in Talents)
			talent.Write(data);

		foreach (var missionReward in MissionRewards)
			foreach (var missionRewardItem in missionReward)
				missionRewardItem.Write(data);

		foreach (var missionReward in MissionOvermaxRewards)
			foreach (var missionRewardItem in missionReward)
				missionRewardItem.Write(data);
	}
}
