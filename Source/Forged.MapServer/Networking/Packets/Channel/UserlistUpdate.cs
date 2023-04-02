// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

internal class UserlistUpdate : ServerPacket
{
    public ChannelFlags ChannelFlags;
    public uint ChannelID;
    public string ChannelName;
    public ObjectGuid UpdatedUserGUID;
    public ChannelMemberFlags UserFlags;
    public UserlistUpdate() : base(ServerOpcodes.UserlistUpdate) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UpdatedUserGUID);
        WorldPacket.WriteUInt8((byte)UserFlags);
        WorldPacket.WriteUInt32((uint)ChannelFlags);
        WorldPacket.WriteUInt32(ChannelID);

        WorldPacket.WriteBits(ChannelName.GetByteCount(), 7);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(ChannelName);
    }
}