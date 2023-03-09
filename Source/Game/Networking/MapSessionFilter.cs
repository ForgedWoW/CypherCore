// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking;

public class MapSessionFilter : PacketFilter
{
	public MapSessionFilter(WorldSession pSession) : base(pSession) { }

	public override bool Process(WorldPacket packet)
	{
		var opHandle = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());

		//check if packet handler is supposed to be safe
		if (opHandle.ProcessingPlace == PacketProcessing.Inplace)
			return true;

		//we do not process thread-unsafe packets
		if (opHandle.ProcessingPlace == PacketProcessing.ThreadUnsafe)
			return false;

		var player = m_pSession.Player;

		if (!player)
			return false;

		//in Map.Update() we do not process packets where player is not in world!
		return player.IsInWorld;
	}
}