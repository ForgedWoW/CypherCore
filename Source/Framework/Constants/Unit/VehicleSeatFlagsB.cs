// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum VehicleSeatFlagsB : uint
{
	None = 0x00,
	UsableForced = 0x02,
	TargetsInRaidUi = 0x08, // Lua_Unittargetsvehicleinraidui
	Ejectable = 0x20,       // Ejectable
	UsableForced2 = 0x40,
	UsableForced3 = 0x100,
	PassengerMirrorsAnims = 0x10000, // Passenger forced to repeat all vehicle animations
	KeepPet = 0x20000,
	UsableForced4 = 0x02000000,
	CanSwitch = 0x4000000,
	VehiclePlayerframeUi = 0x80000000 // Lua_Unithasvehicleplayerframeui - Actually Checked For Flagsb &~ 0x80000000
}