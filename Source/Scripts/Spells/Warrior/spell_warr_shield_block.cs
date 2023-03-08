// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Models;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior;

[SpellScript(2565)]
public class spell_warr_shield_block_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var _player = Caster.AsPlayer;

		if (_player != null)
			_player.CastSpell(_player, WarriorSpells.SHIELD_BLOCKC_TRIGGERED, true);
	}
}

[SpellScript(2565)]
public class spell_warr_shield_block_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return Global.SpellMgr.GetSpellInfo(WarriorSpells.SHIELD_BLOCKC_TRIGGERED, Difficulty.None) != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 0, AuraType.None));
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
	{
		var caster = Caster;

		if (caster != null)
			if (caster.HasAura(WarriorSpells.HEAVY_REPERCUSSIONS))
				amount.Value += 30;
	}
}