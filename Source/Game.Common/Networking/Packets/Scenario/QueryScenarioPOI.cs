// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

public class QueryScenarioPOI : ClientPacket
{
	public Array<int> MissingScenarioPOIs = new(50);
	public QueryScenarioPOI(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadUInt32();

		for (var i = 0; i < count; ++i)
			MissingScenarioPOIs[i] = _worldPacket.ReadInt32();
	}
}