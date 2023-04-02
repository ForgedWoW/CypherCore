// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildCommandResult : ServerPacket
{
    public GuildCommandType Command;
    public string Name;
    public GuildCommandError Result;
    public GuildCommandResult() : base(ServerOpcodes.GuildCommandResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)Result);
        WorldPacket.WriteUInt32((uint)Command);

        WorldPacket.WriteBits(Name.GetByteCount(), 8);
        WorldPacket.WriteString(Name);
    }
}