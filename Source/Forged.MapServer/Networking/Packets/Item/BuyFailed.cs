// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class BuyFailed : ServerPacket
{
    public uint Muid;
    public BuyResult Reason = BuyResult.CantFindItem;
    public ObjectGuid VendorGUID;
    public BuyFailed() : base(ServerOpcodes.BuyFailed) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(VendorGUID);
        WorldPacket.WriteUInt32(Muid);
        WorldPacket.WriteUInt8((byte)Reason);
    }
}