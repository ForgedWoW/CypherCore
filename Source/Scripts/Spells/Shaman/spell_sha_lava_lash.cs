// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 60103 - Lava Lash
[SpellScript(60103)]
public class spell_sha_lava_lash : SpellScript, ISpellOnHit
{
	public override bool Load()
	{
		return Caster.IsPlayer();
	}

	public void OnHit()
	{
		Caster.CastSpell(HitUnit, ShamanSpells.LAVA_LASH_SPREAD_FLAME_SHOCK, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.MaxTargets, EffectValue));

		Caster.RemoveAura(ShamanSpells.HOT_HAND);

		var target = HitUnit;

		if (target == null)
			return;

		if (Caster.HasAura(ShamanSpells.CRASHING_STORM_DUMMY) && Caster.HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
			Caster.CastSpell(target, ShamanSpells.CRASHING_LIGHTNING_DAMAGE, true);

		if (Caster && Caster.HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
			Caster.CastSpell(null, ShamanSpells.CRASH_LIGHTNING_PROC, true);
	}
}