// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class CombatLogServerPacket : ServerPacket
{
	internal SpellCastLogData LogData;
	bool _includeLogData;

	public CombatLogServerPacket(ServerOpcodes opcode, ConnectionType connection = ConnectionType.Realm) : base(opcode, connection)
	{
		LogData = new SpellCastLogData();
	}

	public override void Write() { }

	public void SetAdvancedCombatLogging(bool value)
	{
		_includeLogData = value;
	}

	public void WriteLogDataBit()
	{
		_worldPacket.WriteBit(_includeLogData);
	}

	public void FlushBits()
	{
		_worldPacket.FlushBits();
	}

	public void WriteLogData()
	{
		if (_includeLogData)
			LogData.Write(_worldPacket);
	}
}

//Structs