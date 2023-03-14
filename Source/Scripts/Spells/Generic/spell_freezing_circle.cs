// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 34779 - Freezing Circle
internal class spell_freezing_circle : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDamage(int effIndex)
	{
		var caster = Caster;
		uint spellId = 0;
		var map = caster.Map;

		if (map.IsDungeon)
			spellId = map.IsHeroic ? GenericSpellIds.FreezingCirclePitOfSaronHeroic : GenericSpellIds.FreezingCirclePitOfSaronNormal;
		else
			spellId = map.Id == Misc.MapIdBloodInTheSnowScenario ? GenericSpellIds.FreezingCircleScenario : GenericSpellIds.FreezingCircle;

		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, CastDifficulty);

		if (spellInfo != null)
			if (!spellInfo.Effects.Empty())
				HitDamage = spellInfo.GetEffect(0).CalcValue();
	}
}