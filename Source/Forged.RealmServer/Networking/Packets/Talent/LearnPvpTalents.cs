// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Networking.Packets;

class LearnPvpTalents : ClientPacket
{
	public Array<PvPTalent> Talents = new(4);
	public LearnPvpTalents(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var size = _worldPacket.ReadUInt32();

		for (var i = 0; i < size; ++i)
			Talents[i] = new PvPTalent(_worldPacket);
	}
}