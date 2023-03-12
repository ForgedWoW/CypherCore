// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum VehicleFlags
{
	NoStrafe = 0x01,          // Sets Moveflag2NoStrafe
	NoJumping = 0x02,         // Sets Moveflag2NoJumping
	Fullspeedturning = 0x04,  // Sets Moveflag2Fullspeedturning
	AllowPitching = 0x10,     // Sets Moveflag2AllowPitching
	Fullspeedpitching = 0x20, // Sets Moveflag2Fullspeedpitching
	CustomPitch = 0x40,       // If Set Use Pitchmin And Pitchmax From Dbc, Otherwise Pitchmin = -Pi/2, Pitchmax = Pi/2
	AdjustAimAngle = 0x400,   // LuaIsvehicleaimangleadjustable
	AdjustAimPower = 0x800,   // LuaIsvehicleaimpoweradjustable
	FixedPosition = 0x200000  // Used for cannons, when they should be rooted
}