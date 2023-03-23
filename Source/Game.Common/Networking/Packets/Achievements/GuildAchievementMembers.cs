// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Achievements;

public class GuildAchievementMembers : ServerPacket
{
	public ObjectGuid GuildGUID;
	public uint AchievementID;
	public List<ObjectGuid> Member = new();
	public GuildAchievementMembers() : base(ServerOpcodes.GuildAchievementMembers) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WriteInt32(Member.Count);

		foreach (var guid in Member)
			_worldPacket.WritePackedGuid(guid);
	}
}
