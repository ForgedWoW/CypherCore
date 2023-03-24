// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Guild;

public class GuildCommandResult : ServerPacket
{
	public string Name;
	public GuildCommandError Result;
	public GuildCommandType Command;
	public GuildCommandResult() : base(ServerOpcodes.GuildCommandResult) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WriteUInt32((uint)Command);

		_worldPacket.WriteBits(Name.GetByteCount(), 8);
		_worldPacket.WriteString(Name);
	}
}
