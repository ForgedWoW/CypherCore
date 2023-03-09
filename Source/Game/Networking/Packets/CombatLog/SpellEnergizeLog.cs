// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SpellEnergizeLog : CombatLogServerPacket
{
	public ObjectGuid TargetGUID;
	public ObjectGuid CasterGUID;
	public uint SpellID;
	public PowerType Type;
	public int Amount;
	public int OverEnergize;
	public SpellEnergizeLog() : base(ServerOpcodes.SpellEnergizeLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WritePackedGuid(CasterGUID);

		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt32((uint)Type);
		_worldPacket.WriteInt32(Amount);
		_worldPacket.WriteInt32(OverEnergize);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}