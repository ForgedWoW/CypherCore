// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Grids;

public interface ICoord
{
    uint X { get; set; }
    uint Y { get; set; }
    void Dec_x(uint val);

    void Dec_y(uint val);

    uint GetId();

    void Inc_x(uint val);

    void Inc_y(uint val);

    bool IsCoordValid();
    ICoord Normalize();
}