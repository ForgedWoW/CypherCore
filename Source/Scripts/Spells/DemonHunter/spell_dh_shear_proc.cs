// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(203783)]
public class spell_dh_shear_proc : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = Caster;

		if (caster == null || eventInfo.SpellInfo != null)
			return;

		double procChance = 100f;

		if (eventInfo.SpellInfo.Id == DemonHunterSpells.SHEAR)
		{
			procChance = 15;
			procChance += caster.GetAuraEffectAmount(ShatteredSoulsSpells.SHATTER_THE_SOULS, 0);
		}

		/*
			if (RandomHelper.randChance(procChance))
			    caster->CastSpell(caster, SHATTERED_SOULS_MISSILE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)LESSER_SOUL_SHARD));
			*/

		if (caster.SpellHistory.HasCooldown(DemonHunterSpells.FELBLADE))
			if (RandomHelper.randChance(caster.GetAuraEffectAmount(DemonHunterSpells.SHEAR_PROC, 3)))
				caster.				SpellHistory.ResetCooldown(DemonHunterSpells.FELBLADE);
	}
}