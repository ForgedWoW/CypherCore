// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class SwapItem : ClientPacket
{
	public InvUpdate Inv;
	public byte SlotA;
	public byte ContainerSlotB;
	public byte SlotB;
	public byte ContainerSlotA;
	public SwapItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		ContainerSlotB = _worldPacket.ReadUInt8();
		ContainerSlotA = _worldPacket.ReadUInt8();
		SlotB = _worldPacket.ReadUInt8();
		SlotA = _worldPacket.ReadUInt8();
	}
}