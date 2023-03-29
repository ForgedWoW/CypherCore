// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class BuyFailed : ServerPacket
{
    public ObjectGuid VendorGUID;
    public uint Muid;
    public BuyResult Reason = BuyResult.CantFindItem;
    public BuyFailed() : base(ServerOpcodes.BuyFailed) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(VendorGUID);
        _worldPacket.WriteUInt32(Muid);
        _worldPacket.WriteUInt8((byte)Reason);
    }
}