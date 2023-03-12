// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;
// SpellScript interface - enum used for runtime checks of script function calls

// AuraScript interface - enum used for runtime checks of script function calls
public enum AuraScriptHookType
{
	EffectApply = SpellScriptState.Unloading + 1,
	EffectAfterApply,
	EffectRemove,
	EffectAfterRemove,
	EffectPeriodic,
	EffectUpdatePeriodic,
	EffectCalcAmount,
	EffectCalcPeriodic,
	EffectCalcSpellmod,
	EffectCalcCritChance,
	EffectAbsorb,
	EffectAfterAbsorb,
	EffectManaShield,
	EffectAfterManaShield,
	EffectSplit,
	CheckAreaTarget,
	Dispel,
	AfterDispel,
	EnterLeaveCombat,

	// Spell Proc Hooks
	CheckProc,
	CheckEffectProc,
	PrepareProc,
	Proc,
	EffectProc,
	EffectAfterProc,
	AfterProc,

	//Apply,
	//Remove
	EffectAbsorbHeal,
	EffectAfterAbsorbHeal,
}