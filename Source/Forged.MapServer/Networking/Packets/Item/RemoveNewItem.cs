// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Item;

internal class RemoveNewItem : ClientPacket
{
    public RemoveNewItem(WorldPacket packet) : base(packet) { }

    public ObjectGuid ItemGuid { get; set; }

    public override void Read()
    {
        ItemGuid = WorldPacket.ReadPackedGuid();
    }
}