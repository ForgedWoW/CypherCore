// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public struct SpellMissStatus
{
	public SpellMissStatus(SpellMissInfo reason, SpellMissInfo reflectStatus)
	{
		Reason = reason;
		ReflectStatus = reflectStatus;
	}

	public void Write(WorldPacket data)
	{
		data.WriteBits((byte)Reason, 4);

		if (Reason == SpellMissInfo.Reflect)
			data.WriteBits(ReflectStatus, 4);

		data.FlushBits();
	}

	public SpellMissInfo Reason;
	public SpellMissInfo ReflectStatus;
}