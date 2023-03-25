// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class PlayerAzeriteItemGains : ServerPacket
{
	public ObjectGuid ItemGUID;
	public ulong XP;
	public PlayerAzeriteItemGains() : base(ServerOpcodes.PlayerAzeriteItemGains) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ItemGUID);
		_worldPacket.WriteUInt64(XP);
	}
}