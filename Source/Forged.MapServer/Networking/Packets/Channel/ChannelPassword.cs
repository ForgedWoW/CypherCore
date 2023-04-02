// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Channel;

internal class ChannelPassword : ClientPacket
{
    public string ChannelName;
    public string Password;
    public ChannelPassword(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var channelNameLength = WorldPacket.ReadBits<uint>(7);
        var passwordLength = WorldPacket.ReadBits<uint>(7);
        ChannelName = WorldPacket.ReadString(channelNameLength);
        Password = WorldPacket.ReadString(passwordLength);
    }
}