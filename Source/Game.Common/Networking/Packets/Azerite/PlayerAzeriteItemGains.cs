﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Azerite;

public class PlayerAzeriteItemGains : ServerPacket
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
