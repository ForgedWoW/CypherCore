// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Misc;

namespace Game.Common.Networking.Packets.Misc;

public class PhaseShiftData
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
