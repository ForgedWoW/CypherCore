// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Spell;

public struct SpellChannelStartInterruptImmunities
{
	public void Write(WorldPacket data)
	{
		data.WriteInt32(SchoolImmunities);
		data.WriteInt32(Immunities);
	}

	public int SchoolImmunities;
	public int Immunities;
}
