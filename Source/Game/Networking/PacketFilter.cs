// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking;

public abstract class PacketFilter
{
	protected WorldSession m_pSession;

	protected PacketFilter(WorldSession pSession)
	{
		m_pSession = pSession;
	}

	public abstract bool Process(WorldPacket packet);

	public virtual bool ProcessUnsafe()
	{
		return false;
	}
}