﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class QueryPetition : ClientPacket
{
	public ObjectGuid ItemGUID;
	public uint PetitionID;
	public QueryPetition(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetitionID = _worldPacket.ReadUInt32();
		ItemGUID = _worldPacket.ReadPackedGuid();
	}
}