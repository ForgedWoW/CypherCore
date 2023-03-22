// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

public class MonsterSplineFilter
{
	public List<MonsterSplineFilterKey> FilterKeys = new();
	public byte FilterFlags;
	public float BaseSpeed;
	public short StartOffset;
	public float DistToPrevFilterKey;
	public short AddedToStart;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(FilterKeys.Count);
		data.WriteFloat(BaseSpeed);
		data.WriteInt16(StartOffset);
		data.WriteFloat(DistToPrevFilterKey);
		data.WriteInt16(AddedToStart);

		FilterKeys.ForEach(p => p.Write(data));

		data.WriteBits(FilterFlags, 2);
		data.FlushBits();
	}
}