// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemEnchantTimeUpdate : ServerPacket
{
    public uint DurationLeft;
    public ObjectGuid ItemGuid;
    public ObjectGuid OwnerGuid;
    public uint Slot;
    public ItemEnchantTimeUpdate() : base(ServerOpcodes.ItemEnchantTimeUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ItemGuid);
        WorldPacket.WriteUInt32(DurationLeft);
        WorldPacket.WriteUInt32(Slot);
        WorldPacket.WritePackedGuid(OwnerGuid);
    }
}