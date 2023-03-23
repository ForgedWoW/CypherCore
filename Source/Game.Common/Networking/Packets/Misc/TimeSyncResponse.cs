// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class TimeSyncResponse : ClientPacket
{
	public uint ClientTime;    // Client ticks in ms
	public uint SequenceIndex; // Same index as in request
	public TimeSyncResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SequenceIndex = _worldPacket.ReadUInt32();
		ClientTime = _worldPacket.ReadUInt32();
	}

	public DateTime GetReceivedTime()
	{
		return _worldPacket.GetReceivedTime();
	}
}
