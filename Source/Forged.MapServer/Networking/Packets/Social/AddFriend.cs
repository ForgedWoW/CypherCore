// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Social;

public class AddFriend : ClientPacket
{
    public string Name;
    public string Notes;
    public AddFriend(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var nameLength = WorldPacket.ReadBits<uint>(9);
        var noteslength = WorldPacket.ReadBits<uint>(10);
        Name = WorldPacket.ReadString(nameLength);
        Notes = WorldPacket.ReadString(noteslength);
    }
}