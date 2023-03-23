﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_seaforium_blast : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override bool Load()
	{
		// OriginalCaster is always available in Spell.prepare
		return GObjCaster.OwnerGUID.IsPlayer;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(AchievementCredit, 1, SpellEffectName.GameObjectDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void AchievementCredit(int effIndex)
	{
		// but in effect handling OriginalCaster can become null
		var owner = GObjCaster.OwnerUnit;

		if (owner != null)
		{
			var go = HitGObj;

			if (go)
				if (go.Template.type == GameObjectTypes.DestructibleBuilding)
					owner.CastSpell(null, GenericSpellIds.PlantChargesCreditAchievement, true);
		}
	}
}