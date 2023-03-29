// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildInviteByName : ClientPacket
{
    public string Name;
    public int? Unused910;
    public GuildInviteByName(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var nameLen = _worldPacket.ReadBits<uint>(9);
        var hasUnused910 = _worldPacket.HasBit();

        Name = _worldPacket.ReadString(nameLen);

        if (hasUnused910)
            Unused910 = _worldPacket.ReadInt32();
    }
}