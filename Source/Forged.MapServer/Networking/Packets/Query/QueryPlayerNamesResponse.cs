// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class QueryPlayerNamesResponse : ServerPacket
{
	public List<NameCacheLookupResult> Players = new();

	public QueryPlayerNamesResponse() : base(ServerOpcodes.QueryPlayerNamesResponse) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Players.Count);

		foreach (var lookupResult in Players)
			lookupResult.Write(_worldPacket);
	}
}