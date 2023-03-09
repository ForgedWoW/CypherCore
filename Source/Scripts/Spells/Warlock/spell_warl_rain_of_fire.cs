// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(5740)] // 5740 - Rain of Fire Updated 7.1.5
internal class spell_warl_rain_of_fire : AuraScript, IHasAuraEffects, IAuraOnRemove
{
	readonly List<Unit> _auraUnits = new();
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void AuraRemoved(AuraRemoveMode removeMode)
	{
		foreach (var unit in _auraUnits)
			if (unit != null && unit.IsAlive)
				unit.RemoveAura(WarlockSpells.PYROGENICS_AURA);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
	}

	private void HandleDummyTick(AuraEffect aurEff)
	{
		var caster = Caster;
		var hasPyro = caster.HasAura(WarlockSpells.PYROGENICS);

		var rainOfFireAreaTriggers = Target.GetAreaTriggers(WarlockSpells.RAIN_OF_FIRE);
		List<ObjectGuid> targetsInRainOfFire = new();

		foreach (var rainOfFireAreaTrigger in rainOfFireAreaTriggers)
		{
			var insideTargets = rainOfFireAreaTrigger.InsideUnits;
			targetsInRainOfFire.AddRange(insideTargets);
		}

		foreach (var insideTargetGuid in targetsInRainOfFire)
		{
			var insideTarget = Global.ObjAccessor.GetUnit(Target, insideTargetGuid);

			if (insideTarget)
				if (!Target.IsFriendlyTo(insideTarget))
				{
					Target.CastSpell(insideTarget, WarlockSpells.RAIN_OF_FIRE_DAMAGE, true);

					if (hasPyro && !insideTarget.HasAura(WarlockSpells.PYROGENICS_AURA))
					{
						_auraUnits.Add(insideTarget);
						caster.AddAura(WarlockSpells.PYROGENICS_AURA, insideTarget);
					}
				}
		}
	}
}