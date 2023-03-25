﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class SpellClick : ClientPacket
{
	public ObjectGuid SpellClickUnitGuid;
	public bool TryAutoDismount;
	public bool IsSoftInteract;
	public SpellClick(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellClickUnitGuid = _worldPacket.ReadPackedGuid();
		TryAutoDismount = _worldPacket.HasBit();
		IsSoftInteract = _worldPacket.HasBit();
	}
}