// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonMission
{
    public int ContentTuningID = 0;
    public ulong DbID;
    public List<GarrisonEncounter> Encounters = new();
    public uint Flags = 0;
    public uint MissionDuration;
    public int MissionRecID;
    public float MissionScalar = 1.0f;
    public int MissionState = 0;
    public uint OfferDuration;
    public long OfferTime;
    public List<GarrisonMissionReward> OvermaxRewards = new();
    public List<GarrisonMissionReward> Rewards = new();
    public long StartTime = 2288912640;
    public int SuccessChance = 0;
    public uint TravelDuration;
    public void Write(WorldPacket data)
    {
        data.WriteUInt64(DbID);
        data.WriteInt32(MissionRecID);
        data.WriteInt64(OfferTime);
        data.WriteUInt32(OfferDuration);
        data.WriteInt64(StartTime);
        data.WriteUInt32(TravelDuration);
        data.WriteUInt32(MissionDuration);
        data.WriteInt32(MissionState);
        data.WriteInt32(SuccessChance);
        data.WriteUInt32(Flags);
        data.WriteFloat(MissionScalar);
        data.WriteInt32(ContentTuningID);
        data.WriteInt32(Encounters.Count);
        data.WriteInt32(Rewards.Count);
        data.WriteInt32(OvermaxRewards.Count);

        foreach (var encounter in Encounters)
            encounter.Write(data);

        foreach (var missionRewardItem in Rewards)
            missionRewardItem.Write(data);

        foreach (var missionRewardItem in OvermaxRewards)
            missionRewardItem.Write(data);
    }
}