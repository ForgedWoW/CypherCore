// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// Light's Hammer (Periodic Dummy) - 114918
[SpellScript(114918)]
public class spell_pal_lights_hammer_tick : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDummy));
	}

	private void OnTick(AuraEffect UnnamedParameter)
	{
		var caster = Caster;

		if (caster != null)
			if (caster.GetOwner())
			{
				var args = new CastSpellExtraArgs();
				args.SetTriggerFlags(TriggerCastFlags.FullMask);
				args.SetOriginalCaster(caster.GetOwner().GUID);
				caster.CastSpell(new Position(caster.Location.X, caster.Location.Y, caster.Location.Z), PaladinSpells.ARCING_LIGHT_HEAL, args);
				caster.CastSpell(new Position(caster.Location.X, caster.Location.Y, caster.Location.Z), PaladinSpells.ARCING_LIGHT_DAMAGE, args);
			}
	}
}