// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Achievements;

public class GuildGetAchievementMembers : ClientPacket
{
	public ObjectGuid PlayerGUID;
	public ObjectGuid GuildGUID;
	public uint AchievementID;
	public GuildGetAchievementMembers(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PlayerGUID = _worldPacket.ReadPackedGuid();
		GuildGUID = _worldPacket.ReadPackedGuid();
		AchievementID = _worldPacket.ReadUInt32();
	}
}
