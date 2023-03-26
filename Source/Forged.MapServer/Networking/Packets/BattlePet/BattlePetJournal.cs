// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetJournal : ServerPacket
{
	public ushort Trap;
	public bool HasJournalLock = false;
	public List<BattlePetSlot> Slots = new();
	public List<BattlePetStruct> Pets = new();
	public BattlePetJournal() : base(ServerOpcodes.BattlePetJournal) { }

	public override void Write()
	{
		_worldPacket.WriteUInt16(Trap);
		_worldPacket.WriteInt32(Slots.Count);
		_worldPacket.WriteInt32(Pets.Count);
		_worldPacket.WriteBit(HasJournalLock);
		_worldPacket.FlushBits();

		foreach (var slot in Slots)
			slot.Write(_worldPacket);

		foreach (var pet in Pets)
			pet.Write(_worldPacket);
	}
}

//Structs