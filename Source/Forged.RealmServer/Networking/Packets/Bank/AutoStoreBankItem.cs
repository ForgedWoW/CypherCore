// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

public class AutoStoreBankItem : ClientPacket
{
	public InvUpdate Inv;
	public byte Bag;
	public byte Slot;

	public AutoStoreBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Inv = new InvUpdate(_worldPacket);
		Bag = _worldPacket.ReadUInt8();
		Slot = _worldPacket.ReadUInt8();
	}
}