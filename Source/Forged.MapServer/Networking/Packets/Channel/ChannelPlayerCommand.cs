// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

internal class ChannelPlayerCommand : ClientPacket
{
    public string ChannelName;
    public string Name;

    public ChannelPlayerCommand(WorldPacket packet) : base(packet)
    {
        switch (GetOpcode())
        {
            case ClientOpcodes.ChatChannelBan:
            case ClientOpcodes.ChatChannelInvite:
            case ClientOpcodes.ChatChannelKick:
            case ClientOpcodes.ChatChannelModerator:
            case ClientOpcodes.ChatChannelSetOwner:
            case ClientOpcodes.ChatChannelSilenceAll:
            case ClientOpcodes.ChatChannelUnban:
            case ClientOpcodes.ChatChannelUnmoderator:
            case ClientOpcodes.ChatChannelUnsilenceAll:
                break;
            default:
                //ABORT();
                break;
        }
    }

    public override void Read()
    {
        var channelNameLength = WorldPacket.ReadBits<uint>(7);
        var nameLength = WorldPacket.ReadBits<uint>(9);
        ChannelName = WorldPacket.ReadString(channelNameLength);
        Name = WorldPacket.ReadString(nameLength);
    }
}