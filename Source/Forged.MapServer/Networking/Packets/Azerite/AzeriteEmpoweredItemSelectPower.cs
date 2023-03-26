// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Azerite;

internal class AzeriteEmpoweredItemSelectPower : ClientPacket
{
	public int Tier;
	public int AzeritePowerID;
	public byte ContainerSlot;
	public byte Slot;
	public AzeriteEmpoweredItemSelectPower(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tier = _worldPacket.ReadInt32();
		AzeritePowerID = _worldPacket.ReadInt32();
		ContainerSlot = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}