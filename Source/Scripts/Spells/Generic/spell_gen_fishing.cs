﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 131474 - Fishing
internal class spell_gen_fishing : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override bool Load()
	{
		return Caster.IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		uint spellId;
		var mainHand = Caster.AsPlayer.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

		if (!mainHand ||
			mainHand.			Template.			Class != ItemClass.Weapon ||
			(ItemSubClassWeapon)mainHand.Template.SubClass != ItemSubClassWeapon.FishingPole)
			spellId = GenericSpellIds.FishingNoFishingPole;
		else
			spellId = GenericSpellIds.FishingWithPole;

		Caster.CastSpell(Caster, spellId, false);
	}
}