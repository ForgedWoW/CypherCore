// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
{
	49966, 17253, 16827
})]
public class spell_hun_pet_basic_attack : SpellScript, IHasSpellEffects, ISpellCheckCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		var caster = Caster;

		if (caster == null)
			return SpellCastResult.DontReport;

		var owner = caster.OwnerUnit;

		if (owner == null)
			return SpellCastResult.DontReport;

		var target = ExplTargetUnit;

		if (target == null)
			return SpellCastResult.DontReport;

		if (owner.HasSpell(HunterSpells.BLINK_STRIKES))
		{
			if (owner.AsPlayer.SpellHistory.HasCooldown(HunterSpells.BLINK_STRIKES) && caster.GetDistance(target) > 10.0f)
				return SpellCastResult.OutOfRange;

			if ((caster.HasAuraType(AuraType.ModRoot) || caster.HasAuraType(AuraType.ModStun)) && caster.GetDistance(target) > 5.0f)
				return SpellCastResult.Rooted;

			if (!owner.AsPlayer.SpellHistory.HasCooldown(HunterSpells.BLINK_STRIKES) && target.IsWithinLOSInMap(caster) && caster.GetDistance(target) > 10.0f && caster.GetDistance(target) < 30.0f && !caster.HasAuraType(AuraType.ModStun))
			{
				caster.CastSpell(target, HunterSpells.BLINK_STRIKES_TELEPORT, true);

				if (caster.AsCreature.IsAIEnabled && caster.AsPet)
				{
					caster.AsPet.ClearUnitState(UnitState.Follow);

					if (caster.AsPet.Victim)
						caster.AsPet.AttackStop();

					caster.MotionMaster.Clear();
					caster.AsPet.GetCharmInfo().SetIsCommandAttack(true);
					caster.AsPet.GetCharmInfo().SetIsAtStay(false);
					caster.AsPet.GetCharmInfo().SetIsReturning(false);
					caster.AsPet.GetCharmInfo().SetIsFollowing(false);

					caster.AsCreature.AI.AttackStart(target);
				}

				owner.AsPlayer.
				SpellHistory.AddCooldown(HunterSpells.BLINK_STRIKES, 0, TimeSpan.FromSeconds(20));
			}
		}

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDamage(int effIndex)
	{
		var pet = Caster.AsPet;

		if (pet != null)
		{
			var owner = Caster.OwnerUnit;

			if (owner != null)
			{
				var target = HitUnit;

				if (target == null)
					return;

				// (1.5 * 1 * 1 * (Ranged attack power * 0.333) * (1 + $versadmg))
				double dmg = owner.UnitData.RangedAttackPower * 0.333f;

				var CostModifier = Global.SpellMgr.GetSpellInfo(HunterSpells.BASIC_ATTACK_COST_MODIFIER, Difficulty.None);
				var SpikedCollar = Global.SpellMgr.GetSpellInfo(HunterSpells.SPIKED_COLLAR, Difficulty.None);

				// Increases the damage done by your pet's Basic Attacks by 10%
				if (pet.HasAura(HunterSpells.SPIKED_COLLAR) && SpikedCollar != null)
					MathFunctions.AddPct(ref dmg, SpikedCollar.GetEffect(0).BasePoints);

				// Deals 100% more damage and costs 100% more Focus when your pet has 50 or more Focus.
				if (pet.GetPower(PowerType.Focus) + 25 >= 50)
				{
					if (CostModifier != null)
						dmg += MathFunctions.CalculatePct(dmg, CostModifier.GetEffect(1).BasePoints);

					pet.EnergizeBySpell(pet, SpellInfo, 25, PowerType.Focus);
					// pet->EnergizeBySpell(pet, GetSpellInfo()->Id, -25, PowerType.Focus);
				}

				dmg *= pet.GetPctModifierValue(UnitMods.DamageMainHand, UnitModifierPctType.Total);

				if (target != null)
				{
					dmg = pet.SpellDamageBonusDone(target, SpellInfo, dmg, DamageEffectType.Direct, GetEffectInfo(0), 1, Spell);
					dmg = target.SpellDamageBonusTaken(pet, SpellInfo, dmg, DamageEffectType.Direct);
				}

				HitDamage = dmg;
			}
		}
	}
}