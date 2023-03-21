// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Maps.Grids;

public interface ICoord
{
	uint X_Coord { get; set; }
	uint Y_Coord { get; set; }
	bool IsCoordValid();
	ICoord Normalize();
	uint GetId();
	void Dec_x(uint val);
	void Inc_x(uint val);
	void Dec_y(uint val);
	void Inc_y(uint val);
}