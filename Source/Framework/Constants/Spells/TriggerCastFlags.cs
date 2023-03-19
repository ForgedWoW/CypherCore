// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum TriggerCastFlags : uint
{
	None = 0x0,                       //! Not Triggered
	IgnoreGCD = 0x01,                 //! Will Ignore Gcd
	IgnoreSpellAndCategoryCD = 0x02,  //! Will Ignore Spell And Category Cooldowns
	IgnorePowerAndReagentCost = 0x04, //! Will Ignore Power And Reagent Cost
	IgnoreCastItem = 0x08,            //! Will Not Take Away Cast Item Or Update Related Achievement Criteria
	IgnoreAuraScaling = 0x10,         //! Will Ignore Aura Scaling
	IgnoreCastInProgress = 0x20,      //! Will Not Check If A Current Cast Is In Progress
	IgnoreComboPoints = 0x40,         //! Will Ignore Combo Point Requirement
	CastDirectly = 0x80,              //! In Spell.Prepare, Will Be Cast Directly Without Setting Containers For Executed Spell
	IgnoreAuraInterruptFlags = 0x100, //! Will Ignore Interruptible Aura'S At Cast
	IgnoreSetFacing = 0x200,          //! Will Not Adjust Facing To Target (If Any)
	IgnoreShapeshift = 0x400,         //! Will Ignore Shapeshift Checks

	// reuse
	DisallowProcEvents = 0x1000,             //! Disallows proc events from triggered spell (default)
	IgnoreCasterMountedOrOnVehicle = 0x2000, //! Will Ignore Mounted/On Vehicle Restrictions

	// reuse                                        = 0x4000,
	// reuse                                        = 0x8000,
	IgnoreCasterAuras = 0x10000,      //! Will Ignore Caster Aura Restrictions Or Requirements
	DontResetPeriodicTimer = 0x20000, //! Will allow periodic aura timers to keep ticking (instead of resetting)
	DontReportCastError = 0x40000,    //! Will Return SpellFailedDontReport In Checkcast Functions
	FullMask = 0x0007FFFF,            //! Used when doing CastSpell with triggered == true

	// debug flags (used with .cast triggered commands)
	IgnoreEquippedItemRequirement = 0x80000, //! Will ignore equipped item requirements
	IgnoreTargetCheck = 0x100000,            //! Will ignore most target checks (mostly DBC target checks)
	IgnoreCasterAurastate = 0x200000,        //! Will Ignore Caster Aura States Including Combat Requirements And Death State

	TriggeredAllowProc = 0x1000000,

    FullDebugMask = 0xFFFFFFFF,
}