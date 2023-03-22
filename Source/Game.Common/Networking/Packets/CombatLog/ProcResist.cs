// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class ProcResist : ServerPacket
{
	public ObjectGuid Caster;
	public ObjectGuid Target;
	public uint SpellID;
	public float? Rolled;
	public float? Needed;
	public ProcResist() : base(ServerOpcodes.ProcResist) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(Rolled.HasValue);
		_worldPacket.WriteBit(Needed.HasValue);
		_worldPacket.FlushBits();

		if (Rolled.HasValue)
			_worldPacket.WriteFloat(Rolled.Value);

		if (Needed.HasValue)
			_worldPacket.WriteFloat(Needed.Value);
	}
}