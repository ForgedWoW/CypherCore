// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.CombatLog;

public class EnvironmentalDamageLog : CombatLogServerPacket
{
	public ObjectGuid Victim;
	public EnviromentalDamage Type;
	public int Amount;
	public int Resisted;
	public int Absorbed;
	public EnvironmentalDamageLog() : base(ServerOpcodes.EnvironmentalDamageLog) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteUInt8((byte)Type);
		_worldPacket.WriteInt32(Amount);
		_worldPacket.WriteInt32(Resisted);
		_worldPacket.WriteInt32(Absorbed);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}
