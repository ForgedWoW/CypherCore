// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206891)]
public class spell_dh_intimidated : AuraScript, IHasAuraEffects
{
	private readonly List<ObjectGuid> _uniqueTargets = new();
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ModDamagePercentTaken, AuraScriptHookType.EffectProc));
	}

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var attacker = eventInfo.Actor;
		var auraOwner = Aura.Owner;

		if (attacker == null || auraOwner == null)
			return;

		if (attacker == Caster)
		{
			RefreshDuration();

			return;
		}

		if (_uniqueTargets.Count >= 4 || !auraOwner.ToUnit())
			return;

		if (_uniqueTargets.Contains(attacker.GetGUID()))
		{
			attacker.CastSpell(auraOwner.ToUnit(), SpellInfo.Id, true);
			_uniqueTargets.Add(attacker.GetGUID());
		}
	}
}