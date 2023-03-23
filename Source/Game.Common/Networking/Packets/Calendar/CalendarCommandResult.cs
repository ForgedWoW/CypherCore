// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Calendar;

public class CalendarCommandResult : ServerPacket
{
	public byte Command;
	public CalendarError Result;
	public string Name;
	public CalendarCommandResult() : base(ServerOpcodes.CalendarCommandResult) { }

	public CalendarCommandResult(byte command, CalendarError result, string name) : base(ServerOpcodes.CalendarCommandResult)
	{
		Command = command;
		Result = result;
		Name = name;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt8(Command);
		_worldPacket.WriteUInt8((byte)Result);

		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(Name);
	}
}
