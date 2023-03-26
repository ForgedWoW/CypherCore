// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Bank;

internal class AutoStoreBankReagent : ClientPacket
{
	public InvUpdate Inv;
	public byte Slot;
	public byte PackSlot;
	public AutoStoreBankReagent(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Slot = _worldPacket.ReadUInt8();
		PackSlot = _worldPacket.ReadUInt8();
	}
}