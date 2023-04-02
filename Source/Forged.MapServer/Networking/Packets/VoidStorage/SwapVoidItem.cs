// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

internal class SwapVoidItem : ClientPacket
{
    public uint DstSlot;
    public ObjectGuid Npc;
    public ObjectGuid VoidItemGuid;
    public SwapVoidItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Npc = WorldPacket.ReadPackedGuid();
        VoidItemGuid = WorldPacket.ReadPackedGuid();
        DstSlot = WorldPacket.ReadUInt32();
    }
}