// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Channel;

public class JoinChannel : ClientPacket
{
    public string ChannelName;
    public int ChatChannelId;
    public bool CreateVoiceSession;
    public bool Internal;
    public string Password;
    public JoinChannel(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ChatChannelId = WorldPacket.ReadInt32();
        CreateVoiceSession = WorldPacket.HasBit();
        Internal = WorldPacket.HasBit();
        var channelLength = WorldPacket.ReadBits<uint>(7);
        var passwordLength = WorldPacket.ReadBits<uint>(7);
        ChannelName = WorldPacket.ReadString(channelLength);
        Password = WorldPacket.ReadString(passwordLength);
    }
}