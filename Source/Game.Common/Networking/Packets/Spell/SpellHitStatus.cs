// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public struct SpellHitStatus
{
	public SpellHitStatus(SpellMissInfo reason)
	{
		Reason = reason;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt8((byte)Reason);
	}

	public SpellMissInfo Reason;
}
