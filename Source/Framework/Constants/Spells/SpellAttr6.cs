// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr6 : uint
{
	NoCooldownOnTooltip = 0x01,                                     // No Cooldown On Tooltip (Client Only)
	DoNotResetCooldownInArena = 0x02,                               // Do Not Reset Cooldown In Arena
	NotAnAttack = 0x04, /*Nyi*/                                     // Not An Attack
	CanAssistImmunePc = 0x08,                                       // Can Assist Immune Pc
	IgnoreForModTimeRate = 0x10, /*Nyi, Time Rate Not Implemented*/ // Ignore For Mod Time Rate
	DoNotConsumeResources = 0x20,                                   // Do Not Consume Resources
	FloatingCombatTextOnCast = 0x40,                                // Floating Combat Text On Cast (Client Only)
	AuraIsWeaponProc = 0x80,                                        // Aura Is Weapon Proc
	DoNotChainToCrowdControlledTargets = 0x100,                     // Do Not Chain To Crowd-Controlled Targets Description Implicit Targeting (Chaining And Area Targeting) Will Not Impact Crowd Controlled Targets
	AllowOnCharmedTargets = 0x200, /*Nyi*/                          // Allow On Charmed Targets
	NoAuraLog = 0x400,                                              // No Aura Log
	NotInRaidInstances = 0x800,                                     // Not In Raid Instances
	AllowWhileRidingVehicle = 0x1000,                               // Allow While Riding Vehicle
	IgnorePhaseShift = 0x2000,                                      // Ignore Phase Shift
	AiPrimaryRangedAttack = 0x4000, /*Nyi*/                         // Ai Primary Ranged Attack
	NoPushback = 0x8000,                                            // No Pushback
	NoJumpPathing = 0x10000, /*Nyi*/                                // No Jump Pathing
	AllowEquipWhileCasting = 0x20000,                               // Allow Equip While Casting
	OriginateFromController = 0x40000,                              // Originate From Controller Description Client Will Prevent Casting If Not Possessed, Charmer Will Be Caster For All Intents And Purposes
	DelayCombatTimerDuringCast = 0x80000,                           // Delay Combat Timer During Cast
	AuraIconOnlyForCasterLimit10 = 0x100000,                        // Aura Icon Only For Caster (Limit 10) (Client Only)
	ShowMechanicAsCombatText = 0x200000,                            // Show Mechanic As Combat Text (Client Only)
	AbsorbCannotBeIgnore = 0x400000,                                // Absorb Cannot Be Ignore
	TapsImmediately = 0x800000,                                     // Taps Immediately
	CanTargetUntargetable = 0x1000000,                              // Can Target Untargetable
	DoesntResetSwingTimerIfInstant = 0x2000000,                     // Doesn'T Reset Swing Timer If Instant
	VehicleImmunityCategory = 0x4000000, /*Nyi*/                    // Vehicle Immunity Category
	IgnoreHealingModifiers = 0x8000000,                             // Ignore Healing Modifiers Description This Prevents Certain Healing Modifiers From Applying - See Implementation If You Really Care About Details
	DoNotAutoSelectTargetWithInitiatesCombat = 0x10000000,          // Do Not Auto Select Target With Initiates Combat (Client Only)
	IgnoreCasterDamageModifiers = 0x20000000,                       // Ignore Caster Damage Modifiers Description This Prevents Certain Damage Modifiers From Applying - See Implementation If You Really Care About Details
	DisableTiedEffectPoints = 0x40000000, /*Nyi*/                   // Disable Tied Effect Points
	NoCategoryCooldownMods = 0x80000000                             // No Category Cooldown Mods
}