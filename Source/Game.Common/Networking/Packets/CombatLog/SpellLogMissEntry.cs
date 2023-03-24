﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.CombatLog;

public class SpellLogMissEntry
{
	public ObjectGuid Victim;
	public byte MissReason;
	SpellLogMissDebug? Debug;

	public SpellLogMissEntry(ObjectGuid victim, byte missReason)
	{
		Victim = victim;
		MissReason = missReason;
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Victim);
		data.WriteUInt8(MissReason);

		if (data.WriteBit(Debug.HasValue))
			Debug.Value.Write(data);

		data.FlushBits();
	}
}
