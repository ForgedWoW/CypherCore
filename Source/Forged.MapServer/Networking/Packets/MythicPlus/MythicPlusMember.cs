// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.MythicPlus;

public struct MythicPlusMember
{
    public ObjectGuid BnetAccountGUID;
    public int ChrSpecializationID;
    public int CovenantID;
    public ObjectGuid GUID;
    public ulong GuildClubMemberID;
    public ObjectGuid GuildGUID;
    public int ItemLevel;
    public uint NativeRealmAddress;
    public short RaceID;
    public int SoulbindID;
    public uint VirtualRealmAddress;

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(BnetAccountGUID);
        data.WriteUInt64(GuildClubMemberID);
        data.WritePackedGuid(GUID);
        data.WritePackedGuid(GuildGUID);
        data.WriteUInt32(NativeRealmAddress);
        data.WriteUInt32(VirtualRealmAddress);
        data.WriteInt32(ChrSpecializationID);
        data.WriteInt16(RaceID);
        data.WriteInt32(ItemLevel);
        data.WriteInt32(CovenantID);
        data.WriteInt32(SoulbindID);
    }
}