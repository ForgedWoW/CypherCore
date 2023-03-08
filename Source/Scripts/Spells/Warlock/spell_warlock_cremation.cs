// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 212282 -
[SpellScript(212282)]
public class spell_warlock_cremation : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = Caster;
		var target = eventInfo.ActionTarget;

		if (caster == null || target == null)
			return;

		switch (eventInfo.DamageInfo.GetSpellInfo().Id)
		{
			case WarlockSpells.SHADOWBURN:
			case WarlockSpells.CONFLAGRATE:
				caster.CastSpell(target, SpellInfo.GetEffect(0).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)aurEff.Amount));

				break;
			case WarlockSpells.INCINERATE:
				caster.CastSpell(target, WarlockSpells.IMMOLATE_DOT, true);

				break;
		}
	}
}