// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(152280)]
public class aura_dk_defile : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 2, AuraType.PeriodicDummy));
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var caster = Caster;

		if (caster != null)
			foreach (var at in caster.GetAreaTriggers(Id))
				if (at.InsideUnits.Count != 0)
					caster.CastSpell(caster, DeathKnightSpells.DEFILE_MASTERY, true);
	}
}