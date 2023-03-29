// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemExpirePurchaseRefund : ServerPacket
{
    public ObjectGuid ItemGUID;
    public ItemExpirePurchaseRefund() : base(ServerOpcodes.ItemExpirePurchaseRefund, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(ItemGUID);
    }
}