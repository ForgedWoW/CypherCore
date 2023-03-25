// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets;

class PartyMemberPetStats
{
	public ObjectGuid GUID;
	public string Name;
	public short ModelId;

	public int CurrentHealth;
	public int MaxHealth;

	public List<PartyMemberAuraStates> Auras = new();

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(GUID);
		data.WriteInt32(ModelId);
		data.WriteInt32(CurrentHealth);
		data.WriteInt32(MaxHealth);
		data.WriteInt32(Auras.Count);
		Auras.ForEach(p => p.Write(data));

		data.WriteBits(Name.GetByteCount(), 8);
		data.FlushBits();
		data.WriteString(Name);
	}
}