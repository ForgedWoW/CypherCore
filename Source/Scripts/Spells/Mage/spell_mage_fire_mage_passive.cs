﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(137019)]
public class spell_mage_fire_mage_passive : AuraScript, IHasAuraEffects
{


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		// if (!Global.SpellMgr->GetSpellInfo(SPELL_MAGE_FIRE_MAGE_PASSIVE, Difficulty.None) ||
		//    !Global.SpellMgr->GetSpellInfo(SPELL_MAGE_FIRE_BLAST, Difficulty.None))
		//  return false;
		return true;
	}


	public spell_mage_fire_mage_passive()
	{
	}


	private SpellModifier mod = null;

	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void HandleApply(AuraEffect aurEffect, AuraEffectHandleModes UnnamedParameter)
	{
		Player player = GetCaster().ToPlayer();
		if (player == null)
		{
			return;
		}

		SpellModifierByClassMask mod = new SpellModifierByClassMask(aurEffect.GetBase());
		mod.op      = SpellModOp.CritChance;
		mod.type    = SpellModType.Flat;
		mod.spellId = MageSpells.SPELL_MAGE_FIRE_MAGE_PASSIVE;
		mod.value   = 200;
		mod.mask[0] = 0x2;

		player.AddSpellMod(mod, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Player player = GetCaster().ToPlayer();
		if (player == null)
		{
			return;
		}

		if (mod != null)
		{
			player.AddSpellMod(mod, false);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 4, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 4, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}