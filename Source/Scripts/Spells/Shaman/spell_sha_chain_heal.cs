﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 1064 - Chain Heal
[SpellScript(1064)]
public class spell_sha_chain_heal : SpellScript, IHasSpellEffects
{
	private WorldObject _primaryTarget = null;
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new ObjectTargetSelectHandler(CatchInitialTarget, 0, Targets.UnitChainhealAlly));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectAdditionalTargets, 0, Targets.UnitChainhealAlly));
	}

	private void CatchInitialTarget(WorldObject target)
	{
		_primaryTarget = target;
	}

	private void SelectAdditionalTargets(List<WorldObject> targets)
	{
		var caster = Caster;
		var highTide = caster.GetAuraEffect(ShamanSpells.HIGH_TIDE, 1);

		if (highTide == null)
			return;

		var range = 25.0f;
		var targetInfo = new SpellImplicitTargetInfo(Targets.UnitChainhealAlly);
		var conditions = SpellInfo.GetEffect(0).ImplicitTargetConditions;

		var containerTypeMask = Spell.GetSearcherTypeMask(targetInfo.ObjectType, conditions);

		if (containerTypeMask == 0)
			return;

		var chainTargets = new List<WorldObject>();
		var check = new WorldObjectSpellAreaTargetCheck(range, _primaryTarget.Location, caster, caster, SpellInfo, targetInfo.CheckType, conditions, SpellTargetObjectTypes.Unit);
		var searcher = new WorldObjectListSearcher(caster, chainTargets, check, containerTypeMask);
		Cell.VisitGrid(_primaryTarget, searcher, range);

		chainTargets.RemoveIf(new UnitAuraCheck<WorldObject>(false, ShamanSpells.Riptide, caster.GUID));

		if (chainTargets.Count == 0)
			return;

		chainTargets.Sort();
		targets.Sort();

		var extraTargets = new List<WorldObject>();
		extraTargets = chainTargets.Except(targets).ToList();
		extraTargets.RandomResize((uint)highTide.Amount);
		targets.AddRange(extraTargets);
	}
}