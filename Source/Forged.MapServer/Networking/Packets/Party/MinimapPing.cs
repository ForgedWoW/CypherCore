﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class MinimapPing : ServerPacket
{
	public ObjectGuid Sender;
	public float PositionX;
	public float PositionY;
	public MinimapPing() : base(ServerOpcodes.MinimapPing) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Sender);
		_worldPacket.WriteFloat(PositionX);
		_worldPacket.WriteFloat(PositionY);
	}
}