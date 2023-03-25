﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class GuildAssignMemberRank : ClientPacket
{
	public ObjectGuid Member;
	public int RankOrder;
	public GuildAssignMemberRank(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Member = _worldPacket.ReadPackedGuid();
		RankOrder = _worldPacket.ReadInt32();
	}
}