using Framework.Constants;

namespace Game.Entities;

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