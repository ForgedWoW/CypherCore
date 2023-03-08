// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 44425 - Arcane Barrage
internal class spell_mage_arcane_barrage : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	private ObjectGuid _primaryTarget;

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.ArcaneBarrageR3, MageSpells.ArcaneBarrageEnergize) && spellInfo.Effects.Count > 1;
	}

	public void AfterCast()
	{
		var caster = Caster;

		// Consume all arcane charges
		var arcaneCharges = -caster.ModifyPower(PowerType.ArcaneCharges, -caster.GetMaxPower(PowerType.ArcaneCharges), false);

		if (arcaneCharges != 0)
		{
			var auraEffect = caster.GetAuraEffect(MageSpells.ArcaneBarrageR3, 0, caster.GetGUID());

			if (auraEffect != null)
				caster.CastSpell(caster, MageSpells.ArcaneBarrageEnergize, new CastSpellExtraArgs(SpellValueMod.BasePoint0, arcaneCharges * auraEffect.Amount / 100));
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(MarkPrimaryTarget, 1, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
	}

	private void HandleEffectHitTarget(int effIndex)
	{
		if (HitUnit.GetGUID() != _primaryTarget)
			HitDamage = MathFunctions.CalculatePct(HitDamage, GetEffectInfo(1).CalcValue(Caster));
	}

	private void MarkPrimaryTarget(int effIndex)
	{
		_primaryTarget = HitUnit.GetGUID();
	}
}