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
        _worldPacket.WritePackedGuid(ItemGUID);
        _worldPacket.WriteUInt8(Result);
        _worldPacket.WriteBit(Contents != null);
        _worldPacket.FlushBits();

        Contents?.Write(_worldPacket);
    }
}