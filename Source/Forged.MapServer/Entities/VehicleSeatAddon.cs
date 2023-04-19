// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities;

public class VehicleSeatAddon
{
    public VehicleExitParameters ExitParameter { get; set; }
    public float ExitParameterO { get; set; }
    public float ExitParameterX { get; set; }
    public float ExitParameterY { get; set; }
    public float ExitParameterZ { get; set; }
    public float SeatOrientationOffset { get; set; }

    public VehicleSeatAddon(float orientatonOffset, float exitX, float exitY, float exitZ, float exitO, byte param)
    {
        SeatOrientationOffset = orientatonOffset;
        ExitParameterX = exitX;
        ExitParameterY = exitY;
        ExitParameterZ = exitZ;
        ExitParameterO = exitO;
        ExitParameter = (VehicleExitParameters)param;
    }
}