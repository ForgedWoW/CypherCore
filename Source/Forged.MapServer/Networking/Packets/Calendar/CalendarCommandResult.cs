// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Calendar;

internal class CalendarCommandResult : ServerPacket
{
    public byte Command;
    public string Name;
    public CalendarError Result;
    public CalendarCommandResult() : base(ServerOpcodes.CalendarCommandResult) { }

    public CalendarCommandResult(byte command, CalendarError result, string name) : base(ServerOpcodes.CalendarCommandResult)
    {
        Command = command;
        Result = result;
        Name = name;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt8(Command);
        WorldPacket.WriteUInt8((byte)Result);

        WorldPacket.WriteBits(Name.GetByteCount(), 9);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Name);
    }
}