﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class BattlegroundCapturePointInfo
{
	public ObjectGuid Guid;
	public Vector2 Pos;
	public BattlegroundCapturePointState State = BattlegroundCapturePointState.Neutral;
	public long CaptureTime;
	public TimeSpan CaptureTotalDuration;

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Guid);
		data.WriteVector2(Pos);
		data.WriteInt8((sbyte)State);

		if (State == BattlegroundCapturePointState.ContestedHorde || State == BattlegroundCapturePointState.ContestedAlliance)
		{
			data.WriteInt64(CaptureTime);
			data.WriteUInt32((uint)CaptureTotalDuration.TotalMilliseconds);
		}
	}
}