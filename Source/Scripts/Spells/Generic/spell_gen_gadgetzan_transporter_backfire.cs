﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_gadgetzan_transporter_backfire : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		var r = RandomHelper.IRand(0, 119);

		if (r < 20) // Transporter Malfunction - 1/6 polymorph
			caster.CastSpell(caster, GenericSpellIds.TransporterMalfunctionPolymorph, true);
		else if (r < 100) // Evil Twin               - 4/6 evil twin
			caster.CastSpell(caster, GenericSpellIds.TransporterEviltwin, true);
		else // Transporter Malfunction - 1/6 miss the Target
			caster.CastSpell(caster, GenericSpellIds.TransporterMalfunctionMiss, true);
	}
}