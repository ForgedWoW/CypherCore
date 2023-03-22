﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class StopMirrorTimer : ServerPacket
{
	public MirrorTimerType Timer;

	public StopMirrorTimer(MirrorTimerType timer) : base(ServerOpcodes.StopMirrorTimer)
	{
		Timer = timer;
	}

	public override void Write()
	{
		_worldPacket.WriteInt32((int)Timer);
	}
}