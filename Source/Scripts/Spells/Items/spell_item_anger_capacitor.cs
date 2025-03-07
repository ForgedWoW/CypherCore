﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_tiny_abomination_in_a_jar", 8)]
[Script("spell_item_tiny_abomination_in_a_jar_hero", 7)]
internal class spell_item_anger_capacitor : AuraScript, IHasAuraEffects
{
	private readonly int _stackAmount;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_item_anger_capacitor(int stackAmount)
	{
		_stackAmount = stackAmount;
	}


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.Actor;
		var target = eventInfo.ProcTarget;

		caster.CastSpell((Unit)null, ItemSpellIds.MoteOfAnger, true);
		var motes = caster.GetAura(ItemSpellIds.MoteOfAnger);

		if (motes == null ||
			motes.StackAmount < _stackAmount)
			return;

		caster.RemoveAura(ItemSpellIds.MoteOfAnger);
		var spellId = ItemSpellIds.ManifestAngerMainHand;
		var player = caster.AsPlayer;

		if (player)
			if (player.GetWeaponForAttack(WeaponAttackType.OffAttack, true) &&
				RandomHelper.URand(0, 1) != 0)
				spellId = ItemSpellIds.ManifestAngerOffHand;

		caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
	}

	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Target.RemoveAura(ItemSpellIds.MoteOfAnger);
	}
}