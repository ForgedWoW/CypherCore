// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

public class ChannelNotifyJoined : ServerPacket
{
    public string Channel = "";
    public ChannelFlags ChannelFlags;
    public ObjectGuid ChannelGUID;
    public string ChannelWelcomeMsg = "";
    public int ChatChannelID;
    public ulong InstanceID;
    public ChannelNotifyJoined() : base(ServerOpcodes.ChannelNotifyJoined) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Channel.GetByteCount(), 7);
        WorldPacket.WriteBits(ChannelWelcomeMsg.GetByteCount(), 11);
        WorldPacket.WriteUInt32((uint)ChannelFlags);
        WorldPacket.WriteInt32(ChatChannelID);
        WorldPacket.WriteUInt64(InstanceID);
        WorldPacket.WritePackedGuid(ChannelGUID);
        WorldPacket.WriteString(Channel);
        WorldPacket.WriteString(ChannelWelcomeMsg);
    }
}