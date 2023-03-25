﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class CancelCast : ClientPacket
{
	public uint SpellID;
	public ObjectGuid CastID;
	public CancelCast(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CastID = _worldPacket.ReadPackedGuid();
		SpellID = _worldPacket.ReadUInt32();
	}
}