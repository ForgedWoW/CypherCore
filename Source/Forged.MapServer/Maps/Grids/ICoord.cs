// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Grids;

public interface ICoord
{
    uint X { get; set; }
    uint Y { get; set; }
    void DecX(uint val);

    void DecY(uint val);

    uint GetId();

    void IncX(uint val);

    void IncY(uint val);

    bool IsCoordValid();
    ICoord Normalize();
}