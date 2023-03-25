// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class GuildCriteriaDeleted : ServerPacket
{
	public ObjectGuid GuildGUID;
	public uint CriteriaID;
	public GuildCriteriaDeleted() : base(ServerOpcodes.GuildCriteriaDeleted) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(GuildGUID);
		_worldPacket.WriteUInt32(CriteriaID);
	}
}