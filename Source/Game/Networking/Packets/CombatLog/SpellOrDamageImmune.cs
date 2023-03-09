// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SpellOrDamageImmune : ServerPacket
{
	public ObjectGuid CasterGUID;
	public ObjectGuid VictimGUID;
	public uint SpellID;
	public bool IsPeriodic;
	public SpellOrDamageImmune() : base(ServerOpcodes.SpellOrDamageImmune, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WritePackedGuid(VictimGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(IsPeriodic);
		_worldPacket.FlushBits();
	}
}