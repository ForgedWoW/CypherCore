// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class StoreGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public StoreGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}