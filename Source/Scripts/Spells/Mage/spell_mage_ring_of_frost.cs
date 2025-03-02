﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 136511 - Ring of Frost
internal class spell_mage_ring_of_frost : AuraScript, IHasAuraEffects
{
	private ObjectGuid _ringOfFrostGUID;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.ProcTriggerSpell));
		AuraEffects.Add(new AuraEffectApplyHandler(Apply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var ringOfFrost = GetRingOfFrostMinion();

		if (ringOfFrost)
			Target.CastSpell(ringOfFrost.Location, MageSpells.RingOfFrostFreeze, new CastSpellExtraArgs(true));
	}

	private void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		List<TempSummon> minions = new();
		Target.GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(MageSpells.RingOfFrostSummon, CastDifficulty).GetEffect(0).MiscValue);

		// Get the last summoned RoF, save it and despawn older ones
		foreach (var summon in minions)
		{
			var ringOfFrost = GetRingOfFrostMinion();

			if (ringOfFrost)
			{
				if (summon.GetTimer() > ringOfFrost.GetTimer())
				{
					ringOfFrost.DespawnOrUnsummon();
					_ringOfFrostGUID = summon.GUID;
				}
				else
				{
					summon.DespawnOrUnsummon();
				}
			}
			else
			{
				_ringOfFrostGUID = summon.GUID;
			}
		}
	}

	private TempSummon GetRingOfFrostMinion()
	{
		var creature = ObjectAccessor.GetCreature(Owner, _ringOfFrostGUID);

		if (creature)
			return creature.ToTempSummon();

		return null;
	}
}