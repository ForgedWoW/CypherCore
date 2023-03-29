// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class SetItemPurchaseData : ServerPacket
{
    public uint PurchaseTime;
    public uint Flags;
    public ItemPurchaseContents Contents = new();
    public ObjectGuid ItemGUID;
    public SetItemPurchaseData() : base(ServerOpcodes.SetItemPurchaseData, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(ItemGUID);
        Contents.Write(_worldPacket);
        _worldPacket.WriteUInt32(Flags);
        _worldPacket.WriteUInt32(PurchaseTime);
    }
}