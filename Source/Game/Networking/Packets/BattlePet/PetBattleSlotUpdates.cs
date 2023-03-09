// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

class PetBattleSlotUpdates : ServerPacket
{
	public List<BattlePetSlot> Slots = new();
	public bool AutoSlotted;
	public bool NewSlot;
	public PetBattleSlotUpdates() : base(ServerOpcodes.PetBattleSlotUpdates) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Slots.Count);
		_worldPacket.WriteBit(NewSlot);
		_worldPacket.WriteBit(AutoSlotted);
		_worldPacket.FlushBits();

		foreach (var slot in Slots)
			slot.Write(_worldPacket);
	}
}