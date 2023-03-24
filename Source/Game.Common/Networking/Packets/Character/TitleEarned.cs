﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Character;

public class TitleEarned : ServerPacket
{
	public uint Index;
	public TitleEarned(ServerOpcodes opcode) : base(opcode) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Index);
	}
}
