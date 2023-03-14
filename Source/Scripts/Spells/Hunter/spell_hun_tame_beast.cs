// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[Script]
internal class spell_hun_tame_beast : SpellScript, ISpellCheckCast
{
	private static readonly uint[] CallPetSpellIds =
	{
		883, 83242, 83243, 83244, 83245
	};

	public SpellCastResult CheckCast()
	{
		var caster = Caster.AsPlayer;

		if (caster == null)
			return SpellCastResult.DontReport;

		if (!ExplTargetUnit)
			return SpellCastResult.BadImplicitTargets;

		var target = ExplTargetUnit.AsCreature;

		if (target)
		{
			if (target.GetLevelForTarget(caster) > caster.Level)
				return SpellCastResult.Highlevel;

			// use SMSG_PET_TAME_FAILURE?
			if (!target.Template.IsTameable(caster.CanTameExoticPets))
				return SpellCastResult.BadTargets;

			var petStable = caster.PetStable1;

			if (petStable != null)
			{
				if (petStable.CurrentPetIndex.HasValue)
					return SpellCastResult.AlreadyHaveSummon;

				var freeSlotIndex = Array.FindIndex(petStable.ActivePets, petInfo => petInfo == null);

				if (freeSlotIndex == -1)
				{
					caster.SendTameFailure(PetTameResult.TooMany);

					return SpellCastResult.DontReport;
				}

				// Check for known Call Pet X spells
				if (!caster.HasSpell(CallPetSpellIds[freeSlotIndex]))
				{
					caster.SendTameFailure(PetTameResult.TooMany);

					return SpellCastResult.DontReport;
				}
			}

			if (!caster.CharmedGUID.IsEmpty)
				return SpellCastResult.AlreadyHaveCharm;

			if (!target.OwnerGUID.IsEmpty)
			{
				caster.SendTameFailure(PetTameResult.CreatureAlreadyOwned);

				return SpellCastResult.DontReport;
			}
		}
		else
		{
			return SpellCastResult.BadImplicitTargets;
		}

		return SpellCastResult.SpellCastOk;
	}
}