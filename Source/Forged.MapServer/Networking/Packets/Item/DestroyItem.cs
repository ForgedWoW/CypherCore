// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class DestroyItem : ClientPacket
{
    public uint Count;
    public byte SlotNum;
    public byte ContainerId;
    public DestroyItem(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Count = _worldPacket.ReadUInt32();
        ContainerId = _worldPacket.ReadUInt8();
        SlotNum = _worldPacket.ReadUInt8();
    }
}