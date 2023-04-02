// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class SetItemPurchaseData : ServerPacket
{
    public ItemPurchaseContents Contents = new();
    public uint Flags;
    public ObjectGuid ItemGUID;
    public uint PurchaseTime;
    public SetItemPurchaseData() : base(ServerOpcodes.SetItemPurchaseData, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ItemGUID);
        Contents.Write(WorldPacket);
        WorldPacket.WriteUInt32(Flags);
        WorldPacket.WriteUInt32(PurchaseTime);
    }
}