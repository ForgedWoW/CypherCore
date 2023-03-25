﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets;

public class PartyMemberPhaseStates
{
	public int PhaseShiftFlags;
	public ObjectGuid PersonalGUID;
	public List<PartyMemberPhase> List = new();

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PhaseShiftFlags);
		data.WriteInt32(List.Count);
		data.WritePackedGuid(PersonalGUID);

		foreach (var phase in List)
			phase.Write(data);
	}
}