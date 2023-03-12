// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum MovementFlag2
{
	None = 0x0,
	NoStrafe = 0x1,
	NoJumping = 0x2,
	FullSpeedTurning = 0x4,
	FullSpeedPitching = 0x8,
	AlwaysAllowPitching = 0x10,
	IsVehicleExitVoluntary = 0x20,
	WaterwalkingFullPitch = 0x40, // Will Always Waterwalk, Even If Facing The Camera Directly Down
	VehiclePassengerIsTransitionAllowed = 0x80,
	CanSwimToFlyTrans = 0x100,
	Unk9 = 0x200, // Terrain Normal Calculation Is Disabled If This Flag Is Not Present, Client Automatically Handles Setting This Flag
	CanTurnWhileFalling = 0x400,
	IgnoreMovementForces = 0x800,
	CanDoubleJump = 0x1000,
	DoubleJump = 0x2000,

	// These Flags Are Not Sent
	AwaitingLoad = 0x10000,
	InterpolatedMovement = 0x20000,
	InterpolatedTurning = 0x40000,
	InterpolatedPitching = 0x80000
}