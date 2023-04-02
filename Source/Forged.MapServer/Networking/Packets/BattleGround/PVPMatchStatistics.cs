// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class PVPMatchStatistics
{
    public sbyte[] PlayerCount = new sbyte[2];
    public RatingData Ratings;
    public List<PVPMatchPlayerStatistics> Statistics = new();
    public void Write(WorldPacket data)
    {
        data.WriteBit(Ratings != null);
        data.WriteInt32(Statistics.Count);

        foreach (var count in PlayerCount)
            data.WriteInt8(count);

        Ratings?.Write(data);

        foreach (var player in Statistics)
            player.Write(data);
    }

    public struct HonorData
    {
        public uint ContributionPoints;

        public uint Deaths;

        public uint HonorKills;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(HonorKills);
            data.WriteUInt32(Deaths);
            data.WriteUInt32(ContributionPoints);
        }
    }

    public struct PVPMatchPlayerPVPStat
    {
        public int PvpStatID;
        public uint PvpStatValue;

        public PVPMatchPlayerPVPStat(int pvpStatID, uint pvpStatValue)
        {
            PvpStatID = pvpStatID;
            PvpStatValue = pvpStatValue;
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PvpStatID);
            data.WriteUInt32(PvpStatValue);
        }
    }

    public class PVPMatchPlayerStatistics
    {
        public int CreatureID;
        public uint DamageDone;
        public byte Faction;
        public uint HealingDone;
        public HonorData? Honor;
        public int HonorLevel;
        public bool IsInWorld;
        public uint Kills;
        public int? MmrChange;
        public int PlayerClass;
        public ObjectGuid PlayerGUID;
        public Race PlayerRace;
        public uint? PreMatchMMR;
        public uint? PreMatchRating;
        public int PrimaryTalentTree;
        public int? RatingChange;
        public int Role;
        public int Sex;
        public List<PVPMatchPlayerPVPStat> Stats = new();
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(PlayerGUID);
            data.WriteUInt32(Kills);
            data.WriteUInt32(DamageDone);
            data.WriteUInt32(HealingDone);
            data.WriteInt32(Stats.Count);
            data.WriteInt32(PrimaryTalentTree);
            data.WriteInt32(Sex);
            data.WriteUInt32((uint)PlayerRace);
            data.WriteInt32(PlayerClass);
            data.WriteInt32(CreatureID);
            data.WriteInt32(HonorLevel);
            data.WriteInt32(Role);

            foreach (var pvpStat in Stats)
                pvpStat.Write(data);

            data.WriteBit(Faction != 0);
            data.WriteBit(IsInWorld);
            data.WriteBit(Honor.HasValue);
            data.WriteBit(PreMatchRating.HasValue);
            data.WriteBit(RatingChange.HasValue);
            data.WriteBit(PreMatchMMR.HasValue);
            data.WriteBit(MmrChange.HasValue);
            data.FlushBits();

            Honor?.Write(data);

            if (PreMatchRating.HasValue)
                data.WriteUInt32(PreMatchRating.Value);

            if (RatingChange.HasValue)
                data.WriteInt32(RatingChange.Value);

            if (PreMatchMMR.HasValue)
                data.WriteUInt32(PreMatchMMR.Value);

            if (MmrChange.HasValue)
                data.WriteInt32(MmrChange.Value);
        }
    }

    public class RatingData
    {
        public uint[] Postmatch = new uint[2];
        public uint[] Prematch = new uint[2];
        public uint[] PrematchMMR = new uint[2];

        public void Write(WorldPacket data)
        {
            foreach (var id in Prematch)
                data.WriteUInt32(id);

            foreach (var id in Postmatch)
                data.WriteUInt32(id);

            foreach (var id in PrematchMMR)
                data.WriteUInt32(id);
        }
    }
}