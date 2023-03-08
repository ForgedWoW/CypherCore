// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior;

// 163201  - Execute
// 217955  - Execute
// 281000  - Execute
[SpellScript(new uint[]
{
	163201, 217955, 281000
})]
public class spell_warr_execute : SpellScript, ISpellAfterHit, ISpellOnTakePower
{
	private int m_powerTaken = 0;

	public void AfterHit()
	{
		var caster = Caster;

		if (caster == null)
			return;

		//basepoint on effect 1 is 20 on all spells listed above
		var target = HitUnit;

		if (target != null)
			if (target.IsAlive && caster != null)
				caster.SetPower(PowerType.Rage, m_powerTaken + 20);

		caster.VariableStorage.Remove("spell_warr_execute_damages::multiplier");
		caster.RemoveAura(WarriorSpells.SUDDEN_DEATH);
	}

	public void TakePower(SpellPowerCost powerCost)
	{
		m_powerTaken = powerCost.Amount;
		var requiredAmount = powerCost.Amount - powerCost.Amount;
		double dmgMultiplier = powerCost.Amount / (requiredAmount != 0 ? requiredAmount : 1);
		Caster.VariableStorage.Set("spell_warr_execute_damages::multiplier", dmgMultiplier);
	}
}