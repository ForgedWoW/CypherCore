// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage;

enum CurveInterpolationMode
{
	Linear = 0,
	Cosine = 1,
	CatmullRom = 2,
	Bezier3 = 3,
	Bezier4 = 4,
	Bezier = 5,
	Constant = 6,
}