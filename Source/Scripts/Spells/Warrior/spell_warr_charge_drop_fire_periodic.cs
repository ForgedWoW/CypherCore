// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior;

[SpellScript(126661)] // 126661 - Warrior Charge Drop Fire Periodic
internal class spell_warr_charge_drop_fire_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(DropFireVisual, 0, AuraType.PeriodicTriggerSpell));
	}

	private void DropFireVisual(AuraEffect aurEff)
	{
		PreventDefaultAction();

		if (Target.IsSplineEnabled)
			for (uint i = 0; i < 5; ++i)
			{
				var timeOffset = (int)(6 * i * aurEff.Period / 25);
				var loc = Target.MoveSpline.ComputePosition(timeOffset);
				Target.SendPlaySpellVisual(new Position(loc.X, loc.Y, loc.Z), Misc.SpellVisualBlazingCharge, 0, 0, 1.0f, true);
			}
	}
}