// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Guild;

public class GuildNewsPkt : ServerPacket
{
	public List<GuildNewsEvent> NewsEvents;

	public GuildNewsPkt() : base(ServerOpcodes.GuildNews)
	{
		NewsEvents = new List<GuildNewsEvent>();
	}

	public override void Write()
	{
		_worldPacket.WriteInt32(NewsEvents.Count);

		foreach (var newsEvent in NewsEvents)
			newsEvent.Write(_worldPacket);
	}
}
