// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.Entities;

public class VehicleSeatAddon
{
	public float SeatOrientationOffset;
	public float ExitParameterX;
	public float ExitParameterY;
	public float ExitParameterZ;
	public float ExitParameterO;
	public VehicleExitParameters ExitParameter;

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
