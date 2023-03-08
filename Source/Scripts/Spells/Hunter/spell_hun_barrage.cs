// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(120361)]
public class spell_hun_barrage : SpellScript, IHasSpellEffects, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public void OnHit()
	{
		var player = Caster.AsPlayer;
		var target = HitUnit;

		if (player == null || target == null)
			return;


		player.CalculateMinMaxDamage(WeaponAttackType.RangedAttack, true, true, out var minDamage, out var maxDamage);

		var dmg = (minDamage + maxDamage) / 2 * 0.8f;

		if (!target.HasAura(HunterSpells.BARRAGE, player.GUID))
			dmg /= 2;

		dmg = player.SpellDamageBonusDone(target, SpellInfo, dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, Spell);
		dmg = target.SpellDamageBonusTaken(player, SpellInfo, dmg, DamageEffectType.Direct);

		// Barrage now deals only 80% of normal damage against player-controlled targets.
		if (target.SpellModOwner)
			dmg = MathFunctions.CalculatePct(dmg, 80);

		HitDamage = dmg;
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CheckLOS, 0, Targets.UnitConeEnemy24));
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(CheckLOS, 1, Targets.UnitConeEnemy24));
	}


	private void CheckLOS(List<WorldObject> targets)
	{
		if (targets.Count == 0)
			return;

		var caster = Caster;

		if (caster == null)
			return;


		targets.RemoveIf((WorldObject objects) =>
		{
			if (objects == null)
				return true;

			if (!objects.IsWithinLOSInMap(caster))
				return true;

			if (objects.AsUnit && !caster.IsValidAttackTarget(objects.AsUnit))
				return true;

			return false;
		});
	}
}