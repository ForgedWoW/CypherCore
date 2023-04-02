// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemPurchaseRefund : ClientPacket
{
    public ObjectGuid ItemGUID;
    public ItemPurchaseRefund(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        ItemGUID = WorldPacket.ReadPackedGuid();
    }
}