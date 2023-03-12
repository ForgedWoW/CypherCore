// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum VehicleSeatFlags : uint
{
	HasLowerAnimForEnter = 0x01,
	HasLowerAnimForRide = 0x02,
	DisableGravity = 0x04, // Passenger will not be affected by gravity
	ShouldUseVehSeatExitAnimOnVoluntaryExit = 0x08,
	Unk5 = 0x10,
	Unk6 = 0x20,
	Unk7 = 0x40,
	Unk8 = 0x80,
	Unk9 = 0x100,
	HidePassenger = 0x200,      // Passenger Is Hidden
	AllowTurning = 0x400,       // Needed For CgcameraSyncfreelookfacing
	CanControl = 0x800,         // LuaUnitinvehiclecontrolseat
	CanCastMountSpell = 0x1000, // Can Cast Spells With SpellAuraMounted From Seat (Possibly 4.X Only, 0 Seats On 3.3.5a)
	Uncontrolled = 0x2000,      // Can Override !& CanEnterOrExit
	CanAttack = 0x4000,         // Can Attack, Cast Spells And Use Items From Vehicle
	ShouldUseVehSeatExitAnimOnForcedExit = 0x8000,
	Unk17 = 0x10000,
	Unk18 = 0x20000, // Needs Research And Support (28 Vehicles): Allow Entering Vehicles While Keeping Specific Permanent(?) Auras That Impose Visuals (States Like Beeing Under Freeze/Stun Mechanic, Emote State Animations).
	HasVehExitAnimVoluntaryExit = 0x40000,
	HasVehExitAnimForcedExit = 0x80000,
	PassengerNotSelectable = 0x100000,
	Unk22 = 0x200000,
	RecHasVehicleEnterAnim = 0x400000,
	IsUsingVehicleControls = 0x800000, // LuaIsusingvehiclecontrols
	EnableVehicleZoom = 0x1000000,
	CanEnterOrExit = 0x2000000, // LuaCanexitvehicle - Can Enter And Exit At Free Will
	CanSwitch = 0x4000000,      // LuaCanswitchvehicleseats
	HasStartWaritingForVehTransitionAnimEnter = 0x8000000,
	HasStartWaritingForVehTransitionAnimExit = 0x10000000,
	CanCast = 0x20000000, // LuaUnithasvehicleui
	Unk2 = 0x40000000,    // Checked In Conjunction With 0x800 In Castspell2
	AllowsInteraction = 0x80000000
}