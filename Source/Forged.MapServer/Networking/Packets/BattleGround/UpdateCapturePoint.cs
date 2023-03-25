﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class UpdateCapturePoint : ServerPacket
{
	public BattlegroundCapturePointInfo CapturePointInfo;

	public UpdateCapturePoint() : base(ServerOpcodes.UpdateCapturePoint) { }

	public override void Write()
	{
		CapturePointInfo.Write(_worldPacket);
	}
}