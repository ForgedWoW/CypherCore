﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class PlayerAzeriteItemEquippedStatusChanged : ServerPacket
{
	public bool IsHeartEquipped;
	public PlayerAzeriteItemEquippedStatusChanged() : base(ServerOpcodes.PlayerAzeriteItemEquippedStatusChanged) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsHeartEquipped);
		_worldPacket.FlushBits();
	}
}