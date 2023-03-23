// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Achievements;

public class GuildSetFocusedAchievement : ClientPacket
{
	public uint AchievementID;
	public GuildSetFocusedAchievement(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AchievementID = _worldPacket.ReadUInt32();
	}
}
