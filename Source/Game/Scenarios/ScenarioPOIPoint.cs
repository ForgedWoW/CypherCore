// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Scenarios;

public struct ScenarioPOIPoint
{
	public int X;
	public int Y;
	public int Z;

	public ScenarioPOIPoint(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}
}