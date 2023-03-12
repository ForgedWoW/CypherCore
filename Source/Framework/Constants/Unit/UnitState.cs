// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum UnitState : uint
{
	Died = 0x01,           // Player Has Fake Death Aura
	MeleeAttacking = 0x02, // Player Is Melee Attacking Someone
	Charmed = 0x04,        // having any kind of charm aura on self
	Stunned = 0x08,
	Roaming = 0x10,
	Chase = 0x20,
	Focusing = 0x40,
	Fleeing = 0x80,
	InFlight = 0x100, // Player Is In Flight Mode
	Follow = 0x200,
	Root = 0x400,
	Confused = 0x800,
	Distracted = 0x1000,
	Isolated = 0x2000, // Area Auras Do Not Affect Other Players
	AttackPlayer = 0x4000,
	Casting = 0x8000,
	Possessed = 0x10000, // being possessed by another unit
	Charging = 0x20000,
	Jumping = 0x40000,
	FollowFormation = 0x80000,
	Move = 0x100000,
	Rotating = 0x200000,
	Evade = 0x400000,
	RoamingMove = 0x800000,
	ConfusedMove = 0x1000000,
	FleeingMove = 0x2000000,
	ChaseMove = 0x4000000,
	FollowMove = 0x8000000,
	IgnorePathfinding = 0x10000000,
	FollowFormationMove = 0x20000000,

	AllStateSupported = Died | MeleeAttacking | Charmed | Stunned | Roaming | Chase | Focusing | Fleeing | InFlight | Follow | Root | Confused | Distracted | Isolated | AttackPlayer | Casting | Possessed | Charging | Jumping | Move | Rotating | Evade | RoamingMove | ConfusedMove | FleeingMove | ChaseMove | FollowMove | IgnorePathfinding | FollowFormationMove,

	Unattackable = InFlight,
	Moving = RoamingMove | ConfusedMove | FleeingMove | ChaseMove | FollowMove | FollowFormationMove,
	Controlled = Confused | Stunned | Fleeing,
	LostControl = Controlled | Possessed | Jumping | Charging,
	CannotAutoattack = Controlled | Charging | Casting,
	Sightless = LostControl | Evade,
	CannotTurn = LostControl | Rotating | Focusing,
	NotMove = Root | Stunned | Died | Distracted,

	AllErasable = AllStateSupported & ~IgnorePathfinding,
	AllState = 0xffffffff
}