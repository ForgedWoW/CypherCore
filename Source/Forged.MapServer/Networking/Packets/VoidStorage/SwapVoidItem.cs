// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

internal class SwapVoidItem : ClientPacket
{
    public ObjectGuid Npc;
    public ObjectGuid VoidItemGuid;
    public uint DstSlot;
    public SwapVoidItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Npc = _worldPacket.ReadPackedGuid();
        VoidItemGuid = _worldPacket.ReadPackedGuid();
        DstSlot = _worldPacket.ReadUInt32();
    }
}