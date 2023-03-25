﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class WaitQueueUpdate : ServerPacket
{
	public AuthWaitInfo WaitInfo;
	public WaitQueueUpdate() : base(ServerOpcodes.WaitQueueUpdate) { }

	public override void Write()
	{
		WaitInfo.Write(_worldPacket);
	}
}