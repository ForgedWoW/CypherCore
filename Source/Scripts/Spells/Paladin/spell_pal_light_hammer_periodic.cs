// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(114918)] // 114918 - Light's Hammer (Periodic)
internal class spell_pal_light_hammer_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		var lightHammer = Target;
		var originalCaster = lightHammer.OwnerUnit;

		if (originalCaster != null)
		{
			originalCaster.CastSpell(lightHammer.Location, PaladinSpells.LightHammerDamage, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
			originalCaster.CastSpell(lightHammer.Location, PaladinSpells.LightHammerHealing, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress));
		}
	}
}