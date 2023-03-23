// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Guild;

public class SplitGuildBankItemToInventory : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte? ContainerSlot;
	public byte ContainerItemSlot;
	public uint StackCount;
	public SplitGuildBankItemToInventory(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		;
		ContainerItemSlot = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();

		if (_worldPacket.HasBit())
			ContainerSlot = _worldPacket.ReadUInt8();
	}
}
