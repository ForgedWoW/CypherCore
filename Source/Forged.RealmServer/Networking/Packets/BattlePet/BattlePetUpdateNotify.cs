﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class BattlePetUpdateNotify : ClientPacket
{
	public ObjectGuid PetGuid;

	public BattlePetUpdateNotify(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGuid = _worldPacket.ReadPackedGuid();
	}
}