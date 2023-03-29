// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

public class ChannelListResponse : ServerPacket
{
    public List<ChannelPlayer> Members;
    public string Channel; // Channel Name
    public ChannelFlags ChannelFlags;
    public bool Display;

    public ChannelListResponse() : base(ServerOpcodes.ChannelList)
    {
        Members = new List<ChannelPlayer>();
    }

    public override void Write()
    {
        _worldPacket.WriteBit(Display);
        _worldPacket.WriteBits(Channel.GetByteCount(), 7);
        _worldPacket.WriteUInt32((uint)ChannelFlags);
        _worldPacket.WriteInt32(Members.Count);
        _worldPacket.WriteString(Channel);

        foreach (var player in Members)
        {
            _worldPacket.WritePackedGuid(player.Guid);
            _worldPacket.WriteUInt32(player.VirtualRealmAddress);
            _worldPacket.WriteUInt8((byte)player.Flags);
        }
    }

    public struct ChannelPlayer
    {
        public ChannelPlayer(ObjectGuid guid, uint realm, ChannelMemberFlags flags)
        {
            Guid = guid;
            VirtualRealmAddress = realm;
            Flags = flags;
        }

        public ObjectGuid Guid; // Player Guid
        public uint VirtualRealmAddress;
        public ChannelMemberFlags Flags;
    }
}