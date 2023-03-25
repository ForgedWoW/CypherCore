// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

class LearnTalents : ClientPacket
{
	public Array<ushort> Talents = new(PlayerConst.MaxTalentTiers);
	public LearnTalents(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadBits<uint>(6);

		for (var i = 0; i < count; ++i)
			Talents[i] = _worldPacket.ReadUInt16();
	}
}