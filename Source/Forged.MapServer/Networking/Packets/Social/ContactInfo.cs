// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.World;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Social;

public class ContactInfo
{
    private readonly uint _areaID;
    private readonly PlayerClass _classID;
    private readonly ObjectGuid _guid;
    private readonly uint _level;
    private readonly bool _mobile;
    private readonly uint _nativeRealmAddr;
    private readonly string _notes;
    private readonly FriendStatus _status;
    private readonly SocialFlag _typeFlags;
    private readonly uint _virtualRealmAddr;
    private readonly ObjectGuid _wowAccountGuid;

    public ContactInfo(ObjectGuid guid, FriendInfo friendInfo)
    {
        _guid = guid;
        _wowAccountGuid = friendInfo.WowAccountGuid;
        _virtualRealmAddr = WorldManager.Realm.Id.VirtualRealmAddress;
        _nativeRealmAddr = WorldManager.Realm.Id.VirtualRealmAddress;
        _typeFlags = friendInfo.Flags;
        _notes = friendInfo.Note;
        _status = friendInfo.Status;
        _areaID = friendInfo.Area;
        _level = friendInfo.Level;
        _classID = friendInfo.Class;
    }

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(_guid);
        data.WritePackedGuid(_wowAccountGuid);
        data.WriteUInt32(_virtualRealmAddr);
        data.WriteUInt32(_nativeRealmAddr);
        data.WriteUInt32((uint)_typeFlags);
        data.WriteUInt8((byte)_status);
        data.WriteUInt32(_areaID);
        data.WriteUInt32(_level);
        data.WriteUInt32((uint)_classID);
        data.WriteBits(_notes.GetByteCount(), 10);
        data.WriteBit(_mobile);
        data.FlushBits();
        data.WriteString(_notes);
    }
}