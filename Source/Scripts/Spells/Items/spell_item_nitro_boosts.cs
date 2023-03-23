﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_nitro_boosts : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		if (!CastItem)
			return false;

		return true;
	}


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var areaEntry = CliDB.AreaTableStorage.LookupByKey(caster.Area);
		var success = true;

		if (areaEntry != null &&
			areaEntry.IsFlyable() &&
			!caster.Map.IsDungeon)
			success = RandomHelper.randChance(95);

		caster.CastSpell(caster, success ? ItemSpellIds.NitroBoostsSuccess : ItemSpellIds.NitroBoostsBackfire, new CastSpellExtraArgs(CastItem));
	}
}