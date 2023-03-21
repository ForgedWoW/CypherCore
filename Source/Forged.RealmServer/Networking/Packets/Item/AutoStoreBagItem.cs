// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class AutoStoreBagItem : ClientPacket
{
	public byte ContainerSlotB;
	public InvUpdate Inv;
	public byte ContainerSlotA;
	public byte SlotA;
	public AutoStoreBagItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		ContainerSlotB = _worldPacket.ReadUInt8();
		ContainerSlotA = _worldPacket.ReadUInt8();
		SlotA = _worldPacket.ReadUInt8();
	}
}