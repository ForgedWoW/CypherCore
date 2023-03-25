﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Networking.Packets;

class PhaseShiftData
{
	public uint PhaseShiftFlags;
	public List<PhaseShiftDataPhase> Phases = new();
	public ObjectGuid PersonalGUID;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(PhaseShiftFlags);
		data.WriteInt32(Phases.Count);
		data.WritePackedGuid(PersonalGUID);

		foreach (var phaseShiftDataPhase in Phases)
			phaseShiftDataPhase.Write(data);
	}
}