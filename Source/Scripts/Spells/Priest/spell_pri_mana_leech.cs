﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(28305)]
public class spell_pri_mana_leech : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	private Unit _procTarget;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_pri_mana_leech()
	{
		_procTarget = null;
	}

	public bool CheckProc(ProcEventInfo UnnamedParameter)
	{
		_procTarget = Target.OwnerUnit;

		return _procTarget != null;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
	{
		PreventDefaultAction();
		Target.CastSpell(_procTarget, PriestSpells.MANA_LEECH_PROC, aurEff);
	}
}