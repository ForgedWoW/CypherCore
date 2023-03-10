// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.LIVING_FLAME_DAMAGE)]
public class spell_evoker_living_flame_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(EvokerSpells.ENERGIZING_FLAME, EvokerSpells.LIVING_FLAME);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleManaRestored, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleManaRestored(int effIndex)
	{
		if (Caster.TryGetAuraEffect(EvokerSpells.ENERGIZING_FLAME, 0, out var auraEffect))
		{
			var spellInfo = Global.SpellMgr.AssertSpellInfo(EvokerSpells.LIVING_FLAME, CastDifficulty);

			var cost = spellInfo.CalcPowerCost(PowerType.Mana, false, Caster, SpellInfo.GetSchoolMask(), null);

			if (cost == null)
				return;

			var manaRestored = MathFunctions.CalculatePct(cost.Amount, auraEffect.Amount);
			Caster.ModifyPower(PowerType.Mana, manaRestored);
		}
	}
}