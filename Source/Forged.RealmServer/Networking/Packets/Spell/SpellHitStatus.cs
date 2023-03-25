// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

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