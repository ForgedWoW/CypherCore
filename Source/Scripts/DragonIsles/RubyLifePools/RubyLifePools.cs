﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.DragonIsles.RubyLifePools;

internal struct SpellIds
{
	// Flashfrost Chillweaver
	public const uint IceShield = 372749;

	// Primal Juggernaut
	public const uint Excavate = 373497;
};

// 371652 - Executed
internal class spell_ruby_life_pools_executed : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
	}

	private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var target = Target;
		target.SetUnitFlag3(UnitFlags3.FakeDead);
		target.SetUnitFlag2(UnitFlags2.FeignDeath);
		target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
	}
}

// 384933 - Ice Shield
internal class spell_ruby_life_pools_ice_shield : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var iceShield = Target?.GetAura(SpellIds.IceShield);
		iceShield?.RefreshDuration();
	}
}

// 372793 - Excavate
internal class spell_ruby_life_pools_excavate : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		Caster?.CastSpell(Target, SpellIds.Excavate, true);
	}
}

// 395029 - Storm Infusion
internal class spell_ruby_life_pools_storm_infusion : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 1, Targets.DestDest));
	}

	private void SetDest(SpellDestination dest)
	{
		dest.RelocateOffset(new Position(9.0f, 0.0f, 4.0f, 0.0f));
	}
}