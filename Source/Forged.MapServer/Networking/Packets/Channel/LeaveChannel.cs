// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Channel;

public class LeaveChannel : ClientPacket
{
    public string ChannelName;
    public int ZoneChannelID;
    public LeaveChannel(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ZoneChannelID = _worldPacket.ReadInt32();
        ChannelName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(7));
    }
}