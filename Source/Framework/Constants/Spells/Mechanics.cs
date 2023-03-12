// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum Mechanics
{
	None = 0,
	Charm = 1,
	Disoriented = 2,
	Disarm = 3,
	Distract = 4,
	Fear = 5,
	Grip = 6,
	Root = 7,
	SlowAttack = 8,
	Silence = 9,
	Sleep = 10,
	Snare = 11,
	Stun = 12,
	Freeze = 13,
	Knockout = 14,
	Bleed = 15,
	Bandage = 16,
	Polymorph = 17,
	Banish = 18,
	Shield = 19,
	Shackle = 20,
	Mount = 21,
	Infected = 22,
	Turn = 23,
	Horror = 24,
	Invulnerability = 25,
	Interrupt = 26,
	Daze = 27,
	Discovery = 28,
	ImmuneShield = 29, // Divine (Blessing) Shield/Protection And Ice Block
	Sapped = 30,
	Enraged = 31,
	Wounded = 32,
	Infected2 = 33,
	Infected3 = 34,
	Infected4 = 35,
	Taunted = 36,
	Max = 37,

	ImmuneToMovementImpairmentAndLossControlMask = ((1 << Charm) |
													(1 << Disoriented) |
													(1 << Fear) |
													(1 << Root) |
													(1 << Sleep) |
													(1 << Snare) |
													(1 << Stun) |
													(1 << Freeze) |
													(1 << Silence) |
													(1 << Disarm) |
													(1 << Knockout) |
													(1 << Polymorph) |
													(1 << Banish) |
													(1 << Shackle) |
													(1 << Turn) |
													(1 << Horror) |
													(1 << Daze) |
													(1 << Sapped))
}