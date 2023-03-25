﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class SpellCooldownPkt : ServerPacket
{
	public List<SpellCooldownStruct> SpellCooldowns = new();
	public ObjectGuid Caster;
	public SpellCooldownFlags Flags;
	public SpellCooldownPkt() : base(ServerOpcodes.SpellCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt8((byte)Flags);
		_worldPacket.WriteInt32(SpellCooldowns.Count);
		SpellCooldowns.ForEach(p => p.Write(_worldPacket));
	}
}