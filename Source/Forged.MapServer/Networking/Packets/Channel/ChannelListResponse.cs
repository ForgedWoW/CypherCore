// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

public class ChannelListResponse : ServerPacket
{
    public string Channel;

    // Channel Name
    public ChannelFlags ChannelFlags;

    public bool Display;
    public List<ChannelPlayer> Members;

    public ChannelListResponse() : base(ServerOpcodes.ChannelList)
    {
        Members = new List<ChannelPlayer>();
    }

    public override void Write()
    {
        WorldPacket.WriteBit(Display);
        WorldPacket.WriteBits(Channel.GetByteCount(), 7);
        WorldPacket.WriteUInt32((uint)ChannelFlags);
        WorldPacket.WriteInt32(Members.Count);
        WorldPacket.WriteString(Channel);

        foreach (var player in Members)
        {
            WorldPacket.WritePackedGuid(player.Guid);
            WorldPacket.WriteUInt32(player.VirtualRealmAddress);
            WorldPacket.WriteUInt8((byte)player.Flags);
        }
    }

    public struct ChannelPlayer
    {
        public ChannelMemberFlags Flags;

        public ObjectGuid Guid;

        // Player Guid
        public uint VirtualRealmAddress;

        public ChannelPlayer(ObjectGuid guid, uint realm, ChannelMemberFlags flags)
        {
            Guid = guid;
            VirtualRealmAddress = realm;
            Flags = flags;
        }
    }
}