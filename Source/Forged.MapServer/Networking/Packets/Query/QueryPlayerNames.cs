// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Query;

public class QueryPlayerNames : ClientPacket
{
	public ObjectGuid[] Players;
	public QueryPlayerNames(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Players = new ObjectGuid[_worldPacket.ReadInt32()];

		for (var i = 0; i < Players.Length; ++i)
			Players[i] = _worldPacket.ReadPackedGuid();
	}
}

//Structs