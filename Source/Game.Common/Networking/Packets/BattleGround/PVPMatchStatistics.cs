// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.BattleGround;

namespace Game.Common.Networking.Packets.BattleGround;

public class PVPMatchStatistics
{
	public List<PVPMatchPlayerStatistics> Statistics = new();
	public RatingData Ratings;
	public sbyte[] PlayerCount = new sbyte[2];

	public void Write(WorldPacket data)
	{
		data.WriteBit(Ratings != null);
		data.WriteInt32(Statistics.Count);

		foreach (var count in PlayerCount)
			data.WriteInt8(count);

		if (Ratings != null)
			Ratings.Write(data);

		foreach (var player in Statistics)
			player.Write(data);
	}

	public class RatingData
	{
		public uint[] Prematch = new uint[2];
		public uint[] Postmatch = new uint[2];
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

	public struct HonorData
	{
		public void Write(WorldPacket data)
		{
			data.WriteUInt32(HonorKills);
			data.WriteUInt32(Deaths);
			data.WriteUInt32(ContributionPoints);
		}

		public uint HonorKills;
		public uint Deaths;
		public uint ContributionPoints;
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
		public ObjectGuid PlayerGUID;
		public uint Kills;
		public byte Faction;
		public bool IsInWorld;
		public HonorData? Honor;
		public uint DamageDone;
		public uint HealingDone;
		public uint? PreMatchRating;
		public int? RatingChange;
		public uint? PreMatchMMR;
		public int? MmrChange;
		public List<PVPMatchPlayerPVPStat> Stats = new();
		public int PrimaryTalentTree;
		public int Sex;
		public Race PlayerRace;
		public int PlayerClass;
		public int CreatureID;
		public int HonorLevel;
		public int Role;

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

			if (Honor.HasValue)
				Honor.Value.Write(data);

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
}
