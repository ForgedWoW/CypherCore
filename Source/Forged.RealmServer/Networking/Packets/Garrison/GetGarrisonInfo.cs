﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class GetGarrisonInfo : ClientPacket
{
	public GetGarrisonInfo(WorldPacket packet) : base(packet) { }

	public override void Read() { }
}