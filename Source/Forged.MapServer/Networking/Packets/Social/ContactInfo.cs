// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Social;

public class ContactInfo
{
    private readonly uint AreaID;
    private readonly PlayerClass ClassID;
    private readonly ObjectGuid Guid;
    private readonly uint Level;
    private readonly bool Mobile;
    private readonly uint NativeRealmAddr;
    private readonly string Notes;
    private readonly FriendStatus Status;
    private readonly SocialFlag TypeFlags;
    private readonly uint VirtualRealmAddr;
    private readonly ObjectGuid WowAccountGuid;

    public ContactInfo(ObjectGuid guid, FriendInfo friendInfo)
    {
        Guid = guid;
        WowAccountGuid = friendInfo.WowAccountGuid;
        VirtualRealmAddr = Global.WorldMgr.VirtualRealmAddress;
        NativeRealmAddr = Global.WorldMgr.VirtualRealmAddress;
        TypeFlags = friendInfo.Flags;
        Notes = friendInfo.Note;
        Status = friendInfo.Status;
        AreaID = friendInfo.Area;
        Level = friendInfo.Level;
        ClassID = friendInfo.Class;
    }

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(Guid);
        data.WritePackedGuid(WowAccountGuid);
        data.WriteUInt32(VirtualRealmAddr);
        data.WriteUInt32(NativeRealmAddr);
        data.WriteUInt32((uint)TypeFlags);
        data.WriteUInt8((byte)Status);
        data.WriteUInt32(AreaID);
        data.WriteUInt32(Level);
        data.WriteUInt32((uint)ClassID);
        data.WriteBits(Notes.GetByteCount(), 10);
        data.WriteBit(Mobile);
        data.FlushBits();
        data.WriteString(Notes);
    }
}