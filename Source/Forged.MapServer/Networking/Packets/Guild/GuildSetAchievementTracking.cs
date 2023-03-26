// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Guild;

internal class GuildSetAchievementTracking : ClientPacket
{
	public List<uint> AchievementIDs = new();
	public GuildSetAchievementTracking(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadUInt32();

		for (uint i = 0; i < count; ++i)
			AchievementIDs.Add(_worldPacket.ReadUInt32());
	}
}