// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellAttr2 : uint
{
	AllowDeadTarget = 0x01,                             // Allow Dead Target
	NoShapeshiftUi = 0x02,                              // No Shapeshift Ui (Client Only) Description Does Not Replace Action Bar When Shapeshifted
	IgnoreLineOfSight = 0x04,                           // Ignore Line Of Sight
	AllowLowLevelBuff = 0x08,                           // Allow Low Level Buff
	UseShapeshiftBar = 0x10,                            // Use Shapeshift Bar (Client Only)
	AutoRepeat = 0x20,                                  // Auto Repeat
	CannotCastOnTapped = 0x40,                          // Cannot Cast On Tapped Description Can Only Target Untapped Units, Or Those Tapped By Caster
	DoNotReportSpellFailure = 0x80,                     // Do Not Report Spell Failure
	IncludeInAdvancedCombatLog = 0x100,                 // Include In Advanced Combat Log (Client Only) Description Determines Whether To Include This Aura In List Of Auras In SmsgEncounterStart
	AlwaysCastAsUnit = 0x200, /*Nyi, Unk*/              // Always Cast As Unit
	SpecialTamingFlag = 0x400,                          // Special Taming Flag Description Related To Taming?
	NoTargetPerSecondCosts = 0x800,                     // No Target Per-Second Costs
	ChainFromCaster = 0x1000,                           // Chain From Caster
	EnchantOwnItemOnly = 0x2000,                        // Enchant Own Item Only
	AllowWhileInvisible = 0x4000,                       // Allow While Invisible
	DoNotConsumeIfGainedDuringCast = 0x8000,            // Do Not Consume If Gained During Cast
	NoActivePets = 0x10000,                             // No Active Pets
	DoNotResetCombatTimers = 0x20000,                   // Do Not Reset Combat Timers Description Does Not Reset Melee/Ranged Autoattack Timer On Cast
	NoJumpWhileCastPending = 0x40000,                   // No Jump While Cast Pending (Client Only)
	AllowWhileNotShapeshiftedCasterForm = 0x80000,      // Allow While Not Shapeshifted (Caster Form) Description Even If Stances Are Nonzero, Allow Spell To Be Cast Outside Of Shapeshift (Though Not In A Different Shapeshift)
	InitiateCombatPostCastEnablesAutoAttack = 0x100000, // Initiate Combat Post-Cast (Enables Auto-Attack)
	FailOnAllTargetsImmune = 0x200000,                  // Fail On All Targets Immune Description Causes Bg Flags To Be Dropped If Combined With Attr1DispelAurasOnImmunity
	NoInitialThreat = 0x400000,                         // No Initial Threat
	ProcCooldownOnFailure = 0x800000,                   // Proc Cooldown On Failure
	ItemCastWithOwnerSkill = 0x1000000,                 // Item Cast With Owner Skill
	DontBlockManaRegen = 0x2000000,                     // Don'T Block Mana Regen
	NoSchoolImmunities = 0x4000000,                     // No School Immunities Description Allow Aura To Be Applied Despite Target Being Immune To New Aura Applications
	IgnoreWeaponskill = 0x8000000,                      // Ignore Weaponskill
	NotAnAction = 0x10000000,                           // Not An Action
	CantCrit = 0x20000000,                              // Can'T Crit
	ActiveThreat = 0x40000000,                          // Active Threat
	RetainItemCast = 0x80000000                         // Retain Item Cast Description Passes MCastitem To Triggered Spells
}