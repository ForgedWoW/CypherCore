// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum VehicleExitParameters
{
	VehicleExitParamNone = 0,   // provided parameters will be ignored
	VehicleExitParamOffset = 1, // provided parameters will be used as offset values
	VehicleExitParamDest = 2,   // provided parameters will be used as absolute destination
	VehicleExitParamMax
}