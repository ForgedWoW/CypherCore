﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class QueryCorpseTransport : ClientPacket
{
	public ObjectGuid Player;
	public ObjectGuid Transport;
	public QueryCorpseTransport(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Player = _worldPacket.ReadPackedGuid();
		Transport = _worldPacket.ReadPackedGuid();
	}
}