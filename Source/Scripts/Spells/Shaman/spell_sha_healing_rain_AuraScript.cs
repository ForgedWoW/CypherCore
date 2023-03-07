// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 73920 - Healing Rain (Aura)
[SpellScript(73920)]
internal class spell_sha_healing_rain_AuraScript : AuraScript, IHasAuraEffects
{
	private ObjectGuid _visualDummy;
	private Position _pos;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void SetVisualDummy(TempSummon summon)
	{
		_visualDummy = summon.GetGUID();
        _pos = summon.Location;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffecRemoved, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
	}

	private void HandleEffectPeriodic(AuraEffect aurEff)
	{
		GetTarget().CastSpell(_pos, ShamanSpells.HealingRainHeal, new CastSpellExtraArgs(aurEff));
	}

	private void HandleEffecRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var summon = ObjectAccessor.GetCreature(GetTarget(), _visualDummy);

		summon?.DespawnOrUnsummon();
	}
}