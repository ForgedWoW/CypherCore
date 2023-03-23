// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.CombatLog;

struct SpellLogMissDebug
{
	public void Write(WorldPacket data)
	{
		data.WriteFloat(HitRoll);
		data.WriteFloat(HitRollNeeded);
	}

	public float HitRoll;
	public float HitRollNeeded;
}
