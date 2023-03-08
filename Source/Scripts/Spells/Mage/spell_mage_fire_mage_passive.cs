// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(137019)]
public class spell_mage_fire_mage_passive : AuraScript, IHasAuraEffects
{
	private readonly SpellModifier mod = null;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public spell_mage_fire_mage_passive() { }

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		// if (!Global.SpellMgr->GetSpellInfo(FIRE_MAGE_PASSIVE, Difficulty.None) ||
		//    !Global.SpellMgr->GetSpellInfo(FIRE_BLAST, Difficulty.None))
		//  return false;
		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 4, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 4, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleApply(AuraEffect aurEffect, AuraEffectHandleModes UnnamedParameter)
	{
		var player = Caster.AsPlayer;

		if (player == null)
			return;

		var mod = new SpellModifierByClassMask(aurEffect.Base);
		mod.Op = SpellModOp.CritChance;
		mod.Type = SpellModType.Flat;
		mod.SpellId = MageSpells.FIRE_MAGE_PASSIVE;
		mod.Value = 200;
		mod.Mask[0] = 0x2;

		player.AddSpellMod(mod, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var player = Caster.AsPlayer;

		if (player == null)
			return;

		if (mod != null)
			player.AddSpellMod(mod, false);
	}
}