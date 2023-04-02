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
        _worldPacket.WriteUInt8((byte)FriendResult);
        _worldPacket.WritePackedGuid(Guid);
        _worldPacket.WritePackedGuid(WowAccountGuid);
        _worldPacket.WriteUInt32(VirtualRealmAddress);
        _worldPacket.WriteUInt8((byte)Status);
        _worldPacket.WriteUInt32(AreaID);
        _worldPacket.WriteUInt32(Level);
        _worldPacket.WriteUInt32((uint)ClassID);
        _worldPacket.WriteBits(Notes.GetByteCount(), 10);
        _worldPacket.WriteBit(Mobile);
        _worldPacket.FlushBits();
        _worldPacket.WriteString(Notes);
    }
}