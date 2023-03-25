// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class DFJoin : ClientPacket
{
	public bool QueueAsGroup;
	public byte PartyIndex;
	public LfgRoles Roles;
	public List<uint> Slots = new();
	bool Unknown; // Always false in 7.2.5
	public DFJoin(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QueueAsGroup = _worldPacket.HasBit();
		Unknown = _worldPacket.HasBit();
		PartyIndex = _worldPacket.ReadUInt8();
		Roles = (LfgRoles)_worldPacket.ReadUInt32();

		var slotsCount = _worldPacket.ReadInt32();

		for (var i = 0; i < slotsCount; ++i) // Slots
			Slots.Add(_worldPacket.ReadUInt32());
	}
}

//Structs