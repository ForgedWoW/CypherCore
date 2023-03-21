// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Movement;

namespace Forged.RealmServer.Maps;

public class TransportPathLeg
{
	public List<TransportPathSegment> Segments = new();
	public uint MapId { get; set; }
	public Spline<double> Spline { get; set; }
	public uint StartTimestamp { get; set; }
	public uint Duration { get; set; }
}