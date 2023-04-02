// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemPurchaseRefundResult : ServerPacket
{
    public ItemPurchaseContents Contents;
    public ObjectGuid ItemGUID;
    public byte Result;
    public ItemPurchaseRefundResult() : base(ServerOpcodes.ItemPurchaseRefundResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ItemGUID);
        WorldPacket.WriteUInt8(Result);
        WorldPacket.WriteBit(Contents != null);
        WorldPacket.FlushBits();

        Contents?.Write(WorldPacket);
    }
}