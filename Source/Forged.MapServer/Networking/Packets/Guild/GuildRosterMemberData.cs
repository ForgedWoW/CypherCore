// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.MythicPlus;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRosterMemberData
{
	public ObjectGuid Guid;
	public long WeeklyXP;
	public long TotalXP;
	public int RankID;
	public int AreaID;
	public int PersonalAchievementPoints;
	public int GuildReputation;
	public int GuildRepToCap;
	public float LastSave;
	public string Name;
	public uint VirtualRealmAddress;
	public string Note;
	public string OfficerNote;
	public byte Status;
	public byte Level;
	public byte ClassID;
	public byte Gender;
	public ulong GuildClubMemberID;
	public byte RaceID;
	public bool Authenticated;
	public bool SorEligible;
	public GuildRosterProfessionData[] Profession = new GuildRosterProfessionData[2];
	public DungeonScoreSummary DungeonScore = new();

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WriteInt32(RankID);
		data.WriteInt32(AreaID);
		data.WriteInt32(PersonalAchievementPoints);
		data.WriteInt32(GuildReputation);
		data.WriteFloat(LastSave);

		for (byte i = 0; i < 2; i++)
			Profession[i].Write(data);

		data.WriteUInt32(VirtualRealmAddress);
		data.WriteUInt8(Status);
		data.WriteUInt8(Level);
		data.WriteUInt8(ClassID);
		data.WriteUInt8(Gender);
		data.WriteUInt64(GuildClubMemberID);
		data.WriteUInt8(RaceID);

		data.WriteBits(Name.GetByteCount(), 6);
		data.WriteBits(Note.GetByteCount(), 8);
		data.WriteBits(OfficerNote.GetByteCount(), 8);
		data.WriteBit(Authenticated);
		data.WriteBit(SorEligible);
		data.FlushBits();

		DungeonScore.Write(data);

		data.WriteString(Name);
		data.WriteString(Note);
		data.WriteString(OfficerNote);
	}
}