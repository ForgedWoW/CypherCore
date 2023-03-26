// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr3 : uint
{
	PvpEnabling = 0x01,                                                   // Pvp Enabling
	NoProcEquipRequirement = 0x02,                                        // No Proc Equip Requirement Description Ignores Subclass Mask Check When Checking Proc
	NoCastingBarText = 0x04,                                              // No Casting Bar Text
	CompletelyBlocked = 0x08,                                             // Completely Blocked
	NoResTimer = 0x10,                                                    // No Res Timer
	NoDurabilityLoss = 0x20,                                              // No Durability Loss
	NoAvoidance = 0x40,                                                   // No Avoidance
	DotStackingRule = 0x80,                                               // Dot Stacking Rule Description Stack Separately For Each Caster
	OnlyOnPlayer = 0x100,                                                 // Only On Player
	NotAProc = 0x200,                                                     // Not A Proc Description Without This Attribute, Any Triggered Spell Will Be Unable To Trigger Other Auras' Procs
	RequiresMainHandWeapon = 0x400,                                       // Requires Main-Hand Weapon
	OnlyBattlegrounds = 0x800,                                            // Only Battlegrounds
	OnlyOnGhosts = 0x1000,                                                // Only On Ghosts
	HideChannelBar = 0x2000,                                              // Hide Channel Bar (Client Only)
	HideInRaidFilter = 0x4000,                                            // Hide In Raid Filter (Client Only)
	NormalRangedAttack = 0x8000,                                          // Normal Ranged Attack Description Auto Shoot, Shoot, Throw - Ranged Normal Attack Attribute?
	SuppressCasterProcs = 0x10000,                                        // Suppress Caster Procs
	SuppressTargetProcs = 0x20000,                                        // Suppress Target Procs
	AlwaysHit = 0x40000,                                                  // Always Hit Description Spell Cannot Miss, Or Be Dodged/Parried/Blocked
	InstantTargetProcs = 0x80000,                                         // Instant Target Procs Description Proc Events Are Triggered Before Spell Batching Processes The Spell Hit On Target
	AllowAuraWhileDead = 0x100000,                                        // Allow Aura While Dead
	OnlyProcOutdoors = 0x200000,                                          // Only Proc Outdoors
	DoNotTriggerTargetStand = 0x400000,                                   // Do Not Trigger Target Stand
	NoDamageHistory = 0x800000, /*Nyi, No Damage History Implementation*/ // No Damage History
	RequiresOffHandWeapon = 0x1000000,                                    // Requires Off-Hand Weapon
	TreatAsPeriodic = 0x2000000,                                          // Treat As Periodic
	CanProcFromProcs = 0x4000000,                                         // Can Proc From Procs
	OnlyProcOnCaster = 0x8000000,                                         // Only Proc On Caster
	IgnoreCasterAndTargetRestrictions = 0x10000000, /*Nyi*/               // Ignore Caster & Target Restrictions
	IgnoreCasterModifiers = 0x20000000,                                   // Ignore Caster Modifiers
	DoNotDisplayRange = 0x40000000,                                       // Do Not Display Range (Client Only)
	NotOnAoeImmune = 0x80000000 /*Nyi, No Aoe Immunity Implementation*/   // Not On Aoe Immune
}