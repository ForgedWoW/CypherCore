// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.CHANNEL_DEMONFIRE_DAMAGE)]
public class spell_warl_channel_demonfire_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleHit(int effIndex)
	{
		var caster = Caster;
		var target = HitUnit;
		var dmgEff = Global.SpellMgr.GetSpellInfo(WarlockSpells.ROARING_BLASE_DMG_PCT, Difficulty.None)?.GetEffect(0);

		if (caster == null || target == null || !caster.HasAura(WarlockSpells.ROARING_BLAZE) || dmgEff == null)
			return;

		var damage = HitDamage;
		HitDamage = MathFunctions.AddPct(ref damage, dmgEff.BasePoints);
	}
}