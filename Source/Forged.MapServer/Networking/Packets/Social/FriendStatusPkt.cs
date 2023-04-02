// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Social;

public class FriendStatusPkt : ServerPacket
{
    public uint AreaID;
    public PlayerClass ClassID = PlayerClass.None;
    public FriendsResult FriendResult;
    public ObjectGuid Guid;
    public uint Level;
    public bool Mobile;
    public string Notes;
    public FriendStatus Status;
    public uint VirtualRealmAddress;
    public ObjectGuid WowAccountGuid;
    public FriendStatusPkt() : base(ServerOpcodes.FriendStatus) { }

    public void Initialize(ObjectGuid guid, FriendsResult result, FriendInfo friendInfo)
    {
        VirtualRealmAddress = Global.WorldMgr.VirtualRealmAddress;
        Notes = friendInfo.Note;
        ClassID = friendInfo.Class;
        Status = friendInfo.Status;
        Guid = guid;
        WowAccountGuid = friendInfo.WowAccountGuid;
        Level = friendInfo.Level;
        AreaID = friendInfo.Area;
        FriendResult = result;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)FriendResult);
        WorldPacket.WritePackedGuid(Guid);
        WorldPacket.WritePackedGuid(WowAccountGuid);
        WorldPacket.WriteUInt32(VirtualRealmAddress);
        WorldPacket.WriteUInt8((byte)Status);
        WorldPacket.WriteUInt32(AreaID);
        WorldPacket.WriteUInt32(Level);
        WorldPacket.WriteUInt32((uint)ClassID);
        WorldPacket.WriteBits(Notes.GetByteCount(), 10);
        WorldPacket.WriteBit(Mobile);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Notes);
    }
}