// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.MythicPlus;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildRosterMemberData
{
    public int AreaID;
    public bool Authenticated;
    public byte ClassID;
    public DungeonScoreSummary DungeonScore = new();
    public byte Gender;
    public ObjectGuid Guid;
    public ulong GuildClubMemberID;
    public int GuildRepToCap;
    public int GuildReputation;
    public float LastSave;
    public byte Level;
    public string Name;
    public string Note;
    public string OfficerNote;
    public int PersonalAchievementPoints;
    public GuildRosterProfessionData[] Profession = new GuildRosterProfessionData[2];
    public byte RaceID;
    public int RankID;
    public bool SorEligible;
    public byte Status;
    public long TotalXP;
    public uint VirtualRealmAddress;
    public long WeeklyXP;
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