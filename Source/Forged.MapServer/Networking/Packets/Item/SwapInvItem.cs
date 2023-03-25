// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public class SwapInvItem : ClientPacket
{
	public InvUpdate Inv;
	public byte Slot1; // Source Slot
	public byte Slot2; // Destination Slot
	public SwapInvItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Slot2 = _worldPacket.ReadUInt8();
		Slot1 = _worldPacket.ReadUInt8();
	}
}