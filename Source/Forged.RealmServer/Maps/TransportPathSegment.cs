// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Maps;

public class TransportPathSegment
{
	public uint SegmentEndArrivalTimestamp { get; set; }
	public uint Delay { get; set; }
	public double DistanceFromLegStartAtEnd { get; set; }
}