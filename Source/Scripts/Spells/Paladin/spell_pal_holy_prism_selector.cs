// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 114852 - Holy Prism (Damage)
[SpellScript(new uint[]
{
	114852, 114871
})] // 114871 - Holy Prism (Heal)
internal class spell_pal_holy_prism_selector : SpellScript, IHasSpellEffects
{
	private List<WorldObject> _sharedTargets = new();
	private ObjectGuid _targetGUID;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PaladinSpells.HolyPrismTargetAlly, PaladinSpells.HolyPrismTargetBeamVisual);
	}

	public override void Register()
	{
		if (ScriptSpellId == PaladinSpells.HolyPrismTargetEnemy)
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaAlly));
		else if (ScriptSpellId == PaladinSpells.HolyPrismTargetAlly)
			SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));

		SpellEffects.Add(new ObjectAreaTargetSelectHandler(ShareTargets, 2, Targets.UnitDestAreaEntry));

		SpellEffects.Add(new EffectHandler(SaveTargetGuid, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void SaveTargetGuid(int effIndex)
	{
		_targetGUID = HitUnit.GetGUID();
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		byte maxTargets = 5;

		if (targets.Count > maxTargets)
		{
			if (SpellInfo.Id == PaladinSpells.HolyPrismTargetAlly)
			{
				targets.Sort(new HealthPctOrderPred());
				targets.Resize(maxTargets);
			}
			else
			{
				targets.RandomResize(maxTargets);
			}
		}

		_sharedTargets = targets;
	}

	private void ShareTargets(List<WorldObject> targets)
	{
		targets.Clear();
		targets.AddRange(_sharedTargets);
	}

	private void HandleScript(int effIndex)
	{
		var initialTarget = Global.ObjAccessor.GetUnit(Caster, _targetGUID);

		initialTarget?.CastSpell(HitUnit, PaladinSpells.HolyPrismTargetBeamVisual, true);
	}
}