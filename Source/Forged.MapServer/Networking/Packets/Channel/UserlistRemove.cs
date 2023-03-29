// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

internal class UserlistRemove : ServerPacket
{
    public ObjectGuid RemovedUserGUID;
    public ChannelFlags ChannelFlags;
    public uint ChannelID;
    public string ChannelName;
    public UserlistRemove() : base(ServerOpcodes.UserlistRemove) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(RemovedUserGUID);
        _worldPacket.WriteUInt32((uint)ChannelFlags);
        _worldPacket.WriteUInt32(ChannelID);

        _worldPacket.WriteBits(ChannelName.GetByteCount(), 7);
        _worldPacket.FlushBits();
        _worldPacket.WriteString(ChannelName);
    }
}