// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class BroadcastAchievement : ServerPacket
{
	public ObjectGuid PlayerGUID;
	public string Name = "";
	public uint AchievementID;
	public bool GuildAchievement;
	public BroadcastAchievement() : base(ServerOpcodes.BroadcastAchievement) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Name.GetByteCount(), 7);
		_worldPacket.WriteBit(GuildAchievement);
		_worldPacket.WritePackedGuid(PlayerGUID);
		_worldPacket.WriteUInt32(AchievementID);
		_worldPacket.WriteString(Name);
	}
}