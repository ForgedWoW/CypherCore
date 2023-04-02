// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Channel;

internal class ChannelCommand : ClientPacket
{
    public string ChannelName;

    public ChannelCommand(WorldPacket packet) : base(packet)
    {
        switch (GetOpcode())
        {
            case ClientOpcodes.ChatChannelAnnouncements:
            case ClientOpcodes.ChatChannelDeclineInvite:
            case ClientOpcodes.ChatChannelDisplayList:
            case ClientOpcodes.ChatChannelList:
            case ClientOpcodes.ChatChannelOwner:
                break;
            default:
                //ABORT();
                break;
        }
    }

    public override void Read()
    {
        ChannelName = WorldPacket.ReadString(WorldPacket.ReadBits<uint>(7));
    }
}