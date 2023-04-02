// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

public class ChannelNotify : ServerPacket
{
    public string Channel;
    public int ChatChannelID;
    public ChannelMemberFlags NewFlags;
    public ChannelMemberFlags OldFlags;
    public string Sender = "";
    public ObjectGuid SenderAccountID;
    public ObjectGuid SenderGuid;
    public uint SenderVirtualRealm;
    public ObjectGuid TargetGuid;
    public uint TargetVirtualRealm;
    public ChatNotify Type;
    public ChannelNotify() : base(ServerOpcodes.ChannelNotify) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Type, 6);
        WorldPacket.WriteBits(Channel.GetByteCount(), 7);
        WorldPacket.WriteBits(Sender.GetByteCount(), 6);

        WorldPacket.WritePackedGuid(SenderGuid);
        WorldPacket.WritePackedGuid(SenderAccountID);
        WorldPacket.WriteUInt32(SenderVirtualRealm);
        WorldPacket.WritePackedGuid(TargetGuid);
        WorldPacket.WriteUInt32(TargetVirtualRealm);
        WorldPacket.WriteInt32(ChatChannelID);

        if (Type == ChatNotify.ModeChangeNotice)
        {
            WorldPacket.WriteUInt8((byte)OldFlags);
            WorldPacket.WriteUInt8((byte)NewFlags);
        }

        WorldPacket.WriteString(Channel);
        WorldPacket.WriteString(Sender);
    }
}