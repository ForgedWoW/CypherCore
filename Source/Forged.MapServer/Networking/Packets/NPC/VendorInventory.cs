// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class VendorInventory : ServerPacket
{
    public List<VendorItemPkt> Items = new();
    public byte Reason = 0;
    public ObjectGuid Vendor;
    public VendorInventory() : base(ServerOpcodes.VendorInventory, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Vendor);
        WorldPacket.WriteUInt8(Reason);
        WorldPacket.WriteInt32(Items.Count);

        foreach (var item in Items)
            item.Write(WorldPacket);
    }
}