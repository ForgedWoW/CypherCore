﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class DeathReleaseLoc : ServerPacket
{
	public int MapID;
	public WorldLocation Loc;
	public DeathReleaseLoc() : base(ServerOpcodes.DeathReleaseLoc) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WriteXYZ(Loc);
	}
}