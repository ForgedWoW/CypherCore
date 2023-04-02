// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class BuySucceeded : ServerPacket
{
    public uint Muid;
    public uint NewQuantity;
    public uint QuantityBought;
    public ObjectGuid VendorGUID;
    public BuySucceeded() : base(ServerOpcodes.BuySucceeded) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(VendorGUID);
        WorldPacket.WriteUInt32(Muid);
        WorldPacket.WriteUInt32(NewQuantity);
        WorldPacket.WriteUInt32(QuantityBought);
    }
}