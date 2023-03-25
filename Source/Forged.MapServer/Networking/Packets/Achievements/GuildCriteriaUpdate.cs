// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildCriteriaUpdate : ServerPacket
{
	public List<GuildCriteriaProgress> Progress = new();
	public GuildCriteriaUpdate() : base(ServerOpcodes.GuildCriteriaUpdate) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Progress.Count);

		foreach (var progress in Progress)
		{
			_worldPacket.WriteUInt32(progress.CriteriaID);
			_worldPacket.WriteInt64(progress.DateCreated);
			_worldPacket.WriteInt64(progress.DateStarted);
			_worldPacket.WritePackedTime(progress.DateUpdated);
			_worldPacket.WriteUInt32(0); // this is a hack. this is a packed time written as int64 (progress.DateUpdated)
			_worldPacket.WriteUInt64(progress.Quantity);
			_worldPacket.WritePackedGuid(progress.PlayerGUID);
			_worldPacket.WriteInt32(progress.Flags);
		}
	}
}