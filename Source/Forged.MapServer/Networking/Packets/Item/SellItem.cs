// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Item;

public class SellItem : ClientPacket
{
    public uint Amount;
    public ObjectGuid ItemGUID;
    public ObjectGuid VendorGUID;
    public SellItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        VendorGUID = _worldPacket.ReadPackedGuid();
        ItemGUID = _worldPacket.ReadPackedGuid();
        Amount = _worldPacket.ReadUInt32();
    }
}