﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class DestructibleBuildingDamage : ServerPacket
{
	public ObjectGuid Target;
	public ObjectGuid Caster;
	public ObjectGuid Owner;
	public int Damage;
	public uint SpellID;
	public DestructibleBuildingDamage() : base(ServerOpcodes.DestructibleBuildingDamage, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(Owner);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteInt32(Damage);
		_worldPacket.WriteUInt32(SpellID);
	}
}