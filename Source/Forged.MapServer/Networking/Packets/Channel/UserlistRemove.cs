// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

internal class UserlistRemove : ServerPacket
{
    public ChannelFlags ChannelFlags;
    public uint ChannelID;
    public string ChannelName;
    public ObjectGuid RemovedUserGUID;
    public UserlistRemove() : base(ServerOpcodes.UserlistRemove) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(RemovedUserGUID);
        WorldPacket.WriteUInt32((uint)ChannelFlags);
        WorldPacket.WriteUInt32(ChannelID);

        WorldPacket.WriteBits(ChannelName.GetByteCount(), 7);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(ChannelName);
    }
}