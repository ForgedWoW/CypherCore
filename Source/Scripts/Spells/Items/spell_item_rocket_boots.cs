﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_rocket_boots : SpellScript, ISpellCheckCast, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.TypeId == TypeId.Player;
	}


	public SpellCastResult CheckCast()
	{
		if (Caster.IsInWater)
			return SpellCastResult.OnlyAbovewater;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster.AsPlayer;

		var bg = caster.Battleground;

		if (bg)
			bg.EventPlayerDroppedFlag(caster);

		caster.SpellHistory.ResetCooldown(ItemSpellIds.RocketBootsProc);
		caster.CastSpell(caster, ItemSpellIds.RocketBootsProc, true);
	}
}