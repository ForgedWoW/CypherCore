// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Garrison;

class GarrisonEncounter
{
	public int GarrEncounterID;
	public List<int> Mechanics = new();
	public int GarrAutoCombatantID;
	public int Health;
	public int MaxHealth;
	public int Attack;
	public sbyte BoardIndex;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(GarrEncounterID);
		data.WriteInt32(Mechanics.Count);
		data.WriteInt32(GarrAutoCombatantID);
		data.WriteInt32(Health);
		data.WriteInt32(MaxHealth);
		data.WriteInt32(Attack);
		data.WriteInt8(BoardIndex);

		if (!Mechanics.Empty())
			Mechanics.ForEach(id => data.WriteInt32(id));
	}
}