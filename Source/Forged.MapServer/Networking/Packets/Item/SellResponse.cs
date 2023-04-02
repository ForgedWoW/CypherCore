// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class SellResponse : ServerPacket
{
    public ObjectGuid ItemGUID;
    public SellResult Reason = SellResult.Unk;
    public ObjectGuid VendorGUID;
    public SellResponse() : base(ServerOpcodes.SellResponse) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(VendorGUID);
        WorldPacket.WritePackedGuid(ItemGUID);
        WorldPacket.WriteUInt8((byte)Reason);
    }
}