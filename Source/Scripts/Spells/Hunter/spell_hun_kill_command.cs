// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(34026)]
public class spell_hun_kill_command : SpellScript, IHasSpellEffects, ISpellCheckCast
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public SpellCastResult CheckCast()
	{
		Unit pet = Caster.GetGuardianPet();
		var petTarget = ExplTargetUnit;

		if (pet == null || pet.IsDead)
			return SpellCastResult.NoPet;

		// pet has a target and target is within 5 yards and target is in line of sight
		if (petTarget == null || !pet.IsWithinDist(petTarget, 40.0f, true) || !petTarget.IsWithinLOSInMap(pet))
			return SpellCastResult.DontReport;

		if (pet.HasAuraType(AuraType.ModStun) || pet.HasAuraType(AuraType.ModConfuse) || pet.HasAuraType(AuraType.ModSilence) || pet.HasAuraType(AuraType.ModFear) || pet.HasAuraType(AuraType.ModFear2))
			return SpellCastResult.CantDoThatRightNow;

		return SpellCastResult.SpellCastOk;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		if (Caster.IsPlayer)
		{
			Unit pet = Caster.GetGuardianPet();

			if (pet != null)
			{
				if (!pet)
					return;

				if (!ExplTargetUnit)
					return;

				var target = ExplTargetUnit;
				var player = Caster.AsPlayer;

				pet.CastSpell(ExplTargetUnit, HunterSpells.KILL_COMMAND_TRIGGER, true);

				if (pet.Victim)
				{
					pet.AttackStop();
					pet.AsCreature.AI.AttackStart(ExplTargetUnit);
				}
				else
				{
					pet.AsCreature.AI.AttackStart(ExplTargetUnit);
				}
				//pet->CastSpell(GetExplTargetUnit(), KILL_COMMAND_CHARGE, true);

				//191384 Aspect of the Beast
				if (Caster.HasAura(Sspell.AspectoftheBeast))
				{
					if (pet.HasAura(Sspell.SpikedCollar))
						player.CastSpell(target, Sspell.BestialFerocity, true);

					if (pet.HasAura(Sspell.GreatStamina))
						pet.CastSpell(pet, Sspell.BestialTenacity, true);

					if (pet.HasAura(Sspell.Cornered))
						player.CastSpell(target, Sspell.BestialCunning, true);
				}
			}
		}
	}

	private struct Sspell
	{
		public const uint AnimalInstinctsReduction = 232646;
		public const uint AspectoftheBeast = 191384;
		public const uint BestialFerocity = 191413;
		public const uint BestialTenacity = 191414;
		public const uint BestialCunning = 191397;
		public const uint SpikedCollar = 53184;
		public const uint GreatStamina = 61688;
		public const uint Cornered = 53497;
	}
}