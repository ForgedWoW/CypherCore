// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking;

public class WorldSessionFilter : PacketFilter
{
	public WorldSessionFilter(WorldSession pSession) : base(pSession) { }

	public override bool Process(WorldPacket packet)
	{
		var opHandle = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());

		//check if packet handler is supposed to be safe
		if (opHandle.ProcessingPlace == PacketProcessing.Inplace)
			return true;

		//thread-unsafe packets should be processed in World.UpdateSessions()
		if (opHandle.ProcessingPlace == PacketProcessing.ThreadUnsafe)
			return true;

		//no player attached? . our client! ^^
		var player = m_pSession.Player;

		if (!player)
			return true;

		//lets process all packets for non-in-the-world player
		return !player.IsInWorld;
	}

	public override bool ProcessUnsafe()
	{
		return true;
	}
}