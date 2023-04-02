// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

internal struct VoidItem
{
    public ObjectGuid Creator;

    public ObjectGuid Guid;

    public ItemInstance Item;

    public uint Slot;

    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(Guid);
        data.WritePackedGuid(Creator);
        data.WriteUInt32(Slot);
        Item.Write(data);
    }
}