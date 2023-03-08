// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 63471 -Spawn Blood Pool
internal class spell_spawn_blood_pool : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new DestinationTargetSelectHandler(SetDest, 0, Targets.DestCaster));
	}

	private void SetDest(SpellDestination dest)
	{
		var caster = Caster;
		var summonPos = caster.Location;

		if (caster.GetMap().GetLiquidStatus(caster.PhaseShift, caster.Location.X, caster.Location.Y, caster.Location.Z, LiquidHeaderTypeFlags.AllLiquids, out var liquidStatus, caster.CollisionHeight) != ZLiquidStatus.NoWater)

			dest.Relocate(summonPos);
	}
}