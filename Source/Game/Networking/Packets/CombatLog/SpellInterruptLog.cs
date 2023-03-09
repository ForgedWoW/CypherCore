// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SpellInterruptLog : ServerPacket
{
	public ObjectGuid Caster;
	public ObjectGuid Victim;
	public uint InterruptedSpellID;
	public uint SpellID;
	public SpellInterruptLog() : base(ServerOpcodes.SpellInterruptLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteUInt32(InterruptedSpellID);
		_worldPacket.WriteUInt32(SpellID);
	}
}