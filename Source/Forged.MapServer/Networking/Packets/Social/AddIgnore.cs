// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Social;

public class AddIgnore : ClientPacket
{
    public ObjectGuid AccountGUID;
    public string Name;
    public AddIgnore(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var nameLength = WorldPacket.ReadBits<uint>(9);
        AccountGUID = WorldPacket.ReadPackedGuid();
        Name = WorldPacket.ReadString(nameLength);
    }
}