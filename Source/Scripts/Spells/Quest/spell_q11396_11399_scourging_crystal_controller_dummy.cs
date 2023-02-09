﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Quest;

[Script] // 43882 - Scourging Crystal Controller Dummy
internal class spell_q11396_11399_scourging_crystal_controller_dummy : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellEntry)
	{
		return ValidateSpellInfo(QuestSpellIds.ForceShieldArcanePurpleX3);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Unit target = GetHitUnit();

		if (target)
			if (target.IsTypeId(TypeId.Unit))
				target.RemoveAurasDueToSpell(QuestSpellIds.ForceShieldArcanePurpleX3);
	}
}