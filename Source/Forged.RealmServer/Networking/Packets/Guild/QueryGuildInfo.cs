// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class QueryGuildInfo : ClientPacket
{
	public ObjectGuid GuildGuid;
	public ObjectGuid PlayerGuid;
	public QueryGuildInfo(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GuildGuid = _worldPacket.ReadPackedGuid();
		PlayerGuid = _worldPacket.ReadPackedGuid();
	}
}

//Structs