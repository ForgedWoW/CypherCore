// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class DestroyItem : ClientPacket
{
    public byte ContainerId;
    public uint Count;
    public byte SlotNum;
    public DestroyItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Count = WorldPacket.ReadUInt32();
        ContainerId = WorldPacket.ReadUInt8();
        SlotNum = WorldPacket.ReadUInt8();
    }
}